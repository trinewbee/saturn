using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nano.Json;
using Nano.Json.Expression;
using System.Diagnostics;


namespace Nano.Xapi.Netdisk
{
    public class NdFileListResponse : Model.NdResponse
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
            public int Rights;
            public long Locker;
            public string LockerName;
            public string Stor;
            public string Path;

            public void FromJson(JsonNode node)
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
                Rights = (int)node["right"].IntValue;
                Locker = (int)node["locker"].IntValue;

                var v = node.GetChildItem("editor");
                Editor = v == null ? null : (string)v;

                v = node.GetChildItem("path");
                Path = v == null ? null : (string)v;

                v = node.GetChildItem("lockerName");
                LockerName = v == null ? null : (string)v;

                var subitem = node.GetChildItem("stor");
                Stor = subitem != null ? subitem.TextValue : null;
                if (Stor == "")
                    Stor = null;
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
                if (Editor != null)
                    e = e + JE.Pair("editor", Editor);
                e = e + JE.Pair("locker", Locker);
                if (LockerName != null)
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
                item.FromJson(nItem);
                Items.Add(item);
            }
        }
    }

    public class NdFileInfoResponse : Model.NdResponse
    {
        public NdFileListResponse.Item Item;

        protected override void ParseJson(JsonNode root)
        {
            if (!Succeeded)
                return;

            var nInfo = root["fileInfo"];
            Item = new NdFileListResponse.Item();
            Item.FromJson(nInfo);
        }
    }

    public class NdFileRequestDownloadResponse : Model.NdResponse
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

    public class NdFileRequestUploadPartInfo
    {
        public int Size;
        public byte[] Sha1;
        public int Index;
    }

    public class NdFileRequestUploadResponse : Model.NdResponse
    {
        public class Node
        {
            public string Addr;
        }

        public bool Existed = false;
        public NdFileListResponse.Item Item = null;
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

            var nInfo = root.GetChildItem("fileInfo");
            if (nInfo != null)
            {
                Item = new NdFileListResponse.Item();
                Item.FromJson(nInfo);
                return;
            }

            UploadId = root["fileUploadId"].TextValue;

            var nNodes = root["nodes"];
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

    public class NdFileUploadPartResponse : Model.NdResponse
    {
        public string CommitId = null;

        protected override void ParseJson(JsonNode root)
        {
            if (!Succeeded)
                return;

            CommitId = root["partCommitId"];
        }
    }

    public class NdFileListGroupResponse : Model.NdResponse
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
