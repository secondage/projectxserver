using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Beetle;

namespace ProjectXServer.MessagesWP7
{
    [ProtoContract]
    public class PlayerLogoutMsg
    {
        [ProtoMember(1)]
        public string Name { get; set; }
        [ProtoMember(2)]
        public long ClientID { get; set; }
    }


}
