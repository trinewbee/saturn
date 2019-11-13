using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace Nano.Json
{
    public struct JNode
    {
        public class Field
        {
            public string Name;
            public JNode Value;

            public Field(string name, JNode v)
            {
                Name = name;
                Value = v;
            }
        }

        #region types
        internal const int Jt_Undefined = 0;
        internal const int Jt_Null = 1;
        internal const int Jt_Boolean = 2;
        internal const int Jt_Integer = 4;
        internal const int Jt_Float = 8;
        internal const int Jt_String = 16;
        internal const int Jt_Array = 32;
        internal const int Jt_Object = 64;

        internal const int Jt_Number = Jt_Integer | Jt_Float;
        internal const int Jt_Text = Jt_String | Jt_Null;
        internal const int Jt_Nullable = Jt_String | Jt_Null;

        #endregion

        public object Value { get; }
        internal int Type { get; }

        #region defaults
        public static JNode Undefined = default(JNode);

        public static JNode Null = new JNode(Jt_Null, null);

        public static JNode True = new JNode(Jt_Boolean, true);

        public static JNode False = new JNode(Jt_Boolean, false);
        #endregion

        #region constuctors
        internal JNode(int type, object value)
        {
            Type = type; Value = value;
        }

        public JNode(bool v) : this(Jt_Boolean, v) { }

        public JNode(byte v) : this(Jt_Integer, (long)v) { }

        public JNode(sbyte v) : this(Jt_Integer, (long)v) { }

        public JNode(char v) : this(Jt_Integer, (long)v) { }

        public JNode(short v) : this(Jt_Integer, (long)v) { }

        public JNode(ushort v) : this(Jt_Integer, (long)v) { }

        public JNode(int v) : this(Jt_Integer, (long)v) { }

        public JNode(uint v) : this(Jt_Integer, (long)v) { }

        public JNode(long v) : this(Jt_Integer, (long)v) { }

        public JNode(ulong v) : this(Jt_Integer, (long)v) { }

        public JNode(float v) : this(Jt_Float, (double)v) { }

        public JNode(double v) : this(Jt_Float, (double)v) { }

        public JNode(string v) : this(Jt_String, (string)v) { }

        #endregion

        #region creaters

        public static JNode NewArray()
        {
            return new JNode(Jt_Array, new List<JNode>());
        }

        public static JNode NewArray(Array arr)
        {
            var ret = new List<JNode>();
            foreach (var o in arr)
            {
                var v = Create(o);
                Verify(!v.IsUndefined, "Type is undefined");
                ret.Add(v);
            }
            return new JNode(Jt_Array, ret);
        }

        public static JNode NewArray<T>(IEnumerable<T> arr)
        {
            var ret = new List<JNode>();
            foreach (var o in arr)
            {
                var v = Create(o);
                Verify(!v.IsUndefined, "Type is undefined");
                ret.Add(v);
            }
            return new JNode(Jt_Array, ret);
        }

        public static JNode NewArray(params object[] args)
        {
            var ret = new List<JNode>();
            foreach (var o in args)
            {
                var v = Create(o);
                Verify(!v.IsUndefined, "Type is undefined");
                ret.Add(v);
            }
            return new JNode(Jt_Array, ret);
        }

        public static JNode NewObject()
        {
            return new JNode(Jt_Object, new ConcurrentDictionary<string, JNode>());
        }

        public static JNode NewObject<T>(IDictionary<string, T> ovs)
        {
            var dict = new ConcurrentDictionary<string, JNode>();
            foreach (var ov in ovs)
            {
                var v = Create(ov.Value);
                Verify(!v.IsUndefined, "Type is undefined");
                dict.TryAdd(ov.Key, v);
            }
            return new JNode(Jt_Object, dict);
        }

        public static JNode Create(object o)
        {
            return new JBuilder().Create(o);
        }

        public static bool TryCreate(object o, out JNode json)
        {
            try
            {
                json = Create(o);
                return true;
            }
            catch (Exception ex)
            {
                json = Undefined;
                return false;
            }
        }

        public static JNode Parse(string txt)
        {
            return new JParser().Parse(txt);
        }

        public static bool TryParse(string txt, out JNode json)
        {
            try
            {
                json = Parse(txt);
                return true;
            }
            catch(Exception ex)
            {
                json = Undefined;
                return false;
            }
        }

        public static string Stringify(JNode json)
        {
            Verify(!json.IsUndefined, "Type is undefined.");
            return new JFormater().Format(json);
        }

        public static bool TryStringify(JNode json, out string ret)
        {
            try
            {
                ret = Stringify(json);
                return true;
            }
            catch (Exception ex)
            {
                ret = null;
                return false;
            }
        }

        #endregion

        #region accessers

        public bool IsUndefined { get { return Type == 0; } }

        public bool IsNull { get { return (Type & Jt_Null) != 0; } }

        public bool IsNullable { get { return (Type & Jt_Nullable) != 0; } }

        public bool IsBoolean { get { return (Type & Jt_Boolean) != 0; } }

        public bool IsInteger { get { return (Type & Jt_Integer) != 0; } }

        public bool IsFloat { get { return (Type & Jt_Float) != 0; } }

        public bool IsNumber { get { return (Type & Jt_Number) != 0; } }

        public bool IsString { get { return (Type & Jt_String) != 0; } }

        public bool IsText { get { return (Type & Jt_Text) != 0; } }

        public bool IsArray { get { return (Type & Jt_Array) != 0; } }

        public bool IsObject { get { return (Type & Jt_Object) != 0; } }

        public string TextValue { get { Verify(Jt_Text, "Not Text"); return (string)Value; } }

        public bool BoolValue { get { Verify(Jt_Boolean, "Not Boolean"); return (bool)Value; } }

        public long IntValue { get { Verify(Jt_Integer, "Not Integer"); return (long)Value; } }

        public double FloatValue { get { Verify(Jt_Float, "Not Float"); return (double)Value; } }
               
        #endregion

        #region array
        internal List<JNode> ArrayValue { get { Verify(Jt_Array, "Not Array"); return (List<JNode>)Value;  } }        
        public int Length
        {
            get { return ArrayValue.Count; }
            set { SetLength(value); }
        }

        public IEnumerable<JNode> Items { get { return ArrayValue; } }
        
        public void Add(JNode v)
        {
            Verify(!v.IsUndefined, "Type is undefined");
            ArrayValue.Add(v);
        }

        public bool RemoveItem(JNode v)
        {
            return ArrayValue.Remove(v);
        }

        public void RemoveAt(int index)
        {
            ArrayValue.RemoveAt(index);
        }

        public void SetLength(int length)
        {
            var arr = ArrayValue;
            if (arr.Count > length)
            {
                arr.RemoveRange(length, arr.Count - length);
                return;
            }

            for (var i = arr.Count; i < length; i++)
            {
                arr.Add(Null);
            }
        }

        public JNode this[int index]
        {
            get
            {
                return ArrayValue[index];
            }
            set
            {
                if (value.IsUndefined)
                    ArrayValue.RemoveAt(index);
                else
                    ArrayValue[index] = value;
            }
        }
        #endregion

        #region object
        internal ConcurrentDictionary<string, JNode> DictValue { get { Verify(Jt_Object, "Not Object"); return (ConcurrentDictionary<string, JNode>)Value; } }
        public int Count { get { return DictValue.Count; } }
        public IEnumerable<Field> Fields
        {
            get
            {
                Verify(Jt_Object, "Not Object");
                foreach (var p in DictValue)
                    yield return new Field(p.Key, p.Value);
            }
        }

        public bool TryAdd(string key, JNode v)
        {
            Verify(!v.IsUndefined, "Type is undefined");
            return DictValue.TryAdd(key, v);
        }

        public bool TryGet(string key, out JNode v)
        {
            return DictValue.TryGetValue(key, out v);
        }

        public bool TryRemove(string key, out JNode v)
        {
            return DictValue.TryRemove(key, out v);                        
        }

        public bool Remove(string key)
        {
            JNode v;
            return DictValue.TryRemove(key, out v);
        }

        public JNode this[string key]
        {
            get
            {
                JNode v;
                if (DictValue.TryGetValue(key, out v))
                    return v;
                return Undefined;
            }
            set
            {
                if (value.IsUndefined)
                    DictValue.TryRemove(key, out value);
                else
                    DictValue[key] = value;
            }
        }
        #endregion

        #region operator
        public static implicit operator JNode(bool v) => new JNode(v);
        public static implicit operator JNode(byte v) => new JNode(v);
        public static implicit operator JNode(sbyte v) => new JNode(v);
        public static implicit operator JNode(char v) => new JNode(v);
        public static implicit operator JNode(short v) => new JNode(v);
        public static implicit operator JNode(ushort v) => new JNode(v);
        public static implicit operator JNode(int v) => new JNode(v);
        public static implicit operator JNode(uint v) => new JNode(v);
        public static implicit operator JNode(long v) => new JNode(v);
        public static implicit operator JNode(ulong v) => new JNode(v);
        public static implicit operator JNode(float v) => new JNode(v);
        public static implicit operator JNode(double v) => new JNode(v);
        public static implicit operator JNode(string v) => new JNode(v);
        public static implicit operator JNode(Array arr) => NewArray(arr);
        public static implicit operator JNode(List<object> arr) => NewArray(arr);
        public static implicit operator JNode(Dictionary<string, object> ovs) => NewObject(ovs);
        public static implicit operator JNode(ConcurrentDictionary<string, object> ovs) => NewObject(ovs);

        public static implicit operator bool(JNode n) => n.BoolValue;
        public static implicit operator byte(JNode n) => (byte)n.IntValue;
        public static implicit operator sbyte(JNode n) => (sbyte)n.IntValue;
        public static implicit operator char(JNode n) => (char)n.IntValue;
        public static implicit operator short(JNode n) => (short)n.IntValue;
        public static implicit operator ushort(JNode n) => (ushort)n.IntValue;
        public static implicit operator int(JNode n) => (int)n.GetInteger();
        public static implicit operator uint(JNode n) => (uint)n.GetInteger();
        public static implicit operator long(JNode n) => (long)n.GetInteger();
        public static implicit operator ulong(JNode n) => (ulong)n.GetInteger();
        public static implicit operator float(JNode n) => (float)n.GetFloat();
        public static implicit operator double(JNode n) => (double)n.GetFloat();
        public static implicit operator string(JNode n) => n.TextValue;
        #endregion

        #region overrides ==/!=
        public static bool operator ==(JNode a, JNode b)
        {
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            return AreEquals(a, b);
        }

        public static bool operator !=(JNode a, JNode b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is JNode))
                return false;

            var other = (JNode)obj;
            return AreEquals(this, other);
        }        

        public override int GetHashCode()
        {
            return Tuple.Create(Type, Value).GetHashCode();
        }

        public override string ToString()
        {
            switch (Type)
            {
                case Jt_Array: return string.Format("Array[{0}]", Length);
                case Jt_Object: return string.Format("Object[{0}]", Count);
                case Jt_Integer: return string.Format("Integer({0})", Value);
                case Jt_Float: return string.Format("Float({0})", Value);
                case Jt_String: return string.Format("String({0})", Value);
                case Jt_Boolean: return string.Format("Boolean({0})", Value);
                case Jt_Null: return "null";
                case Jt_Undefined: return "undefined";
                default: throw new TypeException("Type"+Type+" Not supported");
            }
        }
        #endregion

        #region utils
        void Verify(int type, string format, params object[] args)
        {
            if ((type & Type) == 0)
            {
                var msg = format == null ? null : string.Format(format, args);
                throw new TypeException(msg);
            }
        }

        static void Verify(bool conditions, string format, params object[] args)
        {
            if (!conditions)
            {
                var msg = format == null ? null : string.Format(format, args);
                throw new TypeException(msg);
            }
        }

        double GetFloat()
        {
            Verify(Jt_Number, "Not Number");
            return IsFloat ? (double)FloatValue : (double)IntValue;
        }

        long GetInteger()
        {
            Verify(Jt_Number, "Not Number");
            return IsFloat ? (long)FloatValue : (long)IntValue;
        }

        static bool AreEquals(JNode a, JNode b)
        {
            if (a.Type != b.Type)
            {
                if (a.IsNumber)
                {
                    return IsNumberEquals(a, b);
                }

                if (a.IsString)
                {
                    return IsStringEquals(a, b);
                }

                if (a.IsNull)
                {
                    return IsNullEquals(a, b);
                }

                return false;
            }

            return IsValueMatched(b.Type, a.Value, b.Value);
        }

        static bool IsNumberEquals(JNode no, JNode b)
        {
            if (b.IsFloat)
                return b.FloatValue == no.IntValue;

            if (b.IsInteger)
                return b.IntValue == no.FloatValue;

            return false;
        }

        static bool IsStringEquals(JNode a, JNode b)
        {
            if (b.IsNull)
                return a.Value == null;

            return false;
        }

        static bool IsNullEquals(JNode a, JNode b)
        {
            if (b.IsString)
                return b.Value == null;

            return false;
        }

        static bool IsValueMatched(int t, object a, object b)
        {
            switch (t)
            {
                case Jt_Array: return (List<JNode>)a == (List<JNode>)b;
                case Jt_Object: return a == b;
                case Jt_Integer: return (long)a == (long)b;
                case Jt_Float: return (double)a == (double)b;
                case Jt_String: return (string)a == (string)b;
                case Jt_Boolean: return (bool)a == (bool)b;
                case Jt_Null: return true;
                case Jt_Undefined: return true;
                default: return false;
            }
        }
        #endregion
    }
}
