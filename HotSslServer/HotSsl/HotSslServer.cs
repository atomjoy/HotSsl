using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using HotSsl;

namespace HotSsl
{
	class HotSslServer
	{   
		protected Hashtable ConnectedClients = new Hashtable();
		protected Queue BroadcastMessages = new Queue();
		protected Queue LoggerMessages = new Queue();
		protected X509Certificate ServerCertificateTls;
		protected ManualResetEvent tcpClientConnected = new ManualResetEvent(false);
		protected String CertificatePfx = "certificate.pfx";
		protected String CertificatePass = "password12345";
		protected Int32 MaxThreads = 1000;
		public Int32 ServerPort = 8888;
		public String LineEndCRLF = "\r\n";
		public String LoggerPath = @"hotssl-logger.log";
		public bool StartLogger = false;
		public bool StartBroadcast = false;

		// Do something with client text message here
		public void MessageWorker(ref ClientInfo cinfo)
		{
			// Work with message with own class
			Work w = new Work();
			w.DoSomething(ref cinfo);
			
			/*
			// Get client message
			String msg = cinfo.Message;

			// Or disconnect client with
			cinfo.Disconnect = true;

			// Work with message here ....
			// cinfo.Message = "!!! Hello !!! Your message: " + cinfo.Message;
			*/
		}

		public void Start(String PfxPath = "", String Pass = "")
		{
			if(PfxPath.Length > 0){
				CertificatePfx = PfxPath;
			}
			if(Pass.Length > 0){
				CertificatePass = Pass;
			}
			
			ServerCertificateTls = new X509Certificate2(CertificatePfx, CertificatePass);

			// Save to logs
			if(StartLogger){				
				RunLogger();
			}
			// Run broadcast
			if(StartBroadcast){				
				RunBroadcastQueue();
			}

			try{
				Print("[!!!] Starting server: " + DateTime.UtcNow.Ticks);
				IPEndPoint endpoint = new IPEndPoint(IPAddress.IPv6Any, ServerPort);
				TcpListener listener = new TcpListener(endpoint);
				listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
				listener.Start();

				while (true)
				{
					try{
						tcpClientConnected.Reset(); //  Włącz blokowanie wątku
						listener.BeginAcceptTcpClient(new AsyncCallback(ProcessIncomingConnection), listener);
						tcpClientConnected.WaitOne(); // Blokuj wątek
					}catch (Exception e){
						Print("[SERVER_START_ERROR] " + e.Message);
					}
				}
			}catch (Exception e){
				Print("[SERVER_LISTENER_ERROR] " + e.Message);				
			}
		}

