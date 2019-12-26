using System;
using System.Windows.Forms;

namespace Nano.Forms
{
    public class CMenu
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
}
