using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Nano.Common
{
	public static class FileWalker
	{
		public interface INotify
		{
			void EnterFolder(string pathRel);
			void LeaveFolder(string pathRel);
			void GetFile(string pathRel);
		}

		public static void Walk(string path, INotify notify, bool sort = false)
		{
			WalkFolder(path, "", notify, sort);
		}

		static void WalkFolder(string pathAbs, string pathRel, INotify notify, bool sort)
		{
			string[] ss = Directory.GetDirectories(pathAbs);
			if (sort)
				Array.Sort(ss, StringComparer.CurrentCultureIgnoreCase);
			foreach (string s in ss)
			{
				string pathRelSub = GetRelSub(pathRel, s);
				notify.EnterFolder(pathRelSub);
				WalkFolder(s, pathRelSub, notify, sort);
				notify.LeaveFolder(pathRelSub);
			}

			ss = Directory.GetFiles(pathAbs);
            if (sort)
                Array.Sort(ss, StringComparer.CurrentCultureIgnoreCase);
            foreach (string s in ss)
			{
				string pathRelSub = GetRelSub(pathRel, s);
				notify.GetFile(pathRelSub);
			}
		}

		static string GetRelSub(string pathRelParent, string pathAbs)
		{
			string name = Path.GetFileName(pathAbs);
			return pathRelParent.Length != 0 ? pathRelParent + Path.DirectorySeparatorChar + name : name;
		}
	}

	public interface IFileUpdater
	{
		void Process(string source, string target);
	}

	public enum FileUpdateActionType
	{
		EnterDirectory,
		LeaveDirectory,
		CreateDirectory,
		UpdateFile,
		DeleteDirectory,
		DeleteFile,
		SkipFile
	}

	public interface IFileUpdateNotify
	{
		void Send(string relative, FileUpdateActionType act);
	}

	public enum FileUpdateWalkerOption: uint
	{
		Default = CompareLength | PerformDelete,
		CompareLength = 1,
		PerformDelete = 0x10,
		Unsupported = 0xFFFFFFEE
	}

	public class FileUpdateWalker
	{
		string m_source, m_target;
		IFileUpdater m_updater;
		FileUpdateWalkerOption m_option;
		IFileUpdateNotify m_notify;

		class NullNotify : IFileUpdateNotify
		{
			public void Send(string relative, FileUpdateActionType act) { }
		}

		public FileUpdateWalker(string source, string target, IFileUpdater updater)
		{
			m_source = Path.GetFullPath(source);
			m_target = Path.GetFullPath(target);
			m_updater = updater;
			m_option = FileUpdateWalkerOption.Default;
			m_notify = new NullNotify();
		}

		public FileUpdateWalker(string source, string target, IFileUpdater updater, FileUpdateWalkerOption option, IFileUpdateNotify notify)
		{
			m_source = Path.GetFullPath(source);
			m_target = Path.GetFullPath(target);
			m_updater = updater;
			m_option = option;
			m_notify = notify != null ? notify : new NullNotify();

			if ((m_option & FileUpdateWalkerOption.Unsupported) != 0)
				throw new System.ArgumentException("Unsupported options");
		}

		public void Walk()
		{
			_WalkFolder(m_source, m_target, "");
		}

		void _WalkFolder(string source, string target, string relative)
		{
			m_notify.Send(relative, FileUpdateActionType.EnterDirectory);

			if (!Directory.Exists(target))
			{
				m_notify.Send(relative, FileUpdateActionType.CreateDirectory);
				Directory.CreateDirectory(target);
			}

			Dictionary<string, string> dict = new Dictionary<string, string>();
			_DealFiles(source, target, relative, dict);
			_DealFolder(source, target, relative, dict);
			dict = null;			
			
			m_notify.Send(relative, FileUpdateActionType.LeaveDirectory);
		}

		void _DealFiles(string source, string target, string relative, Dictionary<string, string> dict)
		{
			string[] subs = Directory.GetFiles(source);
			string[] subt = Directory.GetFiles(target);
			MapNames(subt, dict);
			foreach (string s in subs)
			{
				string name = Path.GetFileName(s), namelc = name.ToLowerInvariant();
				string t = Path.Combine(target, name);
				bool hasTarget = dict.ContainsKey(namelc);

				if (!hasTarget || JudgeUpdateFile(s, t))
				{
					m_notify.Send(relative + "\\" + name, FileUpdateActionType.UpdateFile);
					m_updater.Process(s, t);
				}
				else
					m_notify.Send(relative + '\\' + name, FileUpdateActionType.SkipFile);

				if (hasTarget)
					dict.Remove(namelc);
			}
			if ((m_option & FileUpdateWalkerOption.PerformDelete) != 0)
			{
				foreach (string name in dict.Values)
				{
					m_notify.Send(relative + "\\" + name, FileUpdateActionType.DeleteFile);
					File.Delete(Path.Combine(target, name));
				}
			}
			dict.Clear();
		}

		void _DealFolder(string source, string target, string relative, Dictionary<string, string> dict)
		{
			string[] subs = Directory.GetDirectories(source);
			string[] subt = Directory.GetDirectories(target);
			MapNames(subt, dict);
			foreach (string s in subs)
			{
				string name = Path.GetFileName(s), namelc = name.ToLowerInvariant();
				string t = Path.Combine(target, name);
				bool hasTarget = dict.ContainsKey(namelc);

				if (!hasTarget)
				{
					m_notify.Send(relative + "\\" + name, FileUpdateActionType.CreateDirectory);
					Directory.CreateDirectory(t);
				}

				_WalkFolder(s, t, relative + "\\" + name);

				if (hasTarget)
					dict.Remove(namelc);
			}
			if ((m_option & FileUpdateWalkerOption.PerformDelete) != 0)
			{
				foreach (string name in dict.Values)
				{
					m_notify.Send(relative + "\\" + name, FileUpdateActionType.DeleteFile);
					Directory.Delete(Path.Combine(target, name), true);
				}
			}
			dict.Clear();
		}

		bool JudgeUpdateFile(string s, string t)
		{
			if (File.GetLastWriteTimeUtc(s) > File.GetLastWriteTimeUtc(t))
				return true;
			else
				return false;
		}

		static void MapNames(string[] pathes, Dictionary<string, string> dict)
		{
			foreach (string path in pathes)
			{
				string name = Path.GetFileName(path);
				dict.Add(name.ToLowerInvariant(), name);
			}
		}
	}
}
