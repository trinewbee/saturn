using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Json
{
    public class JStat
    {
        public const string OK = "OK";

        public const string SyntaxError = "SyntaxError";

        public const string TypeError = "TypeError";
    }

    public class JException : Exception
    {
        public string Stat { get; }

        public JException(string stat, string message, Exception inner = null) : base(message, inner)
        {

        }
    }

    public class SyntaxException : JException
    {
        public SyntaxException(string message) : base(JStat.SyntaxError, message) { }
    }

    public class TypeException : JException
    {
        public TypeException(string message) : base(JStat.TypeError, message) { }
    }

}
