using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text;
using IRCLib.Helpers;
using BaseIRCLib;

namespace IRCLib.Interfaces.IRC
{
    public class IRCClient : IClient
    {
        TcpClient tcpClient;
        Queue<IMessage> messageQueue;
        string nick; string user; string host; string real; string modes; bool greeted; int missedPings; DateTime lastPing;
        string unfinished; bool disposed; string away; DateTime lastPong; DateTime lastCmd; string discMesg;
        Thread t;

        public IRCClient(TcpClient client)
        {
            tcpClient = client;
            messageQueue = new Queue<IMessage>();
            lastCmd = DateTime.Now;
            greeted = false;
            //lastPong = DateTime.Now + TimeSpan.FromSeconds(85);

            nick = ""; user = ""; real = ""; modes = ""; away = "";

            try
            {
                host = Dns.GetHostEntry(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address).HostName;
                SendMessage(IRCMessage.GetStatic().CreateMessage(this, BaseIRCLib.Server.GetServer().Name, "NOTICE", new string[] { "Your hostname is " + host }));
            }
            catch (SocketException ex)
            {
                host = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();
                SendMessage(IRCMessage.GetStatic().CreateMessage(this, BaseIRCLib.Server.GetServer().Name, "NOTICE", new string[] { "Couldn't find your hostname" }));
            }

            t = new Thread(new ThreadStart(UpdateMessages));
            t.Start();
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

        public void SendMessage(IMessage send)
        {
            if (disposed)
                return;

            byte[] data = Encoding.UTF8.GetBytes(send.MessageString + "\n");

            //Console.WriteLine("Out: {0}{1}", (string.IsNullOrEmpty(nick) ? "" : nick + ", "), send.MessageString);

            try
            {
                tcpClient.Client.Send(data);
            }
            catch (SocketException ex)
            {
#if DEBUG
                Console.WriteLine("SocketException occured! {0}", ex.Message);
#else
                System.IO.File.WriteAllText("error.log",string.Format("[{0}] ERROR {1} occured!\n[{0}] Trace: {2}\n", DateTime.Now, ex.Message, ex.StackTrace));
#endif
                Dispose("Lost connection");
            }
        }

        private void UpdateMessages()
        {
            while (!disposed)
            {
                if (tcpClient.Client == null)
                {
                    Dispose();
                    return;
                }

                if (!tcpClient.Connected)
                    Dispose();

                if (!tcpClient.Client.Connected)
                    Dispose();

                if (disposed)
                    return;

                if (tcpClient.Available > 0)
                {
                    byte[] recv = new byte[tcpClient.Available];
                    tcpClient.Client.Receive(recv);
                    string data = Encoding.UTF8.GetString(recv);

                    data = data.Replace("\r", "\n");
                    data = data.Replace("\n\n", "\n");

                    data = unfinished + data;

                    if (!data.EndsWith("\n"))
                    {
                        if (data.Contains("\n"))
                        {
                            unfinished = data.Substring(data.LastIndexOf("\n") + 1);
                            data = data.Remove(data.LastIndexOf("\n") + 1);
                        }
                        else
                        {
                            unfinished = data;
                            continue;
                        }
                    }

                    string[] msgs = data.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string msg in msgs)
                    {
#if !DEBUG
                        try
                        {
#endif
                            IMessage m = IRCMessage.GetStatic().CreateMessage(this, msg);

                            if (string.IsNullOrEmpty(m.Prefix))
                                m.Prefix = this.UserString;

                            messageQueue.Enqueue(m);
#if !DEBUG
                        }
                        catch (Exception ex)
                        {
                            System.IO.File.WriteAllText("error.log",string.Format("[{0}] ERROR {1} occured!\n[{0}] Trace: {2}\n", DateTime.Now, ex.Message, ex.StackTrace));
                        }
#endif
                    }
                }

                Thread.Sleep(50);
            }
        }

        public void Dispose()
        {
            tcpClient.Close();
            disposed = true;
        }

        public void Dispose(string message)
        {
            Dispose();
            discMesg = message;
        }

        public string NickName { get { if (string.IsNullOrEmpty(nick)) { return "*"; } else { return nick; } } set { nick = value; } }
        public string HostName { get { return host; } set { host = value; } }
        public string UserName { get { return user; } set { user = value; } }
        public string RealName { get { return real; } set { real = value; } }
        public string Modes { get { return modes; } set { modes = value; } }
        public string AwayMsg { get { return away; } set { away = value; } }
        public string DisconnectMsg { get { return discMesg; } set { discMesg = value; } }
        public string UserString { get { return string.Format("{0}!{1}@{2}", NickName, UserName, HostName); } }
        public bool IsDisposed { get { return disposed; } }
        public DateTime LastPing { get { return lastPing; } set { lastPing = value; } }
        public DateTime LastPong { get { return lastPong; } set { lastPong = value; } }
        public DateTime LastCommand { get { return lastCmd; } set { lastCmd = value; } }
        public int MissedPings { get { return missedPings; } set { missedPings = value; } }
        public bool Greeted { get { return greeted; } set { greeted = value; } }
    }
}
