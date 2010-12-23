using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BaseIRCLib
{
    /// <summary>
    /// A client connected to the IRC network.
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// The clients nickname, this is a unique identifier over the IRC network.
        /// </summary>
        string NickName { get; set; }
        /// <summary>
        /// The clients username.
        /// </summary>
        string UserName { get; set; }
        /// <summary>
        /// A string containing the clients real name (spaces and such allowed).
        /// </summary>
        string RealName { get; set; }
        /// <summary>
        /// This is set to the clients hostname during connection.
        /// </summary>
        string HostName { get; }
        /// <summary>
        /// The clients current away message.
        /// </summary>
        string AwayMsg { get; set; }
        /// <summary>
        /// The message the client left while disconnecting.
        /// </summary>
        string DisconnectMsg { get; set; }
        /// <summary>
        /// A string containing the clients modes.
        /// </summary>
        string Modes { get; set; }
        /// <summary>
        /// When the client was last pinged.
        /// </summary>
        DateTime LastPing { get; set; }
        /// <summary>
        /// When the client last responded to a ping.
        /// </summary>
        DateTime LastPong { get; set; }
        /// <summary>
        /// The last command the client executed.
        /// </summary>
        DateTime LastCommand { get; set; }
        /// <summary>
        /// If the client has gone through a successful authentication or not.
        /// </summary>
        bool Greeted { get; set; }
        /// <summary>
        /// A string formatted as such: "Nickname!Username@Hostname".
        /// </summary>
        string UserString { get; }
        /// <summary>
        /// If the client is disposed (Disconnected and all resources cleared).
        /// </summary>
        bool IsDisposed { get; }
        /// <summary>
        /// The number of pings the client has missed responding to.
        /// </summary>
        int MissedPings { get; set; }

        /// <summary>
        /// Add a message to the clients message queue.
        /// This is (to the server) equivalent to the client sending the message itself
        /// </summary>
        /// <param name="m">The message to add</param>
        void AddMessage(IMessage m);
        /// <summary>
        /// Checks the clients message queue for messages.
        /// </summary>
        /// <returns>If there is a message </returns>
        bool HasMessage();
        /// <summary>
        /// Removes a message from the clients message queue and returns it.
        /// </summary>
        /// <returns>The message that was removed from the clients message queue, or null if there was none</returns>
        IMessage GetMessage();

        /// <summary>
        /// Sends a message to the client
        /// </summary>
        /// <param name="m">The message to send</param>
        void SendMessage(IMessage m);
        /// <summary>
        /// Disconnects the client from the IRC network and clears all loaded resources
        /// </summary>
        void Dispose();
        /// <summary>
        /// Disconnects the client from the IRC network and clears all loaded resources
        /// </summary>
        /// <param name="message">The message that was left as a reason for the dispose</param>
        void Dispose(string message);
    }

    /// <summary>
    /// An IRC message
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// Is the message a command?
        /// </summary>
        bool IsCommand { get; }
        /// <summary>
        /// Is the message a reply?
        /// </summary>
        bool IsReply { get; }
        /// <summary>
        /// The messages prefix (null if there is none).
        /// </summary>
        string Prefix { get; set; }
        /// <summary>
        /// The list of parameters the message has.
        /// </summary>
        string[] Params { get; set; }
        /// <summary>
        /// The command the message pertains to or null if there is none.
        /// </summary>
        string Command { get; set; }
        /// <summary>
        /// The reply value of the command (defaults to NO_REPLY)
        /// </summary>
        Reply Reply { get; set; }
        /// <summary>
        /// The client that "owns" the message.
        /// </summary>
        IClient Owner { get; set; }
        /// <summary>
        /// A string containing the IRC friendly message (this is what's sent to the client)
        /// </summary>
        string MessageString { get; } //[:prefix ]<Command/Reply> [Parameters]
        /// <summary>
        /// A string containing a user friendly format of the message (used for debugging purposes)
        /// </summary>
        string ShortMessageString { get; } //<Command/Reply> [Parameters]

        /// <summary>
        /// Creates a message out of an IRC message string.
        /// </summary>
        /// <param name="Owner">The owner of the message</param>
        /// <param name="messageString">The IRC message string to parse</param>
        /// <returns></returns>
        IMessage CreateMessage(IClient Owner, string messageString);
        /// <summary>
        /// Creates a message out of a prefix, command and parameters
        /// </summary>
        /// <param name="Owner">The owner of the message</param>
        /// <param name="prefix">The prefix to apply to the message</param>
        /// <param name="command">The command the message is to contain</param>
        /// <param name="parameters">An array of strings containing the parameters for the message</param>
        /// <returns></returns>
        IMessage CreateMessage(IClient Owner, string prefix, string command, string[] parameters);
        /// <summary>
        /// Creates a message out of a prefix, reply value and parameters
        /// </summary>
        /// <param name="Owner">The owner of the message</param>
        /// <param name="prefix">The prefix to apply to the message</param>
        /// <param name="replyValue">The reply value the message contains</param>
        /// <param name="parameters">An array of strings containing the parameters for the message</param>
        /// <returns></returns>
        IMessage CreateMessage(IClient Owner, string prefix, Reply replyValue, string[] parameters);
    }

    /// <summary>
    /// A simple channel
    /// </summary>
    public interface IChannel
    {
        /// <summary>
        /// The name of the channel
        /// </summary>
        string ChannelName { get; set; }
        /// <summary>
        /// An array of clients that are joined to the channel
        /// </summary>
        IClient[] Clients { get; }
        /// <summary>
        /// A dictionary containing the client specific modes on the clients joined to the channel
        /// </summary>
        Dictionary<IClient, string> ClientModes { get; set; }
        /// <summary>
        /// A dictionary containing the channel specific modes and their values
        /// </summary>
        Dictionary<char, string> Modes { get; set; }
        /// <summary>
        /// An array of strings containing the banmasks currently in effect on the channel
        /// </summary>
        string[] Banlist { get; }
        /// <summary>
        /// The current topic of the channel
        /// </summary>
        string Topic { get; set; }
        /// <summary>
        /// The current channel modes
        /// </summary>
        /// <returns>A string containing all the channel modes (Equivalent to putting all the keys in <value>Modes</value> after eachother)</returns>
        string GetModes();

        /// <summary>
        /// Sends a message to the clients in the channel
        /// </summary>
        /// <param name="msg">The message to send</param>
        void SendMessage(IMessage msg);
        /// <summary>
        /// Sends a message to the clients on the channel
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="toSender">Wether to send it to whoever sent the message</param>
        void SendMessage(IMessage msg, bool toSender);
        /// <summary>
        /// Sends a message to the clients on the channel
        /// </summary>
        /// <param name="sender">The sender of the message</param>
        /// <param name="msg">The message to send</param>
        void SendMessage(IClient sender, IMessage msg);
        /// <summary>
        /// Sends a message to the clients on the channel
        /// </summary>
        /// <param name="sender">The sender of the message</param>
        /// <param name="msg">The message to send</param>
        /// <param name="toSender">Wether to send it to whoever sent the message</param>
        void SendMessage(IClient sender, IMessage msg, bool toSender);

        /// <summary>
        /// Adds a client to the channel
        /// </summary>
        /// <param name="cl">The client to add</param>
        void AddClient(IClient cl);
        /// <summary>
        /// Removes a client from the channel
        /// </summary>
        /// <param name="cl">The client to remove</param>
        void RemoveClient(IClient cl);
        /// <summary>
        /// Removes a client from the channel
        /// </summary>
        /// <param name="cl">The client to remove</param>
        /// <param name="leaveMsg">The message the client or command specified</param>
        void RemoveClient(IClient cl, string leaveMsg);
        /// <summary>
        /// Kicks a client from the channel
        /// </summary>
        /// <param name="cl">The client to kick</param>
        void KickClient(IClient cl);
        /// <summary>
        /// Kicks a client from the channel
        /// </summary>
        /// <param name="cl">The client to kick</param>
        /// <param name="kickMsg">The message specified</param>
        void KickClient(IClient cl, string kickMsg);
        /// <summary>
        /// Adds the specified string to the list of ban masks
        /// </summary>
        /// <param name="banMask">The mask to add</param>
        void AddToBanList(string banMask);
        /// <summary>
        /// Removes the specified string from the ban masks
        /// </summary>
        /// <param name="banMask">The mask to remove (Wildcards are allowed)</param>
        void RemoveFromBanList(string banMask);
    }

    /// <summary>
    /// A database containing clients and channels
    /// </summary>
    public interface IDatabase
    {
        /// <summary>
        /// Checks if the database contains a channel
        /// </summary>
        /// <param name="name">The channel name to look for</param>
        /// <returns>If the channel exists in the database</returns>
        bool HasChannel(string name);
        /// <summary>
        /// Checks the database for a specific channel
        /// </summary>
        /// <param name="name">The channel name to look for</param>
        /// <returns>The channel or null if there is no such channel</returns>
        IChannel GetChannel(string name);

        /// <summary>
        /// Checks if the database contains a specific client
        /// </summary>
        /// <param name="client">The client nickname to check for</param>
        /// <returns>If there is a client with that nickname or not</returns>
        bool HasClient(string client);
        /// <summary>
        /// Checks the database for a specific client
        /// </summary>
        /// <param name="nick">the nickname to look for</param>
        /// <returns>The client with the specified nickname or null if there is none</returns>
        IClient GetClient(string nick);

        /// <summary>
        /// Gets a list of channels containing the specified client
        /// </summary>
        /// <param name="user">The client to look for</param>
        /// <returns>An array of channels that the client is joined to</returns>
        IChannel[] GetChannels(IClient user);
        /// <summary>
        /// An array of clients contained in the database
        /// </summary>
        IClient[] Clients { get; }
        /// <summary>
        /// An array of channels contained in the database
        /// </summary>
        IChannel[] Channels { get; }

        /// <summary>
        /// Adds a client to the database
        /// </summary>
        /// <param name="cl">The client to add</param>
        void AddClient(IClient cl);
        /// <summary>
        /// Adds a channel to the database
        /// </summary>
        /// <param name="ch">The channel to add</param>
        void AddChannel(IChannel ch);
        /// <summary>
        /// Removes a channel from the database
        /// </summary>
        /// <param name="ch">The channel to remove</param>
        void RemoveChannel(IChannel ch);
        /// <summary>
        /// Removes a client from the database
        /// </summary>
        /// <param name="cl">The client to remove</param>
        void RemoveClient(IClient cl);
    }

    /// <summary>
    /// An IRC server
    /// </summary>
    public interface IServer
    {
        /// <summary>
        /// The server name (same rules as IRC nicks apply)
        /// </summary>
        string Name { get; }
        /// <summary>
        /// The servers hostname
        /// </summary>
        string HostString { get; }
        /// <summary>
        /// The servers full name (spaces and such allowed)
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Queries information from the server
        /// </summary>
        /// <param name="query">The query to perform</param>
        /// <returns>Server specific data pertaining to the query</returns>
        string QueryServer(string query);
    }

    public class Database
    {
        static IDatabase parent;
        static IDatabase global;

        public static void SetDatabase(IDatabase b)
        {
            parent = b;
        }

        public static void SetGlobalDatabase(IDatabase b)
        {
            global = b;
        }

        public static IDatabase GetDatabase()
        {
            return parent;
        }

        public static IDatabase GetGlobalDatabase()
        {
            return global;
        }
    }

    public class Server
    {
        static IServer parent;

        public static void SetServer(IServer s)
        {
            parent = s;
        }

        public static IServer GetServer()
        {
            return parent;
        }
    }

    /// <summary>
    /// A list of standard IRC replies
    /// </summary>
    public enum Reply
    {
        RPL_WELCOME = 001,          //"Welcome to the Internet Relay Network <nick>!<user>@<host>"
        RPL_YOURHOST = 002,         //"Your host is <servername>, running version <ver>"
        RPL_CREATED = 003,          //"This server was created <date>"
        RPL_MYINFO = 004,           //"<servername> <version> <available user modes> <available channel modes>"
        RPL_BOUNCE = 005,           //"Try server <server name>, port <port number>"

        RPL_TRACELINK = 200,        //"Link <version & debug level> <destination> <next server> V<protocol version> <link uptime in seconds> <backstream sendq> <upstream sendq>"
        RPL_TRACECONNECTING = 201,  //"Try. <class> <server>"
        RPL_TRACEHANDSHAKE = 202,   //"H.S. <class> <server>"
        RPL_TRACEUNKNOWN = 203,     //"???? <class> [<client IP address in dot form>]"
        RPL_TRACEOPERATOR = 204,    //"Oper <class> <nick>"
        RPL_TRACEUSER = 205,        //"User <class> <nick>"
        RPL_TRACESERVER = 206,      //"Serv <class> <int>S <int>C <server>  <nick!user|*!*>@<host|server> V<protocol version>"
        RPL_TRACESERVICE = 207,     //"Service <class> <name> <type> <active type>"
        RPL_TRACENEWTYPE = 208,     //"<newtype> 0 <client name>"
        RPL_TRACECLASS = 209,       //"Class <class> <count>"
        RPL_TRACERECONNECT = 210,   //Unused.
        RPL_STATSLINKINFO = 211,    //"<linkname> <sendq> <sent messages> <sent Kbytes> <received messages> <received Kbytes> <time open>"
        RPL_STATSCOMMANDS = 212,    //"<command> <count> <byte count> <remote count>"
        RPL_ENDOFSTATS = 219,       //"<stats letter> :End of STATS report"
        RPL_UMODEIS = 221,          //"<user mode string>"
        RPL_SERVLIST = 234,         //"<name> <server> <mask> <type>  <hopcount> <info>"
        RPL_SERVLISTEND = 235,      //"<mask> <type> :End of service listing"
        RPL_STATSUPTIME = 242,      //"Server Up %d days %d:%02d:%02d"
        RPL_STATSOLINE = 243,       //"O <hostmask> * <name>"
        RPL_LUSERCLIENT = 251,      //"There are <integer> users and <integer> services on <integer> servers"
        RPL_LUSEROP = 252,          //"<integer> :operator(s) online"
        RPL_LUSERUNKNOWN = 253,     //"<integer> :unknown connection(s)"
        RPL_LUSERCHANNELS = 254,    //"<integer> :channels formed"
        RPL_LUSERME = 255,          //"I have <integer> clients and <integer> servers"
        RPL_ADMINME = 256,          //"<server> :Administrative info"
        RPL_ADMINLOC1 = 257,        //"<admin info>"
        RPL_ADMINLOC2 = 258,        //"<admin info>"
        RPL_ADMINEMAIL = 259,       //"<admin info>"
        RPL_TRACELOG = 261,         //"File <logfile> <debug level>"
        RPL_TRACEEND = 262,         //"<server name> <version & debug level> :End of TRACE"
        RPL_TRYAGAIN = 263,         //"<command> :Please wait a while and try again."
        RPL_AWAY = 301,             //"<nick> :<away message>"
        RPL_USERHOST = 302,         //"*1<reply> *( " " <reply> )"
        RPL_ISON = 303,             //"*1<nick> *( " " <nick> )"
        RPL_UNAWAY = 305,           //"You are no longer marked as being away"
        RPL_NOWAWAY = 306,          //"You have been marked as being away"
        RPL_WHOISUSER = 311,        //"<nick> <user> <host> * :<real name>"
        RPL_WHOISSERVER = 312,      //"<nick> <server> :<server info>"
        RPL_WHOISOPERATOR = 313,    //"<nick> :is an IRC operator"
        RPL_WHOWASUSER = 314,       //"<nick> <user> <host> * :<real name>"
        RPL_ENDOFWHO = 315,         //"<name> :End of WHO list"
        RPL_WHOISIDLE = 317,        //"<nick> <integer> :seconds idle"
        RPL_ENDOFWHOIS = 318,       //"<nick> :End of WHOIS list"
        RPL_WHOISCHANNELS = 319,    //"<nick> :*( ( "@" / "+" ) <channel>  " " )"
        RPL_LISTSTART = 321,        //Obsolete
        RPL_LIST = 322,             //"<channel> <# visible> :<topic>"
        RPL_LISTEND = 323,          //"End of LIST"
        RPL_CHANNELMODEIS = 324,    //"<channel> <mode> <mode params>"
        RPL_UNIQOPIS = 325,         //"<channel> <nickname>"
        RPL_NOTOPIC = 331,          //"<channel> :No topic is set"
        RPL_TOPIC = 332,            //"<channel> :<topic>"
        RPL_INVITING = 341,         //"<channel> <nick>"
        RPL_SUMMONING = 342,        //"<user> :Summoning user to IRC"
        RPL_INVITELIST = 346,       //"<channel> <invitemask>"
        RPL_ENDOFINVITELIST = 347,  //"<channel> :End of channel invite list"
        RPL_EXCEPTLIST = 348,       //"<channel> <exceptionmask>"
        RPL_ENDOFEXCEPTLIST = 349,  //"<channel> :End of channel exception list"
        RPL_VERSION = 351,          //"<version>.<debuglevel> <server>  :<comments>"
        RPL_WHOREPLY = 352,         //"<channel> <user> <host> <server>  <nick> ( "H" / "G" > ["*"] [ ( "@" / "+" ) ] :<hopcount> <real name>"
        RPL_NAMREPLY = 353,         //"( "=" / "*" / "@" ) <channel>  :[ "@" / "+" ] <nick> *( " " [ "@" / "+" ] <nick> )"
        RPL_LINKS = 364,            //"<mask> <server> :<hopcount> <server info>"
        RPL_ENDOFLINKS = 365,       //"<mask> :End of LINKS list"
        RPL_ENDOFNAMES = 366,       //"<channel> :End of NAMES list"
        RPL_BANLIST = 367,          //"<channel> <banmask>"
        RPL_ENDOFBANLIST = 368,     //"<channel> :End of channel ban list"
        RPL_ENDOFWHOWAS = 369,      //"<nick> :End of WHOWAS"
        RPL_INFO = 371,             //"<string>"
        RPL_MOTD = 372,             //"- <text>"
        RPL_ENDOFINFO = 374,        //"End of INFO list"
        RPL_MOTDSTART = 375,        //"- <server> Message of the day - "
        RPL_ENDOFMOTD = 376,        //"End of MOTD command"
        RPL_YOUREOPER = 381,        //"You are now an IRC operator"
        RPL_REHASHING = 382,        //"<config file> :Rehashing"
        RPL_YOURESERVICE = 383,     //"You are service <servicename>"
        RPL_TIME = 391,             //"<server> :<string showing server's local time>"
        RPL_USERSSTART = 392,       //"UserID Terminal Host"
        RPL_USERS = 393,            //"<username> <ttyline> <hostname>"
        RPL_ENDOFUSERS = 394,       //"End of users"
        RPL_NOUSERS = 395,          //"Nobody logged in"

        ERR_NOSUCHNICK = 401,       //"<nickname> :No such nick/channel"
        ERR_NOSUCHSERVER = 402,     //"<server name> :No such server"
        ERR_NOSUCHCHANNEL = 403,    //"<channel name> :No such channel"
        ERR_CANNOTSENDTOCHAN = 404, //"<channel name> :Cannot send to channel"
        ERR_TOOMANYCHANNELS = 405,  //"<channel name> :You have joined too many channels"
        ERR_WASNOSUCHNICK = 406,    //"<nickname> :There was no such nickname"
        ERR_TOOMANYTARGETS = 407,   //"<target> :<error code> recipients. <abort message>"
        ERR_NOSUCHSERVICE = 408,    //"<service name> :No such service"
        ERR_NOORIGIN = 409,         //"No origin specified"
        ERR_NORECIPIENT = 411,      //"No recipient given (<command>)"
        ERR_NOTEXTTOSEND = 412,     //"No text to send"
        ERR_NOTOPLEVEL = 413,       //"<mask> :No toplevel domain specified"
        ERR_WILDTOPLEVEL = 414,     //"<mask> :Wildcard in toplevel domain"
        ERR_BADMASK = 415,          //"<mask> :Bad Server/host mask"
        ERR_TOOMANYMATCHES = 416,   //"<command> [<mask>] :<info> "
        ERR_UNKNOWNCOMMAND = 421,   //"<command> :Unknown command"
        ERR_NOMOTD = 422,           //"MOTD File is missing"
        ERR_NOADMININFO = 423,      //"<server> :No administrative info available"
        ERR_FILEERROR = 424,        //"File error doing <file op> on <file>"
        ERR_NONICKNAMEGIVEN = 431,  //"No nickname given"
        ERR_ERRONEUSNICKNAME = 432, //"<nick> :Erroneous nickname"
        ERR_NICKNAMEINUSE = 433,    //"<nick> :Nickname is already in use"
        ERR_NICKCOLLISION = 436,    //"<nick> :Nickname collision KILL from <user>@<host>
        ERR_UNAVAILRESOURCE = 437,  //"<nick/channel> :Nick/channel is temporarily unavailable"
        ERR_USERNOTINCHANNEL = 441, //"<nick> <channel> :They aren't on that channel"
        ERR_NOTONCHANNEL = 442,     //"<channel> :You're not on that channel"
        ERR_USERONCHANNEL = 443,    //"<user> <channel> :is already on channel"
        ERR_NOLOGIN = 444,          //"<user> :User not logged in"
        ERR_SUMMONDISABLED = 445,   //"SUMMON has been disabled"
        ERR_USERSDISABLED = 446,    //"USERS has been disabled"
        ERR_NOTREGISTERED = 451,    //"You have not registered"
        ERR_NEEDMOREPARAMS = 461,   //"<command> :Not enough parameters"
        ERR_ALREADYREGISTRED = 462, //"Unauthorized command (already registered)"
        ERR_NOPERMFORHOST = 463,    //"Your host isn't among the privileged"
        ERR_PASSWDMISMATCH = 464,   //"Password incorrect"
        ERR_YOUREBANNEDCREEP = 465, //"You are banned from this server"
        ERR_YOUWILLBEBANNED = 466,
        ERR_KEYSET = 467,           //"<channel> :Channel key already set"
        ERR_CHANNELISFULL = 471,    //"<channel> :Cannot join channel (+l)"
        ERR_UNKNOWNMODE = 472,      //"<char> :is unknown mode char to me for <channel>"
        ERR_INVITEONLYCHAN = 473,   //"<channel> :Cannot join channel (+i)"
        ERR_BANNEDFROMCHAN = 474,   //"<channel> :Cannot join channel (+b)"
        ERR_BADCHANNELKEY = 475,    //"<channel> :Cannot join channel (+k)"
        ERR_BADCHANMASK = 476,      //"<channel> :Bad Channel Mask"
        ERR_NOCHANMODES = 477,      //"<channel> :Channel doesn't support modes"
        ERR_BANLISTFULL = 478,      //"<channel> <char> :Channel list is full
        ERR_NOPRIVILEGES = 481,     //"Permission Denied- You're not an IRC operator"
        ERR_CHANOPRIVSNEEDED = 482, //"<channel> :You're not channel operator"
        ERR_CANTKILLSERVER = 483,   //"You can't kill a server!"
        ERR_RESTRICTED = 484,       //"Your connection is restricted!"
        ERR_UNIQOPPRIVSNEEDED = 485,//"You're not the original channel operator"
        ERR_NOOPERHOST = 491,       //"No O-lines for your host"
        ERR_UMODEUNKNOWNFLAG = 501, //"Unknown MODE flag"
        ERR_USERSDONTMATCH = 502    //"Cannot change mode for other users"
    }

    /// <summary>
    /// Defines that the command is to be registered as an IRC command
    /// </summary>
    public class IRCCommand : Attribute
    {
        private string name;
        private int minArgs;
        private int maxArgs;

        /// <summary>
        /// Defines the command as an IRC command
        /// </summary>
        /// <param name="name">The IRC command name</param>
        /// <param name="minArgs">The lowest amount of arguments allowed (If there is no lower limit this value can be skipped)</param>
        /// <param name="maxArgs">The maximum amount of arguments allowed (If the upper limit is the same as the lower limit this value can be skipped)</param>
        public IRCCommand(string name, int minArgs = -1, int maxArgs = -1)
        {
            this.name = name; this.minArgs = minArgs; this.maxArgs = maxArgs;
        }

        /// <summary>
        /// The name of the command
        /// </summary>
        public string Name { get { return name; } }
        /// <summary>
        /// The lowest amount of arguments this command takes (or -1 for unlimited)
        /// </summary>
        public int MinimumArguments { get { return minArgs; } }
        /// <summary>
        /// The highest amount of arguments this command takes (or -1 for the same as the lowest)
        /// </summary>
        public int MaximumArguments { get { return maxArgs; } }
    }

    /// <summary>
    /// Defines that the command is to be registered as a placeholder for an IRC command
    /// </summary>
    public class IRCCommandPlaceholder : IRCCommand
    {
        public IRCCommandPlaceholder(string name, int minArgs = -1, int maxArgs = -1) : base(name, minArgs, maxArgs) { }
    }

    /// <summary>
    /// A wildcard
    /// </summary>
    public class Wildcard : Regex
    {
        public Wildcard(string pattern)
            : base(WildcardToRegex(pattern))
        {
        }

        public Wildcard(string pattern, RegexOptions options)
            : base(WildcardToRegex(pattern), options)
        {
        }


        /// <summary>
        /// Converts the specified wildcard to a Regexp
        /// </summary>
        /// <param name="pattern">The wildcard to convert</param>
        /// <returns>The wildcard in Regexp form</returns>
        public static string WildcardToRegex(string pattern)
        {
            return Regex.Escape(pattern).
             Replace("\\*", ".*").
             Replace("\\?", ".");
        }
    }
}
