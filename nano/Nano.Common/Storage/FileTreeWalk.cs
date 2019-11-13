using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Common;

namespace Nano.Storage
{
	public static class FileTreeWalker
	{
		public interface INotify
		{
			void EnterFolder(FileTreeItem item, string pathRel);
			void LeaveFolder(FileTreeItem item, string pathRel);
			void GetFile(FileTreeItem item, string pathRel);
		}

		public static void Walk(FileTreeItem item, INotify notify)
		{
			WalkFolder(item, "/", notify);
		}

		static void WalkFolder(FileTreeItem parent, string pathRel, INotify notify)
		{
			foreach (var item in parent.List())
			{
				string pathRelSub = pathRel.Length > 1 ? pathRel + '/' + item.Name : '/' + item.Name;
				if (item.IsDir)
				{
					notify.EnterFolder(item, pathRelSub);
					WalkFolder(item, pathRelSub, notify);
					notify.LeaveFolder(item, pathRelSub);
				}
				else
					notify.GetFile(item, pathRelSub);
			}
		}
	}

	/// <summary>用于 FileTreeUpdateWalker 的文件复制接口</summary>
	public interface IFileTreeUpdater
	{
		/// <summary>将 source 文件数据复制到 target 对象</summary>
		/// <param name="source">源文件</param>
		/// <param name="target">目标文件</param>
		/// <param name="pathRel">相对路径</param>
		/// <returns>返回更新后的目标文件对象</returns>
		/// <remarks>需要使用源和目标库支持的访问方法来复制数据，大部分 FileTreeAccess 只实现了部分方法。</remarks>
		FileTreeItem Update(FileTreeItem source, FileTreeItem target, string pathRel);

		/// <summary>用 source 文件的数据在 parent 中创建名为 name 的文件</summary>
		/// <param name="source">源文件</param>
		/// <param name="parent">目标目录</param>
		/// <param name="name">文件名</param>
		/// <param name="pathRel">相对路径</param>
		/// <returns>返回更新后的目标文件对象</returns>
		/// <remarks>需要使用源和目标库支持的访问方法来复制数据，大部分 FileTreeAccess 只实现了部分方法。</remarks>
		FileTreeItem Create(FileTreeItem source, FileTreeItem parent, string name, string pathRel);
	}

	/// <summary>用于 FileTreeUpdateWalker 的通知接口</summary>
	public interface IFileTreeUpdateNotify
	{
		/// <summary>获取事件通知</summary>
		/// <param name="source">源对象</param>
		/// <param name="target">目标对象</param>
		/// <param name="pathRel">相对路径</param>
		/// <param name="act">将进行的操作</param>
		void Send(FileTreeItem source, FileTreeItem target, string pathRel, FileUpdateActionType act);
	}

	/// <summary>文件树（FileTreeItem 树）更新工具</summary>
	public class FileTreeUpdateWalker
	{
		public delegate bool JudgeFileUpdateDelegate(FileTreeItem source, FileTreeItem target);
		public JudgeFileUpdateDelegate JudgeFileUpdate;

		FileTreeItem m_source, m_target;
		IFileTreeUpdater m_updater;
		FileUpdateWalkerOption m_option;
		IFileTreeUpdateNotify m_notify;

		class NullNotify : IFileTreeUpdateNotify
		{
			public void Send(FileTreeItem source, FileTreeItem target, string pathRel, FileUpdateActionType act) { }
		}

		/// <summary>初始化 FileTreeUpdateWalker 对象</summary>
		/// <param name="source">源文件树根对象</param>
		/// <param name="target">目标文件树根对象</param>
		/// <param name="updater">文件复制对象</param>
		/// <param name="notify">更新通知对象</param>
		/// <param name="option">更新参数</param>
		public FileTreeUpdateWalker(FileTreeItem source, FileTreeItem target, IFileTreeUpdater updater, IFileTreeUpdateNotify notify = null, FileUpdateWalkerOption option = FileUpdateWalkerOption.Default)
		{
			JudgeFileUpdate = _def_JudgeFileUpdate;

			m_source = source;
			m_target = target;
			m_updater = updater;
			m_option = option;
			m_notify = notify != null ? notify : new NullNotify();

			if ((m_option & FileUpdateWalkerOption.Unsupported) != 0)
				throw new ArgumentException("Unsupported options");
		}

		/// <summary>运行一次更新操作</summary>
		public void Walk() => _WalkFolder(m_source, m_target, "/");

