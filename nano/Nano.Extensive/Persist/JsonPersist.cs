using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nano.Json;

namespace Nano.Ext.Persist
{
	public interface JsonModelSaver
	{
		void Write(JsonNode jnode);
        void Flush();
		void Close();
	}

	class StringJsonModelSaver : JsonModelSaver
	{
		Stream m_stream;
		TextWriter m_tw;

		internal StringJsonModelSaver(Stream stream)
		{
			m_stream = stream;
			m_tw = new StreamWriter(stream, Encoding.UTF8);
		}

		public void Write(JsonNode jnode)
		{
			var jw = new JsonWriter(jnode);
			string str = jw.GetString();
			WriteString(str);
		}

		void WriteString(string str)
		{
			lock (this)
				m_tw.WriteLine(str);
		}

        public void Flush()
        {
            m_tw.Flush();
            m_stream.Flush();
        }

		public void Close()
		{
			m_tw.Close();
			m_stream.Close();
		}
	}

	public abstract class JsonModelLoader
	{
		public delegate void Accept(JsonNode node);

		public abstract void Load(Stream stream, Accept accept);
	}

	class StringJsonModelLoader : JsonModelLoader
	{
		public override void Load(Stream stream, Accept accept)
		{
			using (var tr = new StreamReader(stream, Encoding.UTF8))
			{
				string s;
				while ((s = tr.ReadLine()) != null)
				{
					var jnode = JsonParser.ParseText(s);
					accept(jnode);
				}
			}
			stream.Close();
		}
	}
}
