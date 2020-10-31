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
		private const bool PrintIndividualStrategyPerformance = true;

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
			const int windowSize = 10;
			int periods = 0;
			/*
			for (int year = firstYear; year <= DateTime.Now.Year - windowSize; year += 2)
			{
				EvaluateStrategies(referenceIndex, stocks, year, year + windowSize);
				periods++;
			}
			*/
			for (int year = firstYear; year <= DateTime.Now.Year - 5; year += 5)
			{
				EvaluateStrategies(referenceIndex, stocks, year, null);
				periods++;
			}
			// EvaluateStrategies(referenceIndex, stocks, 2005, null);
			periods++;
			stopwatch.Stop();
			Output.WriteLine($"Evaluated all strategies over {periods} periods in {stopwatch.Elapsed.TotalSeconds:0.0} s.");
		}

		private static void EvaluateStrategies(Stock referenceIndex, List<Stock> stocks, int? firstYear, int? lastYear)
		{
			var dateRange = GetDateRange(stocks);
			DateTime startDate = GetStartEndDate(firstYear, false, dateRange);
			DateTime endDate = GetStartEndDate(lastYear, true, dateRange);
			var strategies = GetStrategies(out List<StrategyClass> strategyClasses);
			// var strategies = GetSpecificStrategy(out List<StrategyClass> strategyClasses);
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
			PrintStrategyStats(startDate, endDate, referenceIndex, strategies, strategyClasses);
			Output.NewLine();
			Output.WriteLine($"  Evaluated {strategies.Count} strategies in {stopwatch.Elapsed.TotalSeconds:0.0} s.");
			Output.NewLine();
		}

		private static void ProcessStrategies(ConcurrentQueue<BaseStrategy> strategyQueue, List<Stock> stocks, DateTime startDate, DateTime endDate)
		{
			while (strategyQueue.TryDequeue(out BaseStrategy strategy))
			{
				var stockMarket = new StockMarket(stocks);
				stockMarket.Initialize(Constants.InitialCash, Constants.OrderFees, Constants.CapitalGainsTax, Constants.InitialMargin, Constants.MaintenanceMargin, Constants.StockLendingFee, Constants.Spread, startDate);
				while (stockMarket.Date < endDate)
				{
					strategy.Trade(stockMarket);
					stockMarket.NextDay();
				}
				stockMarket.CashOut();
				strategy.Cash = stockMarket.Cash;
				strategy.MarginCallCount = stockMarket.MarginCallCount;
			}
		}

		private static DateRange GetDateRange(List<Stock> stocks)
		{
			var dateRange = new DateRange();
			foreach (var stock in stocks)
				stock.UpdateDateRange(dateRange);
			return dateRange;
		}

		private static void PrintStrategyStats(DateTime startDate, DateTime endDate, Stock referenceIndex, List<BaseStrategy> strategies, List<StrategyClass> strategyClasses)
		{
			Output.WriteLine($"Strategies sorted by performance, in comparison to index ETF (from {startDate.Year} to {endDate.Year}):", ConsoleColor.White);
			decimal referenceCash = GetReferenceCash(startDate, endDate, referenceIndex);

			if (PrintIndividualStrategyPerformance)
			{
				strategies.Sort((x, y) => -x.Cash.Value.CompareTo(y.Cash.Value));
				foreach (var strategy in strategies)
				{
					decimal performance = StockMarket.GetPerformance(strategy.Cash.Value, referenceCash);
					Output.Write($"  {strategy.Name}: {strategy.Cash:C2}");
					if (strategy.MarginCallCount > 0)
					{
						Output.Write(" (");
						Output.Write(strategy.MarginCallCount.ToString(), ConsoleColor.Red);
						Output.Write(" margin call(s))");
					}
					Output.WritePerformance(strategy.Cash.Value, referenceCash);
				}
			}

			foreach (var strategyClass in strategyClasses)
			{
				Output.NewLine();
				Output.WriteLine($"  {strategyClass.Name}:");
				foreach (var parameterPair in strategyClass)
				{
					Output.Write($"    {parameterPair.Parameter}: {parameterPair.Cash:C2}");
					Output.WritePerformance(parameterPair.Cash, referenceCash);
				}
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
			decimal performance = endPrice / startPrice;
			return performance;
		}

		private static List<BaseStrategy> GetStrategies(out List<StrategyClass> strategyClasses)
		{
			var strategies = new List<BaseStrategy>();
			var strategyClass = new StrategyClass("Strategy");
			var longMomentumStopLossThresholdClass = new StrategyClass("Long momentum, stop-loss threshold");
			var stocksClass = new StrategyClass("Long momentum, stocks");
			var longMomentumHoldDaysClass = new StrategyClass("Long momentum, hold days");
			var longMomentumIgnoreDays = new StrategyClass("Long momentum, ignore days");
			strategyClasses = new List<StrategyClass>
			{
				// strategyClass,
				longMomentumStopLossThresholdClass,
				stocksClass,
				longMomentumHoldDaysClass,
				longMomentumIgnoreDays
			};
			for (int stocks = 4; stocks <= 8; stocks += 2)
			{
				for (decimal stopLossThreshold = 0.06m; stopLossThreshold <= 0.12m; stopLossThreshold += 0.02m)
				{
					for (int holdDays = 15; holdDays <= 60; holdDays *= 2)
					{
						const int historyDays = 360;
						for (int ignoreDays = 0; ignoreDays <= 60; ignoreDays += 30)
						{
							/*
							var longShortStrategy = new LongShortMomentumStrategy(stocks, stopLossThreshold, holdDays, historyDays, ignoreDays, LongShortMode.LongShort);
							strategies.Add(longShortStrategy);
							strategyClass.Add("Long-short momentum", longShortStrategy);
							*/

							var longStrategy = new LongShortMomentumStrategy(stocks, stopLossThreshold, holdDays, historyDays, ignoreDays, LongShortMode.LongOnly);
							strategies.Add(longStrategy);
							strategyClass.Add("Long momentum", longStrategy);
							longMomentumStopLossThresholdClass.Add($"{stopLossThreshold:P0}", longStrategy);
							stocksClass.Add($"{stocks} stocks", longStrategy);
							longMomentumHoldDaysClass.Add($"{holdDays} days", longStrategy);
							longMomentumIgnoreDays.Add($"{ignoreDays} days", longStrategy);

							/*
							var shortStrategy = new LongShortMomentumStrategy(stocks, stopLossThreshold, holdDays, historyDays, ignoreDays, LongShortMode.ShortOnly);
							strategies.Add(shortStrategy);
							strategyClass.Add("Short momentum", shortStrategy);
							*/
						}
					}
				}
			}
			return strategies;
		}

		private static List<BaseStrategy> GetSpecificStrategy(out List<StrategyClass> strategyClasses)
		{
			strategyClasses = new List<StrategyClass>();
			var output = new List<BaseStrategy>
			{
				new LongShortMomentumStrategy(4, 0.10m, 60, 360, 30, LongShortMode.LongOnly)
			};
			return output;
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
