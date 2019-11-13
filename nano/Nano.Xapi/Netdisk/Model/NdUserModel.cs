using Nano.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nano.Xapi.Netdisk.Model
{
    public class NdGetUserInfoResponse : NdResponse
    {
        public long Uid;
        public long Cid;
        public long FsId;
        public string Account;
        public string Name;
        public string NickName;
        public long Reserved_1;
        public long Status;
        public long CTime;
        public long MTime;
        public string Email;
        public string Protocol;

        Dictionary<string, object> Attrs;
        protected override void ParseJson(JsonNode root)
        {
            if (!Succeeded)
                return;


            var v = root.GetChildItem("protocol");
            Protocol = v == null ? null : v.TextValue;

            var nUser = root["user"];

            Uid = nUser["uid"].IntValue;
            Cid = nUser["cid"].IntValue;
            FsId = nUser["fsid"].IntValue;

            Account = nUser["account"].TextValue;
            Name = nUser["name"].TextValue;

            v = nUser.GetChildItem("nickName");
            NickName = v == null ? null : (v.TextValue);

            Reserved_1 = nUser["reserved_1"].IntValue;
            Status = nUser["status"].IntValue;
            CTime = nUser["ctime"].IntValue;
            MTime = nUser["mtime"].IntValue;

            v = nUser.GetChildItem("email");
            Email = v == null ? null : (v.TextValue);

            Attrs = ParseAttrs(nUser["attrs"]);
        }

        Dictionary<string, object> ParseAttrs(JsonNode attrs)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            foreach (var attr in attrs.ChildNodes)
            {
                ret.Add(attr.Name, attr.Value);
            }
            return ret;
        } 
    }
}
