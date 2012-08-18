using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beetle;
using NetSyncObject;
using Microsoft.Xna.Framework;
using System.IO;
using System.Xml.Serialization;

namespace ProjectXServer
{
    class Program
    {
        static private Dictionary<string, Player> players_byname = new Dictionary<string, Player>();
        static private Dictionary<long, Player> players_byid = new Dictionary<long, Player>();
        static Scene scene = new Scene("scene1");
        static Random random = new Random();
        static GameTime gametime = new GameTime();
        static object lockobject = new object();
        static void StartServer()
        {
            gametime = new GameTime();
            Console.WriteLine("License:");
            Console.Write(Beetle.LICENSE.GetLICENSE());
            TcpUtils.Setup(100, 1, 1);
            TcpServer server = new TcpServer();
            server.ChannelConnected += OnConnected;
            server.ChannelDisposed += OnDisposed;
            server.Open(9610);

            
        }


        static void OnConnected(object sender, ChannelEventArgs e)
        {
            e.Channel.SetPackage<Messages.HeadSizePackage>().ReceiveMessage = OnMessageReceive;
            e.Channel.BeginReceive();
            Console.WriteLine("{0} connected.", e.Channel.EndPoint);

        }

        static void OnDisposed(object sender, ChannelEventArgs e)
        {
            Console.WriteLine("{0} disconnected.", e.Channel.EndPoint);
            if (players_byid.ContainsKey(e.Channel.ClientID))
            {
                Player pn = players_byid[e.Channel.ClientID];
                foreach (KeyValuePair<string, Player> sp in players_byname)
                {
                    if (sp.Value.ClientID != e.Channel.ClientID)
                    {
                        Messages.PlayerLogoutMsg plm_sc = new Messages.PlayerLogoutMsg();
                        plm_sc.ClientID = e.Channel.ClientID;
                        plm_sc.Name = pn.Name;
                        Messages.ProtobufAdapter.Send(sp.Value.Channel, plm_sc);
                    }
                }

                using (FileStream fs = new FileStream(string.Format(@"accounts/{0:s}.xml", pn.Name), FileMode.Create))
                {
                    PlayerSync ps = new PlayerSync();
                    ps.Name = pn.Name;
                    ps.DEF = pn.DEF;
                    ps.ATK = pn.ATK;
                    ps.MaxHP = pn.MaxHP;
                    ps.HP = pn.HP;
                    ps.Speed = pn.Speed;
                    ps.Position[0] = pn.Position.X;
                    ps.Position[1] = pn.Position.Y;
                    XmlSerializer formatter = new XmlSerializer(typeof(PlayerSync));
                    formatter.Serialize(fs, ps);
                }

                lock (lockobject)
                {
                    players_byname.Remove(pn.Name);
                    players_byid.Remove(e.Channel.ClientID);
                }
                pn = null;
            }
        }


