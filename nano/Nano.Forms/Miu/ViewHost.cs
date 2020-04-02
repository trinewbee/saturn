using System;
using System.Windows.Forms;

namespace Nano.Forms.Miu
{    
    public class MiuViewHost
    {
        public MiuForm Form { get; }
        public Control Parent { get; }

        public MiuViewHost(MiuForm form, Control parent)
        {
            Form = form;
            Parent = parent;
        }

        /// <summary>切换 View</summary>
        public void SwitchView(MiuView view, bool dispose = false) => Form.SwitchView(view, dispose);

        public void PopView() => Form.PopView();
    }

    public interface MiuApplication
    {
        MiuView Open();
        void Close();
    }

    public interface MiuView : IDisposable
    {
        void InitUI(MiuViewHost host);
    }
}
