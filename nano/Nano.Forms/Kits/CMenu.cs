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

    public abstract class CMenuBuilderBase
    {
        protected Stack<ToolStripMenuItem> m_stack = new Stack<ToolStripMenuItem>();

        protected abstract void AddToRoot(ToolStripItem item);

        public ToolStripMenuItem Add(string text, string name = null, Keys shortcut = Keys.None, EventHandler onclick = null)
        {
            var item = CMenu.Create(text, name, shortcut, onclick);
            if (m_stack.Count != 0)
                m_stack.Peek().DropDownItems.Add(item);
            else
                AddToRoot(item);
            return item;
        }

        public void AddSeparator()
        {
            var item = new ToolStripSeparator();
            if (m_stack.Count != 0)
                m_stack.Peek().DropDownItems.Add(item);
            else
                AddToRoot(item);
        }

        public ToolStripMenuItem Begin(string text, string name = null, Keys shortcut = Keys.None, EventHandler onclick = null)
        {
            var item = Add(text, name, shortcut, onclick);
            m_stack.Push(item);
            return item;
        }

        public void End() => m_stack.Pop();
    }

    public class CMenuBuilder : CMenuBuilderBase
    {
        public MenuStrip Menu = new MenuStrip();

        protected override void AddToRoot(ToolStripItem item) => Menu.Items.Add(item);

        public void SetForm(Form form) => CMenu.SetForm(form, Menu);
    }

    public class CCtxMenuBuilder : CMenuBuilderBase
    {
        public ContextMenuStrip Menu = new ContextMenuStrip();

        protected override void AddToRoot(ToolStripItem item) => Menu.Items.Add(item);
    }
}
