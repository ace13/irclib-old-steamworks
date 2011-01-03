using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using IRCLib.Interfaces;
using IRCLib.Interfaces.IRC;
using IRCLib.Interfaces.Steam;
using BaseIRCLib;
using Steam4NET;

namespace IRCLib
{
    public class Channel : IChannel
    {
        protected List<IClient> clients;
        protected List<string> banList;
        public virtual string ChannelName { get; set; }
        public virtual IClient[] Clients { get { return clients.ToArray(); } }
        public virtual Dictionary<IClient, string> ClientModes { get; set; }
        public virtual Dictionary<char, string> Modes { get; set; }
        public virtual string Topic { get; set; }
        public virtual string[] Banlist { get { return banList.ToArray(); } }

        public virtual string GetModes()
        {
            string ret = "";

            foreach (KeyValuePair<char,string> k in Modes)
            {
                ret += k.Key;
            }

            return ret;
        }

        public Channel(string name, string topic = null)
        {
            ChannelName = name;
            Topic = topic;

            clients = new List<IClient>();
            ClientModes = new Dictionary<IClient, string>();
            Modes = new Dictionary<char, string>();

            Modes.Add('n', string.Empty);
            Modes.Add('t', string.Empty);

            banList = new List<string>();
        }

        public virtual void AddClient(IClient c)
        {
            if (clients.Contains(c))
                return;

            clients.Add(c);

            if (!ClientModes.ContainsKey(c))
                ClientModes.Add(c, "");

            SendMessage(c, IRCMessage.GetStatic().CreateMessage(c, (ClientModes[c].Contains('o') ? "@" : "") + c.UserString, "JOIN", new string[] { "&" + ChannelName }), true);

            if (!string.IsNullOrEmpty(Topic))
                c.SendMessage(IRCMessage.GetStatic().CreateMessage(c, BaseIRCLib.Server.GetServer().HostString, Reply.RPL_TOPIC, new string[] { c.NickName, "&" + ChannelName, "" + Topic }));

            int currentNick = 0;

            string names = "";
            while (currentNick < clients.Count)
            {
                
                names += (ClientModes.ContainsKey(clients[currentNick]) && ClientModes[clients[currentNick]].Contains("o") ? "@" : (ClientModes.ContainsKey(clients[currentNick]) && ClientModes[clients[currentNick]].Contains("v") ? "+" : "")) + clients[currentNick].NickName + " ";

                currentNick++;

                if (names.Length > 480)
                {
                    names = names.Substring(0, names.Length - 1);

                    c.SendMessage(IRCMessage.GetStatic().CreateMessage(c, BaseIRCLib.Server.GetServer().HostString, Reply.RPL_NAMREPLY, new string[] { c.NickName, "=", "&" + ChannelName, names }));

                    names = "";
                }
            }

            if (!string.IsNullOrEmpty(names))
                c.SendMessage(IRCMessage.GetStatic().CreateMessage(c, BaseIRCLib.Server.GetServer().HostString, Reply.RPL_NAMREPLY, new string[] { c.NickName, "=", "&" + ChannelName, names }));

            c.SendMessage(IRCMessage.GetStatic().CreateMessage(c, BaseIRCLib.Server.GetServer().HostString, Reply.RPL_ENDOFNAMES, new string[] { c.NickName, "&" + ChannelName, "End of NAMES list" }));
        }

        public virtual void RemoveClient(IClient c)
        {
            RemoveClient(c, "");
        }

        public virtual void RemoveClient(IClient c, string message)
        {
            if (!clients.Contains(c))
                return;

            SendMessage(c, IRCMessage.GetStatic().CreateMessage(c, c.UserString, "PART", new string[] { "&" + ChannelName, message }), true);

            if (!clients.Contains(c))
                return;

            clients.Remove(c);
            ClientModes.Remove(c);
        }

        public virtual void KickClient(IClient c, IClient kicker)
        {
            KickClient(c, kicker, string.Empty);
        }

        public virtual void KickClient(IClient c, IClient kicker, string message)
        {
            if (!clients.Contains(c) || !(clients.Contains(kicker) && ClientModes[kicker].Contains('o')))
                return;
			
            SendMessage(c, IRCMessage.GetStatic().CreateMessage(kicker, kicker.UserString, "KICK", new string[] { "&" + ChannelName, c.NickName, message }), true);

            clients.Remove(c);
            ClientModes.Remove(c);
        }

        public virtual void AddToBanList(string banMask)
        {
        }

        public virtual void RemoveFromBanList(string banMask)
        {
        }

        public virtual void SendMessage(IMessage msg)
        {
            SendMessage(msg.Owner, msg);
        }
        public virtual void SendMessage(IMessage msg, bool toSender)
        {
            SendMessage(msg.Owner, msg, toSender);
        }

