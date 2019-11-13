using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace Nano.Logs
{
	public static class Logger
    {
        public static void Debug(string name, string msg, Exception t)
        {
            try
            {
                if (CheckLevel("debug"))
                {
                    debugLog.Write(name, msg, t);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\t" + e.StackTrace);
            }
        }

        public static void Acc(string name, string msg, Exception t)
        {
            try
            {
                accLog.Write(name, msg, t);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\t" + e.StackTrace);
            }
        }

        public static void Info(string name, string msg, Exception t)
        {
            try
            {
                if (CheckLevel("info"))
                {
                    infoLog.Write(name, msg, t);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\t" + e.StackTrace);
            }
        }

        public static void Warn(string name, string msg, Exception t)
        {
            try
            {
                if (CheckLevel("warn"))
                {
                    warnLog.Write(name, msg, t);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\t" + e.StackTrace);
            }
        }

        public static void Err(string name, string msg, Exception t)
        {
            try
            {
                if (CheckLevel("error"))
                {
                    errLog.Write(name, msg, t);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\t" + e.StackTrace);
            }
        }

        public static void Crit(string name, string msg, Exception t)
        {
            try
            {
                if (CheckLevel("crit"))
                {
                    critLog.Write(name, msg, t);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\t" + e.StackTrace);
            }
        }

        public static void Debug(string name, string msg)
        {
            try
            {
                debugLog.Write(name, msg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\t" + e.StackTrace);
            }
        }

        public static void Acc(string name, string msg)
        {
            try
            {
                accLog.Write(name, msg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\t" + e.StackTrace);
            }
        }

        public static void Info(string name, string msg)
        {
            try
            {
                infoLog.Write(name, msg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\t" + e.StackTrace);
            }
        }

        public static void Warn(string name, string msg)
        {
            try
            {
                warnLog.Write(name, msg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\t" + e.StackTrace);
            }
        }

        public static void Err(string name, string msg)
        {
            try
            {
                errLog.Write(name, msg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\t" + e.StackTrace);
            }
        }

        public static void Crit(string name, string msg)
        {
            try
            {
                critLog.Write(name, msg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\t" + e.StackTrace);
            }
        }

        /*--------------------------------------------------------------------------*/

        private static FileLogger debugLog, accLog, infoLog, warnLog, errLog, critLog;

        private static int logLevel = 0;

        public static string[] levels = { "debug", "info", "warn", "error", "crit" };

        // 日志文件名称
        public static readonly string debugLogFileName = "/debug.log";

        public static readonly string accLogFileName = "/acc.log";

        public static readonly string infoLogFileName = "/info.log";

        public static readonly string warnLogFileName = "/warn.log";

        public static readonly string errLogFileName = "/err.log";

        public static readonly string critLogFileName = "/crit.log";

        public static void SetVerbose(bool b)
        {
            FileLogger.verbose = b;
        }


        public static void ChecLogIsInit()
        {
            if (accLog == null)
            {
                throw new Exception("log not init");
            }
        }

        public static void Init(string logPath, string level, bool rotated, long? maxFileSize)
        {
            if (level != null)
                level = level.ToLower();

            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i].Equals(level))
                {
                    logLevel = i;
                    break;
                }
            }

            //File file = new File(logPath);
            if (System.IO.File.Exists(logPath) == false)
                Directory.CreateDirectory(logPath);
            logPath = Path.GetFullPath(logPath);

            long maxSize = -1;
            if (maxFileSize != null)
                maxSize = (long)maxFileSize;

            debugLog = new FileLogger(logPath + debugLogFileName, rotated, maxSize);
            accLog = new FileLogger(logPath + accLogFileName, rotated, maxSize);
            infoLog = new FileLogger(logPath + infoLogFileName, rotated, maxSize);
            warnLog = new FileLogger(logPath + warnLogFileName, rotated, maxSize);
            errLog = new FileLogger(logPath + errLogFileName, rotated, maxSize);
            critLog = new FileLogger(logPath + critLogFileName, rotated, maxSize);

        }

        private static bool CheckLevel(string level)
        {
            int l = 0;
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i].Equals(level))
                {
                    l = i;
                    break;
                }
            }
            if (l > logLevel)
            {
                return true;
            }

            return false;
        }
    }
}
