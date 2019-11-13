using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Nano.Ext.Marshal
{
	public static class UnixTimestamp
	{
		static long m_baseTicks;

		static UnixTimestamp()
		{
			// unix timestamp since 1970/1/1
			DateTime dtBase = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			m_baseTicks = dtBase.Ticks;
		}

        public static long TimestampToTicks(long value) => value * 10000000 + m_baseTicks;

        public static long TicksToTimestamp(long ticks) => (ticks - m_baseTicks) / 10000000;

        public static long TimeValueToTicks(long value) => value * 10000 + m_baseTicks;

        public static long TicksToTimeValue(long ticks) => (ticks - m_baseTicks) / 10000;

        // timestamp in seconds
        public static DateTime FromTimestamp(long value)
		{
			value = value * 10000000 + m_baseTicks;
			DateTime dt = new DateTime(value, DateTimeKind.Utc);
			return dt;
		}

		// timestamp in seconds
		public static long ToTimestamp(DateTime dt)
		{
			Debug.Assert(dt.Kind == DateTimeKind.Utc);
            return TicksToTimestamp(dt.Ticks);
		}

        // timevalue in milli-seconds
        public static DateTime FromTimeValue(long value)
		{
			value = value * 10000 + m_baseTicks;
			DateTime dt = new DateTime(value, DateTimeKind.Utc);
			return dt;
		}

		// timevalue in milli-seconds
		public static long ToTimeValue(DateTime dt)
		{
			Debug.Assert(dt.Kind == DateTimeKind.Utc);
            return TicksToTimeValue(dt.Ticks);
		}

        // timestamp in seconds
        public static long GetUtcNowTimestamp() => ToTimestamp(DateTime.UtcNow);

        // timevalue in milli-seconds
        public static long GetUtcNowTimeValue() => ToTimeValue(DateTime.UtcNow);
	}
}
