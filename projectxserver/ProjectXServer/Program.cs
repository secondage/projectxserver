using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beetle;
using NetSyncObject;

namespace ProjectXServer
{
    class Program
    {
        static private Dictionary<string, PlayerNet> players_byname = new Dictionary<string, PlayerNet>();
        static private Dictionary<long, PlayerNet> players_byid = new Dictionary<long, PlayerNet>();

        static void StartServer()
        {
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
                PlayerNet pn = players_byid[e.Channel.ClientID];
                foreach (KeyValuePair<string, PlayerNet> sp in players_byname)
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
            if (adapter.Message is Messages.PlayerLoginMsg)
            {
                Messages.PlayerLoginMsg plm_cs = (Messages.PlayerLoginMsg)adapter.Message;
                if (players_byname.ContainsKey(plm_cs.Name))
                {
                    Messages.PlayerLoginResultMsg plrm = new Messages.PlayerLoginResultMsg();
                    plrm.Result = Messages.LoginResult.Failed_AlreadyLogin;
                    Messages.ProtobufAdapter.Send(e.Channel, plrm);
                }
                else
                {
                    PlayerNet pl = new PlayerNet();
                    pl.Channel = e.Channel;
                    pl.ClientID = e.Channel.ClientID;
                    pl.Name = plm_cs.Name;

                    //send succeed msg
                    Messages.PlayerLoginResultMsg plrm = new Messages.PlayerLoginResultMsg();
                    plrm.Result = Messages.LoginResult.Succeed;
                    plrm.ClientID = e.Channel.ClientID;
                    Messages.ProtobufAdapter.Send(e.Channel, plrm);


                    
                    //plm_sc.ClientID = e.Channel.ClientID;
                    //plm_sc.Name = pl.Name;
                    //Messages.ProtobufAdapter.Send(e.Channel, plm_sc);*/

                    //add player to list
                    players_byname[pl.Name] = pl;
                    players_byid[pl.ClientID] = pl;

                    foreach (KeyValuePair<string, PlayerNet> sp in players_byname)
                    {
                        Messages.PlayerLoginMsg plm_sc = new Messages.PlayerLoginMsg();
                        plm_sc.ClientID = sp.Value.ClientID;
                        plm_sc.Name = sp.Value.Name;
                        Messages.ProtobufAdapter.Send(e.Channel, plm_sc);
                    }

                    //send player login to other client
                    foreach (KeyValuePair<string, PlayerNet> sp in players_byname)
                    {
                        if (sp.Value.ClientID != pl.ClientID)
                        {
                            Messages.PlayerLoginMsg plm_sc = new Messages.PlayerLoginMsg();
                            plm_sc.ClientID = pl.ClientID;
                            plm_sc.Name = pl.Name;
                            Messages.ProtobufAdapter.Send(sp.Value.Channel, plm_sc);
                        }
                    }

                    
                }
            }
            /*if (adapter.Message is Messages.PlayerStatus)
            {
                OnPlayerStatus(e.Channel, (Protobuf.Messages.PlayerStatus)adapter.Message);
            }*/
            /*if (adapter.Message is Messages.Register)
            {
                OnRegister(e.Channel, (Messages.Register)adapter.Message);
            }
            else if (adapter.Message is Messages.Query)
            {
                OnQuery(e.Channel, (Messages.Query)adapter.Message);
            }
            else
            {
            }*/

        }

        static void Main(string[] args)
        {
            StartServer();
        }
    }
}
