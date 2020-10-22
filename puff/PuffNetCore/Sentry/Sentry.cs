using Sentry;
using Sentry.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Puff.Ext.Sentry
{

    public static class SentryUtil
    {
        private static SentryHepler hepler = null;
        public static void Init(string dsn, string hostName, string env)
        {
            SentrySdk.Init(dsn);
            SentrySdk.ConfigureScope(scope =>
            {
                scope.SetTag("hostname", hostName);
                scope.Environment = env;
            });
            hepler = new SentryHepler();
        }

        public static void Notify(SentryLevel level, Exception ex, string subject = null, object extra = null)
        {
            if (hepler != null)
                hepler.Notify(level, ex, subject, extra);
        }

    }

    public class SentryHepler
    {
        
        public void Notify(SentryLevel level, Exception ex, string subject = null, object extra = null)
        {
            SentrySdk.WithScope(scope =>
            {
                scope.SetExtra("subject", subject);
                scope.Level = level;
                if (extra != null)
                {
                    scope.SetExtra("extra", extra);
                }
                SentrySdk.CaptureException(ex);
            });
        }
    }
}
