using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Irc_server
{
    class Program
    {
        public static bool verbose = false;

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                foreach (string a in args)
                    if (a == "-v")
                        verbose = true;
            }

            Console.Title = "IRC Gateway";
            Console.WindowWidth += 6;
            IRCLib.Server s = null;
#if !DEBUG
            try
            {
#endif
                s = new IRCLib.Server();
                s.FullName = "Steam Gateway";

                if (verbose)
                {
                    Console.WriteLine("Running verbosely");
                    IRCLib.Server.Verbose = true;
                }
                
                s.Start();

                //IRCLib.Server.channels.Add(new IRCLib.Channel("Topic", "This is a channel"));
                //IRCLib.Server.channels.Add(new IRCLib.Channel("Notopic"));

                Console.ReadLine();

                s.Stop();
#if !DEBUG
            }
            catch (Exception ex)
            {
                try { s.Stop(); }
                catch { }
                System.IO.File.WriteAllText("error.log",string.Format("[{0}] ERROR {1} occured!\n[{0}] Trace: {2}\n", DateTime.Now, ex.Message, ex.StackTrace));
                Console.WriteLine("Exception occured and has been saved to \"error.log\"\nPlease send this file to <email goes here> and give a quick description of what you were doing at the time of the crash\n\nPress any key to exit...");
                Console.ReadKey();
            }
#endif
        }
    }

    //&Friends <- Friends list
    //  Friend -> User

    //#GroupName <- Groups
    //  Join/Leave -> JOIN/PART

    //    Ship ahoy
    //    |   |   |
    //   )_) )_) )_)
    //  )___)___)___)\
    // )___)___)_____)\\
    // ___|___|___|____\\\_
    // \               /
    //~~~~~~~~~~~~~~~~~~~~~

    //RegisterCommand(string commandName, int minArgs, int maxArgs, function ToCall)

    //RegisterCommands(object classToRegister)
    //  [IRCCommand(string commandName, int minArgs, int maxArgs)]
    //  public void commandName(IClient sender, string[] args)

    //HasCommand(string commandName, [int numArgs = -1])
    //CallCommand(string commandName, string[] args)

}
