﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Nano.Forms
{
    public static class LayoutTools
    {
        public const AnchorStyles AnchorFill = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
        public const AnchorStyles AnchorWidth = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

        public class HorzSplitResult
        {
            public Panel panel1, panel2, panel3;
        }

        public static HorzSplitResult HorzSplit(Control parent)
        {
            var split = new SplitContainer { Dock = DockStyle.Fill };
            parent.Controls.Add(split);
            split.SplitterDistance = split.Width / 3;
            return new HorzSplitResult { panel1 = split.Panel1, panel2 = split.Panel2 };
        }

        public static HorzSplitResult HorzSplit3(Control parent)
        {
            var split = new SplitContainer { Dock = DockStyle.Fill };
            parent.Controls.Add(split);
            split.SplitterDistance = split.Width * 4 / 10;

            var split2 = new SplitContainer { Dock = DockStyle.Fill };
            split.Panel1.Controls.Add(split2);
            split2.SplitterDistance = split2.Width / 2;

            return new HorzSplitResult { panel1 = split2.Panel1, panel2 = split2.Panel2, panel3 = split.Panel2 };
        }
    }
}
