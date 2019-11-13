using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Ext.Error
{
    public class XError : Exception
    {

        public const string OK = "OK";

        public const string LERR_UNKOWN = "LERR_UNKOWN";

        public delegate void VerifyDelegate(string stat, string message, Exception inner);

        public string Stat { get; }

        public static List<VerifyDelegate> Verifiers { get; }

        static XError()
        {
            Verifiers = new List<VerifyDelegate>();
        }

        public XError(string stat, string message = "", Exception inner = null) : base(message, inner)
        {
            Stat = stat;
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", Stat, Message);
        }

        public static bool Succeed(string stat)
        {
            return stat == OK;
        }

        public static void TryThrow(string stat, string message, Exception inner)
        {
            foreach (var evr in Verifiers)
            {
                evr?.Invoke(stat, message, inner);
            }
        }

        public static void Throw(string stat, string message, Exception inner)
        {
            TryThrow(stat, message, inner);
            throw new XError(stat, message, inner);
        }
    }
}
