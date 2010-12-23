using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IRCLib.Interfaces.Steam;
using IRCLib.Helpers;
using Steam4NET;
using System.Xml.Serialization;

namespace IRCLib
{
    /// <summary>
    /// A list of clients containing SteamClient specific data
    /// </summary>
    public class ClientList
    {
        [XmlIgnore]
        bool needsSaving;
        public bool NeedsToBeSaved { get { return needsSaving; } }

        public List<SteamClient> RegisteredClients;

        [XmlIgnore]
        Dictionary<ulong, SteamClient> clients;

        [XmlIgnore]
        public Dictionary<ulong, SteamClient> Clients { get { return clients; } }

        public void AddClient(SteamClient cl)
        {
            if (clients.ContainsKey(cl.SteamID))
            {
                clients[cl.SteamID] = cl;
            }
            else
            {
                clients.Add(cl.SteamID, cl);
            }

            needsSaving = true;
        }

        void CreateDict()
        {
            clients = new Dictionary<ulong, SteamClient>();

            foreach (SteamClient s in RegisteredClients)
            {
                clients.Add(s.SteamID, s);
            }

            RegisteredClients = null;

            //clientIDs.Select((k, i) => new { k, v = registeredClients[i] }).ToDictionary(x => x.k, x => x.v);
        }

        public ClientList()
        {
            RegisteredClients = new List<SteamClient>();
            clients = new Dictionary<ulong, SteamClient>();
        }

        public static ClientList LoadClients(string file)
        {
            ClientList cl = ContentHandler.Load<ClientList>(file);
            cl.CreateDict();
            return cl;
        }

        public void Save(string file)
        {
            needsSaving = false;
            RegisteredClients = new List<SteamClient>();
            foreach (KeyValuePair<ulong, SteamClient> kv in clients)
            {
                RegisteredClients.Add(kv.Value);
            }

            ContentHandler.Save<ClientList>(this, file, new System.Xml.XmlWriterSettings() { Indent = true, IndentChars = "\t" });
        }
    }
}
