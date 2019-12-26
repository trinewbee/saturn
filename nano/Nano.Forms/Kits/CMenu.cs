using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Nano.Forms
{
    public static class CMenu
    {
        #region Utils

        public static void SetForm(Form form, MenuStrip strip)
        {
            form.Controls.Add(strip);
            form.MainMenuStrip = strip;
        }

        public static ToolStripMenuItem Create(string text, string name = null, Keys shortcut = Keys.None, EventHandler onclick = null)
        {
            var item = new ToolStripMenuItem { Name = name, Text = text, ShortcutKeys = shortcut };
            if (onclick != null)
                item.Click += onclick;
            return item;
        }

        public static ToolStripMenuItem Add(MenuStrip menu, string text, string name = null, Keys shortcut = Keys.None, EventHandler onclick = null)
        {
            var item = Create(text, name, shortcut, onclick);
            menu.Items.Add(item);
            return item;
        }

        public static ToolStripMenuItem Add(ToolStripMenuItem menu, string text, string name = null, Keys shortcut = Keys.None, EventHandler onclick = null)
        {
            var item = Create(text, name, shortcut, onclick);
            menu.DropDownItems.Add(item);
            return item;
        }

        #endregion
    }

    public class CMenuBuilder
    {
        public MenuStrip Menu = new MenuStrip();
        Stack<ToolStripMenuItem> m_stack = new Stack<ToolStripMenuItem>();

        public ToolStripMenuItem Add(string text, string name = null, Keys shortcut = Keys.None, EventHandler onclick = null)
        {
            var item = CMenu.Create(text, name, shortcut, onclick);
            if (m_stack.Count != 0)
                m_stack.Peek().DropDownItems.Add(item);
            else
                Menu.Items.Add(item);
            return item;
        }

        public ToolStripMenuItem Begin(string text, string name = null, Keys shortcut = Keys.None, EventHandler onclick = null)
        {
            var item = Add(text, name, shortcut, onclick);
            m_stack.Push(item);
            return item;
        }

        public void End() => m_stack.Pop();

        public void SetForm(Form form) => CMenu.SetForm(form, Menu);
    }
}
