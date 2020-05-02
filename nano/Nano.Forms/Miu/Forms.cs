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
        MiuView m_view = null;
        Panel m_panel;

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
        }

        private void Form_Load(object sender, EventArgs e)
        {
            var view = m_manager.Open();
            SwitchView(view, true);
        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            if (m_view != null && m_view.QueryClose())
            {
                e.Cancel = true;
                return;
            }

            m_manager.Close();
            m_manager = null;
        }

        internal void SwitchView(MiuView view, bool dispose)
        {
            if (dispose && m_view != null)
            {
                if (view.QueryClose())
                    return;

                m_view.Dispose();
                m_views.Pop();
            }

            m_panel.Controls.Clear();
            m_views.Push(m_view = view);
            m_view.InitUI(m_viewHost);
        }

        internal void PopView()
        {
            if (m_view != null)
            {
                if (m_view.QueryClose())
                    return;

                m_view.Dispose();
                m_view = null;
            }

            m_panel.Controls.Clear();
            m_views.Pop();
            m_view = m_views.Count != 0 ? m_views.Peek() : null;
            m_view?.InitUI(m_viewHost);
        }
    }
}