        public virtual void SendMessage(IClient C, IMessage msg)
        {
            SendMessage(C, msg, false);
        }
        public virtual void SendMessage(IClient C, IMessage msg, bool toSender)
        {
            IClient[] clientDupe = clients.ToArray();

            foreach (IClient c in clientDupe)
            {
                if (c.GetType() == typeof(IRCClient) && (c != C || toSender))
                    c.SendMessage(msg);
            }
        }
    }

    /// <summary>
    /// A Steam multi-user chat
    /// </summary>
    public class ChatChannel : Channel
    {
        ulong chatID;

        /// <summary>
        /// The chats ID
        /// </summary>
        public CSteamID ChatID { get { return chatID; } }

        /// <summary>
        /// Creates a channel for a steam chat
        /// </summary>
        /// <param name="chatID">The ID of the chat</param>
        public ChatChannel(ulong chatID)
            : base("GROUPCHAT")
        {
            this.chatID = chatID;
        }

        public override void AddClient(IClient c)
        {
            if (c.GetType().Equals(typeof(IRCLib.Interfaces.IRC.IRCClient)))
            {
                Server.clientFriends.JoinChatRoom(chatID);

                int numClients = Server.clientFriends.GetFriendCountFromSource(chatID);
                for (int j = 0; j < numClients; j++)
                {
                    ulong asdff = Server.clientFriends.GetFriendFromSourceByIndex(chatID, j);

                    if (asdff == Server.clientUser.GetSteamID())
                        continue;

                    IClient cl = (BaseIRCLib.Server.GetServer() as Server).GetSteamClient(asdff);

                    if (cl == null)
                    {
                        if (Server.clientList.Clients.ContainsKey(asdff))
                            cl = Server.clientList.Clients[asdff];
                        else
                            cl = new IRCLib.Interfaces.Steam.SteamClient(asdff);

                        Database.GetDatabase().AddClient(cl);
                        Database.GetDatabase().GetChannel("Friends").AddClient(cl);
                    }

                    if (clients.Contains(cl))
                        continue;

                    uint memberDetails = 0;
                    uint localMemberDetails = 0;

                    if (Server.clientFriends.GetChatRoomMemberDetails(chatID, asdff, ref memberDetails, ref localMemberDetails))
                    {
                        if (memberDetails == 2)
                            ClientModes.Add(cl, "o");
                    }

                    base.AddClient(cl);
                }
            }
            else
            {
                uint memberDetails = 0;
                uint localMemberDetails = 0;

                if (Server.clientFriends.GetChatRoomMemberDetails(chatID, (c as IRCLib.Interfaces.Steam.SteamClient).SteamID, ref memberDetails, ref localMemberDetails))
                {
                    if (memberDetails == 2)
                        ClientModes.Add(c, "o");
                }
            }

            base.AddClient(c);
        }

        public override void SendMessage(IClient C, IMessage m, bool toSender = false)
        {
            if (C.GetType().Equals(typeof(IRCLib.Interfaces.Steam.SteamClient)))
            {
                base.SendMessage(C, m, toSender);
                return;
            }

            if (m.IsCommand)
                if (m.Command == "PRIVMSG")
                {
                    byte[] msg = Encoding.UTF8.GetBytes(m.Params[1]);
                    EChatEntryType send = EChatEntryType.k_EChatEntryTypeChatMsg;

                    if (m.Params[1][0] == '' && m.Params[1][1] == 'A')
                    {
                        msg = Encoding.UTF8.GetBytes(m.Params[1].Substring(8, m.Params[1].Length - 9));
                        send = EChatEntryType.k_EChatEntryTypeEmote;
                    }

                    Server.clientFriends.SendChatMsg(chatID, send, msg, msg.Length + 1);
                }
                else
                    base.SendMessage(C, m, toSender);
        }
    }

    /// <summary>
    /// A channel containing all the users friends
    /// </summary>
    public class FriendsChannel : Channel
    {
        public FriendsChannel()
            : base("Friends")
        {
        }

        public override void SendMessage(IClient C, IMessage m, bool toSender = false)
        {
            if (!C.GetType().Equals(typeof(IRCLib.Interfaces.IRC.IRCClient)) || !m.IsCommand)
            {
                base.SendMessage(C, m, toSender);
                return;
            }

            if (m.Command == "PRIVMSG")
            {
                string msg = m.Params[1];

                string[] p = msg.Split(new char[] { ' ' });
                string cmd = p[0].ToLower();

                switch (cmd)
                {
                    case "rename":
                        if (p.Length != 3 || !Database.GetDatabase().HasClient(p[1]) || Database.GetDatabase().HasClient(p[2]))
                            return;

                        IClient cl = Database.GetDatabase().GetClient(p[1]);
                        cl.AddMessage(m.CreateMessage(cl, cl.UserString, "NICK", new string[] { p[2] }));
                        Server.clientList.AddClient((cl as SteamClient));
                        //Server.clientList.Save("clients.list");
                        break;

                    case default(string):
                        return;
                }
            }
            else
                base.SendMessage(C, m, toSender);
        }
    }

