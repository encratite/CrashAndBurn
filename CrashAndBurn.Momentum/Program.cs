using CrashAndBurn.Common;
using CrashAndBurn.Momentum.Strategy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

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
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var referenceIndex = Stock.FromFile(referenceIndexPath);
			var stocks = LoadStocks(stockFolder);
			stopwatch.Stop();
			Output.WriteLine($"Loaded {stocks.Count} stock(s) in {stopwatch.Elapsed.Seconds:0.0} s.");
			var stockMarket = new StockMarket(stocks);
			EvaluateStrategies(referenceIndex, stockMarket, null, null);
		}

		private static void EvaluateStrategies(Stock referenceIndex, StockMarket stockMarket, int? firstYear, int? lastYear)
		{
			var dateRange = stockMarket.GetDateRange();
			DateTime startDate = GetStartEndDate(firstYear, false, dateRange);
			DateTime endDate = GetStartEndDate(lastYear, true, dateRange);
			var strategies = GetStrategies();
			foreach (var strategy in strategies)
			{
				stockMarket.Initialize(Constants.InitialCash, Constants.OrderFees, Constants.CapitalGainsTax, Constants.InitialMargin, Constants.MaintenanceMargin, startDate);
				while (stockMarket.Date < endDate)
				{
					strategy.Trade(stockMarket);
					stockMarket.NextDay();
				}
				stockMarket.LiquidateAll();
			}
		}

		private static List<BaseStrategy> GetStrategies()
		{
			var strategies = new List<BaseStrategy>();
			for (int stocks = 3; stocks <= 5; stocks++)
			{
				for (decimal stopLossThreshold = 0.05m; stopLossThreshold <= 0.15m; stopLossThreshold += 0.05m)
				{
					for (int holdDays = 15; holdDays <= 60; holdDays *= 2)
					{
						const int historyDays = 360;
						for (int ignoreDays = 30; ignoreDays <= 60; ignoreDays += 30)
						{
							var strategy = new LongShortMomentumStrategy(stocks, stopLossThreshold, holdDays, historyDays, ignoreDays);
							strategies.Add(strategy);
						}
					}
				}
			}
			return strategies;
		}

		private static DateTime GetYearDate(int year)
		{
			return new DateTime(year, 1, 1);
		}

		private static DateTime GetStartEndDate(int? year, bool isMax, DateRange dateRange)
		{
			DateTime startEndDate;
			DateTime minMaxDate = isMax ? dateRange.Max.Value : dateRange.Min.Value;
			if (year.HasValue)
			{
				startEndDate = GetYearDate(year.Value);
				if (
					(!isMax && startEndDate < minMaxDate) ||
					(isMax && startEndDate > minMaxDate)
				)
				{
					startEndDate = minMaxDate;
				}
			}
			else
			{
				startEndDate = minMaxDate;
			}
			return startEndDate;
		}

		private static List<Stock> LoadStocks(string stockFolder)
		{
			var stockPaths = Directory.GetFiles(stockFolder, "*.csv").ToList();
			var stocks = new List<Stock>();
			var threads = new List<Thread>();
			for (int i = 0; i < Environment.ProcessorCount; i++)
			{
				var thread = new Thread(() =>
				{
					do
					{
						string stockPath;
						lock (stockPaths)
						{
							if (!stockPaths.Any())
							{
								break;
							}
							stockPath = stockPaths.First();
							stockPaths.RemoveAt(0);
						}
						var stock = Stock.FromFile(stockPath);
						lock (stocks)
						{
							stocks.Add(stock);
						}
					}
					while (true);
				});
				thread.Start();
				threads.Add(thread);
			}
			foreach (var thread in threads)
			{
				thread.Join();
			}
			if (!stocks.Any())
			{
				throw new ApplicationException("Failed to find any stocks.");
			}
			return stocks;
		}
	}
}
