using CrashAndBurn.Common;
using CrashAndBurn.StopLoss.Strategy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CrashAndBurn.StopLoss
{
	class Program
	{
		static void Main(string[] arguments)
		{
			if (arguments.Length != 1)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Output.WriteLine($"{name.Name} <path to .csv file containing Yahoo Finance dump>");
				return;
			}
			string csvPath = arguments[0];
			var history = StockData.FromFile(csvPath);
			int firstYear = history.First().Date.Year;
			const int windowSize = 20;
			for (int year = firstYear; year <= DateTime.Now.Year - windowSize; year++)
			{
				EvaluateStrategies(history, year, year + windowSize);
			}
			for (int year = firstYear; year <= DateTime.Now.Year - 5; year++)
			{
				EvaluateStrategies(history, year);
			}
		}

		private static void EvaluateStrategies(List<StockData> history, int? firstYear = null, int? lastYear = null)
		{
			var referenceStrategy = new BuyAndHoldStrategy();
			var strategies = GetStrategies(referenceStrategy);
			var adjustedHistory = history.Where(stockData => YearMatch(stockData, firstYear, lastYear)).ToList();
			if (adjustedHistory.Count < 1000)
			{
				return;
			}
			var strategyStats = new List<StrategyStats>();
			RunStrategies(referenceStrategy, strategies, adjustedHistory, strategyStats);
			PrintStrategies(strategies, referenceStrategy, adjustedHistory);
			PrintStrategyStats(strategyStats, referenceStrategy);
		}

		private static IEnumerable<BaseStrategy> GetCustomStrategies(List<BaseStrategy> strategies, BaseStrategy referenceStrategy)
		{
			return strategies.Where(s => !ReferenceEquals(s, referenceStrategy));
		}

		private static bool YearMatch(StockData stockData, int? firstYear, int? lastYear)
		{
			return
				(!firstYear.HasValue || stockData.Date.Year >= firstYear.Value) &&
				(!lastYear.HasValue || stockData.Date.Year < lastYear.Value);
		}

		private static void RunStrategies(BuyAndHoldStrategy referenceStrategy, List<BaseStrategy> strategies, List<StockData> adjustedHistory, List<StrategyStats> strategyStats)
		{
			foreach (var strategy in strategies)
			{
				strategy.Initialize(Constants.InitialCash, Constants.OrderFees, Constants.CapitalGainsTax);
				strategy.Buy(adjustedHistory.First());
				foreach (var stockData in adjustedHistory.Skip(1))
				{
					strategy.ProcessStockData(stockData);
				}
				strategy.Sell(adjustedHistory.Last(), false);
				string strategyName = strategy.StrategyName;
				if (!ReferenceEquals(strategy, referenceStrategy))
				{
					var stats = strategyStats.FirstOrDefault(s => s.Name == strategyName);
					if (stats == null)
					{
						stats = new StrategyStats(strategyName);
						strategyStats.Add(stats);
					}
					stats.Add(strategy.Cash);
				}
			}
		}

		private static void PrintStrategyStats(List<StrategyStats> strategyStats, BuyAndHoldStrategy referenceStrategy)
		{
			strategyStats.Sort((x, y) => y.Cash.CompareTo(x.Cash));
			foreach (var stats in strategyStats)
			{
				Output.Write($"  {stats.Name}: {stats.Cash:C2}");
				WritePerformance(stats.Cash, referenceStrategy.Cash);
			}
			Output.WriteLine(string.Empty);
		}

		private static void PrintStrategies(List<BaseStrategy> strategies, BuyAndHoldStrategy referenceStrategy, List<StockData> adjustedHistory)
		{
			strategies.Sort((x, y) => y.Cash.CompareTo(x.Cash));
			Output.WriteLine($"Strategies sorted by returns, starting with {Constants.InitialCash:C0} ({adjustedHistory.First().Date.Year} - {adjustedHistory.Last().Date.Year}):");
			var bestStrategies = strategies.Take(10).ToList();
			if (!bestStrategies.Contains(referenceStrategy))
			{
				bestStrategies.Remove(bestStrategies.Last());
				bestStrategies.Add(referenceStrategy);
			}
			foreach (var strategy in bestStrategies)
			{
				if (ReferenceEquals(strategy, referenceStrategy))
				{
					Output.WriteLine($"  {strategy.Name}: {strategy.Cash:C2} (reference strategy)", ConsoleColor.White);
				}
				else
				{
					Output.Write($"  {strategy.Name}: {strategy.Cash:C2}");
					WritePerformance(strategy.Cash, referenceStrategy.Cash);
				}
			}
			Output.WriteLine(string.Empty);
		}

		private static List<BaseStrategy> GetStrategies(BuyAndHoldStrategy referenceStrategy)
		{
			var strategies = new List<BaseStrategy>
			{
				referenceStrategy
			};
			Action<BaseStrategy> addStrategy = strategy => strategies.Add(strategy);
			for (decimal stopLossPercentage = 0.12m; stopLossPercentage <= 0.15m; stopLossPercentage += 0.03m)
			{
				for (int recoveryDays = 20; recoveryDays <= 60; recoveryDays += 10)
				{
					addStrategy(new TrailingStopStrategy(stopLossPercentage, recoveryDays));
				}
				/*
                for (decimal volatilityPercentage = 0.08m; volatilityPercentage <= 0.12m; volatilityPercentage += 0.02m)
                {
                    addStrategy(new TrailingStopVolatilityStrategy(stopLossPercentage, volatilityPercentage));
                }
                for (decimal rallyPercentage = 0.06m; rallyPercentage <= 0.1m; rallyPercentage += 0.01m)
                {
                    addStrategy(new TrailingStopRallyStrategy(stopLossPercentage, rallyPercentage));
                }
                */
			}

			return strategies;
		}

		private static void WritePerformance(decimal cash, decimal referenceCash)
		{
			decimal performance = GetPerformance(cash, referenceCash);
			var performanceColor = performance >= 0.0m ? ConsoleColor.Green : ConsoleColor.Red;
			Output.Write(" (");
			Output.Write($"{performance:+0.##%;-0.##%;0%}", performanceColor);
			Output.WriteLine(")");
		}

		private static decimal GetPerformance(decimal cash, decimal referenceCash)
		{
			return cash / referenceCash - 1.0m;
		}
	}
}