        static void OnMessageReceive(PacketRecieveMessagerArgs e)
        {
            Messages.ProtobufAdapter adapter = (Messages.ProtobufAdapter)e.Message;
            Type type = adapter.Message.GetType();
            switch (type.Name)
            {
                case "PlayerLoginRequestMsg":
                    {
                        Messages.PlayerLoginRequestMsg plm_cs = (Messages.PlayerLoginRequestMsg)adapter.Message;
                        if (players_byname.ContainsKey(plm_cs.Name))
                        {
                            Messages.PlayerLoginResultMsg plrm = new Messages.PlayerLoginResultMsg();
                            plrm.Result = Messages.LoginResult.Failed_AlreadyLogin;
                            Messages.ProtobufAdapter.Send(e.Channel, plrm);
                        }
                        else
                        {
                            //get accout infomation
                            PlayerSync ps = null;
                            try
                            {
                                using (FileStream fs = new FileStream(string.Format(@"accounts/{0:s}.xml", plm_cs.Name), FileMode.Open))
                                {
                                    XmlSerializer formatter = new XmlSerializer(typeof(PlayerSync));
                                    ps = formatter.Deserialize(fs) as PlayerSync;
                                }
                            }
                            catch (FileNotFoundException)
                            {
                                ps = null;
                            }
                            catch (FileLoadException)
                            {
                                ps = null;
                            }
                            if (ps != null)
                            {
                                //check password
                                if (ps.Password != plm_cs.Password)
                                {
                                    Messages.PlayerLoginResultMsg plrm = new Messages.PlayerLoginResultMsg();
                                    plrm.Result = Messages.LoginResult.Failed_Password;
                                    Messages.ProtobufAdapter.Send(e.Channel, plrm);
                                    break;
                                }
                                else
                                {
                                    Player pl = new Player(plm_cs.Name, scene);
                                    pl.Channel = e.Channel;
                                    pl.ClientID = e.Channel.ClientID;

                                    //pl.Position = new Vector2((float)random.NextDouble() * 1000.0f + 100.0f,
                                    //                                    (float)random.NextDouble() * 1000.0f + 100.0f);
                                    // pl.Speed = 200.0f;
                                    pl.Position = new Vector2(ps.Position[0], ps.Position[1]);
                                    pl.Speed = ps.Speed;
                                    pl.HP = ps.HP;
                                    pl.MaxHP = ps.MaxHP;
                                    pl.Name = ps.Name;
                                    pl.ATK = ps.ATK;
                                    pl.DEF = ps.DEF;

                                    //send succeed msg
                                    Messages.PlayerLoginResultMsg plrm = new Messages.PlayerLoginResultMsg();
                                    plrm.Result = Messages.LoginResult.Succeed;
                                    plrm.ClientID = e.Channel.ClientID;
                                    Messages.ProtobufAdapter.Send(e.Channel, plrm);
                                    //add player to list
                                    lock (lockobject)
                                    {
                                        players_byname[pl.Name] = pl;
                                        players_byid[pl.ClientID] = pl;
                                    }

                                    foreach (KeyValuePair<string, Player> sp in players_byname)
                                    {
                                        Messages.PlayerLoginMsg plm_sc = new Messages.PlayerLoginMsg();
                                        plm_sc.Position = new float[2];
                                        plm_sc.ClientID = sp.Value.ClientID;
                                        plm_sc.Name = sp.Value.Name;
                                        plm_sc.Position[0] = sp.Value.Position.X;
                                        plm_sc.Position[1] = sp.Value.Position.Y;
                                        plm_sc.Speed = sp.Value.Speed;
                                        plm_sc.ATK = sp.Value.ATK;
                                        plm_sc.DEF = sp.Value.DEF;
                                        plm_sc.HP = sp.Value.HP;
                                        plm_sc.MaxHP = sp.Value.MaxHP;
                                        Messages.ProtobufAdapter.Send(e.Channel, plm_sc);
                                    }
                                    //send player login to other client
                                    foreach (KeyValuePair<string, Player> sp in players_byname)
                                    {
                                        if (sp.Value.ClientID != pl.ClientID)
                                        {
                                            Messages.PlayerLoginMsg plm_sc = new Messages.PlayerLoginMsg();
                                            plm_sc.Position = new float[2];
                                            plm_sc.ClientID = pl.ClientID;
                                            plm_sc.Name = pl.Name;
                                            plm_sc.Position[0] = pl.Position.X;
                                            plm_sc.Position[1] = pl.Position.Y;
                                            plm_sc.Speed = pl.Speed;
                                            plm_sc.ATK = pl.ATK;
                                            plm_sc.DEF = pl.DEF;
                                            plm_sc.HP = pl.HP;
                                            plm_sc.MaxHP = pl.MaxHP;
                                            Messages.ProtobufAdapter.Send(sp.Value.Channel, plm_sc);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Messages.PlayerLoginResultMsg plrm = new Messages.PlayerLoginResultMsg();
                                plrm.Result = Messages.LoginResult.Failed_Notfound;
                                Messages.ProtobufAdapter.Send(e.Channel, plrm);
                            }
                        }
                        break;
                    }
                case "PlayerTargetChanged":
                    {
                        Messages.PlayerTargetChanged msg_cs = (Messages.PlayerTargetChanged)adapter.Message;
                        if (players_byid.ContainsKey(e.Channel.ClientID))
                        {

                            Player player = players_byid[e.Channel.ClientID];
                            Console.WriteLine("Player {0:s} PlayerTargetChanged ", player.Name);
                            if (player.State != CharacterState.Moving)
                            {
                                CorrectPlayerPoistionToOrigin(player);
                                return;
                            }
                            player.Timer.Stop();
                            double dur = player.Timer.Duration;
                            Vector2 dir = player.Position - player.PositionBackup;
                            float s = dir.Length();
                            float time = s / player.Speed;
                            if (time - (float)dur > 0.1)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("PTC : Player {0:s} error is {1:f}", player.Name, time - (float)dur);
                                Console.ResetColor();
                                CorrectPlayerPoistionToOrigin(player);
                                return;
                            }
                            player.Target = new Vector2(msg_cs.Target[0], msg_cs.Target[1]);
                            player.Position = new Vector2(msg_cs.Position[0], msg_cs.Position[1]);
                            player.PushPosition();

                            foreach (KeyValuePair<string, Player> sp in players_byname)
                            {
                                if (sp.Value.ClientID != player.ClientID)
                                {
                                    Messages.PlayerTargetChanged msg_sc = new Messages.PlayerTargetChanged();
                                    msg_sc.ClientID = player.ClientID;
                                    msg_sc.Target = new float[2];
                                    msg_sc.Target[0] = player.Target.X;
                                    msg_sc.Target[1] = player.Target.Y;
                                    msg_sc.Position = new float[2];
                                    msg_sc.Position[0] = player.Position.X;
                                    msg_sc.Position[1] = player.Position.Y;
                                    Messages.ProtobufAdapter.Send(sp.Value.Channel, msg_sc);
                                }
                            }
                            player.Timer.Start();
                        }
                        break;
                    }
                case "PlayerMoveRequest":
                    {
                        Messages.PlayerMoveRequest msg_cs = (Messages.PlayerMoveRequest)adapter.Message;
                        if (players_byid.ContainsKey(e.Channel.ClientID))
                        {
                            Player player = players_byid[e.Channel.ClientID];
                            player.State = CharacterState.Moving;

                            Vector2 p = new Vector2(msg_cs.Position[0], msg_cs.Position[1]);
                            if (Vector2.Distance(p, player.Position) > 5.0f)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("PMR : Player {0:s}", player.Name);
                                Console.ResetColor();
                                CorrectPlayerPoistionToOrigin(player);
                                return;
                            }
                            player.Target = new Vector2(msg_cs.Target[0], msg_cs.Target[1]);

                            Console.WriteLine("Player {0:s} PlayerMoveRequest ", player.Name);
                            player.PushPosition();

                            //fixme:
                            //verifying the position and target
                            foreach (KeyValuePair<string, Player> sp in players_byname)
                            {
                                if (sp.Value.ClientID != player.ClientID)
                                {
                                    Messages.PlayerMoveRequest msg_sc = new Messages.PlayerMoveRequest();
                                    msg_sc.ClientID = player.ClientID;
                                    msg_sc.Target = new float[2];
                                    msg_sc.Target[0] = player.Target.X;
                                    msg_sc.Target[1] = player.Target.Y;
                                    Messages.ProtobufAdapter.Send(sp.Value.Channel, msg_sc);
                                }
                            }
                            player.Timer.Start();
                        }
                        break;
                    }
                case "PlayerStopRequest":
                    {
                        Messages.PlayerStopRequest msg_cs = (Messages.PlayerStopRequest)adapter.Message;
                        if (players_byid.ContainsKey(e.Channel.ClientID))
                        {
                            Player player = players_byid[e.Channel.ClientID];
                            Console.WriteLine("Player {0:s} PlayerStopRequest ", player.Name);
                            if (player.State != CharacterState.Moving)
                            {
                                CorrectPlayerPoistionToOrigin(player);
                                return;
                            }
                            player.Timer.Stop();
                            double dur = player.Timer.Duration;
                            Vector2 dir = player.Target - player.PositionBackup;
                            float s = dir.Length() - 15.0f;  //扣除到达目标点误差
                            float time = s / player.Speed;
                            if (time - (float)dur > 0.1)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("PSR : Player {0:s}", player.Name);
                                Console.ResetColor();
                                CorrectPlayerPoistionToOrigin(player);
                                return;
                            }
                            player.Direction = dir.X >= 0 ? 1 : -1;
                            player.Position = player.Target;
                            BroadcastPlayerPosition(player);
                        }
                        break;
                    }
                case "PlayerPositioReport":
                    {
                        /*Messages.PlayerPositioReport msg_cs = (Messages.PlayerPositioReport)adapter.Message;
                        if (players_byid.ContainsKey(e.Channel.ClientID))
                        {
                            Player player = players_byid[e.Channel.ClientID];
                            if (player.State != CharacterState.Moving)
                            {
                                CorrectPlayerPoistionToOrigin(player);
                                return;
                            }
                            double dur = player.Timer.GetTotalDuration();
                            Vector2 dir = player.Target - player.PositionBackup;
                            float s = dir.Length();
                            float time = s / player.Speed;
                            if (time < (float)dur)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("PPR : Player {0:s} ", player.Name);
                                Console.ResetColor();
                                CorrectPlayerPoistionToOrigin(player);
                                return;
                            }
                            player.Position = new Vector2(msg_cs.Position[0], msg_cs.Position[1]);
                            BroadcastPlayerPosition(player);
                        }*/
                        break;
                    }
            }

        }

