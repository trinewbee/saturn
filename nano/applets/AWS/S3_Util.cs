using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.Runtime;

namespace Nano.AWS
{
	class AwsTransferProgress
	{
		public long Last = 0;

		public void OnTransferProgress(object sender, StreamTransferProgressArgs e)
		{
			if ((Last >> 20) == (e.TransferredBytes >> 20))
				return;
			Last = e.TransferredBytes;
			Console.Write($"\r{e.TransferredBytes >> 20}/{e.TotalBytes >> 20} ({e.PercentDone}%)");
		}
	}

	public class S3Utility
	{
        public AmazonS3Client Client { get; }

        #region Creation

        public S3Utility(AmazonS3Client client)
        {
            Client = client;
        }

        public static S3Utility MakeClient(string url, string access_key, string secret_key)
        {
            AmazonS3Config config = new AmazonS3Config();
            config.ServiceURL = url;
            config.ForcePathStyle = true;
            config.SignatureVersion = "2";

            Console.WriteLine("Service Url: " + config.ServiceURL);
            var client = new AmazonS3Client(access_key, secret_key, config);
            return new S3Utility(client);
        }

        public static S3Utility MakeMyClient(bool innerNet)
		{
            var url = innerNet ? "http://172.17.17.5" : "https://s3.cloudhua.com";
            const string access_key = "X795AVB3MCJEFEZXS4ZA";
			const string secret_key = "INjbwhAKIIohxImhph2hy9sK9OFbxwT1vcfnDXNl";
            return MakeClient(url, access_key, secret_key);
		}

        #endregion

        #region 同步方法

        public static T WaitTask<T>(Task<T> task)
        {
            task.Wait();
            return task.Result;
        }

        public ListBucketsResponse ListBuckets()
        {
            var task = Client.ListBucketsAsync();
            return WaitTask(task);
        }

        public PutBucketResponse PutBucket(string name)
        {
            var request = new PutBucketRequest { BucketName = name };
            var task = Client.PutBucketAsync(request);
            return WaitTask(task);
        }

        public DeleteBucketResponse DeleteBucket(string name)
        {
            var request = new DeleteBucketRequest { BucketName = name };
            var task = Client.DeleteBucketAsync(request);
            return WaitTask(task);
        }

        public GetObjectMetadataResponse GetObjectMeta(string bucket, string key)
		{
			var request = new GetObjectMetadataRequest { BucketName = bucket, Key = key };
            // var r = Client.GetObjectMetadata(request);
            var task = Client.GetObjectMetadataAsync(request);
            var r = WaitTask(task);
			Debug.Assert(r.HttpStatusCode == System.Net.HttpStatusCode.OK);
			return r;
		}

        public ListObjectsResponse ListObjects(ListObjectsRequest request)
        {
            var task = Client.ListObjectsAsync(request);
            return WaitTask(task);
        }

		public List<S3Object> List(string bucket, string prefix = null)
		{
			var objects = new List<S3Object>();
			var request = new ListObjectsRequest { BucketName = bucket, Prefix = prefix };
			while (true)
			{
                // var r = Client.ListObjects(request);
                var task = Client.ListObjectsAsync(request);
                var r = WaitTask(task);
                Debug.Assert(r.HttpStatusCode == System.Net.HttpStatusCode.OK);
				objects.AddRange(r.S3Objects);

				if (r.IsTruncated)
					request.Marker = r.NextMarker;
				else
					break;
			}
			return objects;
		}

        public GetObjectResponse GetObject(GetObjectRequest request)
        {
            var task = Client.GetObjectAsync(request);
            return WaitTask(task);
        }

        public GetObjectResponse GetObject(string bucket, string key, ByteRange range = null)
        {
            var request = new GetObjectRequest { BucketName = bucket, Key = key, ByteRange = range };
            return GetObject(request);
        }

        MemoryStream _ToMemoryStream(GetObjectResponse r)
        {
            if (r.ContentLength >= 0x80000000)
                throw new OutOfMemoryException();
            var ostream = new MemoryStream((int)r.ContentLength);
            Net.ResponseReader.CopyStream(r.ResponseStream, ostream, new byte[0x100000]);
            r.ResponseStream.Close();
            return ostream;
        }

        public MemoryStream GetObjectData(string bucket, string key, ByteRange range = null)
        {
            var r = GetObject(bucket, key, range);
            return _ToMemoryStream(r);
        }

        byte[] _ToByteArray(GetObjectResponse r)
        {
            var stream = _ToMemoryStream(r);
            var data = stream.ToArray();
            stream.Close();
            return data;
        }

        public byte[] GetObjectDataArray(string bucket, string key, ByteRange range = null)
        {
            var r = GetObject(bucket, key, range);
            return _ToByteArray(r);
        }

        public PutObjectResponse PutObject(string bucket, string key, string path)
		{
			var request = new PutObjectRequest { BucketName = bucket, Key = key, FilePath = path };
			request.StreamTransferProgress += new AwsTransferProgress().OnTransferProgress;
            // var r = Client.PutObject(request);
            var task = Client.PutObjectAsync(request);
            var r = WaitTask(task);
			Console.WriteLine("\rUpload completed");
			Debug.Assert(r.HttpStatusCode == System.Net.HttpStatusCode.OK);
			return r;
		}

		public PutObjectResponse PutObject(string bucket, string key, Stream istream)
		{
			var request = new PutObjectRequest { BucketName = bucket, Key = key, InputStream = istream };
			request.StreamTransferProgress += new AwsTransferProgress().OnTransferProgress;
            // var r = Client.PutObject(request);
            var task = Client.PutObjectAsync(request);
            var r = WaitTask(task);
			Debug.Assert(r.HttpStatusCode == System.Net.HttpStatusCode.OK);
            return r;
		}

        public DeleteObjectResponse DeleteObject(DeleteObjectRequest request)
        {
            var task = Client.DeleteObjectAsync(request);
            return WaitTask(task);
        }

        public DeleteObjectResponse DeleteObject(string bucket, string key)
        {
            var request = new DeleteObjectRequest { BucketName = bucket, Key = key };
            return DeleteObject(request);
        }

        #endregion

        #region 多次上传（会改变 etag）

        public async Task UploadFileAsync(string bucket, string key, string path)
        {
            var fileTransferUtility = new TransferUtility(Client);
            await fileTransferUtility.UploadAsync(path, bucket, key);
        }

        public async Task UploadStreamAsync(string bucket, string key, Stream istream)
        {
            var fileTransferUtility = new TransferUtility(Client);
            await fileTransferUtility.UploadAsync(istream, bucket, key);
        }

        public void UploadFile(string bucket, string key, string path) => UploadFileAsync(bucket, key, path).Wait();

        public void UploadStream(string bucket, string key, Stream istream) => UploadStreamAsync(bucket, key, istream).Wait();

        #endregion
    }
}