		public void ProcessIncomingConnection(IAsyncResult ar)
		{
			ClientInfo oClient = new ClientInfo(ar);
			Print("[NEW_CLIENT] IP: " + oClient.IPAddr + " Port: " + oClient.Port);

			try{
				try{
					// Handshake ssl
					oClient.Ssl.AuthenticateAsServer(ServerCertificateTls, false, SslProtocols.Tls, true);
				}catch(Exception e){
					Print("[TLS_ONLY_ERR] " + e.Message);
					// Write no ssl
					Write("404 Start Tls", oClient.Client);
				}
				// Time out
				oClient.Ssl.ReadTimeout = 15000;
				oClient.Ssl.WriteTimeout = 15000;

				if(oClient.Ssl.IsAuthenticated && oClient.Ssl.IsEncrypted){
					if(MaxThreads > 0){
						ThreadPool.SetMaxThreads(MaxThreads,1000); // Set max threads
					}
					ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object state) { ProcessIncomingData(oClient); }), null);
					tcpClientConnected.Set(); // Wyłącz blokowanie wątku
				}else{
					DisconnectClient(oClient);
					tcpClientConnected.Set(); // Wyłącz blokowanie wątku
				}
			}catch (Exception e){
				Print("[TLS_ERR]" + e.Message);
				DisconnectClient(oClient);
				tcpClientConnected.Set(); // Wyłącz blokowanie wątku
			}
		}

		public void ProcessIncomingData(object obj)
		{
			ClientInfo ci = (ClientInfo)obj;
			try{
				ConnectedClients.Add(ci.Client, ci); // Add client to list

				// Server Hello message
				ci.Message = "600 Hello from server";
				WriteData(ref ci);

				// Send to worker
				while(true){
					try{
						if(!ci.Ssl.CanRead || !ci.Ssl.CanWrite || !ci.Ssl.IsAuthenticated || !ci.Ssl.IsEncrypted){
							Print("[STREAM_READ_WRITE_ERR]");
							break;
						}

						// Clear messages
						ci.Message = "";
						ci.LoggerMessage = "";
						ci.BroadcastMessage = "";

						// Read message from client
						ReadMessage(ref ci);
						if(ci.Disconnect || ci.Message.Length < 1){
							WriteData(ref ci);
							break;
						}

						// Server main loop
						MessageWorker(ref ci);
						if(ci.Disconnect || ci.Message.Length < 1){
							WriteData(ref ci);
							break;
						}
						if(!String.IsNullOrWhiteSpace(ci.LoggerMessage)){
							AddLoggerMessage(ci.LoggerMessage);
						}
						if(!String.IsNullOrWhiteSpace(ci.BroadcastMessage)){
							AddBroadcastMessage(ci.LoggerMessage);
						}

						// Send ressponse
						WriteData(ref ci);
						if(ci.Disconnect || ci.Message.Length < 1){
							// WriteData(ref ci);
							break;
						}						
					}catch(Exception e){
						Print("[ERROR_MESSAGE] "+ e.Message);
						break;
					}
				}
				DisconnectClient(ci);

			}catch(Exception e){
				Print("[ERROR_WHILE] "+ e.Message);
				DisconnectClient(ci);
			}
		}

		// Write to stream no ssl
		public void Write(String m, TcpClient c){
			Byte[] data = System.Text.Encoding.UTF8.GetBytes(m);
			NetworkStream stream = c.GetStream();
			stream.Write(data, 0, data.Length);
		}

		// Read msg with ssl
		public void ReadMessage(ref ClientInfo cinfo)
		{
			String message = "";
			try{
				byte[] buffer = new byte[2048];
				int bytes = -1;
			
				do{
					try{
						bytes = cinfo.Ssl.Read(buffer, 0, buffer.Length);
						String data = Encoding.UTF8.GetString(buffer);
						message += data;
						// End empty line
						if(bytes == 2 || bytes == 1){
							String s=data.Substring(0,2);
							if(bytes == 2 && (int)s[0] == 13 && (int)s[1] == 10 || bytes == 1 && (int)s[0] == 10){
								message = "200 Disconnecting";
								cinfo.Disconnect = true;
								break;
							}
						}
						// End GET request
						if(message.Contains("\r\n\r\n")){
							message = "200 Disconnecting";
							cinfo.Disconnect = true;
							break;
						}
						// End line CRLF
						if(message.Contains("\r\n")){
							cinfo.Message = message;
							break;
						}
					}catch (Exception e){
						Print("[READ_MESSAGE_EXCEPTION] UID["+cinfo.UniqueId+"] " + e.Message);
						throw;
					}
				} while (bytes != 0);

			}catch (Exception e){
				Print("[READ_MESSAGE_EXCEPTION] UID["+cinfo.UniqueId+"] " + e.Message);
				message = "404 Error " + e.Message;
				cinfo.Disconnect = true;
			}
			if(String.IsNullOrWhiteSpace(message)){
				Print("[READ_MESSAGE_EMPTY] UID["+cinfo.UniqueId+"]");
				message = "404 Empty message";
				cinfo.Disconnect = true;
			}
			cinfo.Message = message;
		}

		// Write msg with ssl
		public void WriteData(ref ClientInfo cinfo)
		{
			try{
				if(cinfo.Ssl.CanRead && cinfo.Ssl.CanWrite){
					byte[] m = Encoding.UTF8.GetBytes(cinfo.Message + LineEndCRLF);
					if(m.Length > 0){
						cinfo.Ssl.Write(m);
						cinfo.Ssl.Flush();
					}else{						
						cinfo.Message = "200 Goodbye";
						cinfo.Disconnect = true;
					}
				}else{
					cinfo.Message = "500 Error Stream Read Write";
					cinfo.Disconnect = true;
				}
			}catch (Exception e){
				Print("[WRITE_MESSAGE_EXCEPTION] UID["+cinfo.UniqueId+"] " + e.Message);
				cinfo.Message = "500 Error " + e.Message;
				cinfo.Disconnect = true;
			}
		}

		public void DisconnectClient(ClientInfo ci){
			ci.Ssl.Dispose();
			ci.Ssl.Close();
			ci.Client.Close();
		}

		public void RunBroadcastQueue(){
			// Resend messages to al clients
			Thread t1 = new Thread(() => Broadcast());
			t1.Start();
		}

		public void AddBroadcastMessage(String str){
			BroadcastMessages.Enqueue(str);
		}

		// Resend message to all connected clients
		public void Broadcast(){
			while(true){				
				if(ConnectedClients.Count > 0 && BroadcastMessages.Count > 0){
					// Do something ...
					// To each client from ConnectedClients
					// Send message from BroadcastMessages
				}
				Print("[Broadcast waits for 10 seconds]");
				Thread.Sleep(10000);
			}
		}

		public void RunLogger(){			
			Thread t1 = new Thread(() => Logger());
			t1.Start();
		}

		public void AddLoggerMessage(String str){
			LoggerMessages.Enqueue("["+Util.currentDate()+"] " + str);
		}

		public void Logger(){			
			if (!File.Exists(LoggerPath)){
				File.Create(LoggerPath);
			}
			String txt = "";
			while(true){
				// Do something with: LoggerMessages
				while(LoggerMessages.Count > 0){
					try{						
						using (StreamWriter sw = File.AppendText(LoggerPath)) 
						{
							txt = (string) LoggerMessages.Dequeue();
							sw.WriteLine(Util.ToUTF8(txt));
						}
					}catch(Exception e){
						LoggerMessages.Enqueue(txt + "[SAVE_ERR]");
						Print("[LOG_ERR] " + e.Message);
					}
				}
				Print("[Logger waits for 1 minute]");
				Thread.Sleep(60000);
			}
		}
		
		public static int GetAvailableThreads(Boolean show){
			int worker = 0;
			int io = 0;
			ThreadPool.GetAvailableThreads(out worker, out io);
			if(show){
				Console.WriteLine("Thread pool threads available for server: ");
				Console.WriteLine("Worker threads: {0:N0}", worker);
				Console.WriteLine("Asynchronous I/O threads: {0:N0}", io);
			}
			return worker;
		}

		public static int GetMaxThreads(Boolean show){
			int worker = 0;
			int io = 0;
			ThreadPool.GetMaxThreads(out worker, out io);
			if(show){
				Console.WriteLine("Thread pool threads available at startup: ");
				Console.WriteLine("Worker threads: {0:N0}", worker);
				Console.WriteLine("Asynchronous I/O threads: {0:N0}", io);
			}
			return io;
		}

		public void Print(String s){
			AddLoggerMessage(s);
			Console.WriteLine(s);
		}
	}
}
