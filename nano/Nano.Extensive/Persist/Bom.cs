using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using Nano.Collection;
using Nano.Json;
using Nano.Storage;

namespace Nano.Ext.Persist
{
	class BomTypeInfo
	{
		public string Name;
		public int Index;
		public Type RT;
	}

	public abstract class BomObject
	{
		internal BomStore m_bom_sto;
		internal int m_bom_ti;
		internal int m_bom_oi;

		protected BomObject(BomStore store)
		{
			m_bom_sto = store;
			var track = store.Track;
			m_bom_ti = track.RegisterType(GetType());
			m_bom_oi = track.ObjectCreated(m_bom_ti);
		}

		protected BomStore Store => m_bom_sto;

		protected void SetValue(string fieldName, object value)
		{
			if (value is BomObject)
			{
				// object types
				BomObject vo = (BomObject)value;
				m_bom_sto.Track.ValueChanged(m_bom_ti, m_bom_oi, fieldName, vo.m_bom_oi);
			}
			else
			{
				// value types
				Debug.Assert(BomTracker.IsValueType(value.GetType()));
				m_bom_sto.Track.ValueChanged(m_bom_ti, m_bom_oi, fieldName, value);
			}
		}

		protected void CustomCommand(string cmd, object m)
		{
			m_bom_sto.Track.CustomCommand(cmd, m_bom_ti, m_bom_oi, m);
		}

		#region Saving

		public virtual bool SaverGetValueFields(Dictionary<string, object> map) { return false; }

		public virtual object SaverWriteCustomModel(BomSaver saver) { throw new NotSupportedException(); }

		#endregion

		#region Loading

		public virtual void LoaderSetValueField(BomLoader kit, string name, JsonNode jnV)
		{
			var rt = GetType();
			var fieldInfo = rt.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			Debug.Assert(fieldInfo != null);
			var vt = fieldInfo.FieldType;
			object vo = LoaderMakeValue(kit, vt, jnV);
			fieldInfo.SetValue(this, vo);
		}

		public virtual void LoaderSetValueFields(BomLoader kit, JsonNode jnArgs)
		{
			Debug.Assert(jnArgs.NodeType == JsonNodeType.Dictionary);
			var rt = GetType();
			foreach (var jnArg in jnArgs.ChildNodes)
			{
				string name = jnArg.Name;
				var fieldInfo = rt.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				Debug.Assert(fieldInfo != null);
				var vt = fieldInfo.FieldType;
				object vo = LoaderMakeValue(kit, vt, jnArg);
				fieldInfo.SetValue(this, vo);
			}
		}

		public virtual void LoaderReadCustomModel(BomLoader kit, JsonNode jnModel) { throw new NotSupportedException(); }

		public virtual void LoaderReadCustomBinlog(BomLoader kit, string cmd, JsonNode jnModel) { throw new NotSupportedException(); }

		protected object LoaderMakeValue(BomLoader kit, Type vt, JsonNode jnv)
		{
			if (BomTracker.IsBomType(vt))
			{
				int oi = (int)jnv.IntValue;
				BomObject o = kit.GetObject(oi);
				return o;
			}
			else
			{
				Debug.Assert(BomTracker.IsValueType(vt));
				object o = BomTracker.MakeValue(jnv, vt.FullName);
				return o;
			}
		}

		#endregion
	}

	class BomTracker
	{
		int m_seed;
		List<BomTypeInfo> m_tis;
		Dictionary<string, int> m_tmap;
		BinlogAccept m_loga;

		internal BomTracker()
		{
			m_seed = 0;
			m_tis = new List<BomTypeInfo>();
			m_tis.Add(null);    // 0 for nothing
			m_tmap = new Dictionary<string, int>();
			m_loga = null;
		}

		internal void SetLogAccept(BinlogAccept loga)
		{
			m_loga = loga;
		}

		#region Type Board

		internal BomTypeInfo GetTypeInfo(int ti)
		{
			return m_tis[ti];
		}

