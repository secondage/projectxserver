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
    public enum CharacterState
    {
        Spawn,
        Idle,
        Moving,
        Launch,
        Landing,
        Attack,
        Attack2,
        Attack3,
        BeAttack,
        Return,
        Dead,
        Dying,  //垂死
    };
    public enum CharacterActionSetChangeFactor
    {
        AnimationCompleted,
        ArriveTarget,
        ArriveAttackTarget,
        ArriveInteractiveTarget,
        EffectCompleted,
        Immediate,
        Time,
    };
    public class CharacterActionSet
    {
        public CharacterActionSet(string n, CharacterState s, CharacterActionSetChangeFactor f, object o)
        {
            animname = n;
            state = s;
            switch (f)
            {
                case CharacterActionSetChangeFactor.AnimationCompleted:
                    break;
                case CharacterActionSetChangeFactor.ArriveTarget:
                    target = (Vector2)o;
                    break;
                case CharacterActionSetChangeFactor.Time:
                    duration = (double)o;
                    break;
                case CharacterActionSetChangeFactor.ArriveAttackTarget:
                    interactive = (Character)o;
                    break;
                case CharacterActionSetChangeFactor.ArriveInteractiveTarget:
                    interactive = (Character)o;
                    break;
                case CharacterActionSetChangeFactor.EffectCompleted:
                    effectname = (string)o;
                    break;
            }
            factor = f;

        }
        public string animname;
        public CharacterState state;
        public double duration;
        public CharacterActionSetChangeFactor factor;
        public Vector2 target = new Vector2();
        public Character interactive;
        public string effectname;
    };

    public class ActionOrder : IComparable
    {
        public Character character;
        int speed;

        public ActionOrder(Character c, int s)
        {
            character = c;
            speed = s;
        }

        public int CompareTo(object obj)
        {
            ActionOrder c = obj as ActionOrder;
            return speed - c.speed;
        }
    }


    public class Character
    {
        public enum DirMethod
        {
            AutoDectect = 0,
            Fixed,
        }


        public enum OperateType
        {
            None,
            Attack,
            Magic,
            Item,
        };

        public event EventHandler OnActionCompleted;
        public event EventHandler OnArrived;
        //public event EventHandler OnActionSetsOver;

        protected int templateid;

        protected int hp = 100;
        protected int maxhp = 100;
        protected int atk = 10;
        protected int def = 10;

        protected Vector2 position = new Vector2();
        protected Vector2 positionbackup = new Vector2();
        protected string name;
        protected Vector2 target;
        protected float speed = 1.0f;
        protected CharacterState state = CharacterState.Idle;
        protected Scene scene;
        protected Character attacktarget;
        protected OperateType op = OperateType.None;   //回合操作 
        protected Character optarget; //回合操作对象
        protected Character interactivetarget;
        protected Vector2 fixedfacedir = new Vector2(1, 0);
        protected DirMethod facedirmethod = DirMethod.AutoDectect;

        private List<CharacterActionSet> actionsets = new List<CharacterActionSet>();
        private CharacterActionSet currentactionset = null;

        public DateTime MoveStartTime { get; set; }

        public Character(string n, Scene s)
        {
            name = n;
            scene = s;
        }

        public Character()
        {
        }

        public int TemplateID
        {
            get
            {
                return templateid;
            }
            set
            {
                templateid = value;
            }
        }

        public int Layer { get; set; }

        public virtual bool NeedTitle
        {
            get
            {
                return true;
            }
        }

        public ActionOrder Order { get; set; }

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

        public int HP
        {
            get
            {
                return hp;
            }
            set
            {
                hp = value;
            }
        }

        public int MaxHP
        {
            get
            {
                return maxhp;
            }
            set
            {
                maxhp = value;
            }
        }

        public int ATK
        {
            get
            {
                return atk;
            }
            set
            {
                atk = value;
            }
        }

        public int DEF
        {
            get
            {
                return def;
            }
            set
            {
                def = value;
            }
        }

        public float Speed
        {
            get
            {
                return speed;
            }
            set
            {
                speed = value;
            }
        }



        public Vector2 FixedDir
        {
            get
            {
                return fixedfacedir;
            }
            set
            {
                fixedfacedir = value;
            }
        }

        public DirMethod FaceDirMethod
        {
            get
            {
                return facedirmethod;
            }
            set
            {
                facedirmethod = value;
            }
        }

        public Vector2 Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }

        public OperateType Operate
        {
            get
            {
                return op;
            }
            set
            {
                op = value;
            }
        }

        public Character OperateTarget
        {
            get
            {
                return optarget;
            }
            set
            {
                optarget = value;
            }
        }

        public Vector2 Target
        {
            get
            {
                return target;
            }
            set
            {
                target = value;
            }
        }


        public Scene Scene
        {
            get
            {
                return scene;
            }
            set
            {
                scene = value;
            }
        }




        public CharacterState State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
                switch (state)
                {
                    case CharacterState.Launch:
                        {
                            break;
                        }
                    case CharacterState.Moving:
                        {
                            break;
                        }
                    case CharacterState.Idle:
                        {
                            break;
                        }
                    case CharacterState.Spawn:
                        {
                            break;
                        }
                    case CharacterState.Landing:
                        {
                            break;
                        }
                    case CharacterState.Attack:
                        {
                            break;
                        }
                    case CharacterState.Dying:
                        {
                            break;
                        }
                    case CharacterState.Dead:
                        {
                            break;
                        }
                }
            }
        }


        protected virtual void SendOnArrived(object sender)
        {
            if (OnArrived != null)
            {
                OnArrived(sender, new EventArgs());
                OnArrived = null;
            }
        }

        public virtual void Update(GameTime gametime)
        {
            try
            {
                UpdateActionSet(gametime);
                if (state == CharacterState.Moving)
                    UpdateMovement(gametime);

            }
            catch
            {
                Console.WriteLine(ToString() + "::Update");
            }
        }


        protected virtual void UpdateMovement(GameTime gametime)
        {

        }

        public void PushPosition()
        {
            positionbackup = Position;
        }

        public void PopPosition()
        {
            Position = positionbackup;
        }

        public void ClearActionSet()
        {
            actionsets.Clear();
            currentactionset = null;
        }

        public void AddActionSet(string animname, CharacterState state, CharacterActionSetChangeFactor factor, object o)
        {
            CharacterActionSet cas = new CharacterActionSet(animname, state, factor, o);
            actionsets.Add(cas);
        }

        public void AddActionSetPre(string animname, CharacterState state, CharacterActionSetChangeFactor factor, object o)
        {
            CharacterActionSet cas = new CharacterActionSet(animname, state, factor, o);
            actionsets.Insert(0, cas);
        }

        private void UpdateActionSet(GameTime gametime)
        {
            while (actionsets.Count > 0 && currentactionset == null)
            {
                currentactionset = actionsets[0];
                Console.WriteLine(string.Format("Now action is {0}", currentactionset.animname));
                State = currentactionset.state;
                switch (currentactionset.factor)
                {
                    case CharacterActionSetChangeFactor.AnimationCompleted:
                        {
                            break;
                        }
                    case CharacterActionSetChangeFactor.ArriveTarget:
                        {
                            Target = currentactionset.target;
                            OnArrived += new EventHandler(OnUpdateActionSets);
                            break;
                        }
                    case CharacterActionSetChangeFactor.ArriveAttackTarget:
                        {
                            break;
                        }
                    case CharacterActionSetChangeFactor.ArriveInteractiveTarget:
                        {
                            break;
                        }
                    case CharacterActionSetChangeFactor.EffectCompleted:
                        {
                            break;
                        }
                    case CharacterActionSetChangeFactor.Immediate:
                        {
                            OnUpdateActionSets(this, new EventArgs());
                            break;
                        }
                }
            }

            if (currentactionset != null)
            {

            }
        }


        private void OnUpdateActionSets(object sender, EventArgs e)
        {
            if (actionsets.Count > 0)
            {
                actionsets.RemoveAt(0);
                if (actionsets.Count == 0 && currentactionset != null)
                {
                    if (OnActionCompleted != null && currentactionset.factor != CharacterActionSetChangeFactor.Immediate)
                    {
                        OnActionCompleted(this, new EventArgs());
                        //OnActionCompleted = null;
                    }
                }
            }
            currentactionset = null;
        }

        
    }


}
