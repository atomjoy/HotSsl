using System;
using HotSsl;

namespace HotSsl
{
    class Program
    {
        static void Main(string[] args)
        {
                Console.WriteLine("Starting server...");

                HotSslServer s = new HotSslServer();
                s.ServerPort = 8888;
                s.StartLogger = true;
                s.StartBroadcast = true;
                s.Start();

                Console.WriteLine("Bye Bye!");
        }
    }
}