		internal int RegisterType(Type rt)
		{
			string name = rt.FullName;
			int tidx;
			if (m_tmap.TryGetValue(name, out tidx))
				return tidx;

			var ti = MakeTypeInfo(rt);
			ti.Index = m_tis.Count;
			m_tis.Add(ti);
			m_tmap.Add(ti.Name, ti.Index);

			if (m_loga != null)
			{
				string gname = MakeShortName(rt);
				//var vfl = CollectionKit.Transform(ti.VFL, x => x.Name + ":" + MakeTypeName(x.FieldType));
				//var ofl = CollectionKit.Transform(ti.OFL, x => x.Name + ":" + MakeTypeName(x.FieldType));
				//var model = new Dictionary<string, object> { { "c", "ty" }, { "n", ti.Name }, { "i", ti.Index }, { "vfl", vfl }, { "ofl", ofl } };
				var model = new Dictionary<string, object> { { "c", "ty" }, { "n", gname }, { "i", ti.Index } };
				m_loga.WriteObject(model);
			}

			return ti.Index;
		}

		static BomTypeInfo MakeTypeInfo(Type rt)
		{
			var ti = new BomTypeInfo();
			ti.Index = 0;
			ti.Name = rt.FullName;
			ti.RT = rt;
			return ti;
		}

		static string MakeShortName(Type rt)
		{
			if (!rt.IsGenericType)
				return rt.FullName;

			// List<>
			// System.Collections.Generic.List`1
			// List<TestApp.User>
			// System.Collections.Generic.List`1[[TestApp.User, ConsoleApplication1, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null]]

			// Dictionary<,>
			// System.Collections.Generic.Dictionary`2
			// Dictionary<string, TestApp.User>
			// System.Collections.Generic.Dictionary`2[[System.String, mscorlib, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089],[TestApp.User, ConsoleApplication1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]

			var sb = new StringBuilder();
			Type gt = rt.GetGenericTypeDefinition();
			sb.Append('*').Append(gt.FullName).Append(':');
			Type[] vts = rt.GetGenericArguments();
			foreach (Type vt in vts)
				sb.Append('(').Append(MakeShortName(vt)).Append(")");
			return sb.ToString();
		}

		internal static bool IsValueType(string fullname)
		{
			switch (fullname)
			{
				case "System.Byte":
				case "System.SByte":
				case "System.Int16":
				case "System.UInt16":
				case "System.Int32":
				case "System.UInt32":
				case "System.Int64":
				case "System.UInt64":
				case "System.Boolean":
				case "System.Single":
				case "System.Double":
				case "System.String":
					return true;
				default:
					return false;
			}
		}

		internal static bool IsValueType(Type ty)
		{
			return IsValueType(ty.FullName);
		}

		internal static bool IsBomType(Type ty)
		{
			return ty.IsSubclassOf(typeof(BomObject));
		}

		internal static Type FromTypeName(string name)
		{
			if (name[0] == '*')
			{
				// *Nano.Ext.Persist.BomList`1:(TestExt.TestApp.User)
				// *Nano.Ext.Persist.BomDictionary`2:(System.String)(TestApp.User)
				int pos = name.IndexOf(':');
				Debug.Assert(pos > 1);
				string gname = name.Substring(1, pos - 1);
				Type gt = SearchType(gname);
				Debug.Assert(gt != null);

				int argn = int.Parse(gname.Substring(gname.IndexOf('`') + 1));
				Type[] vts = new Type[argn];
				++pos;
				for (int i = 0; i < argn; ++i)
				{
					Debug.Assert(name[pos] == '(');
					int pos2 = WalkBracks(name, pos);
					Debug.Assert(name[pos2] == ')');
					string vname = name.Substring(pos + 1, pos2 - pos - 1);
					Type vt = FromTypeName(vname);
					Debug.Assert(vt != null);
					vts[i] = vt;
					pos = pos2 + 1;
				}
				Debug.Assert(pos == name.Length);

				Type rt = gt.MakeGenericType(vts);
				return rt;
			}
			else
			{
				return SearchType(name);
			}
		}

