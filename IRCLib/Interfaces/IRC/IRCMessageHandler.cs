using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRCLib.Interfaces.IRC
{
    /*class IRCMessageHandler : IMessageHandler
    {
        bool greeted;
        bool pinging;
        IClient O;

        DateTime LastPing;
        DateTime LastCmd;

        TimeSpan IdleTime { get { return DateTime.Now - LastCmd; } }
        TimeSpan PingTime { get { return DateTime.Now - LastPing; } }

        public IRCMessageHandler(IClient owner)
        {
            O = owner;
        }

        public void HandleMessage(Message msg)
        {
            if (msg.IsCommand)
            {
                switch (msg.Command)
                {
                    case Command.NICK:
                        foreach (IClient c in Server.clients)
                        {
                            if (c.NickName.Equals(msg.Params[0], StringComparison.CurrentCultureIgnoreCase))
                                O.SendMessage(Message.CreateMessage(O,Server.Name, Reply.ERR_NICKNAMEINUSE, new string[] { msg.Params[0], ":Nickname is already in use." }));
                            else
                                O.NickName = msg.Params[0];
                        }
                        break;

                    case Command.USER:
                        if (msg.Params.Count() != 4)
                        {
                            O.SendMessage(Message.CreateMessage(O,Server.Name, Reply.ERR_NEEDMOREPARAMS, new string[] { "USER", ":Not enough parameters" }));
                            break;
                        }

                        O.UserName = msg.Params[0];
                        O.RealName = msg.Params[3].Remove(0,1);

                        O.SendMessage(Message.CreateMessage(O,"", Command.PING, new string[] { ":PING"}));
                        break;

                    case Command.PONG:
                        if (!msg.Params[0].EndsWith(":PING"))
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

                                    if (mode == "+" && !O.Modes.Contains(m) && m != 'o')
                                        O.Modes += m;
                                    else if (mode == "-" && O.Modes.Contains(m))
                                        O.Modes = O.Modes.Replace(new string(m, 1), "");
                                }
                            }
                            else if (msg.Params.Count() > 2)
                            {
                                O.SendMessage(Message.CreateMessage(O,"", Reply.ERR_NEEDMOREPARAMS, new string[] { "MODE", ":Too many parameters" }));
                                break;
                            }

                            O.SendMessage(Message.CreateMessage(O,O.NickName, Reply.RPL_UMODEIS, new string[] { msg.Params[0], "+" + O.Modes }));
                        }
                        else if (Server.HasChannel(msg.Params[0]))
                        {
                            Channel c = Server.GetChannel(msg.Params[0]);

                            string modes = msg.Params[1].Remove(0, 1);
                            string mode = msg.Params[1].Substring(0, 1);

                            int i = 2;

                            if (c.ClientModes.ContainsKey(O) && c.ClientModes[O].Contains('o'))
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
                                            if (c.ClientModes.ContainsKey(O) && !c.ClientModes[O].Contains(m))
                                            {
                                                c.ClientModes[O] += m;
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
                                            if (c.ClientModes.ContainsKey(O) && c.ClientModes[O].Contains(m))
                                            {
                                                c.ClientModes[O] = c.ClientModes[O].Replace("" + m, "");
                                            }
                                        }

                                    if ("okvbl".Contains(m))
                                        i++;
                                }

                            O.SendMessage(Message.CreateMessage(O,"", Reply.RPL_UMODEIS, new string[] { msg.Params[0], "+" + c.GetModes() }));
                        }
                        else
                            O.SendMessage(Message.CreateMessage(O,"", Reply.ERR_NOSUCHNICK, new string[] { msg.Params[0], ":No such nick/channel" }));
                        break;

                    case Command.WHOIS:
                        if (Server.HasClient(msg.Params[0]))
                        {
                            IClient c = Server.GetClient(msg.Params[0]);

                            O.SendMessage(Message.CreateMessage(O,"", Reply.RPL_WHOISUSER, new string[] { c.NickName, c.UserName, c.HostName, "*", ":" + c.RealName }));
                            O.SendMessage(Message.CreateMessage(O,"", Reply.RPL_WHOISSERVER, new string[] { c.NickName, Server.Name, ":" + Server.FullName }));

                            if (c.Modes.Contains("o"))
                                O.SendMessage(Message.CreateMessage(O,"", Reply.RPL_WHOISOPERATOR, new string[] { c.NickName, ":is an IRC operator" }));

                            O.SendMessage(Message.CreateMessage(O,"", Reply.RPL_ENDOFWHOIS, new string[] { c.NickName, ":End of /WHOIS list" }));
                        }
                        else
                            O.SendMessage(Message.CreateMessage(O,"", Reply.ERR_NOSUCHNICK, new string[] { msg.Params[0], ":No such nick/channel" }));
                        break;

                    case Command.QUIT:
                        O.Dispose();
                        break;

                    case Command.MOTD:
                        string[] data = System.IO.File.ReadAllLines("MOTD.txt");

                        O.SendMessage(Message.CreateMessage(O,"", Reply.RPL_MOTDSTART, new string[] { ":- Begin message of the day - " }, true));

                        foreach (string s in data)
                        {
                            O.SendMessage(Message.CreateMessage(O,"", Reply.RPL_MOTD, new string[] { ":- " + s }, true));
                        }

                        O.SendMessage(Message.CreateMessage(O,"", Reply.RPL_ENDOFMOTD, new string[] { ":End of /MOTD command." }, true));
                        break;

                    case Command.PRIVMSG:
                        if (Server.HasClient(msg.Params[0]))
                        {
                            IClient c = Server.GetClient(msg.Params[0]);

                            c.SendMessage(Message.CreateMessage(O, O.UserString, Command.PRIVMSG, new string[] { O.NickName, msg.Params[1] }));

                            if (c.AwayMsg != string.Empty)
                                O.SendMessage(Message.CreateMessage(O, O.UserString, Reply.RPL_AWAY, new string[] { c.NickName, ":" + c.AwayMsg }));
                        }
                        else if (Server.HasChannel(msg.Params[0]))
                        {
                            Channel c = Server.GetChannel(msg.Params[0]);

                            if (c.clients.Contains(O))
                            {
                                c.SendMessage(O, Message.CreateMessage(O, O.UserString, Command.PRIVMSG, new string[] { c.ChannelName, msg.Params[1] }));
                            }
                            else
                                O.SendMessage(Message.CreateMessage(O, "", Reply.ERR_NOTONCHANNEL, new string[] { c.ChannelName, ":You're not on that channel" }));
                        }
                        else
                            O.SendMessage(Message.CreateMessage(O,"", Reply.ERR_NOSUCHNICK, new string[] { msg.Params[0], ":No such nick/channel" }));
                        break;

                    case Command.JOIN:
                        if (Server.HasChannel(msg.Params[0]))
                        {
                            Channel c = Server.GetChannel(msg.Params[0]);

                            if (c.clients.Contains(O))
                                break;

                            if (c.Modes.ContainsKey('k') && msg.Params.Count() > 1)
                                if (c.Modes['k'] == msg.Params[1])
                                    c.AddClient(O);
                                else
                                    O.SendMessage(Message.CreateMessage(O, "", Reply.ERR_BADCHANNELKEY, new string[] { msg.Params[0], ":Cannot join channel (+k)" }));
                            else
                                c.AddClient(O);
                        }
                        else
                            O.SendMessage(Message.CreateMessage(O, "", Reply.ERR_NOSUCHCHANNEL, new string[] { msg.Params[0], ":No such channel" }));
                        break;

                    case Command.PART:
                        if (Server.HasChannel(msg.Params[0]))
                        {
                            Channel c = Server.GetChannel(msg.Params[0]);

                            if (c.clients.Contains(O))
                                c.RemoveClient(O);
                            else
                                O.SendMessage(Message.CreateMessage(O, "", Reply.ERR_NOTONCHANNEL, new string[] { msg.Params[0], ":You're not on that channel" }));
                        }
                        else
                            O.SendMessage(Message.CreateMessage(O, "", Reply.ERR_NOSUCHCHANNEL, new string[] { msg.Params[0], ":No such channel" }));
                        break;
                }

                if (msg.Command != Command.PONG)
                    LastCmd = DateTime.Now;

                Greet();
            }
        }

        private void Greet()
        {
            if (greeted || ((O.NickName == string.Empty || O.UserName == string.Empty || O.RealName == string.Empty) && !pinging))
                return;

            greeted = true;
            HandleMessage(Message.CreateMessage(O,"MOTD"));
        }
    }*/
}
