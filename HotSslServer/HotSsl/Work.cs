using System;

namespace HotSsl
{
    class Work
    {
        // Work with client message
        public void DoSomething(ref ClientInfo cinfo)
        {
            // Set message
            cinfo.Message = "!!! Hi from Worker.DoSomething() " + Reverse(cinfo.Message) + "\r\n";
            
            // Set log message
            cinfo.LoggerMessage = "[Log from Work() class]";

            // Set broadcast message
            cinfo.BroadcastMessage = "[Broadcast from Work() class]";

            // Or disconnect client with
	    // cinfo.Disconnect = true;
        }

        public static string Reverse( string s )
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse( charArray );
            return new String(charArray).Replace("\r","").Replace("\n","");
        }
    }
}
