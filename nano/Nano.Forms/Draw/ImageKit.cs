using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Nano.Common;
using Nano.Storage;

namespace Nano.Forms
{
	public static class ImageKit
	{
		#region 载入图像 (处理方向参数)

		// 获取方向参数
		public static RotateFlipType GetImageOrientation(Image image)
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

		#region Scale

		const long Precision = 10000;

		public static void ComputeZoomFrame(long wfit, long hfit, long w, long h, out long w2, out long h2)
		{
			long rw = w * Precision / wfit;
			long rh = h * Precision / hfit;
			long ratio = Math.Max(rw, rh);
			if (ratio > Precision)
			{
				w2 = w * Precision / ratio;
				h2 = h * Precision / ratio;
			}
			else
			{
				w2 = w;
				h2 = h;
			}
		}

		public static Bitmap ZoomImage(Image bmSource, int w, int h)
		{
			Bitmap bmTarget = new Bitmap(w, h);
			Graphics g = Graphics.FromImage(bmTarget);
			Rectangle rcT = new Rectangle(0, 0, w, h);
			g.DrawImage(bmSource, rcT);
			g.Dispose();
			return bmTarget;
		}

		public static Bitmap ZoomFitImage(Image bmSource, int w, int h)
		{		
			long w1 = bmSource.Width, h1 = bmSource.Height, w2, h2;
			ComputeZoomFrame(w, h, w1, h1, out w2, out h2);
			return ZoomImage(bmSource, (int)w2, (int)h2);
		}

		#endregion
	}

	public class JpegEncodeKit
	{
		ImageCodecInfo m_codecInfo = null;
		EncoderParameters m_codecParams = null;

		public JpegEncodeKit(int quality)
		{
			InitEncoder(quality);
		}

		public void InitEncoder(int quality)
		{
			// Save the bitmap as a JPEG file
			ImageCodecInfo[] codecInfos = ImageCodecInfo.GetImageEncoders();
			m_codecInfo = Array.Find(codecInfos, x => x.MimeType == "image/jpeg");
			if (m_codecInfo == null)
				throw new NotSupportedException("Encoder not supported");

			// Save the parameter for the specified quality
			// Attention, "Quality" must be an argument not a const value, otherwise, 
			// an exception raised when saving images.
			m_codecParams = new EncoderParameters(1);
			m_codecParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
		}

		public void Save(Stream stream, Bitmap image) =>
			image.Save(stream, m_codecInfo, m_codecParams);

		public void Save(string path, Bitmap image) =>
			image.Save(path, m_codecInfo, m_codecParams);
	}
}
