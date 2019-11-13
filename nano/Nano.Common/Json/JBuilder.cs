using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Nano.Json
{
    public class JBuilder
    {
        public JNode Create(object o)
        {
            if (o == null)
                return JNode.Null;

            if (o is JNode)
                return (JNode)o;

            if (o is Array)
                return FromArray(o);

            if (o is string)
                return new JNode(JNode.Jt_String, (string)o);

            var t = o.GetType();
            return Create(t, o);
        }

        JNode Create(Type t, object o)
        {
            if (IsAnonymous(t))
            {
                return FromAnonymousInstance(t, o);
            }
            else if (t.IsGenericType)
            {
                Type gt = t.GetGenericTypeDefinition();
                if (gt == typeof(List<>))
                    return FromList(t, gt, o);
                else if (gt == typeof(Dictionary<,>))
                    return FromDictionary(t, gt, o);
                else if (gt == typeof(ConcurrentDictionary<,>))
                    return FromDictionary(t, gt, o);
                else if (IsJsonClass(t))
                    return FromJsonClassInstance(t, o);

                throw Error("Unsupport type name: {0}", t.Name);
            }
            else if (t.IsValueType)
            {
                if (IsJsonClass(t))
                    return FromJsonClassInstance(t, o);

                return FromBaseValueType(t, o);
            }
            else
            {
                return FromSimpleClassInstance(t, o);
            }

        }

        JNode FromList(Type t, Type gt, object o)
        {
            var arr = new List<JNode>();
            var ovs = (System.Collections.IList)o;
            foreach (var ov in ovs)
            {
                var v = Create(ov);
                if (!v.IsUndefined)
                {
                    //Verify(!v.IsUndefined, "Type is undefined");
                    arr.Add(v);
                }
            }
            return new JNode(JNode.Jt_Array, arr);
        }

        JNode FromDictionary(Type t, Type gt, object o)
        {
            Type vt = t.GetGenericArguments()[0];
            if (vt != typeof(string))
                throw Error("Key is Not string");

            var dict = new ConcurrentDictionary<string, JNode>();
            var dc = (System.Collections.IDictionary)o;
            foreach (System.Collections.DictionaryEntry e in dc)
            {
                var n = (string)e.Key;
                var v = Create(e.Value);
                Verify(!v.IsUndefined, "Type is undefined");
                dict.TryAdd(n, v);
            }
            return new JNode(JNode.Jt_Object, dict);
        }

        JNode FromArray(object o)
        {
            var arr = new List<JNode>();
            var ovs = (Array)o;
            foreach (var ov in ovs)
            {
                var v = Create(ov);
                Verify(!v.IsUndefined, "Type is undefined");
                arr.Add(v);
            }
            return new JNode(JNode.Jt_Array, arr);
        }

        JNode FromJsonClassInstance(Type t, object o)
        {
            var dict = new ConcurrentDictionary<string, JNode>();
            var at = typeof(JFieldAttribute);
            var pps = t.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            foreach (var p in pps)
            {
                var attr = p.GetCustomAttributes(at, false);
                if (attr.Count() > 0)
                {
                    var jattr = attr[0] as JFieldAttribute;
                    var v = Create(p.GetValue(o, null));
                    var n = jattr.Name ?? p.Name;
                    Verify(!v.IsUndefined, "Type is undefined");
                    dict.TryAdd(n, v);
                }
            }

            var fields = t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            foreach (var f in fields)
            {                
                var attr = f.GetCustomAttributes(at, false);
                if (attr.Count() > 0)
                {
                    var jattr = attr[0] as JFieldAttribute;
                    var v = Create(f.GetValue(o));
                    var n = jattr.Name ?? f.Name;
                    Verify(!v.IsUndefined, "Type is undefined");
                    dict.TryAdd(n, v);
                }
            }

            return new JNode(JNode.Jt_Object, dict);
        }

        JNode FromSimpleClassInstance(Type t, object o)
        {
            var dict = new ConcurrentDictionary<string, JNode>();
            var fields = t.GetFields();
            foreach (var f in fields)
            {
                var v = Create(f.GetValue(o));
                Verify(!v.IsUndefined, "Type is undefined");
                dict.TryAdd(f.Name, v);
            }
            return new JNode(JNode.Jt_Object, dict);
        }


        JNode FromAnonymousInstance(Type t, object o)
        {
            var dict = new ConcurrentDictionary<string, JNode>();
            var pps = t.GetProperties();
            foreach (var p in pps)
            {
                var v = Create(p.GetValue(o, null));
                Verify(!v.IsUndefined, "Type is undefined");
                dict.TryAdd(p.Name, v);
            }
            return new JNode(JNode.Jt_Object, dict);
        }

        JNode FromBaseValueType(Type t, object o)
        {
            switch (t.FullName) 
            {
                case "System.Boolean": return new JNode((bool)o);
                case "System.Byte":return new JNode((byte)o);
                case "System.SByte":return new JNode((sbyte)o);
                case "System.Char": return new JNode((char)o);
                case "System.Int16":return new JNode((short)o);
                case "System.UInt16":return new JNode((ushort)o);
                case "System.Int32":return new JNode((int)o);
                case "System.UInt32":return new JNode((uint)o);
                case "System.Int64":return new JNode((long)o);
                case "System.UInt64":return new JNode((ulong)o);
                case "System.Single":return new JNode((float)o);
                case "System.Double":return new JNode((double)o);
                case "System.String": return new JNode((string)o);
                default:
                    throw Error("Unsupported type: {0}", t.FullName);
            }
        }

        bool IsJsonClass(Type t)
        {
            return JNodeAttribute.IsJsonClass(t);
        }

        bool IsAnonymous(Type t)
        {
            return t.IsClass && t.Namespace == null;
        }

        void Verify(bool condistions, string format, params object[] args)
        {
            if (!condistions)
            {
                var msg = format == null ? null : string.Format(format, args);
                throw new TypeException(msg);
            }
        }

        Exception Error(string format, params object[] args)
        {
            var msg = format == null ? null : string.Format(format, args);
            throw new TypeException(msg);
        }
    }
}
