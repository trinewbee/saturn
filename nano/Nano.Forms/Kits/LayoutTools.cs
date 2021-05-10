using System;
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
        public const AnchorStyles AnchorWidthBottom = AnchorStyles.Bottom | AnchorStyles.Right | AnchorStyles.Top;
        public const AnchorStyles AnchorHeight = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
        public const AnchorStyles AnchorHeightRight = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;

        public const AnchorStyles AnchorLeftTop = AnchorStyles.Left | AnchorStyles.Top;
        public const AnchorStyles AnchorLeftBottom = AnchorStyles.Left | AnchorStyles.Bottom;
        public const AnchorStyles AnchorRightTop = AnchorStyles.Right | AnchorStyles.Top;
        public const AnchorStyles AnchorRightBottom = AnchorStyles.Right | AnchorStyles.Bottom;

        public class PanelSplitResult
        {
            public Panel panel1, panel2, panel3;
        }

        public static PanelSplitResult LayoutSplit(Control parent, bool vert = false)
        {
            var orient = vert ? Orientation.Horizontal : Orientation.Vertical;
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = orient };
            parent.Controls.Add(split);
            split.SplitterDistance = split.Width / 3;
            return new PanelSplitResult { panel1 = split.Panel1, panel2 = split.Panel2 };
        }

        public static PanelSplitResult LayoutSplit3(Control parent, bool vert = false)
        {
            var orient = vert ? Orientation.Horizontal : Orientation.Vertical;
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = orient };
            parent.Controls.Add(split);
            split.SplitterDistance = split.Width * 4 / 10;

            var split2 = new SplitContainer { Dock = DockStyle.Fill, Orientation = orient };
            split.Panel1.Controls.Add(split2);
            split2.SplitterDistance = split2.Width / 2;

            return new PanelSplitResult { panel1 = split2.Panel1, panel2 = split2.Panel2, panel3 = split.Panel2 };
        }        
    }
}
