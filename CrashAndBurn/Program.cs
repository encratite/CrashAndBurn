using CrashAndBurn.Strategy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CrashAndBurn
{
    class Program
    {
        private const decimal _InitialCash = 100000.0m;
        private const decimal _OrderFees = 10.0m;
        private const decimal _CapitalGainsTax = 0.25m;

        static void Main(string[] arguments)
        {
            if (arguments.Length != 1)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var name = assembly.GetName();
                WriteLine($"{name.Name} <path to .csv file containing Yahoo Finance dump>");
                return;
            }
            string csvPath = arguments[0];
            var history = ReadStockHistory(csvPath);
            int firstYear = history.First().Date.Year;
            const int windowSize = 20;
            for (int year = firstYear; year <= DateTime.Now.Year - windowSize; year++)
            {
                EvaluateStrategies(history, year, year + windowSize);
            }
            for (int year = firstYear; year <= DateTime.Now.Year - 10; year++)
            {
                EvaluateStrategies(history, year);
            }
        }

        private static List<StockData> ReadStockHistory(string csvPath)
        {
            var lines = File.ReadAllLines(csvPath);
            var pattern = new Regex(@"^(?<year>\d+)-(?<month>\d+)-(?<day>\d+),(?<open>\d+\.\d+),(?<high>\d+\.\d+),(?<low>\d+\.\d+),(?<close>\d+\.\d+),(?<adjustedClose>\d+\.\d+),(?<volume>\d+)");
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
            RunStrategies(referenceStrategy, strategies, adjustedHistory, strategyStats, firstYear);
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

        private static void RunStrategies(BuyAndHoldStrategy referenceStrategy, List<BaseStrategy> strategies, List<StockData> adjustedHistory, List<StrategyStats> strategyStats, int? firstYear)
        {
            foreach (var strategy in strategies)
            {
                strategy.Initialize(_InitialCash, _OrderFees, _CapitalGainsTax);
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
                Write($"  {stats.Name}: {stats.Cash:C2}");
                WritePerformance(stats.Cash, referenceStrategy.Cash);
            }
            WriteLine(string.Empty);
        }

        private static void PrintStrategies(List<BaseStrategy> strategies, BuyAndHoldStrategy referenceStrategy, List<StockData> adjustedHistory)
        {
            strategies.Sort((x, y) => y.Cash.CompareTo(x.Cash));
            WriteLine($"Strategies sorted by returns, starting with {_InitialCash:C0} ({adjustedHistory.First().Date.Year} - {adjustedHistory.Last().Date.Year}):");
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
                    WriteLine($"  {strategy.Name}: {strategy.Cash:C2} (reference strategy)", ConsoleColor.White);
                }
                else
                {
                    Write($"  {strategy.Name}: {strategy.Cash:C2}");
                    WritePerformance(strategy.Cash, referenceStrategy.Cash);
                }
            }
            WriteLine(string.Empty);
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
            Write(" (");
            Write($"{performance:+0.##%;-0.##%;0%}", performanceColor);
            WriteLine(")");
        }

        private static decimal GetPerformance(decimal cash, decimal referenceCash)
        {
            return cash / referenceCash - 1.0m;
        }

        private static void WithColor(ConsoleColor? color, Action action)
        {
            if (color.HasValue)
            {
                var originalForegroundColor = Console.ForegroundColor;
                Console.ForegroundColor = color.Value;
                try
                {
                    action();
                }
                finally
                {
                    Console.ForegroundColor = originalForegroundColor;
                }
            }
            else
            {
                action();
            }
        }

        private static void WriteLine(string text, ConsoleColor? color = null)
        {
            WithColor(color, () =>
            {
                Console.WriteLine(text);
            });
        }

        private static void Write(string text, ConsoleColor? color = null)
        {
            WithColor(color, () =>
            {
                Console.Write(text);
            });
        }
    }
}
