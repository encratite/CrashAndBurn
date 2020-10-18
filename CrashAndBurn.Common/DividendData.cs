using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CrashAndBurn.Common
{
	public class DividendData
	{
		public DateTime RecordDate { get; set; }
		public decimal Amount { get; set; }

		public static IEnumerable<DividendData> Read(string path)
		{
			string json = File.ReadAllText(path);
			var dividends = JsonConvert.DeserializeObject<DividendData[]>(json);
			return dividends;
		}

		public static void Write(string path, IEnumerable<DividendData> dividends)
		{
			string json = JsonConvert.SerializeObject(dividends.ToArray());
			File.WriteAllText(path, json);
		}

		public DividendData()
		{
		}

		public DividendData(DateTime recordDate, decimal amount)
		{
			RecordDate = recordDate;
			Amount = amount;
		}
	}
}
