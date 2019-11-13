using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace Nano.Json
{
    public class JFormater
    {
        public interface IWriter
        {
            void Write(char ch);
            void Write(string v);
        }

        static string[] Escapee = new string[]
        {
            "\\0", "\\u0001", "\\u0002", "\\u0003",
            "\\u0004", "\\u0005", "\\u0006", "\\u0007",
            "\\b", "\\t", "\\n", "\\u000b",
            "\\f", "\\r", "\\u000e", "\\u000f",
            "\\u0010", "\\u0011", "\\u0012", "\\u0013",
            "\\u0014", "\\u0015", "\\u0016", "\\u0017",
            "\\u0018", "\\u0019", "\\u001a", "\\u001b",
            "\\u001c", "\\u001d", "\\u001e", "\\u001f"
        };

        public string Format(JNode json)
        {
            var w = new JsonStringWriter();
            Format(json, w);
            return w.GetString();
        }

        public void Format(JNode json, System.IO.Stream s, Encoding e)
        {
            var w = new JsonStreamWriter(s, e);
            Format(json, w);
        }

        public void Format(JNode json, IWriter writer)
        {
            Write(json, writer, 0);
        }

        void Write(JNode j, IWriter w, int deep)
        {
            switch (j.Type)
            {
                case JNode.Jt_Array: WriteArray(j.Items, j.Length, w, deep); break;
                case JNode.Jt_Object: WriteObject(j.Fields, j.Count, w, deep); break;
                case JNode.Jt_String: WriteString(j.TextValue, w, deep); break;
                case JNode.Jt_Null: WriteNull(j.Value, w, deep); break;
                case JNode.Jt_Float: WriteFloat(j.FloatValue, w, deep); break;
                case JNode.Jt_Integer: WriteInteger(j.IntValue, w, deep); break;
                case JNode.Jt_Boolean: WriteBoolean(j.BoolValue, w, deep); break;
                default: throw Error("Bad Type");
            }
        }

        void WriteArray(IEnumerable<JNode> children, int count, IWriter w, int deep)
        {
            w.Write('[');
            var i = 0;
            foreach (var v in children)
            {
                Write(v, w, deep);
                if (i < count - 1)
                    w.Write(',');
                i++;
            }
            w.Write(']');
        }

        void WriteObject(IEnumerable<JNode.Field> fields, int count, IWriter w, int deep)
        {
            w.Write('{');
            var i = 0;
            foreach (var f in fields)
            {
                WriteString(f.Name, w, deep);

                w.Write(':');
                Write(f.Value, w, deep);

                if (i < count - 1)
                    w.Write(',');
                i++;
            }
            w.Write('}');
        }

        void WriteString(string v, IWriter w, int deep)
        {
            if (v == null)
            {
                w.Write("null");
                return;
            }

            w.Write('\"');

            foreach (char ch in v)
            {
                if (ch < ' ')
                {
                    w.Write(Escapee[ch]);
                    continue;
                }
                switch (ch)
                {
                    case '\\':
                        w.Write("\\\\");
                        break;
                    case '\"':
                        w.Write("\\\"");
                        break;
                    default:
                        w.Write(ch);
                        break;
                }
            }

            w.Write('\"');
        }

        void WriteFloat(double v, IWriter w, int deep)
        {
            w.Write(Convert.ToString(v));
        }

        void WriteInteger(long v, IWriter w, int deep)
        {
            w.Write(Convert.ToString(v));
        }

        void WriteBoolean(bool v, IWriter w, int deep)
        {
            w.Write(v ? "true" : "false");
        }

        void WriteNull(object v, IWriter w, int deep)
        {
            w.Write("null");
        }

        Exception Error(string format, params object[] args)
        {
            var msg = format == null ? null : string.Format(format, args);
            return new JException("", msg);
        }

        void Verify(bool conditions, string format, params object[] args)
        {
            if (!conditions)
            {
                var msg = format == null ? null : string.Format(format, args);
                throw new JException("", msg);
            }
        }
    }

    public class JsonStringWriter : JFormater.IWriter
    {
        StringBuilder m_sb;
        public JsonStringWriter(StringBuilder sb)
        {
            m_sb = sb;
        }

        public JsonStringWriter() : this(new StringBuilder()) { }

        public void Write(char ch) => m_sb.Append(ch);

        public void Write(string str) => m_sb.Append(str);

        public string GetString() => m_sb.ToString();
    }

    public class JsonStreamWriter : JFormater.IWriter, IDisposable
    {
        Encoding m_encoding;
        System.IO.Stream m_stream;

        public JsonStreamWriter(System.IO.Stream s, Encoding e)
        {
            m_encoding = e;
            m_stream = s;
        }

        public void Write(char ch)
        {
            var data = m_encoding.GetBytes(new char[] { ch });
            m_stream.Write(data, 0, data.Length);
        }

        public void Write(string str)
        {
            var data = m_encoding.GetBytes(str);
            m_stream.Write(data, 0, data.Length);
        }

        public void Dispose()
        {
            if (null != m_stream)
            {
                m_stream.Dispose();
                m_stream = null;
            }
        }
    }
}
