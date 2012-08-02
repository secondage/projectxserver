using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beetle;
using NetSyncObject;
using Microsoft.Xna.Framework;

namespace ProjectXServer
{
    class Program
    {
        static private Dictionary<string, Player> players_byname = new Dictionary<string, Player>();
        static private Dictionary<long, Player> players_byid = new Dictionary<long, Player>();
        static Scene scene = new Scene("scene1");
        static Random random = new Random();
        static GameTime gametime = new GameTime();
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
            
            System.Threading.Thread.Sleep(-1);
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
                players_byname.Remove(pn.Name);
                players_byid.Remove(e.Channel.ClientID);
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
                            Player pl = new Player(plm_cs.Name, scene);
                            pl.Channel = e.Channel;
                            pl.ClientID = e.Channel.ClientID;
                            pl.Position = new Vector2((float)random.NextDouble() * 1000.0f + 100.0f,
                                                                (float)random.NextDouble() * 1000.0f + 100.0f);
                            pl.Speed = 200.0f;
                            //send succeed msg
                            Messages.PlayerLoginResultMsg plrm = new Messages.PlayerLoginResultMsg();
                            plrm.Result = Messages.LoginResult.Succeed;
                            plrm.ClientID = e.Channel.ClientID;
                            Messages.ProtobufAdapter.Send(e.Channel, plrm);
                            //add player to list
                            players_byname[pl.Name] = pl;
                            players_byid[pl.ClientID] = pl;

                            Messages.PlayerLoginMsg plm_sc = new Messages.PlayerLoginMsg();
                            plm_sc.Position = new float[2];
                            foreach (KeyValuePair<string, Player> sp in players_byname)
                            {
                                plm_sc.ClientID = sp.Value.ClientID;
                                plm_sc.Name = sp.Value.Name;
                                plm_sc.Position[0] = sp.Value.Position.X;
                                plm_sc.Position[1] = sp.Value.Position.Y;
                                plm_sc.Speed = sp.Value.Speed;
                                Messages.ProtobufAdapter.Send(e.Channel, plm_sc);
                            }
                            //send player login to other client
                            foreach (KeyValuePair<string, Player> sp in players_byname)
                            {
                                if (sp.Value.ClientID != pl.ClientID)
                                {
                                    //Messages.PlayerLoginMsg plm_sc = new Messages.PlayerLoginMsg();
                                    plm_sc.ClientID = pl.ClientID;
                                    plm_sc.Name = pl.Name;
                                    plm_sc.Position[0] = pl.Position.X;
                                    plm_sc.Position[1] = pl.Position.Y;
                                    plm_sc.Speed = pl.Speed;
                                    Messages.ProtobufAdapter.Send(sp.Value.Channel, plm_sc);
                                }
                            }
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
                            player.Target = new Vector2(msg_cs.Target[0], msg_cs.Target[1]);
                            player.MoveStartTime = DateTime.Now;
                            
                            //Messages.PlayerLoginRequestMsg msg_cs
                            Messages.PlayerPositionUpdate msg_sc = new Messages.PlayerPositionUpdate();
                            msg_sc.ClientID = player.ClientID;
                            msg_sc.Position = new float[2];
                            msg_sc.Position[0] = player.Position.X;
                            msg_sc.Position[1] = player.Position.Y;
                            msg_sc.State = (int)player.State;
                            foreach (KeyValuePair<string, Player> sp in players_byname)
                            {
                                if (sp.Value.ClientID != player.ClientID)
                                {
                                    Messages.ProtobufAdapter.Send(sp.Value.Channel, msg_sc);
                                }
                            }
                        }
                        break;
                    }
                case "PlayerPositioReport":
                    {
                        Messages.PlayerPositioReport msg_cs = (Messages.PlayerPositioReport)adapter.Message;
                        if (players_byid.ContainsKey(e.Channel.ClientID))
                        {
                            Player player = players_byid[e.Channel.ClientID];
                            if (player.State != CharacterState.Moving)
                            {
                                return;
                            }
                            double dur = (DateTime.Now - player.MoveStartTime).TotalSeconds;
                            player.MoveStartTime = DateTime.Now;
                            Console.WriteLine("time dur is {0:f}", dur);
                            Vector2 dir = player.Target - player.Position;
                            dir.Normalize();
                            Vector2 newpos = player.Position + dir * (float)(player.Speed * dur);
                            float error = Vector2.Distance(newpos, new Vector2(msg_cs.Position[0], msg_cs.Position[1]));
                            bool needcorrect = false;
                            if (error > 32.0f)
                            {
                                needcorrect = true;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Player {0:s} position {1:f},{2:f} is incorrect, error {3:f}", player.Name, msg_cs.Position[0], msg_cs.Position[1], error);
                                Console.ResetColor();
                            }
                            Messages.PlayerPositionUpdate msg_sc = new Messages.PlayerPositionUpdate();
                            msg_sc.ClientID = player.ClientID;
                            msg_sc.Position = new float[2];
                            msg_sc.Position[0] = needcorrect ? newpos.X : msg_cs.Position[0];
                            msg_sc.Position[1] = needcorrect ? newpos.Y : msg_cs.Position[1];
                            msg_sc.State = (int)player.State;
                            foreach (KeyValuePair<string, Player> sp in players_byname)
                            {
                                if (needcorrect || sp.Value.ClientID != player.ClientID)
                                {
                                    Messages.ProtobufAdapter.Send(sp.Value.Channel, msg_sc);
                                }
                            }
                            player.Position = needcorrect ? newpos : new Vector2(msg_cs.Position[0], msg_cs.Position[1]);
                        }
                        break;
                    }
            }

        }

        static void Main(string[] args)
        {
            StartServer();
        }
    }
}
