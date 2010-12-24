using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
//using IRCLib.Interfaces;
using BaseIRCLib;

namespace IRCLib.Helpers
{
    /// <summary>
    /// A link to an IRC command
    /// </summary>
    class IRCCommandLink
    {
        /// <summary>
        /// The class containing the command
        /// </summary>
        public object Object;
        /// <summary>
        /// The command
        /// </summary>
        public System.Reflection.MethodInfo Method;
        /// <summary>
        /// The commands information
        /// </summary>
        public IRCCommand Attributes;
    }

    /// <summary>
    /// A list of IRC commands
    /// </summary>
    public class IRCCommandList
    {
        private List<IRCCommandLink> commandList;

        public IRCCommandList()
        {
            commandList = new List<IRCCommandLink>();
        }

        /// <summary>
        /// Add a command to the list
        /// </summary>
        /// <param name="commandName">The command information</param>
        /// <param name="target">The class containing the command</param>
        /// <param name="method">The command</param>
        public void AddCommand(IRCCommand commandName, object target, System.Reflection.MethodInfo method)
        {
			IRCCommandLink val = new IRCCommandLink() { Object = target, Method = method, Attributes = commandName };
            if (!commandList.Contains(val))
                commandList.Add(val);
		}

        /// <summary>
        /// Call the command specified in the message
        /// </summary>
        /// <param name="message">The message to call</param>
        public void CallCommand(IMessage message)
        {
        	if (message.IsCommand && ValidCommand(message.Command, message.Params.Length))
            {
        		IRCCommandLink[] commands = ValidCommands(message.Command, message.Params.Length);

                //Console.WriteLine("Invoking command \"{0}\" to \"{1}.{2}\"", message.MessageString, command.Object.GetType().Name, command.Method.Name);

                if (Server.Verbose)
        			Console.WriteLine("{0}: {1}", message.Owner.NickName, message.ShortMessageString);

				for (int i = commands.Length - 1; i >= 0; i--)
				{
        			object ret = commands[i].Method.Invoke(commands.Last().Object, new object[] { message });
     
					if (ret != null)
        				if (ret.GetType().Equals(typeof(bool)) && (bool)ret == true)
        					break;
				}
            }
            else
                Console.WriteLine("Invalid call to {0}", message.Command);
        }

        /// <summary>
        /// Check if a command is valid
        /// </summary>
        /// <param name="commandName">The name of the command</param>
        /// <param name="numArgs">The number of parameters to feed to the command</param>
        /// <returns>If the command allows the specified number of parameters</returns>
        public bool ValidCommand(string commandName, int numArgs)
        {
        	if (HasCommand(commandName))
			{
        		IRCCommandLink[] commands = commandList.FindAll(item => item.Attributes.Name == commandName).ToArray();
    
				foreach (IRCCommandLink command in commands)
				{
        			if (command.Attributes.MinimumArguments == -1)
        				return true;
					else if (command.Attributes.MinimumArguments <= numArgs)
					{
        				if (command.Attributes.MaximumArguments >= numArgs)
        					return true;
        			}
        		}
        	}
   
            return false;
        }
		
		IRCCommandLink[] ValidCommands(string commandName, int numArgs)
		{
			List<IRCCommandLink> ret = new List<IRCCommandLink>();
			
        	if (HasCommand(commandName))
			{
				IRCCommandLink[] commands = commandList.FindAll(item => item.Attributes.Name == commandName).ToArray();
				
				foreach (IRCCommandLink command in commands)
				{
					if (command.Attributes.MinimumArguments == -1)
						ret.Add(command);
					else if (command.Attributes.MinimumArguments <= numArgs)
					{
						if (command.Attributes.MaximumArguments >= numArgs)
							ret.Add(command);
					}
				}
			}
			
            return ret.ToArray();
        }

        /// <summary>
        /// Checks if a command exists
        /// </summary>
        /// <param name="commandName">The name of the command</param>
        /// <returns>If the command has been registered in the list</returns>
        public bool HasCommand(string commandName)
        {
        	return commandList.Find(item => item.Attributes.Name == commandName) != default(IRCCommandLink);
        }
    }
	
    /// <summary>
    /// A simplified class for handling content
    /// </summary>
    public class ContentHandler
    {
        /// <summary>
        /// Load a class from a XML document
        /// </summary>
        /// <typeparam name="T">The type of class to load</typeparam>
        /// <param name="file">The file to load from</param>
        /// <returns>The class that was contained in the file</returns>
        public static T Load<T>(string file)
        {
            XmlSerializer xS = new XmlSerializer(typeof(T));
            XmlReader r = XmlReader.Create(file);
            T ret = (T)xS.Deserialize(r);
            r.Close();
            return ret;
        }

        /// <summary>
        /// Save a class to a XML document
        /// </summary>
        /// <typeparam name="T">The type of class to save</typeparam>
        /// <param name="save">The class to save</param>
        /// <param name="file">The file to save to</param>
        public static void Save<T>(T save, string file)
        {
            XmlSerializer xS = new XmlSerializer(typeof(T));
            XmlWriter w;
            w = XmlWriter.Create(file);

            xS.Serialize(w, save);
            w.Flush();
            w.Close();
        }

        /// <summary>
        /// Save a class to a XML document
        /// </summary>
        /// <typeparam name="T">The type of class to save</typeparam>
        /// <param name="save">The class to save</param>
        /// <param name="file">The file to save to</param>
        /// <param name="sets">The XmlWriterSettings to use when saving</param>
        public static void Save<T>(T save, string file, XmlWriterSettings sets)
        {
            XmlSerializer xS = new XmlSerializer(typeof(T));
            XmlWriter w;
            w = XmlWriter.Create(file, sets);

            xS.Serialize(w, save);
            w.Flush();
            w.Close();
        }
    }
}
