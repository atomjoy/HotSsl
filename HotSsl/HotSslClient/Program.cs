using System;
﻿using System.Threading;
using HotSsl;

namespace HotSsl
{
    class Program
    {
        static void Main(string[] args)
        {
	        Console.WriteLine("Hello World!");

		Thread t1 = new Thread(() => RunMe("T1"));
		t1.Start();

		Thread t2 = new Thread(() => RunMe("Z1"));
                t2.Start();

		Thread t3 = new Thread(() => RunMe("X1"));
                t3.Start();
	}
	
	public static void RunMe(String t){
		Int64 cnt = 0;
		HotSslClient c = new HotSslClient("localhost", 8888);

		while(true){
			cnt++;
			c.Send("EHLO: From client " + t +  " " + cnt);
			if(cnt == 25){
				// Quit connection
				c.Send("\r\n\r\n");
				c.Close();
				break;
			}
 			// Thread.Sleep(10);
		}
		Console.WriteLine("Closing " + t + " Cnt(25) " + cnt);
	}	
    }
}
