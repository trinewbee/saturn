using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Nano.Forms
{
    public class TextBoxWriter : TextWriter
    {
        TextBox m_textBox;

        public TextBoxWriter(TextBox textBox)
        {
            m_textBox = textBox;
        }

        public override Encoding Encoding => Encoding.UTF8;

        delegate void WriteDelegate(string value);

        // 最低限度需要重写的方法
        public override void Write(string value)
        {
            if (m_textBox.InvokeRequired)
                m_textBox.BeginInvoke((WriteDelegate)Write, value);
            else
                m_textBox.AppendText(value);
        }

        public override void WriteLine(string value)
        {
            if (m_textBox.InvokeRequired)
                m_textBox.BeginInvoke((WriteDelegate)WriteLine, value);
            else
                m_textBox.AppendText(value + "\r\n");
        }
    }
}
