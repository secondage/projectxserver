using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beetle;

namespace NetSyncObject
{
    public class PlayerLogin
    {
        public long ClientID { get; set; }
        public TcpChannel Channel { get; set; }
    }
}
