using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

namespace Nano.Net
{
	public class HttpClient
	{
		CookieContainer m_cookies = new CookieContainer();

		public const string MimeForm = "application/x-www-form-urlencoded";
		public const string MimeBinary = "application/octet-stream";

		#region 构造请求数据包

		public HttpWebRequest CreateRequest(string url)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.CookieContainer = m_cookies;
			return request;
		}

		public void MakePost(HttpWebRequest request, string mime, byte[] data, int off, int len)
		{
			request.Method = "POST";
			request.ContentType = mime;
			request.ContentLength = len;
			System.IO.Stream reqStream = request.GetRequestStream();
			reqStream.Write(data, off, len);
			reqStream.Close();
		}

		public void MakePost(HttpWebRequest request, string mime, byte[] data)
		{
			MakePost(request, mime, data, 0, data.Length);
		}

		public void MakePost(HttpWebRequest request, string mime, string postText)
		{
			byte[] postData = System.Text.Encoding.UTF8.GetBytes(postText);
			MakePost(request, mime, postData, 0, postData.Length);
		}

		#endregion

		#region 发送请求并获取返回

		/// <summary>发送请求并获取返回</summary>
		/// <param name="request"></param>
		/// <returns></returns>
		/// <remarks>在使用后必须关闭返回的 Response 对象，或者 ResponseStream 对象。</remarks>
		public HttpWebResponse GetResponse(HttpWebRequest request)
		{
			return (HttpWebResponse)request.GetResponse();
		}

		#endregion
	}

	public static class ResponseReader
	{
		public static void CopyStream(Stream istream, Stream ostream, byte[] buffer)
		{
			while (true)
			{
				int cbSeg = istream.Read(buffer, 0, buffer.Length);
				if (cbSeg == 0)
					break;
				ostream.Write(buffer, 0, cbSeg);
			}
			ostream.Flush();
		}

		public static MemoryStream ReadResponseData(HttpWebResponse response)
		{
			Stream istream = response.GetResponseStream();
			MemoryStream ostream = new MemoryStream();
			CopyStream(istream, ostream, new byte[1024]);
			istream.Close();
			response.Close();
			return ostream;
		}

		public static byte[] ReadResponseArray(HttpWebResponse response)
		{
			MemoryStream stream = ReadResponseData(response);
			byte[] data = stream.ToArray();
			stream.Close();
			return data;
		}

		public static string ReadResponseText(HttpWebResponse response, Encoding e)
		{
			TextReader tr = new StreamReader(response.GetResponseStream(), e);
			string respText = tr.ReadToEnd();
			tr.Close();

			// Response 和 ResponseStream 两者只需要关闭其中一个即可
			response.Close();
			return respText;
		}

		public static string ReadResponseText(HttpWebResponse response)
		{
			return ReadResponseText(response, Encoding.UTF8);
		}

		public static void SaveStream(HttpWebResponse response, Stream ostream)
		{
			var istream = response.GetResponseStream();
			CopyStream(istream, ostream, new byte[32768]);
			istream.Close();
			response.Close();
		}

		public static void SaveFile(HttpWebResponse response, string path, bool fOverwrite)
		{
			FileMode mode = fOverwrite ? FileMode.Create : FileMode.CreateNew;
			var ostream = new FileStream(path, mode, FileAccess.Write);
			SaveStream(response, ostream);
			ostream.Close();
		}
	}
}
