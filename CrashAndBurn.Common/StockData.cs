using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CrashAndBurn.Common
{
	public class StockData
	{
		public DateTime Date { get; private set; }
		public decimal Open { get; private set; }
		public decimal High { get; private set; }
		public decimal Low { get; private set; }
		public decimal Close { get; private set; }
		public decimal AdjustedClose { get; private set; }
		public long Volume { get; private set; }

		public static List<StockData> FromFile(string csvPath)
		{
			var lines = File.ReadAllLines(csvPath);
			var pattern = new Regex(@"^(?<year>\d+)-(?<month>\d+)-(?<day>\d+),(?<open>\d+\.\d+),(?<high>\d+\.\d+),(?<low>\d+\.\d+),(?<close>\d+\.\d+),(?<adjustedClose>\d+\.\d+),(?<volume>\d+)$");
			var history = new List<StockData>();
			foreach (string line in lines)
			{
				var match = pattern.Match(line);
				if (match.Success)
				{
					var groups = match.Groups;
					Func<string, int> parseInt = (string name) => int.Parse(groups[name].Value);
					Func<string, long> parseLong = (string name) => long.Parse(groups[name].Value);
					Func<string, decimal> parseDecimal = (string name) => decimal.Parse(groups[name].Value);
					int year = parseInt("year");
					int month = parseInt("month");
					int day = parseInt("day");
					decimal open = parseDecimal("open");
					decimal high = parseDecimal("high");
					decimal low = parseDecimal("low");
					decimal close = parseDecimal("close");
					decimal adjustedClose = parseDecimal("adjustedClose");
					long volume = parseLong("volume");
					var date = new DateTime(year, month, day);
					var stockData = new StockData(date, open, high, low, close, adjustedClose, volume);
					history.Add(stockData);
				}
			}
			if (!history.Any())
			{
				throw new ApplicationException("Failed to parse stock data from .csv file.");
			}
			return history;
		}

		public StockData(DateTime date, decimal open, decimal high, decimal low, decimal close, decimal adjustedClose, long volume)
		{
			Date = date;
			Open = open;
			High = high;
			Low = low;
			Close = close;
			AdjustedClose = adjustedClose;
			Volume = volume;
		}
	}
}
