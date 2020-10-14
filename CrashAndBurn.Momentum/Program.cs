using CrashAndBurn.Common;
using CrashAndBurn.Momentum.Strategy;
using System;
using System.Collections.Concurrent;
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
			EvaluatePeriods(referenceIndexPath, stockFolder);
		}

		private static void EvaluatePeriods(string referenceIndexPath, string stockFolder)
		{
			var referenceIndex = Stock.FromFile(referenceIndexPath);
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var stocks = LoadStocks(stockFolder);
			stopwatch.Stop();
			Output.WriteLine($"Loaded {stocks.Count} stock(s) in {stopwatch.Elapsed.TotalSeconds:0.0} s.");
			stopwatch.Reset();
			Output.WriteLine("Evaluating strategies.");
			Output.NewLine();
			stopwatch.Start();
			int firstYear = 1970;
			const int windowSize = 20;
			int periods = 0;
			for (int year = firstYear; year <= DateTime.Now.Year - windowSize; year += 10)
			{
				EvaluateStrategies(referenceIndex, stocks, year, year + windowSize);
				periods++;
			}
			for (int year = firstYear; year <= DateTime.Now.Year - 5; year += 10)
			{
				EvaluateStrategies(referenceIndex, stocks, year, year + windowSize);
				periods++;
			}
			stopwatch.Stop();
			Output.WriteLine($"Evaluated all strategies over {periods} periods in {stopwatch.Elapsed.TotalSeconds:0.0} s.");
		}

		private static void EvaluateStrategies(Stock referenceIndex, List<Stock> stocks, int? firstYear, int? lastYear)
		{
			var dateRange = GetDateRange(stocks);
			DateTime startDate = GetStartEndDate(firstYear, false, dateRange);
			DateTime endDate = GetStartEndDate(lastYear, true, dateRange);
			var strategies = GetStrategies();
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var strategyQueue = new ConcurrentQueue<BaseStrategy>(strategies);
			var threads = new List<Thread>();
			for (int i = 0; i < Environment.ProcessorCount; i++)
			{
				var thread = new Thread(() => ProcessStrategies(strategyQueue, stocks, startDate, endDate));
				thread.Start();
				threads.Add(thread);
			}
			foreach (var thread in threads)
				thread.Join();
			stopwatch.Stop();
			PrintStrategyStats(startDate, endDate, referenceIndex, strategies);
			Output.WriteLine($"  Evaluated {strategies.Count} strategies in {stopwatch.Elapsed.TotalSeconds:0.0} s.");
			Output.NewLine();
		}

		private static void ProcessStrategies(ConcurrentQueue<BaseStrategy> strategyQueue, List<Stock> stocks, DateTime startDate, DateTime endDate)
		{
			while (strategyQueue.TryDequeue(out BaseStrategy strategy))
			{
				var stockMarket = new StockMarket(stocks);
				stockMarket.Initialize(Constants.InitialCash, Constants.OrderFees, Constants.CapitalGainsTax, Constants.InitialMargin, Constants.MaintenanceMargin, Constants.StockLendingFee, startDate);
				while (stockMarket.Date < endDate)
				{
					strategy.Trade(stockMarket);
					stockMarket.NextDay();
				}
				stockMarket.CashOut();
				strategy.Cash = stockMarket.Cash;
			}
		}

		private static DateRange GetDateRange(List<Stock> stocks)
		{
			var dateRange = new DateRange();
			foreach (var stock in stocks)
				stock.UpdateDateRange(dateRange);
			return dateRange;
		}

		private static void PrintStrategyStats(DateTime startDate, DateTime endDate, Stock referenceIndex, List<BaseStrategy> strategies)
		{
			Output.WriteLine($"Strategies sorted by performance, in comparison to index ETF (from {startDate.Year} to {endDate.Year}):", ConsoleColor.White);
			decimal referenceCash = GetReferenceCash(startDate, endDate, referenceIndex);
			strategies.Sort((x, y) => -x.Cash.Value.CompareTo(y.Cash.Value));
			foreach (var strategy in strategies)
			{
				decimal performance = StockMarket.GetPerformance(strategy.Cash.Value, referenceCash);
				Output.Write($"  {strategy.Name}: {strategy.Cash:C2}");
				Output.WritePerformance(strategy.Cash.Value, referenceCash);
			}
		}

		private static decimal GetReferenceCash(DateTime startDate, DateTime endDate, Stock referenceIndex)
		{
			decimal referencePerformance = GetReferenceIndexPerformance(startDate, endDate, referenceIndex);
			decimal referenceOutperformance = referencePerformance - 1.0m;
			if (referenceOutperformance > 0.0m)
				referencePerformance -= Constants.CapitalGainsTax * referenceOutperformance;
			decimal referenceCash = referencePerformance * Constants.InitialCash;
			return referenceCash;
		}

		private static decimal GetReferenceIndexPerformance(DateTime startDate, DateTime endDate, Stock referenceIndex)
		{
			decimal startPrice = referenceIndex.GetPrice(startDate);
			decimal endPrice = referenceIndex.GetPrice(endDate);
			decimal performance = StockMarket.GetPerformance(endPrice, startPrice);
			return performance;
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
						for (int ignoreDays = 0; ignoreDays <= 60; ignoreDays += 30)
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
					(isMax && startEndDate >= minMaxDate)
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
			var stockPathQueue = new ConcurrentQueue<string>(stockPaths);
			var stocks = new ConcurrentQueue<Stock>();
			var threads = new List<Thread>();
			for (int i = 0; i < Environment.ProcessorCount; i++)
			{
				var thread = new Thread(() =>
				{
					while (stockPathQueue.TryDequeue(out string stockPath))
					{
						var stock = Stock.FromFile(stockPath);
						stocks.Enqueue(stock);
					}
				});
				thread.Start();
				threads.Add(thread);
			}
			foreach (var thread in threads)
				thread.Join();

			if (!stocks.Any())
				throw new ApplicationException("Failed to find any stocks.");
			var output = stocks.ToList();
			return output;
		}
	}
}
