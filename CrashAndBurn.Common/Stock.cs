using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CrashAndBurn.Common
{
	public class Stock
	{
		private StockData[] _history;

		// Symbol or ISIN.
		public string Id { get; private set; }
		public SortedDictionary<DateTime, decimal> Dividends { get; private set; } = new SortedDictionary<DateTime, decimal>();
		public DateTime? DateFirstAdded { get; private set; }

		public static Stock FromFile(string path)
		{
			string id = Path.GetFileNameWithoutExtension(path);
			var stockData = StockData.FromFile(path);
			string jsonPath = Path.Combine(Path.GetDirectoryName(path), $"{id}.json");
			var dividends = new List<DividendData>();
			if (File.Exists(jsonPath))
			{
				var jsonData = JsonData.Read(jsonPath);
				dividends = jsonData.Dividends;
			}
			var stock = new Stock(id, stockData, dividends);
			return stock;
		}

		public Stock(string id, IEnumerable<StockData> history, IEnumerable<DividendData> dividends)
		{
			Id = id;
			_history = GetHistory(history);
			foreach (var dividendData in dividends)
				Dividends.Add(dividendData.Date, dividendData.Amount);
		}

		public override bool Equals(object obj)
		{
			var stock = obj as Stock;
			if (stock == null)
			{
				return false;
			}
			return Id == stock.Id;
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public decimal GetPrice(DateTime date)
		{
			var price = MaybeGetPrice(date);
			return price.Value;
		}

		public decimal? MaybeGetPrice(DateTime date)
		{
			var firstStockData = _history.First();
			if (date < firstStockData.Date)
				return null;
			var timeDifference = date - firstStockData.Date;
			int timeDifferenceDays = (int)timeDifference.TotalDays;
			int index = Math.Min(timeDifferenceDays, _history.Length - 1);
			var stockData = _history[index];
			return stockData.Open;
		}

		public override string ToString()
		{
			return Id;
		}

		public void UpdateDateRange(DateRange dateRange)
		{
			var first = _history.First();
			var last = _history.Last();
			dateRange.Process(first.Date);
			dateRange.Process(last.Date);
		}

		private StockData[] GetHistory(IEnumerable<StockData> history)
		{
			if (!history.Any())
				throw new ApplicationException("No data in stock history.");
			var firstStockData = history.First();
			var historyList = new List<StockData>
			{
				firstStockData
			};
			var previousStockData = firstStockData;
			foreach (var stockData in history.Skip(1))
			{
				var timeDifference = stockData.Date - previousStockData.Date;
				int timeDifferenceDays = (int)timeDifference.TotalDays;
				for (int i = 1; i < timeDifferenceDays; i++)
				{
					var gapStockdata = previousStockData;
					gapStockdata.Date = previousStockData.Date + TimeSpan.FromDays(i);
					historyList.Add(gapStockdata);
				}
				historyList.Add(stockData);
				previousStockData = stockData;
			}

			return historyList.ToArray();
		}
	}
}
