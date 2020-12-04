using System;
using System.Drawing;
using System.Windows.Forms;

namespace Nano.Forms
{
	public class NotifyIconUtil
	{
		public bool HideWhenMinimized = true;
		public bool HideNotifyWhenFormShown = true;

		Form m_form = null;
		NotifyIcon m_notify = null;

		public void Setup(Form form, NotifyIcon notify)
		{
			m_form = form;
			m_notify = notify;

			form.SizeChanged += Form_SizeChanged;

			notify.Visible = !HideNotifyWhenFormShown;
			notify.Click += Notify_Click;
		}

		private void Notify_Click(object sender, EventArgs e)
		{
			if (HideWhenMinimized)
			{
				m_form.Show();
				m_form.WindowState = FormWindowState.Normal;
			}
			if (HideNotifyWhenFormShown)
				m_notify.Visible = false;
		}

		private void Form_SizeChanged(object sender, EventArgs e)
		{
			if (m_form.WindowState == FormWindowState.Minimized)
			{
				if (HideWhenMinimized)
					m_form.Hide();
				if (HideNotifyWhenFormShown)
					m_notify.Visible = true;
			}
		}

		public static void Setup(Form form, string text, Icon icon)
        {
			var notify = new NotifyIcon { Text = text, Icon = icon };
			new NotifyIconUtil().Setup(form, notify);
		}

		public static void Setup(Form form, string text, string pathIcon)
        {
			var icon = new Icon(pathIcon);
			Setup(form, text, icon);
		}
	}
}
