using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace IRCLib
{
    public class Client
    {
        /*public string NickName { get; set; }
        public string UserName { get; set; }
        public string HostName { get; set; }
        public string ServerName { get; set; }
        public string RealName { get; set; }
        public string Away { get; set; }
        public int UserID { get; set; }
        public int IdleTime { get { return (DateTime.Now - LastCommand).Seconds; } }
        public int PingTime { get { return (DateTime.Now - LastPing).Seconds; } }
        public bool Connected { get { return tcpClient.Connected; } }
        public string UserString { get { return string.Format("{0}!{1}@{2}", NickName, UserName, HostName); } }

        Thread msgHandler;

        string unfinishedCommand = "";
        bool pinging;
        bool greeted;

        DateTime LastCommand;
        DateTime LastPing;

        public string Modes { get; set; }
        TcpClient tcpClient;

        public string IP { get { return "" + tcpClient.Client.RemoteEndPoint; } }

        public Client(TcpClient client)
        {
            tcpClient = client;
            Modes = "";
            NickName = "";
            UserName = "";
            RealName = "";

            LastPing = DateTime.Now;
            pinging = true;
            HostName = Dns.GetHostEntry(((IPEndPoint)client.Client.RemoteEndPoint).Address).HostName;
            ServerName = Server.Name;
            msgHandler = new Thread(new ThreadStart(NetworkActivity));
        }

        public void SendMessage(Message message, bool suffix = false)
        {
            if (!tcpClient.Connected)
                return;

            if (suffix)
                message.Suffix = true;

            byte[] msg = Encoding.UTF8.GetBytes(message.MessageString);

            byte[] crlf = new byte[] { 0x0d, 0x0a };
            byte[] send = new byte[msg.Length + 2];
            Array.Copy(msg, send, msg.Length);
            Array.Copy(crlf, 0, send, msg.Length, 2);

            tcpClient.Client.Send(send);

            Console.WriteLine("Sent message \"{0}\" to {1}", message.MessageString, HostName);
        }

        public void Tick()
        {
            if (msgHandler.ThreadState != ThreadState.Running)
            {
                msgHandler = new Thread(new ThreadStart(NetworkActivity));
                msgHandler.Start();
            }
        }

        public void NetworkActivity()
        {
            if (tcpClient.Available > 0)
            {
                byte[] recv = new byte[tcpClient.Available];
                tcpClient.Client.Receive(recv);
                string data = Encoding.UTF8.GetString(recv);

                data = data.Replace("\r\n", "\n");

                if (!data.EndsWith("\n"))
                {
                    unfinishedCommand = data;
                    data.Replace(unfinishedCommand, "");
                }
                else if (data.Contains("\n") && unfinishedCommand != null)
                {
                    data = unfinishedCommand + data;
                    unfinishedCommand = null;
                }

                string[] msgs = data.Split(new string[] {"\n"},StringSplitOptions.RemoveEmptyEntries);

                Console.WriteLine("Recieved {0} messages from {1}", msgs.Length, HostName);

                foreach (string msg in msgs)
                {
                    Console.WriteLine("\t" + msg);
                    Message m;
                    try
                    {
                        m = new Message(msg);

                        if (m.Command != Command.PONG && m.Command != Command.PING)
                            LastCommand = DateTime.Now;

                        if (!Server.CallMessageRecieved(this, m))
                        {
                            HandleMessage(m);
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine("Unknown command");
                        SendMessage(new Message("", Reply.ERR_UNKNOWNCOMMAND, new string[] { ex.ParamName, ":Unknown command" }), true);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Parse failed");
                        SendMessage(new Message("", Reply.ERR_UNKNOWNCOMMAND, new string[] { ":Couldn't parse \"" + msg + "\"" }), true);
                    }
                }
            }
            else if (PingTime > 30 && !pinging)
            {
                pinging = true;
                SendMessage(new Message("", Command.PING, new string[] { ":" + UserID }));
            }
            else if (PingTime > 40 && pinging)
            {
                tcpClient.Client.Disconnect(false);
            }
        }

        private void HandleMessage(Message msg)
        {
            if (msg.IsCommand)
            {
                switch (msg.Command)
                {
                    case Command.NICK:
                        foreach (Client c in Server.clients)
                        {
                            if (c.NickName.Equals(msg.Params[0], StringComparison.CurrentCultureIgnoreCase))
                                SendMessage(new Message(Server.Name, Reply.ERR_NICKNAMEINUSE, new string[] { msg.Params[0], ":Nickname is already in use." }));
                            else
                                NickName = msg.Params[0];
                        }
                        break;

                    case Command.USER:
                        if (msg.Params.Count() != 4)
                        {
                            SendMessage(new Message(Server.Name, Reply.ERR_NEEDMOREPARAMS, new string[] { "USER", ":Not enough parameters" }));
                            break;
                        }

                        UserName = msg.Params[0];
                        RealName = msg.Params[3];

                        pinging = true;
                        SendMessage(new Message("", Command.PING, new string[] { ":" + UserID }));
                        break;

                    case Command.PONG:
                        if (!msg.Params[0].EndsWith("" + UserID))
                            break;

                        pinging = false;
                        LastPing = DateTime.Now;
                        break;

                    case Command.MODE:
                        if (Server.HasClient(msg.Params[0]))
                        {
                            if (msg.Params.Count() == 2)
                            {
                                string modes = msg.Params[1].Remove(0, 1);
                                string mode = msg.Params[1].Substring(0, 1);

                                foreach (char m in modes)
                                {
                                    if (!"iswo".Contains(m))
                                        break;

                                    if (mode == "+" && !Modes.Contains(m) && m != 'o')
                                        Modes += m;
                                    else if (mode == "-" && Modes.Contains(m))
                                        Modes = Modes.Replace(new string(m,1), "");
                                }
                            }
                            else if (msg.Params.Count() > 2)
                            {
                                SendMessage(new Message("", Reply.ERR_NEEDMOREPARAMS, new string[] { "MODE", ":Too many parameters" }));
                                break;
                            }

                            SendMessage(new Message(NickName, Reply.RPL_UMODEIS, new string[] { msg.Params[0], "+" + Modes }));
                        }
                        else if (Server.HasChannel(msg.Params[0]))
                        {
                            Channel c = Server.GetChannel(msg.Params[0]);

                            string modes = msg.Params[1].Remove(0, 1);
                            string mode = msg.Params[1].Substring(0, 1);
                            
                            int i = 2;
                            
                            if (c.ClientModes.ContainsKey(this) && c.ClientModes[this].Contains('o'))
                                foreach (char m in modes)
                                {
                                    if (!"opsitnmlbvk".Contains(m))
                                        break;

                                    if (mode == "+")
                                    {
                                        if (!"kbl".Contains(m) && !c.Modes.ContainsKey(m))
                                            c.Modes.Add(m, "");
                                        else if ("ov".Contains(m) && Server.HasClient(msg.Params[i]))
                                        {
                                            if (c.ClientModes.ContainsKey(this) && !c.ClientModes[this].Contains(m))
                                            {
                                                c.ClientModes[this] += m;
                                            }
                                        }
                                        else if (!c.Modes.ContainsKey(m))
                                            c.Modes.Add(m, msg.Params[i]);
                                    }
                                    else if (mode == "-")
                                        if (!"ov".Contains(m) && c.Modes.ContainsKey(m))
                                            c.Modes.Remove(m);
                                        else if (Server.HasClient(msg.Params[i]))
                                        {
                                            if (c.ClientModes.ContainsKey(this) && c.ClientModes[this].Contains(m))
                                            {
                                                c.ClientModes[this] = c.ClientModes[this].Replace("" + m, "");
                                            }
                                        }

                                    if ("okvbl".Contains(m))
                                        i++;
                                }

                            SendMessage(new Message("", Reply.RPL_UMODEIS, new string[] { msg.Params[0], "+" + c.GetModes() }));
                        }
                        else
                            SendMessage(new Message("", Reply.ERR_NOSUCHNICK, new string[] { msg.Params[0], ":No such nick/channel" }));
                        break;

                    case Command.WHOIS:
                        if (Server.HasClient(msg.Params[0]))
                        {
                            Client c = Server.GetClient(msg.Params[0]);

                            SendMessage(new Message(ServerName, Reply.RPL_WHOISUSER, new string[] { c.NickName, c.UserName, c.HostName, "*", ":" + c.RealName }), true);
                            SendMessage(new Message(ServerName, Reply.RPL_WHOISSERVER, new string[] { c.NickName, Server.Name, ":" + Server.FullName }), true);

                            if (c.Modes.Contains("o"))
                                SendMessage(new Message(ServerName, Reply.RPL_WHOISOPERATOR, new string[] { c.NickName, ":is an IRC operator" }), true);

                            SendMessage(new Message(ServerName, Reply.RPL_WHOISIDLE, new string[] { c.NickName, "" + c.IdleTime, ":seconds idle" }), true);
                            SendMessage(new Message(ServerName, Reply.RPL_ENDOFWHOIS, new string[] { c.NickName, ":End of /WHOIS list" }), true);
                        }
                        else
                            SendMessage(new Message(ServerName, Reply.ERR_NOSUCHNICK, new string[] { msg.Params[0], ":No such nick/channel" }), true);
                        break;

                    case Command.QUIT:
                        tcpClient.Client.Disconnect(false);
                        break;

                    case Command.MOTD:
                        greeted = false;
                        break;

                    case Command.PRIVMSG:
                        if (Server.HasClient(msg.Params[0]))
                        {
                            Client c = Server.GetClient(msg.Params[0]);


                        }
                        else if (Server.HasChannel(msg.Params[0]))
                        {
                            Channel c = Server.GetChannel(msg.Params[0]);


                        }
                        else
                            SendMessage(new Message(ServerName, Reply.ERR_NOSUCHNICK, new string[] { msg.Params[0], ":No such nick/channel" }), true);
                        break;
                }

                SendGreeting();
            }
        }

        private void SendGreeting()
        {
            if (greeted)
                return;

            if (NickName == string.Empty || RealName == string.Empty || UserName == string.Empty || pinging)
                return;
            
            greeted = true;
            string[] data = System.IO.File.ReadAllLines("MOTD.txt");

            SendMessage(new Message(ServerName, Reply.RPL_MOTDSTART, new string[] { ":- " + ServerName + " Message of the day - " }), true);

            foreach (string s in data)
            {
                SendMessage(new Message(ServerName, Reply.RPL_MOTD, new string[] { ":- " + s }), true);
            }

            SendMessage(new Message(ServerName, Reply.RPL_ENDOFMOTD, new string[] { ":End of /MOTD command." }), true);
        }*/
    }
}
