using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Nano.Common;
using Nano.Collection;
using Nano.Storage;

namespace Nano.Crypt
{
	public interface IKeyValueCrypt
	{
		byte[] EncryptFinalBlock(string name, byte[] data);

		byte[] DecryptFinalBlock(string name, byte[] data);

		byte[] DecryptPart(string name, BlockCrypt.ReadDelegate f, long length, long pos, int size);

		Stream CreateEncryptStream(string name, Stream istream);

		Stream CreateDecryptStream(string name, Stream istream);
    }

	public class BlockKeyValueCrypt : IKeyValueCrypt
	{
		BlockCrypt m_ebc, m_dbc;

		public BlockKeyValueCrypt(BlockCrypt ebc, BlockCrypt dbc)
		{
			m_ebc = ebc;
			m_dbc = dbc;
		}

		public byte[] EncryptFinalBlock(string name, byte[] data) => m_ebc.TransformFinalBlock(data);

		public byte[] DecryptFinalBlock(string name, byte[] data) => m_dbc.TransformFinalBlock(data);

		public byte[] DecryptPart(string name, BlockCrypt.ReadDelegate f, long length, long pos, int size) => m_dbc.TransformPart(f, length, pos, size);

		public Stream CreateEncryptStream(string name, Stream istream) => ReadonlyTransformStream.CreateInstance(istream, m_ebc);

		public Stream CreateDecryptStream(string name, Stream istream) => ReadonlyTransformStream.CreateInstance(istream, m_dbc);
	}

    class ReadonlyStreamOnAtomRead : Stream
    {
        KeyValueAccess m_access;
        string m_name;
        IKeyValueCrypt m_cr;

        public ReadonlyStreamOnAtomRead(KeyValueAccess access, ObjectInfo obj, IKeyValueCrypt cr)
        {
            m_access = access;
            m_name = obj.Name;
            Length = obj.Size;
            m_cr = cr;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length { get; }

        public override long Position { get; set; } = 0;

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException("Out of buffer range");
            if (Position < 0 || Position > Length)
                throw new ArgumentOutOfRangeException("Position out of range");

            count = (int)Math.Min(Length - Position, count);
            var data = m_cr.DecryptPart(m_name, (pos, size) => m_access.AtomRead(m_name, pos, size), Length, Position, count);
            if (data.Length != count)
                throw new IOException("Wrong data size");
            Array.Copy(data, 0, buffer, offset, count);
            Position += count;
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    return Position = offset;
                case SeekOrigin.Current:
                    return Position += offset;
                case SeekOrigin.End:
                    return Position = Length + offset;
                default:
                    throw new ArgumentException("Wrong seek origin");
            }
        }

        public override void SetLength(long value) => throw new AccessViolationException();

