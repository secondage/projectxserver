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

namespace ProjectXServer
{
    public class Monster : Character
    {
        public Monster(string n, Scene s) :
            base(n, s)
        {
        }

        public Monster() :
            base()
        {
        }

        public override void Update(GameTime gametime)
        {
            base.Update(gametime);
        }

        protected override void UpdateMovement(GameTime gametime)
        {
            Vector2 dir = target - position;
            if (dir.Length() < 15.0f)
            {
                position = target;
                State = CharacterState.Landing;
                SendOnArrived(this);
            }
            else
            {
                dir.Normalize();
                position += ((float)gametime.ElapsedGameTime.TotalSeconds * speed) * dir;
            }
            base.UpdateMovement(gametime);
        }
    }
}
