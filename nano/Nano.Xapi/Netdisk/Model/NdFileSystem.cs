using Nano.Json;
using Nano.Json.Expression;
using Nano.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Nano.Xapi.Netdisk.Model
{
    public class NdFsListResponse : NdResponse
    {
        public class Item
        {
            public long Fid;
            public long ParentId;
            public string Name;
            public long Size;
            public long CTime, MTime, ATime;
            public long SMTime, RSMTime;
            public int Attr;
            public string Editor;
            public long Rights;
            public long LockerId;
            public string Locker;
            public string LockerName;
            public string Stor;

            public string Path;
            public string Protocol;

            public void FromJsonNew(JsonNode node)
            {
                var fileInfo = node.GetChildItem("fileInfo");
                ParseInfo(fileInfo);

                var v = node.GetChildItem("path");
                Path = v == null ? null : (string)v;

                v = node.GetChildItem("protocol");
                Protocol = v == null ? null : (string)v;
            }

            public void FromJson(JsonNode node)
            {
                ParseInfo(node);

                var v = node.GetChildItem("path");
                Path = v == null ? null : (string)v;

                v = node.GetChildItem("protocol");
                Protocol = v == null ? null : (string)v;
            }

            void ParseInfo(JsonNode node)
            {
                Fid = node["fid"].IntValue;
                ParentId = node["parent"].IntValue;
                Name = node["name"].TextValue;
                Size = node["size"].IntValue;
                CTime = node["c_ctime"].IntValue;
                MTime = node["c_mtime"].IntValue;
                ATime = node["c_atime"].IntValue;
                SMTime = node["s_mtime"].IntValue;
                RSMTime = node["rs_mtime"].IntValue;
                Attr = (int)node["attr"].IntValue;
                Rights = (long)node["right"].IntValue;

                var v = node.GetChildItem("editor");
                Editor = v == null ? null : (string)v;

                v = node.GetChildItem("lockerName");
                LockerName = v == null ? null : (string)v;

                v = node.GetChildItem("locker");
                if(v == null)
                {
                    LockerId = 0;
                    Locker = null;
                }
                else if (v.NodeType == JsonNodeType.Integer)
                {
                    Locker = LockerName;
                    LockerId = (int)v.IntValue;
                }
                else
                {
                    LockerId = 1;
                    Locker = v.TextValue;
                }


                v = node.GetChildItem("stor");
                Stor = v == null ? null : (string)v;
                Stor = Stor == "" ? null : Stor;
            }

            public JsonNode ToJson()
            {
                JE e = JE.New() + JE.Dict();
                e = e + JE.Pair("fid", Fid);
                e = e + JE.Pair("parent", ParentId);
                e = e + JE.Pair("name", Name);
                e = e + JE.Pair("size", Size);
                e = e + JE.Pair("c_ctime", CTime);
                e = e + JE.Pair("c_mtime", MTime);
                e = e + JE.Pair("c_atime", ATime);
                e = e + JE.Pair("s_mtime", SMTime);
                e = e + JE.Pair("rs_mtime", RSMTime);
                e = e + JE.Pair("attr", Attr);
                e = e + JE.Pair("right", Rights);
                if(Editor != null)
                    e = e + JE.Pair("editor", Editor);
                e = e + JE.Pair("locker", Locker);
                if(LockerName != null)
                    e = e + JE.Pair("lockerName", LockerName);
                if (Stor != null)
                    e = e + JE.Pair("stor", Stor);
                e = e + JE.EDict();
                return e.Complete();
            }
        }

        public long Total;
        public string Order;
        public string Sort;
        public long DirRight;
        public List<Item> Items = null;

        protected override void ParseJson(JsonNode root)
        {
            if (!Succeeded)
                return;

            var v = root.GetChildItem("total");
            Total = v == null ? 0 : v.IntValue;

            v = root.GetChildItem("order");
            Order = v ?? null;

            v = root.GetChildItem("sort");
            Sort = v ?? null;
            
            v = root.GetChildItem("dirRight");
            DirRight = v == null ? 0 : v.IntValue;

            var nItems = root["items"];
            Debug.Assert(nItems.NodeType == JsonNodeType.NodeList);
            Items = new List<Item>();
            foreach (var nItem in nItems.ChildNodes)
            {
                var item = new Item();
                if (nItem.GetChildItem("protocol") == null)
                    item.FromJson(nItem);
                else
                    item.FromJsonNew(nItem);
                Items.Add(item);
            }
        }
    }

    public class NdFsInfoResponse : NdResponse
    {
        public NdFsListResponse.Item Item;

        protected override void ParseJson(JsonNode root)
        {
            if (!Succeeded)
                return;

            Item = new NdFsListResponse.Item();
            var v = root.GetChildItem("pathInfo");
            if (v == null)
            {
                var nInfo = root["fileInfo"];
                Item.FromJson(nInfo);
            }
            else
            {
                Item.FromJsonNew(v);
            }
        }
    }

    public class NdRequestDownloadResponse : NdResponse
    {
        public const int Access_R = 1;
        public const int Access_RW = 3;

        public class Node
        {
            public string Addr;
            public int Weight;
            public int Access;
        }

        public class Part
        {
            public List<Node> Nodes;
            public string Id;
            public int Size;
            public uint Crc;

            public string Decide()
            {
                return Nodes[0].Addr;   // to be optimized
            }
        }

        public List<Part> Parts = null;
        public string Stor = null; //add by eddie

        protected override void ParseJson(JsonNode root)
        {
            if (!Succeeded)
                return;

            var nParts = root["parts"];
            Debug.Assert(nParts.NodeType == JsonNodeType.NodeList);
            Parts = new List<Part>();
            foreach (var nPart in nParts.ChildNodes)
            {
                var part = new Part();
                var nNodes = nPart["nodes"];
                Debug.Assert(nNodes.NodeType == JsonNodeType.NodeList);
                part.Nodes = new List<Node>();
                foreach (var nNode in nNodes.ChildNodes)
                {
                    var node = new Node();
                    node.Addr = nNode["addr"].TextValue;
                    node.Weight = (int)nNode["weight"].IntValue;
                    //node.Access = ToAccess(nNode["access"].TextValue);
                    part.Nodes.Add(node);
                }
                part.Id = nPart["downloadId"].TextValue;
                part.Size = (int)nPart["size"].IntValue;
                part.Crc = (uint)nPart["crc"].IntValue;
                Parts.Add(part);
            }
        }

        static int ToAccess(string s)
        {
            switch (s)
            {
                case "r":
                    return Access_R;
                case "rw":
                    return Access_RW;
                default:
                    throw new ArgumentException("Access=" + s);
            }
        }
    }

    public class NdRequestUploadPartInfo
    {
        public int Size;
        public byte[] Sha1;
        public int Index;
    }

    public class NdRequestUploadResponse : NdResponse
    {
        public class Node
        {
            public string Addr;
        }

        public bool Existed = false;
        public NdFsListResponse.Item Item = null;
        public string UploadId = null;
        public List<Node> Nodes = null;

        public string Decide()
        {
            return Nodes[0].Addr;
        }

        protected override void ParseJson(JsonNode root)
        {
            if (!Succeeded)
                return;

            Existed = root["existed"].BoolValue;

            Item = new NdFsListResponse.Item();

            var fileInfo = root.GetChildItem("fileInfo");
            if(fileInfo != null)
                Item.FromJson(fileInfo);

            var v = root.GetChildItem("fileUploadId");
            UploadId = v?.TextValue;

            v = root.GetChildItem("nodes");
            if (v != null)
            {
                var nNodes = v;
                Debug.Assert(nNodes.NodeType == JsonNodeType.NodeList);
                Nodes = new List<Node>();
                foreach (var nNode in nNodes.ChildNodes)
                {
                    Debug.Assert(nNode.NodeType == JsonNodeType.Dictionary);
                    string addr = nNode["addr"].TextValue;
                    var node = new Node() { Addr = addr };
                    Nodes.Add(node);
                }
            }
        }
    }

    public class NdUploadPartResponse : NdResponse
    {
        public string CommitId = null;

        protected override void ParseJson(JsonNode root)
        {
            if (!Succeeded)
                return;

            CommitId = root["partCommitId"];
        }
    }

    public class NdSyncMTimeResponse : NdResponse
    {
        public class Item
        {
            public long Fid;
            public long SMTime, XSMTime, RSMTime;
        }

        public Dictionary<long, Item> Items;

        protected override void ParseJson(JsonNode root)
        {
            if (!Succeeded)
                return;
            var nMTimes = root["mtimes"];
            Debug.Assert(nMTimes.NodeType == JsonNodeType.Dictionary);
            Items = new Dictionary<long, Item>();
            foreach (var nItem in nMTimes.ChildNodes)
            {
                Item item = new Item();
                item.Fid = long.Parse(nItem.Name);
                item.SMTime = nItem["smtime"].IntValue;
                item.XSMTime = nItem["x_smtime"].IntValue;
                item.RSMTime = nItem["r_smtime"].IntValue;
                Items.Add(item.Fid, item);
            }
        }
    }

    public class NdListGroupResponse : NdResponse
    {
        public class Item
        {
            public long Fid;
            public string Name;
            public int Gid;
            public long CTime, MTime;
            public int Right;
            public int Creator;
            public Dictionary<string, object> Attrs;
        }

        public List<Item> Items = null;

        protected override void ParseJson(JsonNode root)
        {
            if (!Succeeded)
                return;

            var nRows = root["rows"];
            Debug.Assert(nRows.NodeType == JsonNodeType.NodeList);
            Items = new List<Item>();
            foreach (var nRow in nRows.ChildNodes)
            {
                var item = new Item();
                item.Fid = nRow["fid"].IntValue;
                item.Name = nRow["name"].TextValue;
                item.Gid = (int)nRow["gid"].IntValue;
                item.CTime = nRow["ctime"].IntValue;
                item.MTime = nRow["mtime"].IntValue;
                item.Right = (int)nRow["right"].IntValue;
                //item.Creator = (int)nRow["creator"].IntValue;

                var nAttrs = nRow["attrs"];
                Debug.Assert(nAttrs.NodeType == JsonNodeType.Dictionary);
                item.Attrs = new Dictionary<string, object>();
                foreach (var nAttr in nAttrs.ChildNodes)
                    item.Attrs.Add(nAttr.Name, nAttr.Value);

                Items.Add(item);
            }
        }
    }
}
