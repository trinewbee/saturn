using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Nano.Forms.Miu
{
    public class MiuForm : Form
    {
        MiuApplication m_manager;
        Stack<MiuView> m_views;
        MiuViewHost m_viewHost;
        Panel m_panel;

        internal MiuView TopView => m_views.Count != 0 ? m_views.Peek() : null;

        public MiuForm(MiuApplication manager)
        {
            m_manager = manager;

            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(800, 450);
            Load += Form_Load;
            FormClosing += Form_Closing;

            m_panel = new Panel { Dock = DockStyle.Fill };
            Controls.Add(m_panel);

            m_views = new Stack<MiuView>();
            m_viewHost = new MiuViewHost(this, m_panel);

            m_manager.FormInit(this);
        }

        private void Form_Load(object sender, EventArgs e)
        {
            var view = m_manager.Open();
            SwitchView(view, true);
        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            while (m_views.Count != 0)
            {
                if (!PopView())
                {
                    // Cancelled
                    e.Cancel = true;
                    return;
                }
            }

            m_manager.Close();
            m_manager = null;
        }

        internal void SwitchView(MiuView view, bool dispose)
        {
            var topView = TopView;
            if (dispose && topView != null)
            {
                if (!topView.QueryClose())
                    return;

                topView.Dispose();
                m_views.Pop();
            }

            m_panel.Controls.Clear();
            m_views.Push(view);
            view.InitUI(m_viewHost, false);
        }

        internal bool PopView()
        {
            var topView = TopView;
            if (topView == null)
                return true;

            if (!topView.QueryClose())
                return false;

            topView.Dispose();
            m_views.Pop();
            m_panel.Controls.Clear();

            topView = TopView;
            topView?.InitUI(m_viewHost, true);
            return true;
        }

        public void SetDoubleBuffered() => DoubleBuffered = true;
    }
}