    /// <summary>
    /// A steam group chat
    /// </summary>
    public class SteamClanChannel : Channel
    {
        CSteamID clanID; CSteamID chatID;

        /// <summary>
        /// The ID of the group chat
        /// </summary>
        public CSteamID ChatID { get { return chatID; } }

        /// <summary>
        /// Creates a group chat channel
        /// </summary>
        /// <param name="clanID">The groups ID</param>
        public SteamClanChannel(ulong clanID)
            : base(Server.StripString(Server.clientFriends.GetClanName(clanID)))
        {
            this.clanID = clanID;
            chatID = new CSteamID(this.clanID.AccountID, 0x80000, EUniverse.k_EUniversePublic, EAccountType.k_EAccountTypeChat);
        }

        public override void AddClient(IClient c)
        {
            if (c.GetType().Equals(typeof(IRCLib.Interfaces.IRC.IRCClient)))
            {
                if (clients.Contains(c))
                    return;

                bool asdf = Server.clientFriends.JoinChatRoom(chatID);
                if (!asdf)
                    return;

                while (string.IsNullOrEmpty(Server.clientFriends.GetChatRoomName(chatID)))
                    System.Threading.Thread.Sleep(150);

                int numClients = Server.clientFriends.GetFriendCountFromSource(chatID);
                for (int j = 0; j < numClients; j++)
                {
                    ulong asdff = Server.clientFriends.GetFriendFromSourceByIndex(chatID, j);

                    if (asdff == Server.clientUser.GetSteamID())
                        continue;

                    IClient cl = (BaseIRCLib.Server.GetServer() as Server).GetSteamClient(asdff);

                    if (cl == null)
                    {
                        if (Server.clientList.Clients.ContainsKey(asdff))
                            cl = Server.clientList.Clients[asdff];
                        else
                            cl = new IRCLib.Interfaces.Steam.SteamClient(asdff);

                        EPersonaState st = Server.clientFriends.GetFriendPersonaState(asdff);
                        cl.AwayMsg = (st == EPersonaState.k_EPersonaStateOnline ? "" : st.ToString().Remove(0, 15));
                        Database.GetDatabase().AddClient(cl);
                        Database.GetDatabase().GetChannel("&Friends").AddClient(cl);
                    }

                    if (clients.Contains(cl))
                        continue;

                    uint memberDetails = 0;
                    uint localMemberDetails = 0;

                    if (Server.clientFriends.GetChatRoomMemberDetails(chatID, asdff, ref memberDetails, ref localMemberDetails))
                    {
                        if (memberDetails == 2 || memberDetails == 1)
                            ClientModes.Add(cl, "o");
                    }

                    base.AddClient(cl);
                }
            }
            else
            {
                uint memberDetails = 0;
                uint localMemberDetails = 0;

                if (Server.clientFriends.GetChatRoomMemberDetails(chatID, (c as IRCLib.Interfaces.Steam.SteamClient).SteamID, ref memberDetails, ref localMemberDetails))
                {
                    if ((memberDetails == 2 || memberDetails == 1) && !ClientModes.ContainsKey(c))
                        ClientModes.Add(c, "o");
                }
            }

            base.AddClient(c);
        }

        public override void RemoveClient(IClient c, string message = "")
        {
            if (c.GetType().Equals(typeof(IRCLib.Interfaces.IRC.IRCClient)))
            {
                foreach (IClient C in clients)
                {
                    if (C.GetType().Equals(typeof(IRCLib.Interfaces.IRC.IRCClient)) && c != C)
                    {
                        base.RemoveClient(c, message);
                        return;
                    }
                }
                Server.clientFriends.LeaveChatRoom(chatID);
            }

            base.RemoveClient(c, message);
        }

        public override void SendMessage(IClient C, IMessage m)
        {
            SendMessage(C, m, false);
        }

        public override void SendMessage(IClient C, IMessage m, bool toSender)
        {
            if (C.GetType().Equals(typeof(IRCLib.Interfaces.Steam.SteamClient)))
            {
                base.SendMessage(C, m, toSender);
                return;
            }

            if (m.IsCommand)
                if (m.Command == "PRIVMSG")
                {
                    byte[] msg = Encoding.UTF8.GetBytes(m.Params[1]);
                    EChatEntryType send = EChatEntryType.k_EChatEntryTypeChatMsg;

                    if (m.Params[1][0] == '' && m.Params[1][1] == 'A')
                    {
                        msg = Encoding.UTF8.GetBytes(m.Params[1].Substring(8, m.Params[1].Length - 9));
                        send = EChatEntryType.k_EChatEntryTypeEmote;
                    }

                    Server.clientFriends.SendChatMsg(chatID, send, msg, msg.Length + 1);
                }

            base.SendMessage(C, m, toSender);
        }
    }
}
