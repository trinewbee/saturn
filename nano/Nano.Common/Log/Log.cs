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

        TextWriter m_tw1, m_tw2;

        public MiniLog(TextWriter tw1, TextWriter tw2)
        {
            Buffer = new MiniLogBuffer(1000);
            m_tw1 = tw1;
            m_tw2 = tw2;
        }

        public MiniLog(bool console, TextWriter tw)
        {
            Buffer = new MiniLogBuffer(1000);
            m_tw1 = console ? Console.Out : null;
            m_tw2 = tw;
        }

        public MiniLog(bool console = true, string path = null) : this(console, CreateWriter(path))
        {
        }

        static TextWriter CreateWriter(string path)
        {
            if (path == null)
                return null;
            
            var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            fs.Seek(0, SeekOrigin.End);
            return new StreamWriter(fs, Encoding.UTF8);
        }

        public void WriteLine(string s)
        {
            var dt = DateTime.UtcNow;
            lock (Buffer)
                Buffer.Add(dt.Ticks, s);

            if (m_tw1 != null || m_tw2 != null)
            {
                var ds = dt.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss ") + s;
                LockWriteLine(m_tw1, ds);
                LockWriteLine(m_tw2, ds);
            }
        }

        static void LockWriteLine(TextWriter tw, string s)
        {
            if (tw != null)
            {
                lock (tw)
                    tw.WriteLine(s);
            }
        }

        public void Flush()
        {
            m_tw1?.Flush();
            m_tw2?.Flush();
        }

        public void Close()
        {
            CloseWriter(ref m_tw1);
            CloseWriter(ref m_tw2);
        }

        static void CloseWriter(ref TextWriter tw)
        {
            if (tw != null && tw != Console.Out)
            {
                tw.Flush();
                tw.Close();
                tw = null;
            }
        }
    }
}
