using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Nano.Ext.Error;

namespace Nano.Ext.Logs
{
    public interface ILogFilter
    {
        bool IsActionEnabled(string action);
    }

    public interface ILogger
    {
        ILogFilter Filter { get; set; }

        void Report(string action, params object[] args);

        void Log(string type, string format, params object[] args);

        void Log(string type, Exception ex, string format, params object[] args);

        void Action(string type, string action, string format, params object[] args);

        void Action(string type, string action, Exception ex, string format, params object[] args);

        bool IsEnabled(string action);
    }

    public class Log
    {
        private static ILogger m_logger;

        public static ILogger Default { get; }

        public static ILogger Logger { get { return m_logger??Default; } }

        static Log()
        {
            Default = new ConsoleLogger();
        }

        public static ILogger SetLogger(ILogger logger)
        {
            var old = m_logger;
            m_logger = logger;
            return old;
        }

        public static void r(string action, params object[] args)
        {
            Logger.Report(action, args);
        }

        #region custom
        /// <summary>
        /// custom log format
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void c(string type, string format, params object[] args)
        {
            Logger.Log(type, format, args);
        }

        /// <summary>
        /// custom log format
        /// </summary>
        /// <param name="type"></param>
        /// <param name="ex"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void c(string type, Exception ex, string format, params object[] args)
        {
            Logger.Log(type, ex, format, args);
        }

        /// <summary>
        /// custom log format
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void ac(string type, string action, string format, params object[] args)
        {
            Logger.Action(type, action, format, args);
        }

        /// <summary>
        /// custom log format
        /// </summary>
        /// <param name="type"></param>
        /// <param name="ex"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void ac(string type, string action, Exception ex, string format, params object[] args)
        {
            Logger.Action(type, action, ex, format, args);
        }

        #endregion

        #region debug

        public static void d(string format, params object[] args)
        {
            Logger.Log("debug", format, args);
        }

        public static void d(Exception ex, string format, params object[] args)
        {
            Logger.Log("debug", ex, format, args);
        }

        public static void ad(string action, string format, params object[] args)
        {
            Logger.Action("debug", action, format, args);
        }

        public static void ad(string action, Exception ex, string format, params object[] args)
        {
            Logger.Action("debug", action, ex, format, args);
        }
        #endregion

        #region info
        public static void i(string format, params object[] args)
        {
            Logger.Log("info", format, args);
        }

        public static void i(Exception ex, string format, params object[] args)
        {
            Logger.Log("info", ex, format, args);
        }

        public static void ai(string action, string format, params object[] args)
        {
            Logger.Action("info", action, format, args);
        }

        public static void ai(string action, Exception ex, string format, params object[] args)
        {
            Logger.Action("info", action, ex, format, args);
        }

        #endregion

        #region warn
        public static void w(string format, params object[] args)
        {
            Logger.Log("warn", format, args);
        }

        public static void w(Exception ex, string format, params object[] args)
        {
            Logger.Log("warn", ex, format, args);
        }
        public static void aw(string action, string format, params object[] args)
        {
            Logger.Action("warn", action, format, args);
        }

        public static void aw(string action, Exception ex, string format, params object[] args)
        {
            Logger.Action("warn", action, ex, format, args);
        }
        #endregion

        #region error
        public static void e(string format, params object[] args)
        {
            Logger.Log("error", format, args);
        }

        public static void e(Exception ex, string format, params object[] args)
        {
            Logger.Log("error", ex, format, args);
        }

        public static void ae(string action, string format, params object[] args)
        {
            Logger.Action("error", action, format, args);
        }

        public static void ae(string action, Exception ex, string format, params object[] args)
        {
            Logger.Action("error", action, ex, format, args);
        }

        #endregion
    }

    public abstract class BaseLogger : ILogger
    {
        public ILogFilter Filter { get; set; }

        public void Report(string action, params object[] args)
        {
            if (!IsEnabled(action)) return;

            Debug.Assert(args.Length <= 4);
            var format = "";
            for (var i = 0; i < args.Length; i++)
            {
                format += "{" + (i + 1) + "}";
            }
            Print("report", action, string.Format(format, args));
        }

        public void Log(string type, string format, params object[] args)
        {
            Print(type, null, format, args);
        }

        public void Log(string type, Exception ex, string format, params object[] args)
        {
            if (ex == null)
            {
                Print(type, null, format, args);
                return;
            }
            string msg = string.Format(format, args);
            string err = PrintExceptionStack(ex);
            Print(type, null, "{0}\r\n{1}", msg, err);
        }

        public void Action(string type, string action, string format, params object[] args)
        {
            Print(type, action, format, args);
        }

        public void Action(string type, string action, Exception ex, string format, params object[] args)
        {
            if (ex == null)
            {
                Print(type, action, format, args);
                return;
            }
            
            string msg = string.Format(format, args);
            string err = PrintExceptionStack(ex);
            Print(type, action, "{0}\r\n{1}", msg, err);
        }

        protected void Print(string type, string action, string format, params object[] args)
        {
            if (action == null)
            {
                format = string.Format(format, args);
                Output(type, format);
            }
            else if (IsEnabled(action))
            {
                if (null != format)
                {
                    format = string.Format(format, args);
                    format = string.Format("[{0}] : {1}", action, format);
                }
                Output(type, format);
            }
        }

        public bool IsEnabled(string action)
        {
            return Filter == null ? true : Filter.IsActionEnabled(action);
        }

        protected abstract void Output(string type, string message);

        string PrintExceptionStack(Exception ex)
        {
            var sb = new StringBuilder();
            var _ex = ex;
            sb.Append(ex.Message).Append("\r\n");
            while (_ex != null)
            {
                var xe = _ex as XError;
                var stat = xe == null ? "InternalError" : xe.Stat;
                sb.Append(_ex.GetType().FullName);
                sb.Append("(").Append(stat).Append("):").Append(_ex.Message).Append("\r\n");
                sb.Append(_ex.StackTrace.ToString());
                _ex = _ex.InnerException;
                if (_ex == null)
                    break;

                sb.Append(string.Format("\r\nAt "));
            }

            return sb.ToString();
        }
    }

    public class ConsoleLogger : BaseLogger
    {
        protected override void Output(string type, string message)
        {
            var text = string.Format(@"{0} :: {1:yy-MM-dd HH:mm:ss.ffff} {2}", type, DateTime.Now, message);
            Console.WriteLine(text);
        }
    }
}
