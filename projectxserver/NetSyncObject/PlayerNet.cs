using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beetle;

namespace NetSyncObject
{
    [Serializable]
    public class PlayerSync
    {
        public PlayerSync()
        {
            Position = new float[2];
            Password = "69-8D-51-A1-9D-8A-12-1C-E5-81-49-9D-7B-70-16-68";
        }
        public string Name { get; set; }
        public string Password { get; set; }
        public float Speed { get; set; }
        public int HP { get; set; }
        public int MaxHP { get; set; }
        public int ATK { get; set; }
        public int DEF { get; set; }
        public float[] Position { get; set; }
    }
}
