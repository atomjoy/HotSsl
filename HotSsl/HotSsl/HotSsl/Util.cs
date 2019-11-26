using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading; // Interlocked.CompareExchange
using System.Security.Cryptography;

namespace HotSsl
{
	public static class Util
	{
		// Last timestamp
		private static long lastTimeStamp = DateTime.UtcNow.Ticks;

		// Unique id
		public static long UniqueNumber
		{
			get
			{
				long original, newValue;
				do
				{
					original = lastTimeStamp;
					long now = DateTime.UtcNow.Ticks;
					newValue = Math.Max(now, original + 1);
				} while (Interlocked.CompareExchange(ref lastTimeStamp, newValue, original) != original);

				return newValue;
			}
		}

		public static String ToUTF8(string t){
			return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(t));
		}

		public static String currentDate(){
			DateTime d = DateTime.UtcNow.ToLocalTime();
			return d.ToString("yyyy-MM-dd hh:mm:ss");
		}

		public static long unixTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
		public static long unixTimestampSeconds = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
		public static long unixTimestampMilliseconds = (Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))).TotalMilliseconds;

		public static String MD5(String txt)
		{
			using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
			{
				// byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(txt);
				byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(txt);
				byte[] hashBytes = md5.ComputeHash(inputBytes);

				// Convert the byte array to hexadecimal string
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < hashBytes.Length; i++)
				{
					sb.Append(hashBytes[i].ToString("X2"));
				}
				return sb.ToString();
			}
		}
	}
}
