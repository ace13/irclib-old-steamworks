using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IRCLib.Interfaces;
using BaseIRCLib;

namespace IRCLib.Helpers
{
    public class ClientHelpers
    {
        public static bool IsUtf8(byte[] buffer, int length)
        {
            int position = 0;
            int bytes = 0;
            while (position < length)
            {
                if (!IsValid(buffer, position, length, ref bytes))
                {
                    return false;
                }
                position += bytes;
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="position"></param>
        /// <param name="length"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static bool IsValid(byte[] buffer, int position, int length, ref int bytes)
        {
            if (length > buffer.Length)
            {
                throw new ArgumentException("Invalid length");
            }

            if (position > length - 1)
            {
                bytes = 0;
                return true;
            }

            byte ch = buffer[position];

            if (ch <= 0x7F)
            {
                bytes = 1;
                return true;
            }

            if (ch >= 0xc2 && ch <= 0xdf)
            {
                if (position >= length - 2)
                {
                    bytes = 0;
                    return false;
                }
                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }
                bytes = 2;
                return true;
            }

            if (ch == 0xe0)
            {
                if (position >= length - 3)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0xa0 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }
                bytes = 3;
                return true;
            }


            if (ch >= 0xe1 && ch <= 0xef)
            {
                if (position >= length - 3)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }

                bytes = 3;
                return true;
            }

            if (ch == 0xf0)
            {
                if (position >= length - 4)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0x90 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }

                bytes = 4;
                return true;
            }

            if (ch == 0xf4)
            {
                if (position >= length - 4)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0x8f ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }

                bytes = 4;
                return true;
            }

            if (ch >= 0xf1 && ch <= 0xf3)
            {
                if (position >= length - 4)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }

                bytes = 4;
                return true;
            }

            return false;
        }

        private static string ModifyString(string inp, string allowed = "isw", char[] add = null, char[] del = null)
        {
            string outp = inp;

            if (add != null)
            foreach (char c in add)
                if (!outp.Contains(c) && allowed.Contains(c))
                    outp += c;

            if (del != null)
            foreach (char c in del)
                if (outp.Contains(c) && allowed.Contains(c))
                    outp = outp.Remove(outp.IndexOf(c), 1);

            return outp;
        }

        public static void ModifyClientModes(IClient client, string modes)
        {
            if (modes == null) { }
            else if (modes.StartsWith("+"))
            {
                client.Modes = ModifyString(client.Modes, "isw", modes.Remove(0, 1).ToCharArray());
            }
            else if (modes.StartsWith("-"))
            {
                client.Modes = ModifyString(client.Modes, "iswo", null, modes.Remove(0, 1).ToCharArray());
            }

            client.SendMessage(IRCMessage.GetStatic().CreateMessage(client, client.NickName, Reply.RPL_UMODEIS, new string[] { client.NickName, "+" + client.Modes }));
        }

        public static void ModifyChannelModes(IClient client, Channel channel, string modes, string more = null)
        {
            if (modes.Length == 2)
            {
                string mode = modes.Substring(0, 1);
                string mode2 = modes.Substring(1, 1);

                if ("psitnm".Contains(mode2))
                    foreach (char m in more)
                    {
                        if (mode == "+")
                        {
                            if (!channel.Modes.ContainsKey(m))
                                channel.Modes.Add(m, "");
                        }
                        else if (mode == "-")
                            if (channel.Modes.ContainsKey(m))
                                channel.Modes.Remove(m);
                    }
                else if ("ov".Contains(mode2))
                {
                    
                }
                else if (mode2 == "l")
                {

                }
                else if (mode2 == "b")
                {

                }
                else if (mode2 == "k")
                {

                }
            }

            client.SendMessage(IRCMessage.GetStatic().CreateMessage(client, "", Reply.RPL_CHANNELMODEIS, new string[] { client.NickName, "&" + channel.ChannelName, "+" + channel.GetModes() }));
        }

        public static void MeetAndGreet(IClient client)
        {
            if (!client.Greeted)
            {
                client.Greeted = true;

                Console.WriteLine("{0} Has finished connecting as {1}", client.HostName, client.NickName);

                client.AddMessage(IRCMessage.GetStatic().CreateMessage(client, "MOTD"));

                string joins = "&Friends";

                int numChats = Server.clientFriends.GetChatRoomCount();
                for (int i = 0; i < numChats; i++)
                {
                    Steam4NET.CSteamID id = Server.clientFriends.GetChatRoomByIndex(i);

                    IChannel c = (BaseIRCLib.Server.GetServer() as Server).GetSteamChannel(id);
                    if (c == null)
                        continue;

                    joins += ",&" + c.ChannelName;
                }

                client.AddMessage(IRCMessage.GetStatic().CreateMessage(client, client.UserString, "JOIN", new string[] { joins }));
                client.LastPong = DateTime.Now;
            }
        }

        public static void SendMOTD(IClient client, string[] motd)
        {
            
        }

        public static void JoinClientToChannel(IMessage message)
        {
            //#Chan,#Chan2 ChanKey,ChanKey2
            string[] channels = null;
            string[] chanKeys = null;

            if (message.Params.Count() > 1)
            {
                if (message.Params[1].Contains(','))
                    chanKeys = message.Params[1].Split(',');
                else
                    chanKeys = new string[] { message.Params[1] };
            }

            if (message.Params[0].Contains(','))
                channels = message.Params[0].Split(',');
            else
                channels = new string[] {message.Params[0]};

            foreach (string s in channels)
            {
                if (Database.GetDatabase().HasChannel(s.Remove(0, 1)))
                {
                    IChannel j = Database.GetDatabase().GetChannel(s.Remove(0, 1));
                    if (chanKeys != null && chanKeys.Count() > Array.IndexOf<string>(channels, s))
                    {
                        if (j.Modes.ContainsKey('k') && j.Modes['k'] == chanKeys[Array.IndexOf<string>(channels, s)])
                        {
                            j.AddClient(message.Owner);
                        }
                        else
                            message.Owner.SendMessage(IRCMessage.GetStatic().CreateMessage(message.Owner, "", Reply.ERR_BADCHANNELKEY, new string[] { s, "Cannot join channel (+k)" })); 
                    }
                    else
                    {
                        j.AddClient(message.Owner);
                    }
                }
                else
                    message.Owner.SendMessage(IRCMessage.GetStatic().CreateMessage(message.Owner, "", Reply.ERR_NOSUCHCHANNEL, new string[] { s, "No such channel" }));
            }
        }

        public static void PartClient(IMessage message)
        {
            PartClient(message.Owner, message.Params[0], (message.Params.Count() > 1 ? message.Params[1] : ""));
        }

        public static void PartClient(IClient client, string channel, string message = "")
        {
            if (!Database.GetDatabase().HasChannel(channel.Remove(0, 1)))
                return;

            IChannel c = Database.GetDatabase().GetChannel(channel.Remove(0, 1));
            if (c.Clients.Contains(client))
            {
                c.RemoveClient(client, message);
            }
        }

        public static void PartClient(IClient client, string message = "")
        {
            foreach (IChannel c in Database.GetDatabase().GetChannels(client))
            {
                c.RemoveClient(client, message);
            }
        }

        public static void SendMessage(IMessage message)
        {
            //PRIVMSG (#chan|pers) :message
            
        }
    }
}
