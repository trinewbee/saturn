﻿using System;
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
        /// <summary>Invoked by constructor of Form class</summary>
        /// <param name="form">Form instance</param>
        void FormInit(Form form);

        /// <summary>Create first view</summary>
        /// <returns>View</returns>
        /// <remarks>Invoked in Form_Load event</remarks>
        MiuView Open();

        void Close();
    }

    public interface MiuView : IDisposable
    {
        /// <summary>Initialize view instance</summary>
        /// <param name="host">View host context</param>
        /// <param name="restore">True if the view instance is restored from view stack</param>
        void InitUI(MiuViewHost host, bool restore);
        bool QueryClose(); // 如果要取消关闭，返回 false
    }
}
