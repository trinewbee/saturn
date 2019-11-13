using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.IO;

namespace Nano.Json
{
       

    public class JParser
    {
        #region declare
        public interface IReader
        {
            bool IsValid { get; }

            int Index { get; }

            char Current { get; }

            bool Is(char ch);

            bool IsLowerTo(char ch);

            bool Next();
        }

        IReader Reader { get; set; }        

        static Dictionary<char, char> Escapee = new Dictionary<char, char>
        {
            { '\"', '"'},
            { '\\', '\\'},
            { '/', '/'},
            { 'b', '\b'},
            { 'f', '\f'},
            { 'n', '\n'},
            { 'r', '\r'},
            { 't', '\t'}
        };

        #endregion

        public JNode Parse(string txt)
        {
            var r = new JsonTextReader(txt);
            return Parse(r);
        }

        public JNode Parse(IReader reader)
        {
            Reader = reader;

            var ret = ParseValue();

            ParseWhite();
            Verify(!reader.IsValid, "Syntax error");

            return ret;
        }

        JNode ParseNumber()
        {
            bool isFloat = false;
            var sb = new StringBuilder();
            if (Reader.Is('-') || Reader.Is('+'))
            {
                sb.Append(Reader.Current);
                Next(Reader.Current);
            }

            while (Reader.Current >= '0' && Reader.Current <= '9')
            {
                sb.Append(Reader.Current);

                if (!Next())
                    break;
            }

            if (Reader.Is('.'))
            {
                isFloat = true;
                sb.Append(Reader.Current);
                while (Next() && Reader.Current >= '0' && Reader.Current <= '9')
                {
                    sb.Append(Reader.Current);
                }
            }

            if (Reader.IsLowerTo('e'))
            {
                isFloat = true;
                sb.Append(Reader.Current); Next();

                if (Reader.Is('+') || Reader.Is('-'))
                {
                    sb.Append(Reader.Current); Next();
                }

                while (Reader.Current >= '0' && Reader.Current <= '9')
                {
                    sb.Append(Reader.Current); Next();
                }
            }

            if (isFloat)
            {
                double v;
                var ret = double.TryParse(sb.ToString(), out v);
                Verify(ret, "Bad Number");
                return new JNode(JNode.Jt_Float, v);
            }
            else
            {
                long v;
                var ret = long.TryParse(sb.ToString(), out v);
                Verify(ret, "Bad Number");
                return new JNode(JNode.Jt_Integer, v);
            }
        }

