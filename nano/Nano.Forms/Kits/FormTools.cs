using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Nano.Forms
{
    public class MoveFormKit
    {
		public Func<MouseEventArgs, bool> EnableDrag = e => true;
		public MouseEventHandler OnClick = null;

		Form m_form;
		int m_mst = 0; // 0 normal 1 mouse-down 2 dragging
		int m_rx = 0, m_ry = 0;

		public MoveFormKit(Form form)
		{
			m_form = form;
			m_form.MouseDown += Form_MouseDown;
			m_form.MouseMove += Form_MouseMove;
			m_form.MouseUp += Form_MouseUp;
		}

		private void Form_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && EnableDrag(e))
			{
				m_mst = 1;
				m_rx = e.X;
				m_ry = e.Y;
			}
		}

		private void Form_MouseMove(object sender, MouseEventArgs e)
		{
			if (m_mst == 1)
			{
				if (Math.Abs(e.X - m_rx) + Math.Abs(e.Y - m_ry) > 4)
					m_mst = 2;
			}
			else if (m_mst == 2)
			{
				m_form.Left += e.X - m_rx;
				m_form.Top += e.Y - m_ry;
			}
		}

		private void Form_MouseUp(object sender, MouseEventArgs e)
		{
			if (m_mst != 0)
			{
				if (m_mst == 1)
					OnClick?.Invoke(sender, e);
				m_mst = 0;
			}
		}
	}
}
