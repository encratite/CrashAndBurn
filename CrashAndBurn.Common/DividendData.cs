using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace CrashAndBurn.Common
{
	public class DividendData
	{
		public DateTime Date { get; set; }
		public decimal Amount { get; set; }

		public static IEnumerable<DividendData> Read(string path)
		{
			string json = File.ReadAllText(path);
			var dividends = JsonConvert.DeserializeObject<DividendData[]>(json);
			return dividends;
		}

		public static void Write(string path, List<DividendData> dividends)
		{
			string json = JsonConvert.SerializeObject(dividends);
			File.WriteAllText(path, json);
		}

		public DividendData()
		{
		}

		public DividendData(DateTime date, decimal amount)
		{
			Date = date;
			Amount = amount;
		}
	}
}
