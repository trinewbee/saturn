using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Nano.Forms.Miu
{
    public class MiuForm : Form
    {
        MiuViewManager m_manager;
        Stack<MiuView> m_views;
        MiuViewHost m_viewHost;
        MiuView m_view = null;

        public MiuForm(MiuViewManager manager)
        {
            m_manager = manager;
            m_views = new Stack<MiuView>();
            m_viewHost = new MiuViewHost(this, this);

            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(800, 450);
            Load += Form_Load;
            FormClosing += Form_Closing;
        }

        private void Form_Load(object sender, EventArgs e)
        {
            m_view = m_manager.Open();
            m_views.Push(m_view);
            m_view.InitUI(m_viewHost);
        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            m_manager.Close();
            m_manager = null;
        }
    }
}
