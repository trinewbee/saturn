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
}
