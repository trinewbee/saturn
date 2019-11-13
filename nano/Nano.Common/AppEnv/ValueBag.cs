using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
#if WIN32
using Microsoft.Win32;
#endif

namespace Nano.Common.AppEnv
{
	public enum ConfigValueType
	{
		None, Int, Long, String, StringArray, Binary, List, Group
	}

	public interface IConfigValueAccpt
	{
		object InitValue(string name, object defaultValue);
	}

	public abstract class ConfigValue
	{
		protected IConfigValueAccpt m_accpt;
		protected ConfigValueType m_vt;
		protected string m_name;
		protected object m_value = null;

		protected ConfigValue(IConfigValueAccpt accpt, ConfigValueType vt, string name)
		{
			m_accpt = accpt;
			m_vt = vt;
			m_name = name;
		}

		#region Properties

		public ConfigValueType Type
		{
			get { return m_vt; }
		}

		public string Name
		{
			get { return m_name; }
		}

		public object Value
		{
			get { return m_value; }
			set
			{
				UpdateValue(value);
			}
		}

		#endregion

		#region Updater

		protected abstract void UpdateValue(object value);

		#endregion
	}

	public class ConfigValueT<T> : ConfigValue
	{
		public ConfigValueT(IConfigValueAccpt accpt, ConfigValueType vt, string name, object defaultValue)
			: base(accpt, vt, name)
		{
			m_value = accpt.InitValue(name, defaultValue);
		}

		protected override void UpdateValue(object value)
		{
			throw new NotImplementedException();
		}
	}

	public class ListConfigValue : ConfigValue
	{
		List<ConfigValue> m_values;

		public ListConfigValue(IConfigValueAccpt accpt, string name)
			: base(accpt, ConfigValueType.List, name)
		{
		}

		protected override void UpdateValue(object value)
		{
			throw new NotSupportedException();
		}
	}

	public class GroupConfigValue : ConfigValue
	{
		Dictionary<string, ConfigValue> m_values;

		public GroupConfigValue(IConfigValueAccpt accpt, string name)
			: base(accpt, ConfigValueType.Group, name)
		{
		}

		protected override void UpdateValue(object value)
		{
			throw new NotSupportedException();
		}
	}

	public abstract class ConfigValueBag
	{
	}

#if WIN32

	public class RegistryValueBag: ConfigValueBag
	{
		RegistryKey m_key;
		GroupConfigValue m_root;

		class RegistryAccpt : IConfigValueAccpt
		{
			public RegistryKey Key;

			public RegistryAccpt(RegistryKey _key) { Key = _key; }

			public object InitValue(string name, object defaultValue)
			{
				return Key.GetValue(name, defaultValue);
			}
		}

		public RegistryValueBag(string path)
		{
			int pos = 0, pos2;
			RegistryKey key = Registry.CurrentUser;
			while ((pos2 = path.IndexOf('\\', pos)) >= 0)
			{
				key = ValidateSubKey(key, path.Substring(pos, pos2));
				pos = pos2 + 1;
			}
			m_key = ValidateSubKey(key, path.Substring(pos));
		}

		RegistryKey ValidateSubKey(RegistryKey key, string name)
		{
			RegistryKey subkey = key.OpenSubKey(name);
			if (subkey == null)
				subkey = key.CreateSubKey(name);

			Debug.Assert(subkey != null);
			return subkey;
		}

		public GroupConfigValue Root
		{
			get { return m_root; }
		}
	}


#endif
}
