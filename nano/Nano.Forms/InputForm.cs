using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Nano.Forms
{
	public partial class InputForm : Form
	{
		private string m_text = null;

		private InputForm()
		{
			InitializeComponent();
		}

		private void Init(string prompt, string title, string text)
		{
			this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Text = title;
			lblPrompt.Text = prompt;
			txtInput.Text = text;
			txtInput.SelectAll();
		}

		public static DialogResult ShowDialog(string prompt, string title, ref string text)
		{
			InputForm form = new InputForm();
			form.Init(prompt, title, text);
			DialogResult r = form.ShowDialog();
			if (r == DialogResult.OK)
				text = form.m_text;
			return r;
		}

		private void cmdOK_Click(object sender, EventArgs e)
		{
			m_text = txtInput.Text;
			this.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.Close();
		}
	}
}
