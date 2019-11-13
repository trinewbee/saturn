using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using Nano.Collection;

namespace Nano.Data.MySql
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
	public class TableFieldAttribute : Attribute
	{
		public string SqlType = null;

		public int Size = 0;
		public bool Nullable = false;
        public string indexName = null;
        public bool unique=false;
	}

	public class DataTableItem
	{
		public uint Id;

		protected DataTableItem()
		{
			Id = 0;
		}

		protected DataTableItem(uint id)
		{
			Id = id;
		}
	}

	public interface IMysqlDataOperator<T> where T : DataTableItem
	{
		string TableName { get; }

		string MakeCreateTableSql();
		string MakeSelectSql(string where);
		string MakeInsertWithIdSql(T item);
		string MakeInsertAutoIdSql(T item);
		string MakeUpdateSql(T item);
		string MakeUpdateBatchSql(string set, string where);
		string MakeDeleteSql(string where);

		T ReadItem(MySqlDataReader rs);
	}

	public class MysqlDataOperator<T> : IMysqlDataOperator<T> where T : DataTableItem
	{
		public enum FieldFlags
		{
			// Types
			Mask_Type = 0xFF,
			VarChar = 1,
			Text,

			SByte = 0x11,
			Byte,
			Short,
			UShort,
			Int, 
			UInt,
			Long,
			ULong,

			Float = 0x21,
			Double,

			// Flags
			Nullable = 0x100,	// only available for VarChar, Text
			DefaultNull = 0x200,	// only available for nullable types
			AutoInc = 0x400,	// only for integers
		}

		public class Field
		{
			public string FieldName, PropName;
			public FieldFlags Flags;
			public int Size = 0;	// only for varchar

			public Field(string _field, string _prop, FieldFlags _flags)
			{
				FieldName = _field;
				PropName = _prop;
				Flags = _flags;
			}

			public bool IsString
			{
				get
				{
					switch (Flags & FieldFlags.Mask_Type)
					{
						case FieldFlags.VarChar:
						case FieldFlags.Text:
							return true;
						default:
							return false;
					}
				}
			}
		}

		public class Index
		{
			public string IndexName;	// can be null
			public string[] FieldNames;
			public bool Unique;

			public Index(string _name, string[] _field, bool _unique)
			{
				IndexName = _name;
				FieldNames = _field;
				Unique = _unique;
			}

			public Index(string _name, string _field, bool _unique)
			{
				IndexName = _name;
				FieldNames = new string[1] { _field };
				Unique = _unique;
			}

            public void AddIndexFieldName(string _field, bool _unique)
            {
                Debug.Assert(Unique == _unique);
                string[] names = new string[FieldNames.Length + 1];
                FieldNames.CopyTo(names, 0);
                names[names.Length - 1] = _field;
                FieldNames = names;
            }
        }

		string m_tableName;
		List<Field> m_fields;
		Dictionary<string, Field> m_fieldMap;
		List<Index> m_indices;

		#region Properties

		public virtual string TableName
		{
			get { return m_tableName; }
		}

		public Field this[int index]
		{
			get { return m_fields[index]; }
		}

		public Field this[string name]
		{
			get { return m_fieldMap[name]; }
		}

		#endregion

		#region Static members

		class SqlType
		{
			public FieldFlags Flags;
			public string SqlName, ProgName;
			public bool HasSize, Nullable;

			public SqlType(FieldFlags _flags, string _sql, string _prog)
			{
				Flags = _flags;
				SqlName = _sql;
				ProgName = _prog;
				HasSize = Nullable = false;
			}
		}

		static Dictionary<FieldFlags, SqlType> ms_sqlType;
		static Dictionary<string, SqlType> ms_sqlnType;
		static Dictionary<string, SqlType> ms_csType;

		static void _AddSqlType(SqlType stype)
		{
			ms_sqlType.Add(stype.Flags, stype);
			ms_sqlnType.Add(stype.SqlName, stype);
		}

		static void _AddCsType(string csType, FieldFlags sqlType)
		{
			ms_csType.Add(csType, ms_sqlType[sqlType]);
		}

		static MysqlDataOperator()
		{
			ms_sqlType = new Dictionary<FieldFlags, SqlType>();
			ms_sqlnType = new Dictionary<string, SqlType>();
			ms_csType = new Dictionary<string, SqlType>();

			_AddSqlType(new SqlType(FieldFlags.VarChar, "varchar", "string"));
			SqlType stype = ms_sqlType[FieldFlags.VarChar];
			stype.HasSize = stype.Nullable = true;

			_AddSqlType(new SqlType(FieldFlags.Text, "text", "string"));
			ms_sqlType[FieldFlags.Text].Nullable = true;

			_AddSqlType(new SqlType(FieldFlags.SByte,	"tinyint",			"sbyte"));
			_AddSqlType(new SqlType(FieldFlags.Byte,	"tinyint unsigned", "byte"));
			_AddSqlType(new SqlType(FieldFlags.Short,	"smallint",			"short"));
			_AddSqlType(new SqlType(FieldFlags.UShort,	"smallint unsigned","ushort"));
			_AddSqlType(new SqlType(FieldFlags.Int,		"int",				"int"));
			_AddSqlType(new SqlType(FieldFlags.UInt,	"int unsigned",		"uint"));
			_AddSqlType(new SqlType(FieldFlags.Long,	"bigint",			"long"));
			_AddSqlType(new SqlType(FieldFlags.ULong,	"bigint unsigned",	"ulong"));

			_AddSqlType(new SqlType(FieldFlags.Float,	"float",	"float"));
			_AddSqlType(new SqlType(FieldFlags.Double,	"double",	"double"));

			_AddCsType("String",	FieldFlags.VarChar);
			_AddCsType("Int8",		FieldFlags.SByte);
			_AddCsType("UInt8",		FieldFlags.Byte);
			_AddCsType("Int16",		FieldFlags.Short);
			_AddCsType("UInt16",	FieldFlags.UShort);
			_AddCsType("Int32",		FieldFlags.Int);
			_AddCsType("UInt32",	FieldFlags.UInt);
			_AddCsType("Int64",		FieldFlags.Long);
			_AddCsType("UInt64",	FieldFlags.ULong);
			_AddCsType("float",		FieldFlags.Float);
			_AddCsType("double",	FieldFlags.Double);
		}

		#endregion

		#region Initialize

		public MysqlDataOperator(string tableName)
		{
			m_tableName = tableName;
			m_fields = new List<Field>();
			m_fieldMap = new Dictionary<string, Field>();
			AddField(new Field("Id", "Id", FieldFlags.AutoInc | FieldFlags.UInt));
			m_indices = new List<Index>();
		}

		public void AddField(Field field)
		{
			m_fields.Add(field);
			m_fieldMap.Add(field.FieldName, field);
		}

		public void AddIndex(Index index)
		{
			m_indices.Add(index);
		}

		public void InitAutoFields(Type type)
		{
			FieldInfo[] props = type.GetFields();
			foreach (FieldInfo prop in props)			
			{
				if (prop.Name == "Id")
					continue;

				// default values
				string propName = prop.Name;
				string fieldName = propName;
				string typeName = prop.FieldType.Name;
				FieldFlags sqltype = ms_csType[typeName].Flags;
				FieldFlags flags = sqltype;
				int size = sqltype == FieldFlags.VarChar ? 0 : 64;

				// custom values
				TableFieldAttribute attr = (TableFieldAttribute)Attribute.GetCustomAttribute(prop, typeof(TableFieldAttribute));
				if (attr != null)
				{
					if (attr.Nullable)
					{
						Debug.Assert(sqltype == FieldFlags.VarChar || sqltype == FieldFlags.Text);
						flags |= (FieldFlags.Nullable | FieldFlags.DefaultNull);
					}
					if (attr.Size != 0)
					{
						Debug.Assert(sqltype == FieldFlags.VarChar);
						size = attr.Size;
					}
					if (attr.SqlType != null)
					{
						if (attr.SqlType == "text")
						{
							Debug.Assert(sqltype == FieldFlags.VarChar);
							sqltype = FieldFlags.Text;
							flags = (flags & ~FieldFlags.Mask_Type) | sqltype;
						}
						else
							Debug.Assert(false);
					}
				}

				Field field = new Field(propName, fieldName, flags);
				field.Size = size;
				AddField(field);
			}
		}

        public void InitIndices(Type type)
        {
            FieldInfo[] props = type.GetFields();
            Dictionary<string, Index> indices = new Dictionary<string, Index>();
            foreach (FieldInfo prop in props)
            {
                TableFieldAttribute attr = (TableFieldAttribute)Attribute.GetCustomAttribute(prop, typeof(TableFieldAttribute));
                if (attr != null && attr.indexName != null)
                {
                    string propName = prop.Name;
                    string fieldName = propName;
                    string indexName = attr.indexName;
                    Index index = null;
                    indices.TryGetValue(indexName, out index);
                    // 存在则添加新的字段
                    if (index != null)
                        index.AddIndexFieldName(fieldName, attr.unique);
                    else
                        // 不存在添加新的
                        indices.Add(indexName, new Index(indexName, fieldName, attr.unique));
                }
            }
            m_indices = indices.Values.ToList();
        }

        #endregion

        #region Create Table SQL

        public virtual string MakeCreateTableSql()
		{			
			// const string sql = @"create table TestItems(
			//			`Id` int unsigned not null auto_increment,
			//			`Name` varchar(64) default null,
			//			`Birth` int unsigned not null,
			//			primary key (`Id`),
			//			unique key (`Name`)
			//		) engine=innodb default charset=utf8";

			StringBuilder sb = new StringBuilder();
			sb.Append(@"create table ").Append(TableName).Append('(');

			// fields
			foreach (Field field in m_fields)
				MakeCreateField(field, sb);

			// primary
			sb.Append(@"primary key (`Id`),");

			// indices
			foreach (Index index in m_indices)
				MakeCreateIndex(index, sb);

			sb.Remove(sb.Length - 1, 1);	// last ,
			sb.Append(@") engine=innodb default charset=utf8");
			return sb.ToString();
		}

		void MakeCreateField(Field field, StringBuilder sb)
		{
			SqlType stype = ms_sqlType[field.Flags & FieldFlags.Mask_Type];
			sb.Append('`').Append(field.FieldName).Append("` ");
			sb.Append(stype.SqlName);
			if (stype.HasSize)
				sb.Append('(').Append(field.Size).Append(')');
			if ((field.Flags & FieldFlags.Nullable) != 0)
			{
				Debug.Assert(stype.Nullable);
				if ((field.Flags & FieldFlags.DefaultNull) != 0)
					sb.Append(@" default null");
			}
			else
				sb.Append(@" not null");
			if ((field.Flags & FieldFlags.AutoInc) != 0)
				sb.Append(@" auto_increment");
			sb.Append(',');
		}

		void MakeCreateIndex(Index index, StringBuilder sb)
		{
			sb.Append(index.Unique ? @"unique key " : @"key ");
			if (index.IndexName != null)
				sb.Append('`').Append(index.IndexName).Append('`');
			sb.Append('(');
			Debug.Assert(index.FieldNames.Length != 0);
			foreach (string name in index.FieldNames)
				sb.Append('`').Append(name).Append("`,");
			sb.Remove(sb.Length - 1, 1);
			sb.Append(@"),");
		}

		#endregion

		#region Select SQL & read

		public virtual string MakeSelectSql(string where)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(@"select * from ").Append(TableName);
			if (where != null)
				sb.Append(@" where ").Append(where);
			return sb.ToString();
		}

		public virtual T ReadItem(MySqlDataReader rs)
		{
			Type type = typeof(T);
			T item = (T)Activator.CreateInstance(type);

			for (int i = 0; i < m_fields.Count; ++i)
			{
				Field field = m_fields[i];
				FieldInfo prop = type.GetField(field.PropName);
				object value = rs[i];
				prop.SetValue(item, value);
			}

			return item;
		}

		#endregion

		#region Insert SQL

		public virtual string MakeInsertAutoIdSql(T item)
		{
			// return string.Format("insert into TestItems(Name,Birth) values('{0}',{1})", item.Name, item.Birth);

			StringBuilder sb = new StringBuilder();
			sb.Append(@"insert into ").Append(TableName).Append('(');
			for (int i = 1; i < m_fields.Count; ++i)
				sb.Append(m_fields[i].FieldName).Append(',');
			sb.Remove(sb.Length - 1, 1);
			sb.Append(@") values(");
			for (int i = 1; i < m_fields.Count; ++i)
			{
				Field field = m_fields[i];
				sb.Append(field.IsString ? "\"{" : "{");
				sb.Append(i - 1);
				sb.Append(field.IsString ? "}\"," : "},");
			}
			sb[sb.Length - 1] = ')';
			string pat = sb.ToString();

			Type type = typeof(T);
			object[] values = new object[m_fields.Count - 1];
			for (int i = 1; i < m_fields.Count; ++i)
			{
				Field field = m_fields[i];
				FieldInfo prop = type.GetField(field.PropName);
				values[i - 1] = prop.GetValue(item);
			}

			return string.Format(pat, values);
		}

		public virtual string MakeInsertWithIdSql(T item)
		{
			// return string.Format("insert into TestItems(Id,Name,Birth) values({0},'{1}',{2})", item.Id, item.Name, item.Birth);

			StringBuilder sb = new StringBuilder();
			sb.Append(@"insert into ").Append(TableName).Append('(');
			for (int i = 0; i < m_fields.Count; ++i)
				sb.Append(m_fields[i].FieldName).Append(',');
			sb.Remove(sb.Length - 1, 1);
			sb.Append(@") values(");
			for (int i = 0; i < m_fields.Count; ++i)
			{
				Field field = m_fields[i];
				sb.Append(field.IsString ? "\"{" : "{");
				sb.Append(i);
				sb.Append(field.IsString ? "}\"," : "},");
			}
			sb[sb.Length - 1] = ')';
			string pat = sb.ToString();

			Type type = typeof(T);
			object[] values = new object[m_fields.Count];
			for (int i = 0; i < m_fields.Count; ++i)
			{
				Field field = m_fields[i];
				FieldInfo prop = type.GetField(field.PropName);
                
				values[i] = prop.GetValue(item);
			}

			return string.Format(pat, values);
		}

		#endregion

		#region Update SQL

		public virtual string MakeUpdateSql(T item)
		{
			// return string.Format("update TestItems set Name='{0}',Birth={1} where Id={2}", item.Name, item.Birth, item.Id);

			StringBuilder sb = new StringBuilder();
			sb.Append(@"update ").Append(TableName).Append(@" set ");
			for (int i = 1; i < m_fields.Count; ++i)
			{
				Field field = m_fields[i];
				sb.Append(field.FieldName).Append('=');
				sb.Append(field.IsString ? "\"{" : "{");
				sb.Append(i);
				sb.Append(field.IsString ? "}\"," : "},");
			}
			sb.Remove(sb.Length - 1, 1);
			sb.Append(@" where Id={0}");
			string pat = sb.ToString();

			Type type = typeof(T);
			object[] values = new object[m_fields.Count];
			for (int i = 0; i < m_fields.Count; ++i)
			{
				Field field = m_fields[i];
				FieldInfo prop = type.GetField(field.PropName);
				values[i] = prop.GetValue(item);
			}

			string sql = string.Format(pat, values);
			return sql;
		}

		public virtual string MakeUpdateBatchSql(string set, string where)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(@"update ").Append(TableName).Append(@" set ").Append(set);
			if (where != null)
				sb.Append(@" where ").Append(where);
			return sb.ToString();
		}

		#endregion

		public virtual string MakeDeleteSql(string where)
		{
			Debug.Assert(where != null);
			StringBuilder sb = new StringBuilder();
			sb.Append(@"delete from ").Append(TableName).Append(@" where ").Append(where);
			return sb.ToString();
		}
	}

	public class MysqlDataListTable<T> : IEnumerable<T> where T : DataTableItem
	{
		IMysqlConnectionFactory m_factory;
		IMysqlDataOperator<T> m_dop;

		public MysqlDataListTable(IMysqlConnectionFactory factory, IMysqlDataOperator<T> dop)
		{
			m_factory = factory;
			m_dop = dop;
		}

		public MysqlDataListTable(IMysqlConnectionFactory factory, string tableName)
		{
			m_factory = factory;
			MysqlDataOperator<T> dop = new MysqlDataOperator<T>(tableName);
			dop.InitAutoFields(typeof(T));
            dop.InitIndices(typeof(T));
			m_dop = dop;
		}

		#region Select

		public T this[uint id]
		{
			get
			{
				List<T> items = Select("Id=" + id.ToString());
				Debug.Assert(items.Count <= 1);
				return items.Count != 0 ? items[0] : null;
			}
		}

		public uint Count
		{
			get
			{
				string sql = "select count(*) from " + m_dop.TableName;
				object o = ExecuteSingleValue(sql);
				return (uint)(long)o;
			}
		}

		public List<T> Select(string where)
		{
			string sql = m_dop.MakeSelectSql(where);
			MySqlDataReader rs = ExecuteRead(sql);
			return ReadResultSet(rs);
		}

		#endregion

		#region Insert

		public T Add(T item)
		{
			MySqlConnection con = m_factory.Retrieve();
			try
			{
				if (item.Id == 0)
				{
					string sql = m_dop.MakeInsertAutoIdSql(item);
					int r = MySqlKit.ExecuteCommand(con, sql);
					Debug.Assert(r == 1);

					object o = MySqlKit.ExecuteSingleValue(con, "select @@identity");
					Debug.Assert(o != null);
					item.Id = (uint)(ulong)o;

					return item;
				}
				else
				{
					string sql = m_dop.MakeInsertWithIdSql(item);
					int r = MySqlKit.ExecuteCommand(con, sql);
					Debug.Assert(r == 1);
					return item;
				}
			}
			finally
			{
				m_factory.Return(con);
			}
		}

		public int Update(T item)
		{
			Debug.Assert(item.Id != 0);
			string sql = m_dop.MakeUpdateSql(item);
			return ExecuteCommand(sql);
		}

		public int BatchUpdate(string set, string where)
		{
			string sql = m_dop.MakeUpdateBatchSql(set, where);
			return ExecuteCommand(sql);
		}

		#endregion

		#region Remove

		public int RemoveAt(uint id)
		{
			string sql = m_dop.MakeDeleteSql("Id=" + id.ToString());
			return ExecuteCommand(sql);
		}

		// Remove items whose ID value in range [id1, id2)
		public int RemoveRange(uint id1, uint id2)
		{
			Debug.Assert(id1 < id2);
			string where = string.Format("Id>={0} and Id<{1}", id1, id2);
			string sql = m_dop.MakeDeleteSql(where);
			return ExecuteCommand(sql);
		}

		public int Remove(string where)
		{
			string sql = m_dop.MakeDeleteSql(where);
			return ExecuteCommand(sql);
		}

		public void Clear()
		{
			string sql = "truncate table " + m_dop.TableName;
			ExecuteCommand(sql);
		}

		#endregion

		#region Enumerators

		public IEnumerator<T> GetEnumerator()
		{
			return Enumerate(null).GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return Enumerate(null).GetEnumerator();
		}

		public IEnumerable<T> Enumerate(string where)
		{
			string sql = m_dop.MakeSelectSql(where);
			MySqlDataReader rs = ExecuteRead(sql);
			while (rs.Read())
				yield return m_dop.ReadItem(rs);
			rs.Close();
		}

		#endregion

		#region Table

		public void DropTable()
		{
			string name = m_dop.TableName;
			ExecuteCommand("drop table if exists " + name);
		}

		public void CreateTable()
		{
			string sql = m_dop.MakeCreateTableSql();
			ExecuteCommand(sql);
		}

		#endregion

		#region Toolkit

		int ExecuteCommand(string sql)
		{
			MySqlConnection con = m_factory.Retrieve();
			try
			{
				return MySqlKit.ExecuteCommand(con, sql);
			}
			finally
			{
				m_factory.Return(con);
			}
		}

		public object ExecuteSingleValue(string sql)
		{
			MySqlConnection con = m_factory.Retrieve();
			try
			{
				return MySqlKit.ExecuteSingleValue(con, sql);
			}
			finally
			{
				m_factory.Return(con);
			}
		}

		public MySqlDataReader ExecuteRead(string sql)
		{
			MySqlConnection con = m_factory.Retrieve();
			try
			{
				return MySqlKit.ExecuteReader(con, sql);
			}
			finally
			{
				m_factory.Return(con);
			}
		}

		public List<T> ReadResultSet(MySqlDataReader rs)
		{
			List<T> items = new List<T>();
			while (rs.Read())
			{
				T item = m_dop.ReadItem(rs);
				items.Add(item);
			}
			rs.Close();
			return items;
		}

		#endregion
	}
}