        JNode ParseString()
        {
            uint uffff = 0;
            uint hex = 0;
            char c;
            var sb = new StringBuilder();
            if (Reader.Is('\"'))
            {
                while (Next())
                {
                    if (Reader.Current == '\"')
                    {
                        Next();
                        return new JNode(JNode.Jt_String, sb.ToString());
                    }

                    if (Reader.Current == '\\')
                    {
                        Next();
                        if (Reader.Current == 'u')
                        {
                            uffff = 0;
                            for (var i = 0; i < 4; i += 1)
                            {
                                Next();
                                if(!uint.TryParse(Reader.Current + "", System.Globalization.NumberStyles.AllowHexSpecifier, null, out hex))
                                {
                                    break;
                                }
                                uffff = uffff * 16 + hex;
                            }

                            sb.Append(Convert.ToChar(uffff));
                        }
                        else if (Escapee.TryGetValue(Reader.Current, out c))
                        {
                            sb.Append(c);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        sb.Append(Reader.Current);
                    }
                }
            }
            
            throw Error("Bad String");
        }

        void ParseWhite()
        {
            while (Reader.IsValid && Reader.Current <= ' ')
            {
                Next();
            }
        }

        JNode ParseWord()
        {
            var lc = char.ToLowerInvariant(Reader.Current);
            switch (lc)
            {
                case 't':
                    NextLowerTo('t');
                    NextLowerTo('r');
                    NextLowerTo('u');
                    NextLowerTo('e');
                    return JNode.True;
                case 'f':
                    NextLowerTo('f');
                    NextLowerTo('a');
                    NextLowerTo('l');
                    NextLowerTo('s');
                    NextLowerTo('e');
                    return JNode.False;
                case 'n':
                    NextLowerTo('n');
                    NextLowerTo('u');
                    NextLowerTo('l');
                    NextLowerTo('l');
                    return JNode.Null;
            }

            throw Error("Unexpected '{0}'", Reader.Current);
        }

        JNode ParseObject()
        {
            var obj = new ConcurrentDictionary<string, JNode>();

            if (Reader.Is('{'))
            {
                Next('{');
                ParseWhite();
                if (Reader.Is('}'))
                {
                    Next('}');
                    return new JNode(JNode.Jt_Object, obj);   // empty object
                }
                while (Reader.IsValid)
                {
                    var key = ParseString();
                    ParseWhite();
                    Next(':');
                    Verify(!obj.ContainsKey(key.TextValue), "Duplicate key {0}", key);

                    var v = ParseValue();
                    var r = obj.TryAdd(key.TextValue, v);
                    Verify(r, "Duplicate key {0}", key);

                    ParseWhite();
                    if (Reader.Is('}'))
                    {
                        Next('}');
                        return new JNode(JNode.Jt_Object, obj);
                    }

                    Next(',');
                    ParseWhite();
                }
            }

            throw Error("Bad object");
        }

        JNode ParseArray()
        {
            var arr = new List<JNode>();

            if (Reader.Is('['))
            {
                Next('[');
                ParseWhite();

                if (Reader.Is(']'))
                {
                    Next(']');
                    return new JNode(JNode.Jt_Array, arr);   // empty array
                }

                while (Reader.IsValid)
                {
                    arr.Add(ParseValue());
                    ParseWhite();

                    if (Reader.Is(']'))
                    {
                        Next(']');
                        return new JNode(JNode.Jt_Array, arr);
                    }

                    Next(',');
                    ParseWhite();
                }
            }

            throw Error("Bad array");
        }

        JNode ParseValue()
        {
            ParseWhite();

            switch (Reader.Current)
            {
                case '{':return ParseObject();
                case '[':return ParseArray();
                case '\"':return ParseString();
                case '-':return ParseNumber();
                case '+': return ParseNumber();
                default:
                    return Reader.Current >= '0' && Reader.Current <= '9'
                        ? ParseNumber() 
                        : ParseWord();
            }
        }

        bool Next(char ch)
        {
            Verify(Reader.Is(ch), "Expected {0} instead of {1}", ch, Reader.Current);
            return Reader.Next();
        }

        bool NextLowerTo(char ch)
        {
            Verify(Reader.IsLowerTo(ch), "Expected {0} instead of {1}", ch, Reader.Current);
            return Reader.Next();
        }

        bool Next()
        {
            return Reader.Next();
        }

        void Verify(bool conditions, string format, params object[] args)
        {
            if (!conditions)
            {
                var msg = format == null ? null : string.Format(format, args);
                throw new SyntaxException(msg);
            }
        }

        Exception Error(string format, params object[] args)
        {
            var msg = format == null ? null : string.Format(format, args);
            throw new SyntaxException(msg);
        }
    }

    public class JsonTextReader : JParser.IReader
    {
        string Text;
        public JsonTextReader(string txt)
        {
            Text = txt;
            Index = 0;
            Current = (char)0;
            IsValid = Text != null && Text.Length > Index;
        }

        public int Index { get; protected set; }
        public char Current { get; protected set; }
        public bool IsValid { get; protected set; }

        public bool Is(char ch)
        {
            return IsValid && Current == ch;
        }

        public bool IsLowerTo(char ch)
        {
            return IsValid && char.ToLowerInvariant(Current) == ch;
        }

        public bool Next()
        {
            if (Text != null && Text.Length > Index)
            {
                Current = Text[Index++];
                IsValid = true;
                return true;
            }

			Current = (char)0;
            IsValid = false;
            return false;
        }
    }
}
