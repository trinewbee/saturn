using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Nano.Forms.Miu
{
    public class MiuSingleView : MiuApplication
    {
        public MiuForm Form { get; private set; }
        MiuView m_view;

        public MiuSingleView(MiuView view)
        {
            Form = new MiuForm(this);
            m_view = view;
        }

        public void FormInit(Form form) { }

        public MiuView Open() => m_view;

        public void Close() { }

        public DialogResult ShowDialog() => Form.ShowDialog();
    }

    public class MiuOverlay
    {
        MiuSingleView m_host;
        MiuForm m_form = null;

        public MiuOverlay(MiuView view)
        {
            m_host = new MiuSingleView(view);
        }

        public void Show(Control parent = null)
        {
            if (m_form != null)
                return;

            m_form = new MiuForm(m_host);
            m_form.FormClosed += (o, args) => m_form = null;

            if (parent != null)
                SetFloatIn(m_form, parent);

            m_form.Show();
        }

        public static void SetFloatIn(Form form, Control parent)
        {
            form.TopLevel = false;
            form.Parent = parent;
            form.BringToFront();
        }
    }
}
