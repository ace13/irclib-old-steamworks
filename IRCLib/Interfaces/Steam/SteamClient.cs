using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Steam4NET;
using IRCLib.Helpers;
using BaseIRCLib;

namespace IRCLib.Interfaces.Steam
{
    /// <summary>
    /// A fake client for steam users
    /// </summary>
    public class SteamClient : IClient
    {
        CSteamID steamID;
        bool disposed;
        int failedNicks = 0;

        /// <summary>
        /// The clients SteamID
        /// </summary>
        public CSteamID SteamID { get { return steamID; }
            set
            { 
                steamID = value;
                EPersonaState st = Server.clientFriends.GetFriendPersonaState(steamID);
                AwayMsg = (st == EPersonaState.k_EPersonaStateOnline ? "" : st.ToString().Remove(0, 15));
                RealName = Server.clientFriends.GetFriendPersonaName(steamID);
            }
        }
        Queue<IMessage> messageQueue;

        [XmlIgnore]
        public string RealName { get; set; }
        [XmlIgnore]
        public string UserName { get { return steamID.Render().Replace(":", "_"); } set { } }
        public string NickName { get; set; }
        [XmlIgnore]
        public string HostName { get { return "steampowered.com"; } set { } }
        [XmlIgnore]
        public string AwayMsg { get; set; }
        [XmlIgnore]
        public string DisconnectMsg { get; set; }
        [XmlIgnore]
        public string Modes { get { return "i"; } set { } }
        [XmlIgnore]
        public DateTime LastPong { get { return DateTime.Now; } set { } }
        [XmlIgnore]
        public DateTime LastPing { get { return DateTime.Now; } set { } }
        [XmlIgnore]
        public DateTime LastCommand { get; set; }
        [XmlIgnore]
        public bool Greeted { get { return true; } set { } }
        [XmlIgnore]
        public string UserString { get { return string.Format("{0}!{1}@{2}", NickName, UserName, HostName); } }
        [XmlIgnore]
        public bool IsDisposed { get { return disposed; } }
        [XmlIgnore]
        public int MissedPings { get { return 0; } set { } }
        [XmlIgnore]
        public ClientType ClientType { get { return ClientType.TYP_CLIENT; } set { } }

        public SteamClient(CSteamID steamID)
        {
            SteamID = steamID;
            messageQueue = new Queue<IMessage>();

            NickName = Server.StripString(RealName);
        }

        public SteamClient()
        {
            messageQueue = new Queue<IMessage>();
        }

        public void AddMessage(IMessage msg)
        {
            messageQueue.Enqueue(msg);
        }

        public bool HasMessage()
        {
            return messageQueue.Count > 0;
        }

        public IMessage GetMessage()
        {
            return messageQueue.Dequeue();
        }

        public void SendMessage(IMessage m)
        {
            if (m.IsCommand)
            {
                if (m.Command == "PRIVMSG" || m.Command == "NOTICE")
                {
                    byte[] msg = Encoding.UTF8.GetBytes(m.Params[1]);
                    EChatEntryType send = EChatEntryType.k_EChatEntryTypeChatMsg;

                    if (m.Params[1][0] == '' && m.Params[1][1] == 'A')
                    {
                        msg = Encoding.UTF8.GetBytes(m.Params[1].Substring(8, m.Params[1].Length - 9));
                        send = EChatEntryType.k_EChatEntryTypeEmote;
                    }

                    Server.clientFriends.SendMsgToFriend(steamID, send, msg, msg.Length + 1);
                }
            }
            else if (m.IsReply)
            {
                if (m.Reply == Reply.ERR_NICKNAMEINUSE)
                {
                    failedNicks++;
                    AddMessage(m.CreateMessage(this, this.UserString, "NICK", new string[] { Server.StripString(Server.clientFriends.GetFriendPersonaName(steamID)) + "[" + failedNicks + "]" }));
                }
            }

        }

        public void Dispose()
        {
            disposed = true;
        }

        public void Dispose(string message)
        {
            DisconnectMsg = message;
            disposed = true;
        }
    }
}
