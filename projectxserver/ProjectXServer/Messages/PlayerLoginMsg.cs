using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Beetle;

namespace ProjectXServer.Messages
{
    public enum LoginResult
    {
        Failed_AlreadyLogin = -2,
        Failed = -1,
        Succeed = 1,
    }


    [ProtoContract]
    public class PlayerLoginMsg
    {
        [ProtoMember(1)]
        public string Name { get; set; }
        [ProtoMember(2)]
        public long ClientID { get; set; }
    }


    [ProtoContract]
    public class PlayerLoginResultMsg
    {
        [ProtoMember(1)]
        public LoginResult Result { get; set; }
        [ProtoMember(2)]
        public long ClientID { get; set; }
    }

}
