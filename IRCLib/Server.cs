using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using BaseIRCLib;
using IRCLib.Helpers;
using IRCLib.Interfaces;
using IRCLib.Interfaces.IRC;
using IRCLib.Interfaces.Steam;
using Steam4NET;

namespace IRCLib
{
    public class Server : IServer, IDatabase
    {
        public static bool Verbose = false;
        int user;
        int pipe;
        static DateTime start;

        List<object> antiGCList;
        public static ClientList clientList;
        //public static IClientUser clientUser;
        public static ISteamClient009 steamClient;
        public static ISteamUser014 clientUser;
        public static IClientEngine clientEngine;
        public static IClientFriends clientFriends;

        public static IRCCommandList IRCCommands;

        string hostName;

        public DateTime StartTime { get { return start; } }
        public string HostString { get { return hostName; } }

        Random pingRandom = new Random();

        public Server()
        {
            antiGCList = new List<object>();
            IRCCommands = new IRCCommandList();

            BaseIRCLib.Database.SetDatabase(this);
            BaseIRCLib.Server.SetServer(this);

            Steamworks.Load();
            steamClient = Steamworks.CreateInterface<ISteamClient009>("SteamClient009");
            clientEngine = Steamworks.CreateInterface<IClientEngine>("CLIENTENGINE_INTERFACE_VERSION001");

            pipe = steamClient.CreateSteamPipe();
            user = steamClient.ConnectToGlobalUser(pipe);

            clientFriends = Steamworks.CastInterface<IClientFriends>(clientEngine.GetIClientFriends(user, pipe, "CLIENTFRIENDS_INTERFACE_VERSION001"));
            clientUser = Steamworks.CastInterface<ISteamUser014>(steamClient.GetISteamUser(user, pipe, "SteamUser014"));
            //clientUser = Steamworks.CastInterface<IClientUser>(clientEngine.GetIClientUser(user, pipe, "CLIENTUSER_INTERFACE_VERSION001"));

            Callback<PersonaStateChange_t> stateChange = new Callback<PersonaStateChange_t>(StateChange, PersonaStateChange_t.k_iCallback);
            Callback<FriendChatMsg_t> friendMessage = new Callback<FriendChatMsg_t>(FriendMessage, FriendChatMsg_t.k_iCallback);
            Callback<ChatRoomMsg_t> chatMessage = new Callback<ChatRoomMsg_t>(ChatMessage, ChatRoomMsg_t.k_iCallback);
            Callback<ChatMemberStateChange_t> chatResult = new Callback<ChatMemberStateChange_t>(ChatResult, ChatMemberStateChange_t.k_iCallback);
            Callback<ChatRoomInvite_t> chatInvite = new Callback<ChatRoomInvite_t>(ChatInvite, ChatRoomInvite_t.k_iCallback);

            if (File.Exists("clients.list"))
                clientList = ClientList.LoadClients("clients.list");
            else
            {
                clientList = new ClientList();
                clientList.Save("clients.list");
            }

            foreach (string f in Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Commands"), "*.dll"))
            {
                Assembly fA = Assembly.LoadFile(f);
                foreach (Type m in fA.GetTypes())
                {
                    object a = fA.CreateInstance(m.FullName);
                    if (a == null)
                    {
                        Console.WriteLine("Skipping implementation of {0}", m.Name);
                        continue;
                    }

                    antiGCList.Add(a);

                    RegisterCommands(a);
                }
            }
        }

        public static List<IClient> clients;
        public IClient[] Clients { get { return clients.ToArray(); } }
        public static List<IChannel> channels;
        public IChannel[] Channels { get { return channels.ToArray(); } }
        TcpListener server = new TcpListener(IPAddress.Any, 6667);
        Thread t;
        bool running;

        public string Name { get; set; }
        public string FullName { get; set; }

        public void Start()
        {
            hostName = System.Net.Dns.GetHostEntry(System.Net.IPAddress.Loopback).HostName;

            start = DateTime.Now;
            if (t == null)
                t = new Thread(new ThreadStart(Work));

            clients = new List<IClient>();
            channels = new List<IChannel>();
            channels.Add(new FriendsChannel());
            channels.Add(new Channel("Nope", "Normal IRC channel, nothing to see here. Move on."));
            running = true;
            start = DateTime.Now;
            server.Start();
            t.Start();
            CallbackDispatcher.SpawnDispatchThread(pipe);

            for (int i = 0; i < clientFriends.GetFriendCount(4); i++)
            {
                EPersonaState st = clientFriends.GetFriendPersonaState(clientFriends.GetFriendByIndex(i, 4));
                if (st == EPersonaState.k_EPersonaStateOffline)
                    continue;

                ulong asdf = clientFriends.GetFriendByIndex(i, 4);
                SteamClient cl;

                if (clientList.Clients.ContainsKey(asdf))
                    cl = clientList.Clients[asdf];
                else
                    cl = new SteamClient(asdf);

                clients.Add(cl);
                GetChannel("&Friends").AddClient(cl);
            }

            for (int i = 0; i < clientFriends.GetKnownClanCount(); i++)
            {
                ulong cla = clientFriends.GetKnownClanByIndex(i);
                Channel ch = new SteamClanChannel(cla);
                channels.Add(ch);
            }

            for (int i = 0; i < clientFriends.GetChatRoomCount(); i++)
            {
                CSteamID ch = clientFriends.GetChatRoomByIndex(i);

                IChannel c = GetSteamChannel(new CSteamID(ch.AccountID, 0x80000, EUniverse.k_EUniversePublic, EAccountType.k_EAccountTypeChat));
                if (c == null)
                {
                    c = new ChatChannel(ch);
                    channels.Add(c);
                }
            }

            Console.WriteLine("Started listening");
        }

        public void Stop()
        {
            CallbackDispatcher.StopDispatchThread(pipe);
            running = false;
            server.Stop();
            clients = null;
        }

        private void Work()
        {
            Console.WriteLine("Work thread begun");

            while (running)
            {
                if (server.Pending())
                {
                    IRCClient cl = new IRCClient(server.AcceptTcpClient());

                    clients.Add(cl);
                    Console.WriteLine("Recieved connection from {0}", clients[clients.Count - 1].HostName);
                }
                else if (clientList.NeedsToBeSaved)
                {
                    foreach (KeyValuePair<ulong, SteamClient> c in clientList.Clients)
                    {
                        if (c.Value.HasMessage())
                        {
                            IMessage m = c.Value.GetMessage();
                            Server.IRCCommands.CallCommand(m);
                        }
                    }

                    clientList.Save("clients.list");
                }
                else
                {
                    IClient[] clientDupe = clients.ToArray();
                    foreach (IClient c in clientDupe)
                    {
                        if (c == null)
                            continue;

                        if (c.IsDisposed)
                        {
                            Console.WriteLine("Client {0} disconnected: {1}", c.UserString, c.DisconnectMsg);
                            ClientHelpers.PartClient(c, c.DisconnectMsg);
                            clients.Remove(c);
                            continue;
                        }

                        if (c.HasMessage())
                        {
                            IMessage m = c.GetMessage();

                            if (m.IsCommand && IRCCommands.HasCommand(m.Command))
                            {
        						if (IRCCommands.ValidCommand(m.Command, m.Params.Length))
                                {
        							IRCCommands.CallCommand(m);
        							c.LastCommand = DateTime.Now;
        						}
                                else
								{
        							Console.WriteLine("Invalid command call: \"{0}\"", m.MessageString);
        							c.SendMessage(IRCMessage.GetStatic().CreateMessage(c, c.NickName, Reply.ERR_UNKNOWNCOMMAND, new string[] { c.NickName, m.Command, "Wrong parameters" }));
        						}
        					}
                            else
							{
								Console.WriteLine("Unknown command: \"{0}\"", m.MessageString);
        						c.SendMessage(IRCMessage.GetStatic().CreateMessage(c, c.NickName, Reply.ERR_UNKNOWNCOMMAND, new string[] { c.NickName, m.Command, "Unknown command" }));
							}
                        }

                        if (!c.Greeted && c.NickName != "*" && c.ClientType == ClientType.TYP_CLIENT)
                            ClientHelpers.MeetAndGreet(c);
                        else if (!c.Greeted && (DateTime.Now - c.LastCommand).TotalSeconds > 15)
                            c.Dispose("Never finished handshake");
                        else if (c.Greeted && (DateTime.Now - c.LastPing).TotalSeconds > 30)
                        {
                            c.LastPing = DateTime.Now;
                            c.SendMessage(IRCMessage.GetStatic().CreateMessage(c, null, "PING", new string[] { pingRandom.Next().ToString() }));
                            c.MissedPings++;
                        }
                        else if (c.Greeted && c.MissedPings > 5)
                        {
                            c.Dispose("Ping Timeout!");
                        }
                    }
                }

                Thread.Sleep(5);
            }

            Console.WriteLine("Work thread finished");
        }

        private void StateChange(PersonaStateChange_t change)
        {
            IClient cl;
            if (change.m_nChangeFlags == EPersonaChange.k_EPersonaChangeComeOnline)
            {
                cl = new SteamClient(change.m_ulSteamID);
                clients.Add(cl);
                Database.GetDatabase().GetChannel("Friends").AddClient(cl);
            }
            else if (change.m_nChangeFlags == EPersonaChange.k_EPersonaChangeGoneOffline)
            {
                cl = GetSteamClient(change.m_ulSteamID);
                if (cl != null)
                    cl.Dispose();
            }
            else if (change.m_nChangeFlags == EPersonaChange.k_EPersonaChangeName)
            {
                cl = GetSteamClient(change.m_ulSteamID);

                if (cl != null)
                {
                    cl.RealName = clientFriends.GetFriendPersonaName(change.m_ulSteamID);

                    if (!clientList.Clients.ContainsKey(change.m_ulSteamID))
                        cl.AddMessage(IRCMessage.GetStatic().CreateMessage(cl, "NICK " + StripString(cl.RealName)));
                    //Server.GetChannel("Friends").SendMessage(Message.CreateMessage(cl, cl.UserString, "NICK", new string[] { StripString(cl.RealName) }));
                    //cl.NickName = StripString(cl.RealName);
                }
            }
            else if (change.m_nChangeFlags == EPersonaChange.k_EPersonaChangeStatus)
            {
                EPersonaState st = clientFriends.GetFriendPersonaState(change.m_ulSteamID);
                cl = GetSteamClient(change.m_ulSteamID);

                if (cl != null)
                    if (st == EPersonaState.k_EPersonaStateOffline)
                        cl.Dispose();
                    else
                        cl.AddMessage(IRCMessage.GetStatic().CreateMessage(cl, cl.UserString, "AWAY", new string[] { (st == EPersonaState.k_EPersonaStateOnline ? "" : st.ToString().Remove(0, 15)) }));
                        //cl.AwayMsg = (st == EPersonaState.k_EPersonaStateOnline ? "" : st.ToString().Remove(0, 15));
            }
        }

        private void FriendMessage(FriendChatMsg_t message)
        {
            if (message.m_ulSender == clientUser.GetSteamID())
                return;
            IClient cl = GetSteamClient(message.m_ulSender);

            if (cl == null)
            {
                if (clientList.Clients.ContainsKey(message.m_ulSender))
                    cl = clientList.Clients[message.m_ulSender];
                else
                    cl = new SteamClient(message.m_ulSender);

                clients.Add(cl);
                GetChannel("&Friends").AddClient(cl);
            }

            byte[] msg = new byte[4096];
            EChatEntryType type = EChatEntryType.k_EChatEntryTypeInvalid;
            ulong chatter = 0;

            int len = clientFriends.GetChatMessage(message.m_ulSender, (int)message.m_iChatID, msg, 4096, ref type, ref chatter);
            len--;

            if (type != EChatEntryType.k_EChatEntryTypeChatMsg && type != EChatEntryType.k_EChatEntryTypeEmote)
                return;

            byte[] a = new byte[len];
            Array.Copy(msg, a, len);

            string[] Messs = Encoding.UTF8.GetString(a).Split(new char[] { '\n' });

            foreach (string Mess in Messs)
                foreach (IClient cli in clients)
                    if (cli.GetType().Equals(typeof(IRCClient)))
                        cl.AddMessage(IRCMessage.GetStatic().CreateMessage(cl, cl.UserString, "PRIVMSG", new string[] { cli.NickName, (type == EChatEntryType.k_EChatEntryTypeEmote ? "ACTION " : "") + Mess + (type == EChatEntryType.k_EChatEntryTypeEmote ? "" : "") }));
                            //cl.SendMessage(Message.CreateMessage(GetClient(message.m_ulSender), GetClient(message.m_ulSender).UserString, "PRIVMSG", new string[] { client.NickName, "" + (type == EChatEntryType.k_EChatEntryTypeEmote ? "ACTION " : "") + Mess + (type == EChatEntryType.k_EChatEntryTypeEmote ? "" : "" ) }));
        }

        private void ChatMessage(ChatRoomMsg_t message)
        {
            if (message.m_ulSteamIDUser == clientUser.GetSteamID())
                return;

            IChannel ch = GetSteamChannel(message.m_ulSteamIDChat);
            if (ch == null)
                return;

            byte[] msg = new byte[4096];
            EChatEntryType type = EChatEntryType.k_EChatEntryTypeInvalid;

            int len = clientFriends.GetChatRoomEntry(message.m_ulSteamIDChat, (int)message.m_iChatID, ref message.m_ulSteamIDUser, msg, 4096, ref type);
            len--;

            if (type != EChatEntryType.k_EChatEntryTypeChatMsg && type != EChatEntryType.k_EChatEntryTypeEmote)
                return;

            byte[] a = new byte[len];
            Array.Copy(msg, a, len);

            string[] Messs = Encoding.UTF8.GetString(a).Split(new char[] {'\n'});
            IClient cl = GetSteamClient(message.m_ulSteamIDUser);

            if (cl == null)
            {
                if (clientList.Clients.ContainsKey(message.m_ulSteamIDUser))
                    cl = clientList.Clients[message.m_ulSteamIDUser];
                else
                    cl = new SteamClient(message.m_ulSteamIDUser);
                clients.Add(cl);
                ch.AddClient(cl);
            }

            foreach (string Mess in Messs)
                cl.AddMessage(IRCMessage.GetStatic().CreateMessage(cl, cl.UserString, "PRIVMSG", new string[] { "&" + ch.ChannelName, (type == EChatEntryType.k_EChatEntryTypeEmote ? "ACTION " : "") + Mess + (type == EChatEntryType.k_EChatEntryTypeEmote ? "" : "") }));
                //ch.SendMessage(Message.CreateMessage(cl, cl.UserString, "PRIVMSG", new string[] { "&" + Server.StripString(clientFriends.GetChatRoomName(message.m_ulSteamIDChat)), "" + (type == EChatEntryType.k_EChatEntryTypeEmote ? "ACTION " : "") + Mess + (type == EChatEntryType.k_EChatEntryTypeEmote ? "" : "") }));
        }

        private void ChatResult(ChatMemberStateChange_t entry)
        {
            EChatMemberStateChange ch = entry.m_rgfChatMemberStateChange;
            if (ch == EChatMemberStateChange.k_EChatMemberStateChangeEntered)
            {
                IClient cl = GetSteamClient(entry.m_ulSteamIDUserChanged);

                if (cl == null)
                {
                    if (clientList.Clients.ContainsKey(entry.m_ulSteamIDUserChanged))
                        cl = clientList.Clients[entry.m_ulSteamIDUserChanged];
                    else
                        cl = new SteamClient(entry.m_ulSteamIDUserChanged);

                    EPersonaState st = clientFriends.GetFriendPersonaState(entry.m_ulSteamIDUserChanged);
                    cl.AwayMsg = (st == EPersonaState.k_EPersonaStateOnline ? "" : st.ToString().Remove(0, 15));
                    clients.Add(cl);
                }

                cl.AddMessage(IRCMessage.GetStatic().CreateMessage(cl, cl.UserString, "JOIN", new string[] { "&" + GetSteamChannel(entry.m_ulSteamIDChat).ChannelName }));

                /*Channel c = GetChannel(Server.StripString(clientFriends.GetChatRoomName(entry.m_ulSteamIDChat)));
                c.AddClient(cl);*/
            }
            else if (ch == EChatMemberStateChange.k_EChatMemberStateChangeLeft || ch == EChatMemberStateChange.k_EChatMemberStateChangeKicked || ch == EChatMemberStateChange.k_EChatMemberStateChangeDisconnected || ch == EChatMemberStateChange.k_EChatMemberStateChangeBanned)
            {
                IChannel c = GetSteamChannel(entry.m_ulSteamIDChat);

                IClient cl = GetSteamClient(entry.m_ulSteamIDUserChanged);

                if (cl == null || c == null)
                    return;

                string leaveMsg = entry.m_rgfChatMemberStateChange.ToString();

                if (leaveMsg.Length > 24)
                    leaveMsg = leaveMsg.Remove(0,24);

                c.RemoveClient(cl, leaveMsg);
            }
        }

        private void ChatInvite(ChatRoomInvite_t invite)
        {
            Channel c = new ChatChannel(invite.m_ulSteamIDChat);
            channels.Add(c);

            foreach (IClient cl in clients)
            {
                if (cl.GetType() == typeof(IRCClient))
                    cl.AddMessage(IRCMessage.GetStatic().CreateMessage(cl, "JOIN &" + c.ChannelName));
            }
        }

        #region Commands
        public static void RegisterCommands(object commandClass)
        {
            IRCCommandList returnValue = IRCCommands;
            Type commandClassType = commandClass.GetType();

            int maxCom = 0; int place = 0;

            Console.WriteLine("Registering {0}...", commandClass.GetType().Name);

            foreach (MethodInfo methodInfo in commandClassType.GetMethods())
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(methodInfo))
                {
                    if (attr.GetType() == typeof(IRCCommand) || attr.GetType() == typeof(IRCCommandPlaceholder))
                    {
                        maxCom++;
                        if (attr.GetType() == typeof(IRCCommandPlaceholder))
                            place++;

                        IRCCommand command = (IRCCommand)attr;
                        ParameterInfo[] parameters = methodInfo.GetParameters();

                        if (parameters.Length != 1 && parameters[0].ParameterType != typeof(IMessage))
                        {
                            Console.WriteLine("\tCommand {0} ({1}) not registered, argument mismatch (requires one argument of type 'Message')", command.Name, methodInfo.Name);
                            continue;
                        }

                        Console.WriteLine("\tCommand {0} {1} registered{2}", command.Name, (command.MinimumArguments != -1 ? "(" + command.MinimumArguments + (command.MaximumArguments != command.MinimumArguments ? "-" + command.MaximumArguments : "") + " argument" + (command.MinimumArguments == 1 && command.MaximumArguments == 1 ? "" : "s") + ")" : "(no arguments)"), (attr.GetType() == typeof(IRCCommandPlaceholder) ? " (Placeholder implementation)" : ""));
                        returnValue.AddCommand(command, commandClass, methodInfo);
                    }
                }
            }

