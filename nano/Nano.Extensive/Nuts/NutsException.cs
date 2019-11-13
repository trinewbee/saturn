using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Nuts
{
	public class NutsException : Exception
	{
		public const string OK = "ok";
		public const string Fail = "Fail";
		public const string NotFound = "NotFound";
		public const string InvalidArg = "InvalidArg";
		public const string KeyNotFound = "KeyNotFound";
		public const string OutOfRange = "OutOfRange";
		public const string AccessDenied = "AccessDenied";
		public const string TooManyResults = "TooManyResults";
		public const string RuleStricted = "RuleStricted";
		public const string Unexpected = "Unexpected";
		public const string Inconsistency = "Inconsistency";
		public const string NotSupported = "NotSupported";
		public const string AlreadyExists = "AlreadyExists";
		public const string AlreadyOpen = "AlreadyOpen";

		public const string FileNotFound = "FileNotFound";
		public const string DirNotFound = "DirNotFound";

		public const string InvalidReferenceFlag = "InvalidReferenceFlag";

		public string Code;
		public NutsException(string code, string message = "") : base(message)
		{
			Code = code;
		}

	}

	public static class G
	{
		public static void Error(string code)
		{
			throw new NutsException(code);
		}

		public static void Verify(bool f, string code)
		{
			if (!f)
				throw new NutsException(code);
		}
	}
}
