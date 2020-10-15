using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Nano.Collection;

namespace Nano.Logs
{
    public class MiniLogBuffer : RingBuffer<MiniLogBuffer.Item>
    {
        public class Item
        {
            public long Ticks;  // Ticks of DateTime.UtcNow
            public string Line;
        }

        public MiniLogBuffer(int capacity) : base(capacity) { }

        public void Add(long ticks, string line)
        {
            var item = new Item { Ticks = ticks, Line = line };
            Add(item);
        }
    }

    public class MiniLog
    {
        public MiniLogBuffer Buffer { get; private set; }

        bool m_console;
        TextWriter m_tw;

        public MiniLog(bool console = true, string path = null)
        {
            Buffer = new MiniLogBuffer(1000);
            m_console = console;

            if (path != null)
            {
                var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                fs.Seek(0, SeekOrigin.End);
                m_tw = new StreamWriter(fs, Encoding.UTF8);
            }
        }

        public void WriteLine(string s)
        {
            var dt = DateTime.UtcNow;
            lock (Buffer)
                Buffer.Add(dt.Ticks, s);

            if (m_console || m_tw != null)
            {
                var ds = dt.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss ") + s;
                if (m_tw != null)
                {
                    lock (m_tw)
                        m_tw.WriteLine(ds);
                }                
                if (m_console)
                    Console.WriteLine(ds);
            }
        }

        public void Flush() => m_tw?.Flush();

        public void Close()
        {
            if (m_tw != null)
            {
                m_tw.Flush();
                m_tw.Close();
                m_tw = null;
            }
        }
    }
}
