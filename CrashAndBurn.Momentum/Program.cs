using CrashAndBurn.Common;
using CrashAndBurn.Momentum.Strategy;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CrashAndBurn.Momentum
{
	class Program
	{
		static void Main(string[] arguments)
		{
			if (arguments.Length != 2)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Output.WriteLine($"{name.Name} <path to reference stock index .csv> <folder containing stock .csv files>");
				return;
			}
			string referenceIndexPath = arguments[0];
			string stockFolder = arguments[1];
			var referenceIndex = Stock.FromFile(referenceIndexPath);
			var stockPaths = Directory.GetFiles(stockFolder, "*.csv");
			var stocks = stockPaths.Select(path => Stock.FromFile(path)).ToList();
			if (!stocks.Any())
			{
				throw new ApplicationException("Failed to find any stocks.");
			}
			Output.WriteLine($"Loaded {stocks.Count} stock(s).");
			var stockMarket = new StockMarket(stocks);
			EvaluateStrategies(referenceIndex, stockMarket);
		}

		private static void EvaluateStrategies(Stock referenceIndex, StockMarket stockMarket)
		{
			var strategies = new BaseStrategy[] { };
			foreach (var strategy in strategies)
			{
				var date = new DateTime(2000, 1, 1);
				stockMarket.Initialize(Constants.InitialCash, Constants.OrderFees, Constants.CapitalGainsTax, Constants.InitialMargin, Constants.MaintenanceMargin, date);
				while (stockMarket.Date < DateTime.Now)
				{
					strategy.Trade(stockMarket);
					stockMarket.NextDay();
				}
			}
			throw new NotImplementedException();
		}
	}
}
