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

namespace HotSsl
{
    class HotSslClient
    {
        protected TcpClient Client = null;
        protected SslStream Ssl = null;
        protected String Host = "localhost";
        protected Int32 Port = 8888;
        public String HelloMsg = "";

        public HotSslClient(String host = "localhost", Int32 port = 8888, bool allowSelfSigned = true){
            Port = port;
            Host = host;
            Connect();
            HelloMsg = ReadMessage();            
        }

        public String Send(String cmd)
        {            
            String msg = "";
            try{
                // Write line
                WriteMessage(cmd + "\r\n");
                msg = ReadMessage();               
            }catch (Exception e){                
                Ssl.Close();
                Client.Close();
                Print(e);
            }
            return msg;
        }

        public void Connect(){
            Client = new TcpClient(Host, Port);
            Ssl = new SslStream(Client.GetStream(), false, new RemoteCertificateValidationCallback(IsValidCert), null);
            try{
                // Tls handshake
                Ssl.AuthenticateAsClient(Host);
            }catch (Exception e){                
                Ssl.Close();
                Client.Close();
                Print(e);
            }
        }

        public void WriteMessage(String s){
            try{
                byte[] m = Encoding.UTF8.GetBytes(s);
                Ssl.Write(m);
                Ssl.Flush();
            }catch (Exception e){
                Ssl.Close();
                Client.Close();
                Print(e);
            }
        }

        public String ReadMessage()
        {
            String message = "";
            try{
                byte[] buffer = new byte[2048];
                int bytes = -1;
                do{
                    try{
                        bytes = Ssl.Read(buffer, 0, buffer.Length);
                        message += Encoding.UTF8.GetString(buffer);
                        // End line CRLF
                        if(message.Contains("\r\n")){
                            break;
                        }
                    }catch (Exception e){
                        throw e;
                    }
                } while (bytes != 0);

            }catch (Exception e){
                Print(e);
            }
            return message;
        }

        public bool IsValidCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None){
                return true;
            }
            // Allow self signed
            if(allowSelfSigned){
                return true;
            }else{
                return false;
            }
        }

        public void Print(Exception e){
            throw e;
        }

        public void Close(){
            Ssl.Close();
            Client.Close();
            Ssl.Dispose();
        }
    }
}