            if (maxCom != 0)
                Console.WriteLine("{0} Commands implemented on {1} with {2} ({3}%)", maxCom, commandClass.GetType().Name, place + " placeholder" + (place != 1 ? "s" : ""), Math.Round((place / (double)maxCom) * 100));
            else
                Console.WriteLine("No commands implemented on {0}", commandClass.GetType().Name);
        }
        #endregion

        #region IServer

        public string QueryServer(string query)
        {
            return string.Empty;
        }
        
        #endregion

        #region IDatabase

        public bool HasClient(string nick)
        {
            IClient[] clientDupe = clients.ToArray();

            foreach (IClient c in clientDupe)
                if (c.NickName.Equals(nick, StringComparison.CurrentCultureIgnoreCase))
                    return true;

            return false;
        }
        public IClient GetClient(string nick)
        {
            IClient[] clientDupe = clients.ToArray();

            foreach (IClient c in clientDupe)
                if (c.NickName.Equals(nick, StringComparison.CurrentCultureIgnoreCase))
                    return c;

            return null;
        }

        public IClient GetSteamClient(CSteamID steamID)
        {
            IClient[] clientDupe = clients.ToArray();

            foreach (IClient c in clientDupe)
                if (c.GetType().Equals(typeof(SteamClient)))
                    if ((c as SteamClient).SteamID == steamID)
                        return c;

            return null;
        }

