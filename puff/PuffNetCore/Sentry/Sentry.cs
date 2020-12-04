﻿using Sentry;
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
            hepler = new SentryHepler(hostName, env);
        }

        public static void Notify(SentryLevel level, Exception ex, string subject = null, object extra = null)
        {
            if (hepler != null)
                hepler.Notify(level, ex, subject, extra);
        }

    }

    public class SentryHepler
    {
        private string hostName;
        private string env;

        public SentryHepler(string _hostName, string _env)
        {
            hostName = _hostName;
            env = _env;
        }

        public void Notify(SentryLevel level, Exception ex, string subject = null, object extra = null)
        {
            SentrySdk.WithScope(scope =>
            {
                scope.SetTag("hostname", hostName);
                scope.Environment = env;
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