		void _WalkFolder(FileTreeItem source, FileTreeItem target, string pathRel)
		{
			m_notify.Send(source, target, pathRel, FileUpdateActionType.EnterDirectory);

			var tmap = new Dictionary<string, FileTreeItem>(); // children of Target Dir
			foreach (var sub_t in target.List())
				tmap.Add(sub_t.Name.ToLowerInvariant(), sub_t);

			foreach (var sub_s in source.List())
			{
				var pathRelSub = pathRel.Length > 1 ? pathRel + '/' + sub_s.Name : '/' + sub_s.Name;
				var key = sub_s.Name.ToLowerInvariant();
				FileTreeItem sub_t;
				if (tmap.TryGetValue(key, out sub_t))
				{
					tmap.Remove(key);
					if (sub_s.IsDir)
					{
						// 比较两个目录
						if (!sub_t.IsDir)
						{
							// 如果源是目录，目标同名项是文件，则删除目标同名项，然后创建目录
							if ((m_option & FileUpdateWalkerOption.PerformDelete) == 0)
								throw new AccessViolationException($"Target {pathRel} should be deleted before updating");

							m_notify.Send(null, sub_t, pathRel, FileUpdateActionType.DeleteFile);
							sub_t.Delete(true);
							m_notify.Send(null, sub_t, pathRelSub, FileUpdateActionType.CreateDirectory);
							sub_t = target.CreateDir(sub_s.Name);
						}
						_WalkFolder(sub_s, sub_t, pathRelSub);
						sub_t.LastWriteTimeUtc = sub_s.LastWriteTimeUtc;
					}
					else
					{
						// 比较两个文件
						if (sub_t.IsDir)
						{
							// 如果源是文件，目标同名项是目录，则删除目标同名项，然后复制文件
							if ((m_option & FileUpdateWalkerOption.PerformDelete) == 0)
								throw new AccessViolationException($"Target {pathRel} should be deleted before updating");

							m_notify.Send(null, sub_t, pathRel, FileUpdateActionType.DeleteDirectory);
							sub_t.Delete(true);
							m_notify.Send(sub_s, null, pathRelSub, FileUpdateActionType.UpdateFile);
							sub_t = m_updater.Create(sub_s, target, sub_s.Name, pathRel);
							sub_t.LastWriteTimeUtc = sub_s.LastWriteTimeUtc;
						}
						else if (JudgeFileUpdate(sub_s, sub_t))
						{
							m_notify.Send(sub_s, sub_t, pathRelSub, FileUpdateActionType.UpdateFile);
							sub_t = m_updater.Update(sub_s, sub_t, pathRelSub);
							sub_t.LastWriteTimeUtc = sub_s.LastWriteTimeUtc;
						}
					}
				}
				else
				{
					if (sub_s.IsDir)
					{
						// 创建目标目录
						m_notify.Send(null, sub_t, pathRelSub, FileUpdateActionType.CreateDirectory);
						sub_t = target.CreateDir(sub_s.Name);
						_WalkFolder(sub_s, sub_t, pathRelSub);
						sub_t.LastWriteTimeUtc = sub_s.LastWriteTimeUtc;
					}
					else
					{
						// 创建目标文件
						m_notify.Send(sub_s, null, pathRelSub, FileUpdateActionType.UpdateFile);
						sub_t = m_updater.Create(sub_s, target, sub_s.Name, pathRelSub);
						sub_t.LastWriteTimeUtc = sub_s.LastWriteTimeUtc;
					}
				}
			}

			if ((m_option & FileUpdateWalkerOption.PerformDelete) != 0)
			{
				foreach (var sub_t in tmap.Values)
				{
					// 删除目标对象
					var pathRelSub = pathRel.Length > 1 ? pathRel + '/' + sub_t.Name : '/' + sub_t.Name;
					m_notify.Send(null, sub_t, pathRelSub, sub_t.IsDir ? FileUpdateActionType.DeleteDirectory : FileUpdateActionType.DeleteFile);
					sub_t.Delete(true);
				}
			}
			tmap = null;

			m_notify.Send(source, target, pathRel, FileUpdateActionType.LeaveDirectory);
		}

		bool _def_JudgeFileUpdate(FileTreeItem source, FileTreeItem target) => source.Size != target.Size || source.LastWriteTimeUtc.Ticks / 10000000 > target.LastWriteTimeUtc.Ticks / 10000000;
	}
}
