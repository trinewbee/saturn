using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;


namespace Nano.Logs
{
	public class FileLogger
    {
         /// <summary>
         /// 日志文件路径
         /// </summary>
        private String path = "";

        /// <summary>
        /// 日志文件最大大小
        /// </summary>
        private long maxSize;

        /// <summary>
        /// 距离上次检测文件的大小到目前写了多少条日志
        /// </summary>
        private int checkCount;

        /// <summary>
        /// 按天切分日志
        /// </summary>
        private bool rotated = false;

        /// <summary>
        /// 当前写日志的日期
        /// </summary>
        private String writtingDate;

        /// <summary>
        /// 进程ID
        /// </summary>
        private String pid = "";

        /// <summary>
        /// 文件写入流
        /// </summary>
        private StreamWriter bf = null;
             
        /// <summary>
        /// 是否将日志打印到控制台
        /// </summary>
        public static bool verbose = false;
        
        public FileLogger(string path):this(path, false, -1)
        {
        }
        
        public FileLogger(string path, bool rotated, long maxSize)
        {
            this.path = path;
            this.rotated = rotated;
            this.maxSize = maxSize;

            pid = Process.GetCurrentProcess().Id.ToString();
        }
        
        private bool _checkFile()
        {
            if (path == null && path.Trim().Length == 0)
                return false;
            bool isEmptyFile = false;
            System.IO.FileInfo file = new System.IO.FileInfo(path);
            if (file.Exists == false)
                isEmptyFile = true;
            if (bf == null)
                bf = new StreamWriter(path, true);

            if (rotated)
            {
                string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
                if (isEmptyFile == false && writtingDate == null)
                {
                    writtingDate = file.LastWriteTime.ToString("yyyy-MM-dd");
                }

                if (isEmptyFile == false && currentDate.ToLower() != writtingDate.ToLower())
                {
                    bf.Close();
                    string temp = path + "." + writtingDate;
                    file.MoveTo(temp);
                    bf = new StreamWriter(path, true);
                }
                writtingDate = currentDate;
            }
            else if (maxSize > 0)
            {
                checkCount = ++checkCount % 100;
                if (checkCount == 0)
                {
                    if (file.Length > maxSize)
                    {
                        bf.Close();
                        string temp = path + "." + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
                        file.MoveTo(temp);
                        bf = new StreamWriter(path, true);
                    }
                }
            }
            return true;
        }

        private readonly object lockObject = new object();
        public void Write(string name, string msg)
        {
            lock (lockObject)
            {
                if (_checkFile() == false)
                    return;
                string logline = string.Format("{0} {1} {2} {3}\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), pid, name, msg);

                if (verbose == true)
                    Console.WriteLine(logline);
                bf.WriteLine(logline);
                bf.Flush();
            }
            
        }

        public void Write(string name, string msg, Exception e)
        {
            string exString = e.StackTrace;
            msg = string.Format("{0}, Exception {1}", msg, exString);
            Write(name, msg);
        }
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              
    }
}

