using System;
using System.Net; //IPAddress, IPEndPoint 
using System.Net.Sockets; // TcpClient
using System.Net.Security; // SslStream

namespace HotSsl
{
    class ClientInfo
	{
		public IAsyncResult Ar = null;
		public TcpListener Listener = null;
		public TcpClient Client = null;
		public SslStream Ssl = null;		
		public Int32 Port = 0;        
		public String IPAddr = "";
		public bool Disconnect = false;
		public String Message = "";
		public String LoggerMessage = "";
		public String BroadcastMessage = "";
		public long UniqueId = Util.UniqueNumber;

		public ClientInfo(IAsyncResult arResult){
			Ar = arResult;
			Listener = (TcpListener)Ar.AsyncState;
			Client = Listener.EndAcceptTcpClient(Ar);			
			Ssl = new SslStream(Client.GetStream(), false);
			IPAddr = ((IPEndPoint)Client.Client.RemoteEndPoint).Address.ToString();
			Port = Int32.Parse(((IPEndPoint)Client.Client.RemoteEndPoint).Port.ToString());
		}
	}
}
