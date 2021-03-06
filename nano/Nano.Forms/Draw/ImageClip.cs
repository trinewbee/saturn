﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using Nano.Collection;
using Nano.Storage;

namespace Nano.Forms
{
    public abstract class ImageClip
    {
        public int Index;
        public Image Image;
    }

    public interface ImageLibraryProvider : IDisposable
    {
        void LoadImages(List<ImageClip> clips);
        void LoadImage(ImageClip clip);
    }

    public class FileTreeImageClip : ImageClip
    {
        public string Path;
        public FileTreeItem Item;
        public bool SupportStream;

        public FileTreeImageClip(int index, string path, FileTreeItem item, bool supportStream)
        {
            Index = index;
            Path = path;
            Item = item;
            SupportStream = supportStream;
            Image = null;
        }

        public override int GetHashCode() => Path.GetHashCode();

        public override bool Equals(object obj) => ((FileTreeImageClip)obj).Path == Path;
    }

    public class ImageLibraryFileTreeProvider : ImageLibraryProvider
    {
        protected FileTreeAccess m_acc;

        public ImageLibraryFileTreeProvider(FileTreeAccess acc)
        {
            m_acc = acc;
        }

        public ImageLibraryFileTreeProvider(string path)
        {
            m_acc = new LocalFileTreeAccess(path);
        }

        public void Dispose()
        {
            m_acc?.Close();
            m_acc = null;
        }

        #region Load

        public void LoadImages(List<ImageClip> clips) => LoadDir(clips, m_acc.Root, "/");

        void LoadDir(List<ImageClip> clips, FileTreeItem fi, string path)
        {
            Debug.Assert(fi.IsDir);
            var subfis = fi.List();
            subfis.Sort((x, y) => string.Compare(x.Name, y.Name, true));
            foreach (var subfi in subfis)
            {
                string subpath = path + subfi.Name;
                if (subfi.IsDir)
                    LoadDir(clips, subfi, subpath + '/');
                else
                    LoadFile(clips, subfi, subpath);
            }
        }

        void LoadFile(List<ImageClip> clips, FileTreeItem fi, string path)
        {
            string ext = Path.GetExtension(fi.Name).ToLowerInvariant();
            if (!(ext == ".jpg" || ext == ".jpeg"))
                return;

            var clip = new FileTreeImageClip(clips.Count, path, fi, m_acc.SupportStream);
            clips.Add(clip);
        }

        #endregion

        public virtual void LoadImage(ImageClip clip)
        {
            if (clip.Image != null)
                return;

            var _clip = (FileTreeImageClip)clip;
            clip.Image = ImageKit.LoadImage(_clip.Item, _clip.SupportStream);
        }
    }

    public class ImageLibrary
    {
        ImageLibraryProvider m_provider;
        List<ImageClip> m_clips;
        LRUCachePool<ImageClip, Image> m_imgc;
        ImageClip m_cur;
        Random m_random;

        public ImageLibrary(ImageLibraryProvider provider)
        {
            const int cached = 10;
            m_provider = provider;
            m_clips = new List<ImageClip>();
            m_imgc = new LRUCachePool<ImageClip, Image>(cached);
            m_imgc.CreateObject += Imgc_CreateObject;
            m_imgc.OnObjectObsoleted += Imgc_OnObjectObsoleted;
            m_cur = null;
            m_random = new Random();
        }

        public void Load()
        {
            m_provider.LoadImages(m_clips);
            Debug.Assert(m_clips.Count > 0);
            m_cur = m_clips[0];
        }
        
        public void Close()
        {
            m_provider.Dispose();
            m_imgc.Dispose();
        }        

        Image Imgc_CreateObject(ImageClip clip)
        {
            Debug.Assert(clip.Image == null);
            m_provider.LoadImage(clip);
            return clip.Image;
        }

        static void Imgc_OnObjectObsoleted(ImageClip clip, Image image)
        {
            Debug.Assert(clip.Image == image && image != null);
            image.Dispose();
            clip.Image = null;
        }

        public Image RetrieveCurrentImage() => m_imgc.RetrieveForce(m_cur);

        public void ReturnCurrentImage() => m_imgc.Return(m_cur);

        void Jump(int index)
        {
            if (index < 0)
                index += m_clips.Count;
            Debug.Assert(index >= 0 && index < m_clips.Count);
            m_cur = m_clips[index];
        }

        public void Skip(int n) => Jump((m_cur.Index + n) % m_clips.Count);

        public void Random() => Jump(m_random.Next(m_clips.Count));
    }
}
