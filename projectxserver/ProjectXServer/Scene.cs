using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Xml;
using System.Diagnostics;

namespace ProjectXServer
{
    public class Scene
    {
        public enum SceneState
        {
            Map,
            Battle,
            ToMap,
            ToBattle,
        };

        enum Turn
        {
            Player,
            Enemy,
        };

        private Vector4 viewport;
        private Vector4 actualSize;
        
        private SceneState state = SceneState.Map;

        private Random random = new Random();

        private List<Player> netplayers = new List<Player>();
        private List<Character> characters = new List<Character>();
        private List<Character> battlecharacters = new List<Character>();
        private List<Spell> spells = new List<Spell>();
        private string name;
       
        private Vector2 wind = new Vector2(1, 0);
        private Player player;

        public Scene(string _name)
        {
            name = _name;
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public SceneState State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
            }
        }

        public Player Player
        {
            get
            {
                return player;
            }
            set
            {
                player = value;
                player.OnActionCompleted += new EventHandler(Player_OnActionCompleted);
            }
        }




        public Vector4 Viewport
        {
            get
            {
                return viewport;
            }
            set
            {
                viewport = value;
            }
        }

        public Vector4 ActualSize
        {
            get
            {
                return actualSize;
            }
            set
            {
                actualSize = value;
            }
        }

        public Vector2 Wind
        {
            get
            {
                return wind;
            }
            set
            {
                wind = value;
                Vector2.Normalize(wind);
            }
        }

        public void Update(GameTime gametime)
        {
            if (state == SceneState.Map)
            {
                foreach (Character ch in characters)
                {
                    if (!(ch is Player))
                    {
                       ch.Update(gametime);
                    }
                }
                if (player != null)
                {
                    player.Update(gametime);
                }
                foreach (Player p in netplayers)
                {
                    p.Update(gametime);
                }
            }
            else if (state == SceneState.Battle)
            {
                foreach (Character ch in characters)
                {
                    if (ch is Player)
                        ch.Update(gametime);
                }
                foreach (Character ch in battlecharacters)
                {
                    ch.Update(gametime);
                }
                foreach (Spell s in spells)
                {
                    s.Update(gametime);
                }
        
            }

          

          
        }

        public Character GetCharacterByName(string name)
        {
            foreach (Character ch in characters)
            {
                if (ch.Name == name)
                    return ch;
            }
            return null;
        }


        public Player FindNetPlayer(long clientid)
        {
            if (netplayers.Count == 0)
                return null;
            foreach (Player p in netplayers)
            {
                if (p.ClientID == clientid)
                    return p;
            }
            return null;
        }

        public void DelNetPlayer(Player ch)
        {
            netplayers.Remove(ch);
        }

        public void AddNetPlayer(Player ch)
        {
            netplayers.Add(ch);
        }

        public void AddCharacter(Character ch)
        {
            characters.Add(ch);
        }

        public void AddMonster(Character ch)
        {
            battlecharacters.Add(ch);
        }

        public void AddSpell(Spell ch)
        {
            spells.Add(ch);
        }

        public void SetViewportPos(float x, float y)
        {
            viewport.X = x;
            viewport.Y = y;
        }

        public void SetViewportSize(float width, float height)
        {
            viewport.Z = width;
            viewport.W = height;
        }

    

     
   


        protected void Spell_OnActionCompleted(object sender, EventArgs e)
        {
            Spell spell = sender as Spell;
                    
        }


        protected void Monster_OnActionCompleted(object sender, EventArgs e)
        {
            Monster monster = sender as Monster;
            
        }



        protected void Player_OnActionCompleted(object sender, EventArgs e)
        {
            
        }

        protected void Player_OnArrived(object sender, EventArgs e)
        {
            
        }
   }
}