        public override void Write(byte[] buffer, int offset, int count) => throw new AccessViolationException();
    }

    public class CryptKeyValueAccess : KeyValueAccess
	{
		KeyValueAccess m_inner;
		IKeyValueCrypt m_cr;

		public CryptKeyValueAccess(KeyValueAccess inner, IKeyValueCrypt cr)
		{
			m_inner = inner;
			m_cr = cr;
		}

		public CryptKeyValueAccess(KeyValueAccess inner, BlockCrypt ebc, BlockCrypt dbc)
		{
			m_inner = inner;
			m_cr = new BlockKeyValueCrypt(ebc, dbc);
		}

        #region Properties

        public bool CanCreate => true;

        public bool CanAppend => false;

        public bool CanModify => false;

        public bool CanResize => false;

		public bool SupportStream => true;

        public bool IsRemote => m_inner.IsRemote;

        public long MaxSize => m_inner.MaxSize;

        public string[] HashSaved => null;

		#endregion

		#region Bucket methods

		public ObjectInfo this[string name]
		{
			get
			{
				ObjectInfo obj = m_inner[name];
				return GetObjectInfo(obj);
			}
		}

		public List<ObjectInfo> ListObjects()
		{
			return CollectionKit.Transform<ObjectInfo, ObjectInfo>(m_inner.ListObjects(), x => GetObjectInfo(x));
		}

		public ObjectInfo Refresh(string name)
		{
			ObjectInfo obj = m_inner.Refresh(name);
			return GetObjectInfo(obj);
		}

		public bool DeleteObject(string name)
		{
			return m_inner.DeleteObject(name);
		}

		public void DeleteAll()
		{
			m_inner.DeleteAll();
		}

		// fixed-tail-size
		ObjectInfo GetObjectInfo(ObjectInfo inner)
		{
			if (inner != null)
				return new ObjectInfo() { Name = inner.Name, Size = inner.Size };
			else
				return null;
		}

		#endregion

		#region Atom read / write methods

		public ObjectInfo AtomCreate(string name, Stream istream)
		{
			var cstream = m_cr.CreateEncryptStream(name, istream);
			var obj = m_inner.AtomCreate(name, cstream);
			cstream.Close();
			return GetObjectInfo(obj);
		}

		public ObjectInfo AtomCreate(string name, byte[] data)
		{
			data = m_cr.EncryptFinalBlock(name, data);
			var obj = m_inner.AtomCreate(name, data);
			return GetObjectInfo(obj);
		}

		public ObjectInfo AtomCreate(string name, Stream istream, int off, int size)
		{
			throw new NotImplementedException();
		}

		public ObjectInfo AtomCreate(string name, byte[] data, int off, int size)
		{
			var final_data = new byte[size];
			Array.Copy(data, final_data, size);
			return AtomCreate(name, final_data);
		}

		public byte[] AtomRead(string name)
		{
			byte[] data = m_inner.AtomRead(name);
			return m_cr.DecryptFinalBlock(name, data);
		}

		public byte[] AtomRead(string name, long pos, int size)
		{
			return m_cr.DecryptPart(name, (p, s) => m_inner.AtomRead(name, p, s), m_inner[name].Size, pos, size);
		}

		public void AtomWrite(string name, long pos, Stream istream)
		{
			throw new NotImplementedException();
		}

		public void AtomWrite(string name, long pos, Stream istream, int off, int size)
		{
			throw new NotImplementedException();
		}

		public void AtomWrite(string name, long pos, byte[] data, int off, int size)
		{
			throw new NotImplementedException();
		}

		public byte[] ComputeHash(string name, string algorithm)
		{
			// todo
			byte[] data = AtomRead(name);
			return StorageKit.ComputeHash(data, 0, data.Length, algorithm);
		}

		public void WalkData(string name, StorageKit.AcceptDataDelegate f)
		{
			// todo
			byte[] data = AtomRead(name);
			f(data, 0, data.Length);
		}

		#endregion

		#region Stream methods

		public Tuple<ObjectInfo, Stream> CreateObject(string name, long size)
		{
			throw new NotImplementedException();
		}

		public Stream OpenObject(string name, bool writable)
		{
            if (writable)
                throw new AccessViolationException();

            if (m_inner.SupportStream)
            {
                var stream = m_inner.OpenObject(name, writable);
                return m_cr.CreateDecryptStream(name, stream);
            }
            else
            {
                var stream = new ReadonlyStreamOnAtomRead(m_inner, this[name], m_cr);
                return stream;
            }
		}

		#endregion

        public void Close()
        {
            m_cr = null;
            m_inner.Close();
            m_inner = null;
        }
	}

	internal class CryptFileTreeItem : FileTreeItem
	{
		BlockCrypt m_dc;
		FileTreeItem m_parent, m_inner;
		List<FileTreeItem> m_children;

		#region Constructor

		internal CryptFileTreeItem(BlockCrypt dc, CryptFileTreeItem parent, FileTreeItem inner)
		{
			m_dc = dc;
			m_parent = parent;
			m_inner = inner;
			m_children = null;
		}

		void ValidateChildren()
		{
			if (m_children != null)
				return;
			if (!m_inner.IsDir)
				throw new System.IO.IOException("Not a folder");

			List<FileTreeItem> list = m_inner.List();
			m_children = CollectionKit.Transform<FileTreeItem, FileTreeItem>(list, x => new CryptFileTreeItem(m_dc, this, x));
		}

		#endregion

		#region Properties

		public FileTreeItem Parent
		{
			get { return m_parent; }
		}

		public string Name
		{
			get { return m_inner.Name; }
		}

		public bool IsDir
		{
			get { return m_inner.IsDir; }
		}

		public long Size
		{
			get { return m_inner.Size; }
		}

		public DateTime LastWriteTimeUtc
		{
			get { return m_inner.LastWriteTimeUtc; }
			set { m_inner.LastWriteTimeUtc = value; }
		}

		public FileTreeItem this[string name]
		{
			get
			{
				ValidateChildren();
				return m_children.Find(x => x.Name == name);
			}
		}

		#endregion

		#region Dir methods

		public List<FileTreeItem> List()
		{
			ValidateChildren();
			return m_children;
		}

		public void Refresh()
		{
			m_inner.Refresh();
		}

		public void Delete(bool recursive)
		{
			throw new NotImplementedException();
		}

		public FileTreeItem CreateDir(string name)
		{
			throw new NotImplementedException();
		}

		public void DeleteChildren()
		{
			throw new NotImplementedException();
		}

		public void MoveTo(FileTreeItem parent, string name)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Atom methods

		public byte[] AtomRead(long pos, int size)
		{
			return m_dc.TransformPart((p, s) => m_inner.AtomRead(p, s), m_inner.Size, pos, size);
		}

		public byte[] AtomRead()
		{
			byte[] s = m_inner.AtomRead();
			return m_dc.TransformFinalBlock(s);
		}

		public void AtomWrite(long pos, byte[] data, int off, int size)
		{
			throw new NotImplementedException();
		}

		public void AtomWrite(long pos, System.IO.Stream istream, long off, long size)
		{
			throw new NotImplementedException();
		}

		public void AtomWrite(long pos, System.IO.Stream istream)
		{
			throw new NotImplementedException();
		}

		public FileTreeItem AtomCreateChild(string name, byte[] data, int off, int size)
		{
			throw new NotImplementedException();
		}

		public FileTreeItem AtomCreateChild(string name, System.IO.Stream istream, long off, long size)
		{
			throw new NotImplementedException();
		}

		public FileTreeItem AtomCreateChild(string name, byte[] data)
		{
			throw new NotImplementedException();
		}

		public FileTreeItem AtomCreateChild(string name, System.IO.Stream istream)
		{
			throw new NotImplementedException();
		}

		public void WalkData(StorageKit.AcceptDataDelegate f)
		{
			// todo
			byte[] data = AtomRead();
			f(data, 0, data.Length);
		}

		public byte[] ComputeHash(string algorithm)
		{
			// todo
			byte[] data = AtomRead();
			return StorageKit.ComputeHash(data, 0, data.Length, algorithm);
		}

		#endregion

		#region Stream methods

		public Tuple<FileTreeItem, System.IO.Stream> CreateChild(string name, long size)
		{
			throw new NotImplementedException();
		}

		public System.IO.Stream Open(bool writable)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	public class CryptFileTreeAccess : FileTreeAccess
	{
		BlockCrypt m_dc;
		FileTreeAccess m_inner;
		FileTreeItem m_root;

		#region Constructor

		public CryptFileTreeAccess(BlockCrypt dc, FileTreeAccess inner, FileTreeItem root)
		{
			m_dc = dc;
			m_inner = inner;
			m_root = new CryptFileTreeItem(m_dc, null, root);
		}

		#endregion

		#region Properties

		public bool CanCreate
		{
			get { return false; }
		}

		public bool CanResize
		{
			get { return false; }
		}

		public bool CanAppend
		{
			get { return false; }
		}

		public bool CanModify
		{
			get { return false; }
		}

		public bool SupportStream
		{
			get { return false; }
		}

		public bool IsRemote
		{
			get { return m_inner.IsRemote; }
		}

		public long MaxSize
		{
			get { return m_inner.MaxSize; }
		}

		public string[] HashSaved
		{
			get { return null; }
		}

		public FileTreeItem Root
		{
			get { return m_root; }
		}

		#endregion

        public void Close()
        {
            m_root = null;
            m_inner.Close();
            m_inner = null;
            m_dc = null;
        }
	}

}
