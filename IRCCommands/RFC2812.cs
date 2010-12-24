using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BaseIRCLib;

/// <summary>
/// RFC2812; Internet Relay Chat: Client protocol
/// </summary>
public class RFC2812
{
	[IRCCommandPlaceholder("PASS", 1)]
	public void PASS(IMessage message)
	{
		Console.WriteLine("Client password");
	}
	
    [IRCCommand("USER", 4)]
    public void USER(IMessage IMessage)
    {
    	if (IMessage.Owner.ClientType == ClientType.TYP_NONE)
        {
    		IMessage.Owner.UserName = IMessage.Params[0];
    		IMessage.Owner.RealName = IMessage.Params[3];

            IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, null, "PING", new string[] { "31337" }));
    		IMessage.Owner.ClientType = ClientType.TYP_CLIENT;
        }
        else
            IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, IMessage.Owner.UserString, Reply.ERR_ALREADYREGISTRED, new string[] { "Unauthorized command (already registered)" }));
    }

    [IRCCommand("NICK", 1)]
    public void NICK(IMessage IMessage)
    {
    	if (Database.GetDatabase().HasClient(IMessage.Params[0]) && Database.GetDatabase().GetClient(IMessage.Params[0]) != IMessage.Owner)
        {
    		IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, IMessage.Params[0], Reply.ERR_NICKNAMEINUSE, new string[] { IMessage.Owner.NickName, IMessage.Params[0], "Nickname already in use" }));
    		return;
    	}

        if (IMessage.Owner.Greeted)
        {
    		IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, IMessage.Owner.UserString, "NICK", new string[] { IMessage.Params[0] }));

            List<IClient> toInform = new List<IClient>();

            foreach (IChannel c in Database.GetDatabase().Channels)
            {
    			if (c.Clients.Contains(IMessage.Owner))
                {
    				foreach (IClient cl in c.Clients)
                    {
    					if (!toInform.Contains(cl) && cl != IMessage.Owner)
    						toInform.Add(cl);
    				}
    			}
    		}

            foreach (IClient cl in toInform)
            {
    			cl.SendMessage(IMessage.CreateMessage(IMessage.Owner, IMessage.Owner.UserString, "NICK", new string[] { IMessage.Params[0] }));
    		}
    	}

        IMessage.Owner.NickName = IMessage.Params[0];
    }
	
	[IRCCommand("MODE")]
	public void NoMode(IMessage message)
	{
		if (message.Params.Length > 0)
			message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.ERR_NOSUCHNICK, new string[] { message.Owner.NickName, message.Params[0], "No such nick/channel" }));
		else
			message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.ERR_UNKNOWNCOMMAND, new string[] { message.Owner.NickName, message.Command, "Wrong parameters" }));
	}
	
    [IRCCommand("MODE")]
    public bool ChangeChannelmodes(IMessage IMessage)
    {
    	if (IMessage.Params.Count() >= 2 && Database.GetDatabase().HasChannel(IMessage.Params[0]))
        {
    		IChannel ch = Database.GetDatabase().GetChannel(IMessage.Params[0]);

            if (!ch.Clients.Contains(IMessage.Owner))
            {
    			IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, Server.GetServer().HostString, Reply.ERR_NOTONCHANNEL, new string[] { IMessage.Owner.NickName, IMessage.Params[0], "You're not on that channel" }));
    			return true;
    		}
   
			if (IMessage.Params[1][0] == '+')
			{
    			if (IMessage.Params[1] == "+b")
				{
    				if (IMessage.Params.Length == 2)
					{
    					foreach (string s in ch.Banlist)
						{
    						IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, Server.GetServer().HostString, Reply.RPL_BANLIST, new string[] { IMessage.Owner.NickName, IMessage.Params[0], s }));
    					}
    	
						IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, Server.GetServer().HostString, Reply.RPL_ENDOFBANLIST, new string[] { IMessage.Owner.NickName, IMessage.Params[0], "End of channel ban list" }));
    				}
    			}
	            else if ((IMessage.Params[1] == "+o" && (IMessage.Owner.Modes.Contains('o')) || ch.ClientModes[IMessage.Owner].Contains('o')))
	            {
    				if (Database.GetDatabase().HasClient(IMessage.Params[2]))
    					if (ch.Clients.Contains(Database.GetDatabase().GetClient(IMessage.Params[2])))
						{
    						IClient target = Database.GetDatabase().GetClient(IMessage.Params[2]);
    					
							if (!ch.ClientModes[target].Contains('o'))
							{
    							ch.ClientModes[target] += "o";
    							ch.SendMessage(IMessage, true);
    						}
    					}
    			}
	            else if (IMessage.Params[1].Contains('l') && IMessage.Params.Length == 3 && (IMessage.Owner.Modes.Contains('o') || ch.ClientModes[IMessage.Owner].Contains('o')))
	            {
    				//ClientHelpers.ModifyChannelModes(IMessage.Owner, ch, (IMessage.Params[1].Contains('+') ? "+" : "-") + "l", IMessage.Params[2]);
    			}
	            else if (IMessage.Params[1].Contains('v') && ch.Clients.Contains(Database.GetDatabase().GetClient(IMessage.Params[2])) && (IMessage.Owner.Modes.Contains('o') || ch.ClientModes[IMessage.Owner].Contains('o')))
	            {
    				//ClientHelpers.ModifyChannelModes(IMessage.Owner, ch, (IMessage.Params[1].Contains('+') ? "+" : "-") + "v", IMessage.Params[2]);
    			}
    		}
			else if (IMessage.Params[1][0] == '-')
			{
    
			}
			else
			{
				
			}
			
			return true;
    	}

		return false;
    }
	
	[IRCCommand("MODE", 1)]
	public bool ListChannelmodes(IMessage message)
	{
		if (Database.GetDatabase().HasChannel(message.Params[0]))
		{
			IChannel ch = Database.GetDatabase().GetChannel(message.Params[0]);
			
			if (!ch.Clients.Contains(message.Owner))
            {
				message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.ERR_NOTONCHANNEL, new string[] { message.Owner.NickName, message.Params[0], "You're not on that channel" }));
				return true;
			}
			
			message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.RPL_CHANNELMODEIS, new string[] { message.Owner.NickName, message.Params[0], "+" + ch.GetModes() }));
			return true;
		}
		
		return false;
	}
	
	[IRCCommand("MODE", 2)]
	public bool ChangeUsermodes(IMessage message)
	{
		if (message.Params[0] == message.Owner.NickName)
		{
			bool add = (message.Params[1][0] == '+');

            for (int i = 1; i < message.Params[1].Length; i++)
            {
                char mode = message.Params[1][i];

                if ((add && "oO".Contains(mode)) || (!add && "r".Contains(mode)))
                    continue;

                if (add)
                    message.Owner.Modes += mode;
                else
                    message.Owner.Modes.Replace(mode.ToString(), "");
            }
			
			message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.RPL_UMODEIS, new string[] { message.Owner.NickName, "+" + message.Owner.Modes }));
			return true;
		}
		
		return false;
	}
	
	[IRCCommand("MODE", 1)]
	public bool ListUsermodes(IMessage message)
	{
		if (message.Params[0] == message.Owner.NickName)
		{
			message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.RPL_UMODEIS, new string[] { message.Owner.NickName, "+" + message.Owner.Modes }));
			return true;
		}
		
		return false;
	}

    [IRCCommandPlaceholder("MOTD")]
    public void MOTD(IMessage IMessage)
    {
        IClient client = IMessage.Owner;
        client.SendMessage(IMessage.CreateMessage(client, client.NickName, Reply.RPL_WELCOME, new string[] { client.NickName, "Welcome to this IRC Gateway " + client.UserString }));
        client.SendMessage(IMessage.CreateMessage(client, client.NickName, Reply.RPL_YOURHOST, new string[] { client.NickName, "Your host is " + BaseIRCLib.Server.GetServer().HostString + ", running version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() }));
        client.SendMessage(IMessage.CreateMessage(client, client.NickName, Reply.RPL_CREATED, new string[] { client.NickName, "This server was created " + BaseIRCLib.Server.GetServer().QueryServer("StartTime") }));
        client.SendMessage(IMessage.CreateMessage(client, client.NickName, Reply.RPL_MYINFO, new string[] { client.NickName, BaseIRCLib.Server.GetServer().HostString + " " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " iswo opsitnmlbvk" }));

        client.SendMessage(IMessage.CreateMessage(client, client.NickName, Reply.RPL_MOTDSTART, new string[] { client.NickName, "- " + BaseIRCLib.Server.GetServer().HostString + " Message of the day - " }));
        foreach (string m in System.IO.File.ReadAllLines("MOTD"))
        {
            client.SendMessage(IMessage.CreateMessage(client, client.NickName, Reply.RPL_MOTD, new string[] { client.NickName, " " + m }));
        }
        client.SendMessage(IMessage.CreateMessage(client, client.NickName, Reply.RPL_ENDOFMOTD, new string[] { client.NickName, "End of MOTD command" }));
    }

    [IRCCommand("JOIN", 1, 2)]
    public void JOIN(IMessage IMessage)
    {
        string[] channels = IMessage.Params[0].Split(',');
        string[] keys = null;
        if (IMessage.Params.Length == 2)
            keys = IMessage.Params[1].Split(',');

        foreach (string s in channels)
        {
            if (Database.GetDatabase().HasChannel(s))
            {
                IChannel ch = Database.GetDatabase().GetChannel(s);
                int i = Array.IndexOf<string>(channels, s);
                if (ch.Modes.ContainsKey('k') && ch.Modes['k'] == keys[i])
                {
                    ch.AddClient(IMessage.Owner);
                }
                else if (!ch.Modes.ContainsKey('k'))
                    ch.AddClient(IMessage.Owner);
                else
                    IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, Server.GetServer().HostString, Reply.ERR_BADCHANNELKEY, new string[] { IMessage.Owner.NickName, s, "Cannot join channel (+k)" }));
            }
            else
                IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, Server.GetServer().HostString, Reply.ERR_NOSUCHCHANNEL, new string[] { IMessage.Owner.NickName, s, "No such channel" }));
        }
    }

    [IRCCommand("PART", 1, 2)]
    public void PART(IMessage IMessage)
    {
        string[] channels = IMessage.Params[0].Split(',');

        foreach (string s in channels)
        {
            if (Database.GetDatabase().HasChannel(s))
                if (IMessage.Params.Length > 1)
                    Database.GetDatabase().GetChannel(s).RemoveClient(IMessage.Owner, IMessage.Params[1]);
                else
                    Database.GetDatabase().GetChannel(s).RemoveClient(IMessage.Owner);
        }
    }

    [IRCCommand("PRIVMSG", 2)]
    public void PRIVMSG(IMessage message)
    {
        if (Database.GetDatabase().HasChannel(message.Params[0]))
        {
            IChannel c = Database.GetDatabase().GetChannel(message.Params[0]);

            if (c.Clients.Contains(message.Owner))
                c.SendMessage(message.Owner, message);
        }
        else if (Database.GetDatabase().HasClient(message.Params[0]))
        {
            IClient cl = Database.GetDatabase().GetClient(message.Params[0]);
            cl.SendMessage(message);

            if (!string.IsNullOrEmpty(cl.AwayMsg))
                message.Owner.SendMessage(message.CreateMessage(cl, "", Reply.RPL_AWAY, new string[] { cl.NickName, cl.NickName, cl.AwayMsg }));
        }
    }

    [IRCCommand("WHOIS", 1)]
    public void WHOIS(IMessage msg)
    {
        if (Database.GetDatabase().HasClient(msg.Params[0]))
        {
            IClient c = Database.GetDatabase().GetClient(msg.Params[0]);

            msg.Owner.SendMessage(msg.CreateMessage(msg.Owner, null, Reply.RPL_WHOISUSER, new string[] {msg.Owner.NickName, c.NickName, c.UserName, c.HostName, "*", c.RealName }));

            string channels = "";
            foreach (IChannel ch in Database.GetDatabase().GetChannels(c))
                channels += (ch.ClientModes[c].Contains('o') ? "@" : "") + "&" + ch.ChannelName + " ";

            msg.Owner.SendMessage(msg.CreateMessage(msg.Owner, null, Reply.RPL_WHOISCHANNELS, new string[] { msg.Owner.NickName, c.NickName, channels }));
            msg.Owner.SendMessage(msg.CreateMessage(msg.Owner, null, Reply.RPL_WHOISSERVER, new string[] { msg.Owner.NickName, c.NickName, Server.GetServer().HostString, Server.GetServer().FullName }));

            if (c.Modes.Contains("o"))
                msg.Owner.SendMessage(msg.CreateMessage(msg.Owner, null, Reply.RPL_WHOISOPERATOR, new string[] { msg.Owner.NickName, c.NickName, "is an IRC operator" }));
            if (c.AwayMsg != "")
                msg.Owner.SendMessage(msg.CreateMessage(msg.Owner, null, Reply.RPL_AWAY, new string[] { msg.Owner.NickName, c.NickName, c.AwayMsg }));

            msg.Owner.SendMessage(msg.CreateMessage(msg.Owner, null, Reply.RPL_ENDOFWHOIS, new string[] { msg.Owner.NickName, c.NickName, "End of /WHOIS list" }));
        }
        else
            msg.Owner.SendMessage(msg.CreateMessage(msg.Owner, null, Reply.ERR_NOSUCHNICK, new string[] { msg.Owner.NickName, msg.Params[0], "No such nick/channel" }));
    }

    [IRCCommand("WHO", 1, 2)]
    public void WHO(IMessage IMessage)
    {
        if (Database.GetDatabase().HasChannel(IMessage.Params[0]))
        {
            IChannel ch = Database.GetDatabase().GetChannel(IMessage.Params[0]);

            foreach (IClient cl in ch.Clients)
            {
                if (IMessage.Params.Length == 1 || (IMessage.Params.Length == 2 && IMessage.Params[1] == "o" && cl.Modes.Contains("o")))
                    IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, "", Reply.RPL_WHOREPLY, new string[] { IMessage.Owner.NickName, IMessage.Params[0], cl.UserName, cl.HostName, Server.GetServer().HostString, cl.NickName, (string.IsNullOrEmpty(cl.AwayMsg) ? "H" : "G"), "0 " + cl.RealName }));
            }

            IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, "", Reply.RPL_ENDOFWHO, new string[] { IMessage.Owner.NickName, "End of WHO list" }));
        }
        else
        {
            Wildcard c = new Wildcard(IMessage.Params[0]);

            foreach (IClient cl in Database.GetDatabase().Clients)
            {
                if (c.IsMatch(cl.UserString))
                    if (IMessage.Params.Length == 1 || (IMessage.Params.Length == 2 && IMessage.Params[1] == "o" && cl.Modes.Contains("o")))
                        IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, Server.GetServer().HostString, Reply.RPL_WHOREPLY, new string[] { IMessage.Owner.NickName, IMessage.Params[0], cl.UserName, cl.HostName, Server.GetServer().HostString, cl.NickName, (string.IsNullOrEmpty(cl.AwayMsg) ? "H" : "G"), "0 " + cl.RealName }));
            }

            IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, Server.GetServer().HostString, Reply.RPL_ENDOFWHO, new string[] { IMessage.Owner.NickName, "End of WHO list" }));
        }
    }

    [IRCCommandPlaceholder("OPER", 2)]
    public void OPER(IMessage IMessage)
    {
    	if (IMessage.Params[0] == "Iamnotauser" && IMessage.Params[1] == "Iamnotapass")
        {
    		IClient client = IMessage.Owner;
    		client.Modes += "o";
    		client.SendMessage(IMessage.CreateMessage(client, Server.GetServer().HostString, Reply.RPL_UMODEIS, new string[] { client.NickName, "+" + client.Modes }));
    		return;
		}

        IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, null, Reply.ERR_NOOPERHOST, new string[] { IMessage.Owner.NickName, "No O-lines for your host" }));
    }

    [IRCCommand("AWAY", 0, 1)] //Command, minimum amount of parameters, maximum amount of parameters
    public void irccommandAWAY(IMessage msg)
    {
        if (msg.Params.Length > 0 && !string.IsNullOrEmpty(msg.Params[0]))
        {
            msg.Owner.AwayMsg = msg.Params[0];
            msg.Owner.SendMessage(msg.CreateMessage(msg.Owner, null, Reply.RPL_NOWAWAY, new string[] { msg.Owner.NickName, "You have been marked as being away" }));
        }
        else
        {
            msg.Owner.AwayMsg = string.Empty;
            msg.Owner.SendMessage(msg.CreateMessage(msg.Owner, null, Reply.RPL_UNAWAY, new string[] { msg.Owner.NickName, "You are no longer marked as being away" }));
        }
    }


    [IRCCommand("NOTICE", 2)]
    public void NOTICE(IMessage message)
    {
        message.Prefix = message.Owner.UserString;

        if (Database.GetDatabase().HasChannel(message.Params[0]))
        {
            IChannel c = Database.GetDatabase().GetChannel(message.Params[0]);

            if (c.Clients.Contains(message.Owner))
                c.SendMessage(message.Owner, message);
        }
        else if (Database.GetDatabase().HasClient(message.Params[0]))
        {
            IClient cl = Database.GetDatabase().GetClient(message.Params[0]);
            
            cl.SendMessage(message);
        }
    }

    [IRCCommand("PING", 1)]
    public void PING(IMessage IMessage)
    {
        IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, "PONG :" + IMessage.Params[0]));
        IMessage.Owner.LastPing = DateTime.Now;
    }

    [IRCCommand("PONG", 1)]
    public void PONG(IMessage IMessage)
    {
        IMessage.Owner.MissedPings = 0;
        IMessage.Owner.LastPong = DateTime.Now;
    }

    [IRCCommand("QUIT", 0, 1)]
    public void QUIT(IMessage IMessage)
    {
        if (IMessage.Params.Length == 1)
            IMessage.Owner.Dispose(IMessage.Params[0]);
        else
            IMessage.Owner.Dispose();
    }

    [IRCCommandPlaceholder("ERROR", 1)]
    public void ERROR(IMessage IMessage)
    {
        Console.WriteLine("{0}", IMessage.ShortMessageString);
    }

    [IRCCommandPlaceholder("INFO", 0, 1)]
    public void INFO(IMessage IMessage)
    {
        IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, Server.GetServer().HostString, Reply.RPL_INFO, new string[] { IMessage.Owner.NickName, "This is a server yo!" }));
        IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, Server.GetServer().HostString, Reply.RPL_INFO, new string[] { IMessage.Owner.NickName, "You use it for chatting" }));
        /*System.Resources.ResourceManager man = new System.Resources.ResourceManager("Build", System.Reflection.Assembly.GetCallingAssembly());
        string time = man.GetString("BUILD_TIME");
        time = time.Substring(0, time.Length - 6);
        IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, Database.GetDatabase().Name, Reply.RPL_INFO, new string[] { "Build date: " + time + ". Build #" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Build }, true));*/
        IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, Server.GetServer().HostString, Reply.RPL_INFO, new string[] { IMessage.Owner.NickName, "On-line since " + Server.GetServer().QueryServer("StartTime") }));
        IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, Server.GetServer().HostString, Reply.RPL_ENDOFINFO, new string[] { IMessage.Owner.NickName, "End of /INFO list" }));
    }

    [IRCCommand("USERHOST", 1)]
    public void USERHOST(IMessage IMessage)
    {
        string reply = "";
        foreach (string nick in IMessage.Params)
        {
            if (Database.GetDatabase().HasClient(nick))
            {
                IClient cl = Database.GetDatabase().GetClient(nick);
                reply += string.Format("{0}{1}={2}{3} ", nick, (cl.Modes.Contains('o') ? "*" : ""), (string.IsNullOrEmpty(cl.AwayMsg) ? "+" : "-"), cl.HostName);
            }
        }

        IMessage.Owner.SendMessage(IMessage.CreateMessage(IMessage.Owner, IMessage.Owner.NickName, Reply.RPL_USERHOST, new string[] { IMessage.Owner.NickName, reply }));
    }

    [IRCCommandPlaceholder("SERVICE", 6)]
    public void SERVICE(IMessage message)
    {
    	if (message.Owner.ClientType == ClientType.TYP_NONE)
		{
    		message.Owner.ClientType = ClientType.TYP_SERVICE;
    		message.Owner.Dispose("Tried to provide a naughty, naughty, little service o3o");
    	}
		else
        	message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.ERR_ALREADYREGISTRED, new string[] { message.Owner.NickName, "Unauthorized command (already registered)" }));
    }

    [IRCCommandPlaceholder("SQUIT", 2)]
    public void SQUIT(IMessage message)
    {
        message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.ERR_NOPRIVILEGES, new string[] { message.Owner.NickName, "Permission Denied- You're not an IRC operator" }));
    }

    [IRCCommand("TOPIC", 1, 2)]
    public void TOPIC(IMessage message)
    {
        if (!Database.GetDatabase().HasChannel(message.Params[0]))
        {
            message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.ERR_NOSUCHCHANNEL, new string[] { message.Owner.NickName, message.Params[0], "No such channel" }));
            return;
        }

        IChannel ch = Database.GetDatabase().GetChannel(message.Params[0]);

        if (ch.Clients.Contains(message.Owner))
        {
            if (message.Params.Length > 1 && ch.ClientModes[message.Owner].Contains('o'))
                ch.Topic = message.Params[1];
            else if (message.Params.Length > 1 && !ch.ClientModes[message.Owner].Contains('o'))
            {
                message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.ERR_CHANOPRIVSNEEDED, new string[] { message.Owner.NickName, message.Params[0], "You're not channel operator" }));
                return;
            }

            message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.RPL_TOPIC, new string[] { message.Owner.NickName, message.Params[0], ch.Topic }));
        }
        else if (!ch.Clients.Contains(message.Owner))
            message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.ERR_NOTONCHANNEL, new string[] { message.Owner.NickName, message.Params[0], "You're not on that channel" }));
    }

    [IRCCommandPlaceholder("NAMES", 0, 2)] //[ <channel> *( "," <channel> ) [ <target> ] ]
    public void NAMES(IMessage message)
    {
        message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.ERR_TOOMANYMATCHES, new string[] { message.Owner.NickName, message.Command, (message.Params.Length > 0 ? message.Params[0] : "*"), "Too many matches" }));
    }

    [IRCCommandPlaceholder("LIST", 0, 2)] //[ <channel> *( "," <channel> ) [ <target> ] ]
    public void LIST(IMessage message)
    {
        string[] channels = null;
        if (message.Params.Length > 0)
            channels = message.Params[0].Split(',');

        if (channels == null)
        {
            foreach (IChannel ch in Database.GetDatabase().Channels)
                message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.RPL_LIST, new string[] { message.Owner.NickName, ch.ChannelName, ch.Clients.Length.ToString(), ch.Topic }));
        }
        else
        {
            foreach (string s in channels)
            {
                if (Database.GetDatabase().HasChannel(s))
                {
                    IChannel ch = Database.GetDatabase().GetChannel(s);
                    message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.RPL_LIST, new string[] { message.Owner.NickName, ch.ChannelName, ch.Clients.Length.ToString(), ch.Topic }));
                }
            }
        }

        message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.RPL_LISTEND, new string[] { message.Owner.NickName, "End of LIST" }));
    }

    [IRCCommand("INVITE", 2)]
    public void INVITE(IMessage message)
    {
        if (!Database.GetDatabase().HasClient(message.Params[0]) || !Database.GetDatabase().HasChannel(message.Params[1]))
        {
            message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.ERR_NOSUCHNICK, new string[] { message.Owner.NickName, (Database.GetDatabase().HasChannel(message.Params[1]) ? message.Params[0] : message.Params[1]), "No such nick/channel" }));
            return;
        }

        IClient cl = Database.GetDatabase().GetClient(message.Params[0]);
        IChannel ch = Database.GetDatabase().GetChannel(message.Params[1]);

        if (ch.Clients.Contains(cl))
        {
            message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.ERR_USERONCHANNEL, new string[] { message.Owner.NickName, cl.NickName, message.Params[1], "Is already on channel" }));
            return;
        }

        if (!ch.Clients.Contains(message.Owner))
        {
            message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.ERR_NOTONCHANNEL, new string[] { message.Owner.NickName, message.Params[1], "You're not on that channel" }));
            return;
        }

        if (ch.Modes.ContainsKey('i') && !ch.ClientModes[message.Owner].Contains('o'))
        {
            message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.ERR_CHANOPRIVSNEEDED, new string[] { message.Owner.NickName, message.Params[1], "You're not channel operator" }));
            return;
        }

        cl.SendMessage(message);
        message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.RPL_INVITING, new string[] { message.Owner.NickName, message.Params[1], cl.NickName }));
		ch.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.RPL_INVITED, new string[] { "*", message.Params[1], cl.NickName, message.Owner.NickName, cl.NickName + " has been invited by " + message.Owner.NickName }));
    }

    [IRCCommandPlaceholder("KICK", 2, 3)] //TODO: Return values
    public void KICK(IMessage message)
    {
        string[] channels = message.Params[0].Split(',');
        string[] users = message.Params[1].Split(',');

        if (channels.Length == 1)
        {
            if (Database.GetDatabase().HasChannel(channels[0]))
            {
                IChannel ch = Database.GetDatabase().GetChannel(channels[0]);

                foreach (string u in users)
                {
                    if (Database.GetDatabase().HasClient(u))
                        ch.KickClient(Database.GetDatabase().GetClient(u), message.Owner, (message.Params.Length == 3 ? message.Params[2] : string.Empty));
                }
            }
        }
        else if (channels.Length == users.Length)
            for (int i = 0; i < channels.Length; i++)
            {
                if (Database.GetDatabase().HasChannel(channels[i]) && Database.GetDatabase().HasClient(users[i]))
                {
                    IChannel ch = Database.GetDatabase().GetChannel(channels[i]);
                    IClient cl = Database.GetDatabase().GetClient(users[i]);
                    ch.KickClient(cl, message.Owner, (message.Params.Length == 3 ? message.Params[2] : string.Empty));
                }
            }
    }

    [IRCCommandPlaceholder("LUSERS", 0, 2)] //TODO: Get LUSERS working
    public void LUSERS(IMessage message)
    {
        return;
    }

    [IRCCommandPlaceholder("VERSION", 0, 1)] //TODO: [<target>] value
    public void VERSION(IMessage message)
    {
        message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.RPL_VERSION, new string[] { message.Owner.NickName, "0." + System.Reflection.Assembly.GetCallingAssembly().GetName().Version.Build, Server.GetServer().HostString, "Not a final product" }));
    }

    [IRCCommandPlaceholder("STATS", 0, 2)] //TODO: Everything
    public void STATS(IMessage message)
    {
        message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.RPL_ENDOFSTATS, new string[] { message.Owner.NickName, "End of STATS report" }));
    }

    [IRCCommandPlaceholder("TIME", 0, 1)] //TODO: [<target>] value
    public void TIME(IMessage message)
    {
        message.Owner.SendMessage(message.CreateMessage(message.Owner, Server.GetServer().HostString, Reply.RPL_TIME, new string[] { message.Owner.NickName, Server.GetServer().HostString, DateTime.Now.ToLongDateString() + " - " + DateTime.Now.ToLongTimeString() }));
    }

    [IRCCommandPlaceholder("BAN", 2)] //FIXME
    public void BAN(IMessage message)
    {

    }
}