        public bool HasChannel(string name)
        {
            if (name[0] == '&')
            {
                name = name.Remove(0, 1);
                foreach (IChannel c in Channels)
                    if (c.ChannelName.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                        return true;
            }

            return false;
        }
        public IChannel GetChannel(string name)
        {
            if (name[0] == '&')
            {
                name = name.Remove(0, 1);
                foreach (IChannel c in Channels)
                    if (c.ChannelName.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                        return c;
            }

            return null;
        }
        public IChannel GetSteamChannel(CSteamID steamID)
        {
            foreach (IChannel c in Channels)
                if (c.GetType().Equals(typeof(ChatChannel)))
                {
                    if ((c as ChatChannel).ChatID == steamID)
                        return c;
                }
                else if (c.GetType().Equals(typeof(SteamClanChannel)))
                {
                    if ((c as SteamClanChannel).ChatID == steamID)
                        return c;
                }

            return null;
        }
        public IChannel[] GetChannels(IClient cl)
        {
            List<IChannel> ret = new List<IChannel>();

            foreach (IChannel c in Channels)
                if (-1 != Array.IndexOf<IClient>(c.Clients, cl))
                    ret.Add(c);

            return ret.ToArray();
        }

        public void AddClient(IClient cl)
        {
            clients.Add(cl);
        }

        public void RemoveClient(IClient cl)
        {
            clients.Remove(cl);
        }

        public void AddChannel(IChannel ch)
        {
            channels.Add(ch);
        }

        public void RemoveChannel(IChannel ch)
        {
            channels.Remove(ch);
        }

        static System.Text.RegularExpressions.Regex nickRegex = new System.Text.RegularExpressions.Regex(@"[^A-Za-z0-9\[\]\\`_\^\{\|\}]");

        public static string StripString(string input)
        {
            // [A-Za-z0-9 "[", "]", "\", "`", "_", "^", "{", "|", "}"]

            return nickRegex.Replace(input, "");
        }

        /*public static string AggressiveStripString(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, @"\W", "");
        }*/
        #endregion
    }
}
