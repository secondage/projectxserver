﻿using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Beetle;
using Microsoft.Xna.Framework;

namespace ProjectXServer.Messages
{
    [ProtoContract]
    public class PlayerMoveRequest
    {
        [ProtoMember(1)]
        public float[] Target { get; set; }
    }


    [ProtoContract]
    public class PlayerPositionUpdate
    {
        [ProtoMember(1)]
        public long ClientID { get; set; }
        [ProtoMember(2)]
        public float[] Position { get; set; }
        [ProtoMember(3)]
        public int State { get; set; }
    }

    [ProtoContract]
    public class PlayerPositioReport
    {
        [ProtoMember(1)]
        public float[] Position { get; set; }
    }

    

}
