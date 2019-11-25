using Nano.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Extensive.RedLock
{
    public interface IRedLock : IDisposable 
    {
        
    }

    class RedLock : IRedLock
    {
        public TimeSpan expiredTime; // 默认10s
        public TimeSpan waitTime; // 默认 
        public TimeSpan retryTime; // 默认重试时间 1s
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IRedLock CreateLock(string resource, )
        {

        }
    }

}