        static void CorrectPlayerPoistionToOrigin(Player player)
        {
            foreach (KeyValuePair<string, Player> sp in players_byname)
            {
                Messages.PlayerPositionUpdate msg_sc = new Messages.PlayerPositionUpdate();
                msg_sc.ClientID = player.ClientID;
                player.PopPosition();
                player.State = CharacterState.Idle;
                msg_sc.Position = new float[2];
                msg_sc.Position[0] = player.Position.X;
                msg_sc.Position[1] = player.Position.Y;
                msg_sc.State = (int)CharacterState.Correct;
                Messages.ProtobufAdapter.Send(sp.Value.Channel, msg_sc);
            }
        }


        static void BroadcastPlayerPosition(Player player)
        {
            foreach (KeyValuePair<string, Player> sp in players_byname)
            {
                Messages.PlayerPositionUpdate msg_sc = new Messages.PlayerPositionUpdate();
                msg_sc.ClientID = player.ClientID;
                msg_sc.Position = new float[2];
                msg_sc.Position[0] = player.Position.X;
                msg_sc.Position[1] = player.Position.Y;
                msg_sc.State = (int)CharacterState.Idle;
                msg_sc.Dir = player.Direction;
                Messages.ProtobufAdapter.Send(sp.Value.Channel, msg_sc);
            }
        }


        static void Main(string[] args)
        {
            StartServer();
            HiPerfTimer timer = new HiPerfTimer();
            timer.Start();
            double durtime = 0;
            double totaltime = 0;
            while (true)
            {
                //System.Threading.Thread.Sleep(0);
                durtime += timer.GetDuration();
                if (durtime >= 1.0)
                {
                    totaltime += durtime;
                    //Console.WriteLine("time is {0:f}", durtime);
                    Messages.PlayerTimeSyncMsg msg_sc = new Messages.PlayerTimeSyncMsg();
                    msg_sc.Total = totaltime;
                    msg_sc.Duration = durtime;
                    foreach (KeyValuePair<string, Player> sp in players_byname)
                    {
                        Messages.ProtobufAdapter.Send(sp.Value.Channel, msg_sc);
                    }

                    durtime = 0;
                }
                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