		internal static Type SearchType(string name)
		{
			Type rt = null;
			var asms = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var asm in asms)
			{
				if ((rt = asm.GetType(name)) != null)
					break;
			}
			return rt;
		}

		static int WalkBracks(string name, int pos)
		{
			Debug.Assert(name[pos] == '(');
			int level = 1;
			while (true)
			{
				switch (name[++pos])
				{
					case '(':
						++level;
						break;
					case ')':
						if (--level == 0)
							return pos;
						break;
				}
			}
		}

		internal static object MakeValue(JsonNode node, string tname)
		{
			object o = node.Value;
			switch (tname)
			{
				case "System.Byte":
					return (byte)(long)o;
				case "System.SByte":
					return (sbyte)(long)o;
				case "System.Int16":
					return (short)(long)o;
				case "System.UInt16":
					return (ushort)(long)o;
				case "System.Int32":
					return (int)(long)o;
				case "System.UInt32":
					return (uint)(long)o;
				case "System.Int64":
					return (long)o;
				case "System.UInt64":
					return (ulong)(long)o;
				case "System.Boolean":
					return (bool)o;
				case "System.Single":
					return (float)(double)o;
				case "System.Double":
					return (double)o;
				case "System.String":
					return (string)o;
				default:
					throw new ArgumentException("Not a value type: " + tname);
			}
		}

		#endregion

		#region Called by BomObject

		// Commands:
		// ty: Register type, @ RegisterType
		// oc: Create object, @ ObjectCreated
		// cv: Change value field, @ ValueChanged
		// sfs: Save fields @ SaveFields
		// ctm: Custom object model @ WriteCustomModel

		void WriteCmds(List<object> cmds)
		{
			if (m_loga != null)
			{
				foreach (object cmd in cmds)
					m_loga.WriteObject(cmd);
			}
		}

		internal int ObjectCreated(int ti)
		{
			int oi = ++m_seed;
			if (m_loga != null)
			{
				var model = new Dictionary<string, object> { { "c", "oc" }, { "ti", ti }, { "oi", oi } };
				m_loga.WriteObject(model);
			}
			return oi;
		}

		internal void ValueChanged(int ti, int oi, string name, object v)
		{
			if (m_loga != null)
			{
				var model = new Dictionary<string, object> { { "c", "cv" }, { "ti", ti }, { "oi", oi }, { "n", name }, { "v", v } };
				m_loga.WriteObject(model);
			}
		}

		internal void SaveFields(int ti, int oi, Dictionary<string, object> args)
		{
			CustomCommand("sfs", ti, oi, args);
		}

		internal void CustomCommand(string cmd, int ti, int oi, object m)
		{
			if (m_loga != null)
			{
				var model = new Dictionary<string, object> { { "c", cmd }, { "ti", ti }, { "oi", oi }, { "m", m } };
				m_loga.WriteObject(model);
			}
		}

