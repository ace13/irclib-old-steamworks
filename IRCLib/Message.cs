using System;
using System.Collections.Generic;
//using IRCLib.Interfaces;
using System.Text;
using System.Text.RegularExpressions;
using BaseIRCLib;

namespace IRCLib
{
    public class IRCMessage : IMessage
    {
        static IRCMessage singleton;
        static Regex messageEx = new Regex(@"(?<prefix>:\S+ )?(?<command>\S+)?(?<reply>\d{3})? ?(?<params>[(\S+ )(:.*$)]*)");

        bool command = false;

        public bool IsCommand { get { return command; } }
        public bool IsReply { get { return !command; } }
        public bool Suffix { get; set; }
        public string Prefix { get; set; }
        public string[] Params { get; set; }
        public string Command { get; set; }
        public Reply Reply { get; set; }
        public BaseIRCLib.IClient Owner { get; set; }

        public IRCMessage()
        {
        }

        public IRCMessage(string MessageString)
        {
            Match msg = messageEx.Match(MessageString);

            if (msg.Success)
            {
                GroupCollection parts = msg.Groups;

                Prefix = parts["prefix"].Value;

                if (parts["command"].Success)
                {
                    try
                    {
                        Command = parts["command"].Value.ToUpper();
                    }
                    catch
                    {
                        throw new ArgumentException("", msg.Groups["command"].Value);
                    }
                    command = true;
                }
                else if (parts["reply"].Success)
                {
                    Reply = (Reply)int.Parse(parts["reply"].Value);
                    command = false;
                }

                if (parts["params"].Success)
                {
                    List<string> pp = new List<string>();
                    pp.AddRange((parts["params"].Value.Contains(":") ? parts["params"].Value.Substring(0, parts["params"].Value.IndexOf(":")) : parts["params"].Value).Split(new string[] {" "},StringSplitOptions.RemoveEmptyEntries));
                    
                    if (parts["params"].Value.Contains(":"))
                        pp.Add(parts["params"].Value.Remove(0,parts["params"].Value.IndexOf(":")+1));

                    Params = pp.ToArray();
                    pp = null;
                }

                if (!command && !Enum.IsDefined(typeof(Reply),Reply))
                    throw new Exception();
            }
            else
            {
                throw new Exception();
            }
        }

        public IRCMessage(string prefix, string cmd, string[] parameters)
        {
            Prefix = prefix;
            Command = cmd;
            command = true;
            Params = parameters;
        }

        public IRCMessage(string prefix, Reply reply, string[] parameters)
        {
            Prefix = prefix;
            Reply = reply;
            command = false;
            Params = parameters;
        }

        private bool V(string value)
        {
            return (value != null && value != string.Empty);
        }
        private bool V(string[] value)
        {
            return (value != null && value.Length > 0);
        }

        private string P()
        {
            string retur = "";

            if (V(Params))
                foreach (string a in Params) { if (a == null) { continue; } retur += (Array.IndexOf(Params, a) == Params.Length - 1 && (a.Contains(" ") || (a.Length > 0 && a[0] == ':') || a.Length == 0) ? " :" : " ") + a; }

            return retur;
        }

        public string MessageString
        {
            get
            {
                return (V(Prefix) ? ":" + Prefix + " " : "") +
                    (IsCommand ? Command : ((int)Reply).ToString("D3")) +
                    P();
            }
        }

        public string ShortMessageString
        {
            get
            {
                return (IsCommand ? Command : ((int)Reply).ToString("D3")) + P();
            }
        }

        public IMessage CreateMessage(IClient owner, string prefix, string cmd, string[] parameters)
        {
            return new IRCMessage(prefix, cmd, parameters) { Owner = owner };
        }
        public IMessage CreateMessage(IClient owner, string prefix, Reply rpl, string[] parameters)
        {
            return new IRCMessage(prefix, rpl, parameters) { Owner = owner };
        }
        public IMessage CreateMessage(IClient owner, string msg)
        {
            return new IRCMessage(msg) { Owner = owner };
        }

        public static IRCMessage GetStatic()
        {
            if (singleton == null)
                singleton = new IRCMessage();

            return singleton;
        }
    }
}
