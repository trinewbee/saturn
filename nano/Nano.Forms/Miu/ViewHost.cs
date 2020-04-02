using System;
using System.Windows.Forms;

namespace Nano.Forms.Miu
{    
    public class MiuViewHost
    {
        public Form Form { get; }
        public Control Parent { get; }

        public MiuViewHost(Form form, Control parent)
        {
            Form = form;
            Parent = parent;
        }
    }

    public interface MiuViewManager
    {
        MiuView Open();
        void Close();
    }

    public interface MiuView : IDisposable
    {
        void InitUI(MiuViewHost host);
    }
}