		#endregion
	}

	public class BomSaver
	{
		class Inf
		{
			public int TI, OI;
		}

		BomTracker m_track;
		Dictionary<BomObject, Inf> m_omap;

		internal BomSaver(BinlogAccept acpt)
		{
			m_track = new BomTracker();
			m_track.SetLogAccept(acpt);
			m_omap = new Dictionary<BomObject, Inf>();
		}

		internal int SaveObject(BomObject o)
		{
			Inf inf;
			if (m_omap.TryGetValue(o, out inf))
				return inf.OI;

			int ti = m_track.RegisterType(o.GetType());
			int oi = m_track.ObjectCreated(ti);
			inf = new Inf() { TI = ti, OI = oi };
			m_omap.Add(o, inf);

			Dictionary<string, object> vfs = new Dictionary<string, object>();
			Dictionary<string, object> vfso = new Dictionary<string, object>();
			if (o.SaverGetValueFields(vfs))
			{
				foreach (var pair in vfs)
				{
					BomObject subo = pair.Value as BomObject;
					if (subo != null)
						vfso[pair.Key] = SaveObject(subo);
					else
						vfso[pair.Key] = pair.Value;
				}
				m_track.SaveFields(ti, oi, vfso);
			}
			else
			{
				var model = o.SaverWriteCustomModel(this);
				m_track.CustomCommand("ctm", ti, oi, model);
			}

			return oi;
		}

		internal BomTracker UpdateObjects()
		{
			foreach (var pair in m_omap)
			{
				BomObject o = pair.Key;
				Inf inf = pair.Value;
				o.m_bom_ti = inf.TI;
				o.m_bom_oi = inf.OI;
			}
			return m_track;
		}
	}

	/// <summary>读取BOM日志并还原数据</summary>
	public class BomLoader
	{
		BomStore m_store;
		BomTracker m_track;
		List<BomObject> m_os;

		internal BomLoader(BomStore store)
		{
			m_store = store;
			m_track = store.Track;
			m_os = new List<BomObject>();
			m_os.Add(null);	// 0 for nothing
		}

		#region Properties

		public BomStore Store
		{
			get { return m_store; }
		}

		internal BomObject Root
		{
			get { return m_os[1]; }
		}

		internal List<BomObject> Objects
		{
			get { return m_os; }
		}

		internal BomObject GetObject(int oi)
		{
			return m_os[oi];
		}

		#endregion

		#region Map & bin-log

		internal void AcceptTypeRegister(JsonNode node)
		{
			// {"c":"ty","n":"TestExt.TestBom+Company","i":1}
			// {"c":"ty","n":"*Nano.Ext.Persist.BomList`1:(TestExt.TestBom+User)","i":2}

			string name = node["n"].TextValue;
			Type rt = BomTracker.FromTypeName(name);
			if (rt == null)
				throw new ArgumentException("Type not found: " + name);

			int tidx = m_track.RegisterType(rt);
			if (node["i"].IntValue != tidx)
				throw new ArgumentException("Type index mismatch: " + name);
		}

		internal void AcceptObjectCreation(JsonNode node)
		{
			// {"c":"oc","ti":1,"oi":1}

			int tidx = (int)node["ti"].IntValue;
			var ti = m_track.GetTypeInfo(tidx);
			object[] args = new object[] { m_store };
			BomObject o = (BomObject)Activator.CreateInstance(ti.RT, args);
			if (o.m_bom_oi != (int)node["oi"].IntValue)
				throw new ArgumentException("Object index mismatch");

			if (m_os.Count != o.m_bom_oi)
				throw new ArgumentException("Object index mismatch");
			m_os.Add(o);
		}

		#endregion

		#region Map only

		internal void AcceptSetFields(JsonNode node)
		{
			// {"c":"sfs","ti":3,"oi":3,"m":{"m_name":"Yang"}}

			var o = LeaObject(node);
			JsonNode jnArgs = node["m"];
			Debug.Assert(jnArgs != null);
			o.LoaderSetValueFields(this, jnArgs);
		}

		internal void AcceptCustomModel(JsonNode node)
		{
			// {"c":"ctm","ti":2,"oi":2,"m":[3,4,5]}

			var o = LeaObject(node);
			var jnModel = node["m"];
			o.LoaderReadCustomModel(this, jnModel);
		}

		#endregion

		#region Bin-log only

		internal void AcceptSetField(JsonNode node)
		{
			// {"c":"cv","ti":1,"oi":1,"n":"m_users","v":2}
			// {"c":"cv","ti":4,"oi":4,"n":"m_name","v":"Yang"}

			var o = LeaObject(node);
			string name = node["n"].TextValue;
			var jnv = node["v"];
			o.LoaderSetValueField(this, name, jnv);
		}

		internal void AcceptCustomActionLog(string cmd, JsonNode node)
		{
			// {"c":"ls:ad","ti":2,"oi":2,"m":4}
			// {"c":"dc:a","ti":3,"oi":3,"m":{"k":"Yang","v":4}}
			int pos = cmd.IndexOf(':');
			if (pos < 1)
				throw new NotSupportedException("Unknown bin-log command: " + cmd);

			var o = LeaObject(node);
			var jnM = node["m"];
			o.LoaderReadCustomBinlog(this, cmd, jnM);
		}

		#endregion

		#region Kit methods

		BomObject LeaObject(JsonNode node)
		{
			int oi = (int)node["oi"].IntValue;
			BomObject o = m_os[oi];
			Debug.Assert(o != null);
			return o;
		}

		#endregion

		internal void LoadMap(JsonModelLoader jld, Stream stream)
		{
			jld.Load(stream, AcceptMap);
		}

		void AcceptMap(JsonNode node)
		{
			string cmd = node["c"].TextValue;
			switch (cmd)
			{
				case "ty":
					AcceptTypeRegister(node);
					break;
				case "oc":
					AcceptObjectCreation(node);
					break;
				case "sfs":
					AcceptSetFields(node);
					break;
				case "ctm":
					AcceptCustomModel(node);
					break;
			}
		}

		internal void LoadBinlog(JsonModelLoader jld, Stream stream)
		{
			jld.Load(stream, AcceptBinlog);
		}

		void AcceptBinlog(JsonNode node)
		{
			string cmd = node["c"].TextValue;
			switch (cmd)
			{
				case "ty":
					AcceptTypeRegister(node);
					break;
				case "oc":
					AcceptObjectCreation(node);
					break;
				case "cv":
					AcceptSetField(node);
					break;
				default:
					AcceptCustomActionLog(cmd, node);
					break;
			}
		}
	}

	public class BomStore : BinlogAccess
	{
		BinlogStore m_bls;
		BomObject m_root;
		Type m_rootType;
		BomTracker m_track;
		BomLoader m_loadkit;

		public BomStore(FileTreeItem fi, Type rootType)
		{
			m_bls = new BinlogStore(this, fi);
			m_root = null;
			m_rootType = rootType;
			m_track = null;
			m_loadkit = null;
		}

		internal BomTracker Track
		{
			get { return m_track; }
		}

		public BomObject Root
		{
			get { return m_root; }
		}

		public void Open()
		{
			m_bls.Open();
			m_track.SetLogAccept(m_bls.ChangeAccept);
		}

		public void Close(bool interrupt = false)
		{
			m_track = null;
			m_bls.Close(interrupt);
			m_bls = null;
		}

		#region BinlogAccess

		void BinlogAccess.LoadStarted()
		{
			// m_track is disposed when BomStore closed
			Debug.Assert(m_track == null);
			m_track = new BomTracker();

			// m_loadkit is disposed right after loading
			Debug.Assert(m_loadkit == null);
			m_loadkit = new BomLoader(this);
		}

		void BinlogAccess.MakeNew()
		{
			Debug.Assert(m_root == null);			
			object[] args = new object[] { this };
			m_root = (BomObject)Activator.CreateInstance(m_rootType, args);
			Debug.Assert(m_root.m_bom_oi == 1 && m_loadkit.Objects.Count == 1);
			m_loadkit.Objects.Add(m_root);
		}

		void BinlogAccess.LoadMap(Stream stream)
		{
			Debug.Assert(m_root == null);
			var jld = m_bls.CreateJsonLoader();
			m_loadkit.LoadMap(jld, stream);
			m_root = m_loadkit.Root;
			Debug.Assert(m_root != null);
		}

		void BinlogAccess.LoadLog(JsonModelLoader jld, Stream stream)
		{
			m_loadkit.LoadBinlog(jld, stream);
		}

		void BinlogAccess.LoadCompleted()
		{
			Debug.Assert(m_loadkit != null);
			m_loadkit = null;
			GC.Collect();
		}

		void BinlogAccess.SaveMap(Stream stream)
		{
			var js = m_bls.CreateJsonSaver(stream);
			var acpt = new BinlogAccept(js);
			var saver = new BomSaver(acpt);
			saver.SaveObject(m_root);
			m_track = saver.UpdateObjects();
			acpt.Close();
		}

		#endregion
	}
}
