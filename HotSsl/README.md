## HotSsl .NetCore Tls/Ssl server
C# Tls/Ssl Socket server, multiple clients. Server working with IPv4 and IPv6 addresses (Server starts on all vps ip addresses).

### Start HotSsl server
```cs
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
                s.Start("certificate.pfx","password12345");

                Console.WriteLine("Bye Bye!");
        }
    }
}
```

### Create your own functionality in Work class with DoSomething() method
```cs
// Do something with client text message
public void DoSomething(ref ClientInfo cinfo)
{
    // Set message
    cinfo.Message = "!!! Hi from Worker.DoSomething() " + Reverse(cinfo.Message) + "\r\n";

    // Set log message
    cinfo.LoggerMessage = "[Log from Work() class]";
    
    // Set broadcast message
    cinfo.BroadcastMessage = "[Broadcast from Work() class]";

    // Or disconnect client with
    cinfo.Disconnect = true;
}
```

### Run HotSsl server C# or .NetCore 2.2
```bash
cd HotSsl

# Run program
dotnet run

# Or with daemon
nohup dotnet run &

# Close daemon
killall -9 dotnet

# Show daemons
netstat -tlpn
netstat -tW
```

### Connect from ssl client
```bash
openssl s_client -connect 1.1.1.1:8888 -crlf
openssl s_client -connect hostname.com:8888 -crlf

# Server disconnects where you send
1. End of line: \r\n
2. Empty line: \r\n\r\n
```

## HotSslClient .NetCore Tls/Ssl socket client

```cs
using System;
// Import client
using HotSsl;

namespace HotSsl
{
    class Program
    {
        static void Main(string[] args)
        {
            String msg = "";
            
            // Connect to server
            HotSslClient c = new HotSslClient("localhost", 8888);

            // Send message
            msg = c.Send("Hello from client");
            Console.WriteLine(msg);
            
            // Send message
            msg = c.Send("Bye from client");
            Console.WriteLine(msg);
            
            // Quit connection
            c.Send("\r\n\r\n");

            // Close connection
            c.Close();
        }
    }
}

```
