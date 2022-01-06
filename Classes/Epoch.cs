using System;

namespace BullyBot.Classes
{
	public class Epoch
	{
		private static readonly DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0);
		
		public static DateTime? FromUnix(long? secondsSinceepoch)
		{
			if( secondsSinceepoch == null ) return null;
			return epochStart.AddSeconds((long)secondsSinceepoch);
		}

		public static long? ToUnix(DateTime? dateTime)
		{
			if( dateTime == null ) return null;
			return (long)((DateTime)dateTime - epochStart).TotalSeconds;
		}
		
		public static long ToUnix(DateTime dateTime)
		{
			return (long)(dateTime - epochStart).TotalSeconds;
		}

		public static long Now
	    {
			get
	        {
				return (long)(DateTime.UtcNow - epochStart).TotalSeconds;
			}
		}
	}
}