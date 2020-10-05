using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CrashAndBurn.Common
{
	public class Stock
	{
		// Symbol or ISIN.
		public string Id { get; private set; }

		public SortedDictionary<DateTime, StockData> History { get; private set; }

		private StockData _Last;

		public static Stock FromFile(string path)
		{
			string id = Path.GetFileNameWithoutExtension(path);
			var stockData = StockData.FromFile(path);
			var stock = new Stock(id, stockData);
			return stock;
		}

		public Stock(string id, IEnumerable<StockData> history)
		{
			Id = id;
			History = new SortedDictionary<DateTime, StockData>();
			foreach (var stockData in history)
			{
				History[stockData.Date] = stockData;
				_Last = stockData;
			}
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
			if (History.Any())
			{
				for (int i = 0; i < 3; i++)
				{
					if (History.TryGetValue(date - TimeSpan.FromDays(i), out StockData stockData))
					{
						return stockData.Open;
					}
				}
				if (_Last.Date <= date)
				{
					return _Last.Open;
				}
			}
			return null;
		}
	}
}
