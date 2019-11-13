using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Nano.Common
{
    /// <summary>全局定时器</summary>
    public class Ticker
    {
        /// <summary>回调函数</summary>
        public Action Tick = null;

        ManualResetEvent Event = null;
        int m_elapse;
        Thread m_thread = null;
        volatile int m_stat = 0;

        /// <summary>初始化定时器</summary>
        /// <param name="elapse">间隔时间</param>
        public Ticker(int elapse)
        {
            m_elapse = elapse;
        }

        /// <summary>启动定时器</summary>
        public void Start()
        {
            Event = new ManualResetEvent(false);
            m_stat = 1;

            m_thread = new Thread(ThreadFunc);
            m_thread.Start();
        }

        void ThreadFunc()
        {
            while (m_stat != 0)
            {
                Event.WaitOne(m_elapse);
                Tick?.Invoke();
            }
        }

        /// <summary>关闭定时器</summary>
        public void Close()
        {
            Tick = null;
            m_stat = 0;
            Event.Set();
            m_thread.Join();
            m_thread = null;
        }
    }
}
