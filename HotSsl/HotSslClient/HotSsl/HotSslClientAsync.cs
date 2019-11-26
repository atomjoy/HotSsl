/*
  Nie działa w: While, Task, Thread 
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HotSsl
{
    class HotSslClientAsync
    {        
        public List<String> certErrors = new List<String>();
        public static TcpClient Client = null;
        public static SslStream Ssl = null;
        public bool AllowSelfSigned = true;
        public String Host = "localhost";
        public Int32 Port = 8888;
		    public String IPAddr = "";
        public String LineEndCRLF = "\r\n";
        public static String Response;
        public static Int32 BufferSize = 2048;        

        private static ManualResetEvent connectDone = new ManualResetEvent(false);  
        private static ManualResetEvent sendDone = new ManualResetEvent(false);  
        private static ManualResetEvent readDone = new ManualResetEvent(false);  

        public class ClientInfo
        {
            public SslStream Ssl;
            public byte[] buffer;
            public string message;
            public static Int32 BufferSize = HotSslClientAsync.BufferSize;

            public ClientInfo()
            {
                buffer = new byte[BufferSize];
            }
        }

        public HotSslClientAsync(String host = "localhost", Int32 port = 8888, bool SelfSigned = true){
            Host = host;
            Port = port;
            AllowSelfSigned = SelfSigned;
            Connect();
        }

        public async Task Connect()
        {            
            //readDone.Reset();
            //sendDone.Reset();
            //connectDone.Reset();

            TcpClient client = new TcpClient();
            client.BeginConnect(Host, Port, new AsyncCallback(ConnectCallback), client);
            connectDone.WaitOne();
            // Hello message from server in Response
            await ReadMessage();
            readDone.WaitOne();            
        }
        
        public async Task Send(String msg = "Hello From Ciient"){
            
            try{
                await WriteMessage(msg);                
                sendDone.WaitOne();
                
                await ReadMessage();
                readDone.WaitOne();
                
            } catch (Exception e) {  
                Console.WriteLine(e.Message);  
            }            
        }

        public void Close()
        {
            Client.Close();
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            // grab the connection			
			Client = (TcpClient) ar.AsyncState;
			IPAddr = ((IPEndPoint)Client.Client.RemoteEndPoint).Address.ToString();
			Port = Int32.Parse(((IPEndPoint)Client.Client.RemoteEndPoint).Port.ToString());

            if(!Client.Connected)
            {
                Print(new Exception("Couldn't connect to server!"));
                return;
            }
            // Handshake
            Ssl = new SslStream(Client.GetStream(), false, new RemoteCertificateValidationCallback(IsValidCert), null);

            try{
                Ssl.AuthenticateAsClient(Host);                
                connectDone.Set();
            }catch(Exception e){
                Client.Close();
                Console.WriteLine(e.Message);                
            }   
            // connectDone.Set();         
        }

        public async Task WriteMessage(String m)
        {        
            if (!Client.Connected){
                Print(new Exception("Connection losts!"));               
            }
            
            Console.WriteLine("Send : " + m);
            ClientInfo ci = new ClientInfo();
            ci.buffer =  Encoding.ASCII.GetBytes(m + "\r\n");
            // Asynchronously send the message to the client.
            if(Ssl.IsAuthenticated && Ssl.IsEncrypted){
                Ssl.BeginWrite(ci.buffer, 0, ci.buffer.Length, new AsyncCallback(WriteCallback), ci);
            }
        }

        private void WriteCallback(IAsyncResult ar)
        {   
            // Asynchronously read a message from the server.
            // Ssl.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReadCallback), stream);
            // ClientInfo c = (ClientInfo) ar.AsyncState;
            // Complete sending the data to the remote device.
            Ssl.EndWrite(ar);        
            // Signal that all bytes have been sent.  
            sendDone.Set();
        }

        private async Task ReadMessage() {         
            ClientInfo c = new ClientInfo();
            var stream = Client.GetStream();
            if (stream.DataAvailable){
                if(Ssl.CanRead && Ssl.IsAuthenticated && Ssl.IsEncrypted){
                    Ssl.BeginRead(c.buffer, 0, BufferSize, new AsyncCallback(ReadCallback), c);
                }else{
                    Console.WriteLine("Error SSL AUTH");
                    HotSslClientAsync.Response = c.message;
                    readDone.Set();
                    return;
                }
            }
        }  

        private static void ReadCallback( IAsyncResult ar ) {              
            ClientInfo c = (ClientInfo) ar.AsyncState;                
            int bytesRead = -1;
            // Read data from the remote device.  
            bytesRead = Ssl.EndRead(ar);
            c.message += Encoding.UTF8.GetString(c.buffer,0,bytesRead);
            Array.Clear(c.buffer, 0, c.buffer.Length);                                
            if (c.message.Contains("\r\n")){ 
                // Console.WriteLine("Message line end !!!");
                HotSslClientAsync.Response = c.message;
                readDone.Set();
                return;
            }
            if (bytesRead > 0){
                Console.WriteLine("ReadMessageCallback loop: " + bytesRead);
                Ssl.ReadTimeout = 1000;
                Ssl.WriteTimeout = 1000;

                if(Ssl.CanRead && Ssl.IsAuthenticated && Ssl.IsEncrypted){
                    Ssl.BeginRead(c.buffer, 0, BufferSize, new AsyncCallback(ReadCallback), c);
                }else{
                    Console.WriteLine("Error SSL AUTH");
                    HotSslClientAsync.Response = c.message;
                    readDone.Set();
                    return;
                }
            } else {
                Console.WriteLine("ReadMessageCallback loop end: " + bytesRead);
                // Signal that all bytes have been received.                    
                HotSslClientAsync.Response = c.message;
                readDone.Set();                    
                return;
            }         
        }  

        public bool IsValidCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None){
                return true;
            }
            if(AllowSelfSigned){
                return true;
            }else{
                return false;
            }
        }

        public static void Print(Exception e){
            Console.WriteLine(e.ToString());
            return;
        }
    }
}
/*
int result = await Task.Run(() => SumNow(count)).ContinueWith(task => Multiply(task));
*/
