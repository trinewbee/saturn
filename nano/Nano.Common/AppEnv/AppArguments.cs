using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Nano.Common.AppEnv
{
	public abstract class AppInteract
	{
		public abstract string Read();
		public abstract void Write(string s);	// May be a WriteLine in a console, or an message dialog

		public void Write(string format, params object[] args)
		{
			string msg = string.Format(format, args);
			Write(msg);
		}
	}

	public class NullAppInteract : AppInteract
	{
		public override string Read()
		{
			throw new NotImplementedException();
		}

		public override void Write(string s)
		{
		}
	}

	public class ConsoleAppInteract : AppInteract
	{
		public override string Read()
		{
			return Console.ReadLine();
		}

		public override void Write(string s)
		{
			Console.WriteLine(s);
		}
	}

	/// <summary>Interface to accept the result of ArgumentParser</summary>
	public interface IArgumentAccept
	{
		bool AddSwitch(string key, string value);
		bool AddName(string name);
		bool Complete();
	}

	/// <summary>Parse command arguments to key-value switches and name list</summary>
	/// <remarks>
	/// e.g. /port=9090 /copy source target
	/// The key-value pairs: { port="9090", copy=null }
	/// The name list: [ "source", "target" ]
	/// 
	/// The key-value switches may be in the following form:
	/// -key, /key: Value would be null
	/// -key=value, /key=value, -key:value, /key:value
	/// -key="value", /key="value", -key:"value", /key:"value"
	/// </remarks>
	public class ArgumentParser
	{
		static char[] cc = new char[] { '=', ':' };

		AppInteract m_appi;

		/// <summary>扫描输入的命令行参数，并将解析结果发送到 IArgumentAccept 接口</summary>
		/// <param name="args">Main 函数的参数列表</param>
		/// <param name="accp">解析结果接收器</param>
		/// <param name="appi">（可选）接收错误信息</param>
		/// <returns>所有参数都成功处理时，返回 true。</returns>
		public static bool Parse(string[] args, IArgumentAccept accp, AppInteract appi = null)
		{
			appi = appi ?? new NullAppInteract();
			var o = new ArgumentParser(appi);
			return o.ParseImpl(args, accp);
		}

		ArgumentParser(AppInteract appi) { m_appi = appi; }

		bool ParseImpl(string[] args, IArgumentAccept accp)
		{
			bool fAccpAll = true;
			foreach (string arg in args)
			{
				char prefix = arg[0];
				bool fAccp;
				if (prefix == '/' || prefix == '-')
				{
					int pos = arg.IndexOfAny(cc);
					if (pos >= 0)
						fAccp = accp.AddSwitch(arg.Substring(1, pos - 1), arg.Substring(pos + 1));
					else
						fAccp = accp.AddSwitch(arg.Substring(1), null);
				}
				else
					fAccp = accp.AddName(arg);

				if (!fAccp)
				{
					fAccpAll = false;
					m_appi.Write("Invalid argument: " + arg);
				}
			}

			if (!accp.Complete())
			{
				fAccpAll = false;
				m_appi.Write("Invaid argument found");
			}

			return fAccpAll;
		}
	}

	public class ProgressBoard
	{
		public class Step
		{
			public string Message;
			public int Index;
			public int Weight;
			public int Partial = 0;

			public Step(int i, string m, int w)
			{
				Index = i;
				Message = m;
				Weight = w;
			}
		}

		// step will be set only when step changed
		public delegate void OnChange(int prog, int total, Step step, int state);

		List<Step> m_steps = new List<Step>();
		OnChange m_onchange = null;
		int m_sum = 0;

		volatile string m_message = null;
		volatile int m_weight = 0, m_partial = 0;
		volatile int m_istep = -1;
		volatile int m_prog = 0, m_total = 0;		// 0 - 10000

		public int Count
		{
			get { return m_steps.Count; }
		}

		public int StepProgress
		{
			get { return m_prog; }
		}

		public int TotalProgress
		{
			get { return m_total; }
		}

		// 0 pending, 1 running, 2 completed
		public int State
		{
			get
			{
				if (m_istep < 0)
					return 0;
				else if (m_istep < m_steps.Count)
					return 1;
				else
					return 2;
			}			
		}

		// OnChange delegate might be null
		public void Reset(OnChange f)
		{
			m_steps.Clear();
			m_onchange = f;
			m_istep = -1;
			m_sum = m_partial = m_weight = 0;
			m_prog = m_total = 0;
		}

		public int AddStep(string message, int weight)
		{
			weight = weight > 0 ? weight : 1;	// avoid devided by zero
			m_steps.Add(new Step(m_steps.Count, message, weight));
			return m_steps.Count;
		}

		public void CompleteInit()
		{
			Debug.Assert(m_sum == 0);
			foreach (Step step in m_steps)
			{
				step.Partial = m_sum;
				m_sum += step.Weight;
			}
		}

		public void EnterStep(int index)
		{
			Step step = null;
			int prog, total;

			lock (this)
			{
				Debug.Assert(index > m_istep && index < m_steps.Count);
				step = m_steps[index];
				m_istep = index;
				m_message = step.Message;
				m_weight = step.Weight;
				m_partial = step.Partial;
				m_prog = prog = 0;
				m_total = total = m_partial * 10000 / m_sum;
			}

			if (m_onchange != null)
				m_onchange(prog, total, step, 1);
		}

		public void SetStepProg(int value)
		{
			int prog, total;
			lock (this)
			{
				Debug.Assert(value > m_prog);
				m_prog = prog = value;
				m_total = total = (m_partial + value / m_weight) * 10000 / m_sum;
			}

			if (m_onchange != null)
				m_onchange(prog, total, null, 1);
		}

		public void CompleteSteps()
		{
			Debug.Assert(m_istep < m_steps.Count);
			m_istep = m_steps.Count;
			m_message = null;
			m_weight = 0;
			m_partial = m_sum;
			m_prog = m_total = 10000;

			if (m_onchange != null)
				m_onchange(m_prog, m_total, null, 2);
		}
	}
}
