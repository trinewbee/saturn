using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using Nano.Collection;
using Nano.Storage;

namespace Nano.Forms
{
    public class ImageClip
    {
        public int Index;
        public string Path;
        public FileTreeItem Item;
        public bool SupportStream;
        public Image Image;

        public ImageClip(int index, string path, FileTreeItem item, bool supportStream)
        {
            Index = index;
            Path = path;
            Item = item;
            SupportStream = supportStream;
            Image = null;
        }

        public override int GetHashCode() => Path.GetHashCode();

        public override bool Equals(object obj) => ((ImageClip)obj).Path == Path;
    }

    public class ImageLibrary
    {
        FileTreeAccess m_acc;
        List<ImageClip> m_clips;
        LRUCachePool<ImageClip, Image> m_imgc;
        ImageClip m_cur;
        Random m_random;

        public ImageLibrary()
        {
            const int cached = 10;
            m_clips = new List<ImageClip>();
            m_imgc = new LRUCachePool<ImageClip, Image>(cached);
            m_imgc.CreateObject += Imgc_CreateObject;
            m_imgc.OnObjectObsoleted += Imgc_OnObjectObsoleted;
            m_cur = null;
            m_random = new Random();
        }

        #region Load

        public void Load(FileTreeAccess acc)
        {
            m_acc = acc;
            LoadDir("/", acc.Root);
            Debug.Assert(m_clips.Count > 0);
            m_cur = m_clips[0];
        }

        public void LoadLocal(string path)
        {
            var acc = new LocalFileTreeAccess(path);
            Load(acc);
        }

        void LoadDir(string path, FileTreeItem fi)
        {
            Debug.Assert(fi.IsDir);
            var subfis = fi.List();
            subfis.Sort((x, y) => string.Compare(x.Name, y.Name, true));
            foreach (var subfi in subfis)
            {
                string subpath = path + subfi.Name;
                if (subfi.IsDir)
                    LoadDir(subpath + '/', subfi);
                else
                    LoadFile(subpath, subfi);
            }
        }

        void LoadFile(string path, FileTreeItem fi)
        {
            string ext = Path.GetExtension(fi.Name).ToLowerInvariant();
            if (!(ext == ".jpg" || ext == ".jpeg"))
                return;

            var clip = new ImageClip(m_clips.Count, path, fi, m_acc.SupportStream);
            m_clips.Add(clip);
        }

        #endregion

        public void Close()
        {
            m_acc.Close();
            m_imgc.Dispose();
        }

        #region 载入图像 (处理方向参数)

        public static Image LoadImage(Stream stream)
        {
            var image = Bitmap.FromStream(stream);

            // 调整方向
            var rft = GetImageOrientation(image);
            image.RotateFlip(rft);
            return image;
        }

        public static Image LoadImage(FileTreeItem item, bool supportStream)
        {
            Stream istream;
            if (!supportStream)
            {
                var data = item.AtomRead();
                istream = new MemoryStream(data);
            }
            else
                istream = item.Open(false);

            using (istream)
                return LoadImage(istream);
        }

        public static Image LoadImage(KeyValueAccess acc, string key)
        {
            Stream istream;
            if (!acc.SupportStream)
            {
                var data = acc.AtomRead(key);
                istream = new MemoryStream(data);
            }
            else
                istream = acc.OpenObject(key, false);

            using (istream)
                return LoadImage(istream);
        }

        public static Image LoadImage(string path)
        {
            using (var istream = new FileStream(path, FileMode.Open, FileAccess.Read))
                return LoadImage(istream);
        }

        #endregion

        static Image Imgc_CreateObject(ImageClip clip)
        {
            Debug.Assert(clip.Image == null);
            clip.Image = LoadImage(clip.Item, clip.SupportStream);
            return clip.Image;
        }

        // 获取方向参数
        static RotateFlipType GetImageOrientation(Image image)
        {
            foreach (var prop in image.PropertyItems)
            {
                if (prop.Id == 0x112)
                {
                    var val = prop.Value[0];
                    switch (val)
                    {
                        case 0:
                        case 1: // 0
                            return RotateFlipType.RotateNoneFlipNone;
                        case 8: // 90
                            return RotateFlipType.Rotate270FlipNone;
                        case 3: // 180
                            return RotateFlipType.Rotate180FlipNone;
                        case 6: // 270
                            return RotateFlipType.Rotate90FlipNone;
                        default:
                            throw new NotSupportedException("UnsupportedOrientation:" + val);
                    }
                }
            }
            return RotateFlipType.RotateNoneFlipNone;
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
