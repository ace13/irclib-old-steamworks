using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BaseIRCLib;

/// <summary>
/// RFC2813; Internet Relay Chat: Server protocol
/// </summary>
class RFC2813
{
	[IRCCommandPlaceholder("PASS", 3, 4)]
	public void PASS(IMessage message)
	{
		Console.WriteLine("Server password");
	}
}
