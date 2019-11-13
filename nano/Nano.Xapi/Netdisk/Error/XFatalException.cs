using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Ext.Error;

namespace Nano.Xapi.Netdisk.Error
{
    public class XFatalException : XError
    {
        static Dictionary<string, bool> Errors;

        static XFatalException()
        {
            Errors = new Dictionary<string, bool>();
            Errors[XStat.ERR_TOKEN_EXPIRED] = true;
            Errors[XStat.ERR_TOKEN_NOT_FOUND] = true;
            Errors[XStat.ERR_TOKEN_UID_MISMATCHING] = true;
            Errors[XStat.ERR_TOKEN_UID_NOT_FOUND] = true;
        }

        public XFatalException(string stat, string message = null, Exception innerException = null) : base(stat, message, innerException)
        {

        }

        public static void Verify(string stat, string message = null, Exception e = null)
        {
            if (IsFatal(stat))
            {
                throw new XFatalException(stat, message);
            }
        }

        public static bool IsFatal(string stat)
        {
            return Errors.ContainsKey(stat);
        }

        public static void SetFatal(string stat)
        {
            Errors[stat] = true;
        }
    }
}
