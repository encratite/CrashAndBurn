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

		public List<StockData> History { get; private set; }

		public static Stock FromFile(string path)
		{
			string id = Path.GetFileNameWithoutExtension(path);
			var stockData = StockData.FromFile(path);
			var stock = new Stock(id, stockData);
			return stock;
		}

		public Stock(string id, List<StockData> history)
		{
			Id = id;
			History = history;
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
			var latestStockData = History.First();
			foreach (var stockData in History)
			{
				if (stockData.Date > date)
				{
					break;
				}
				latestStockData = stockData;
			}
			return latestStockData.Open;
		}
	}
}
