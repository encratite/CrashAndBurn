using CrashAndBurn.Strategy;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace CrashAndBurn
{
    class Program
    {
        static void Main(string[] arguments)
        {
            if (arguments.Length != 1)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var name = assembly.GetName();
                WriteLine($"{name.Name} <path to .csv file containing Yahoo! Finance dump>");
                return;
            }
            string csvPath = arguments[0];
            var history = ReadStockHistory(csvPath);
            EvaluateStrategies(history);
            for (int year = 1970; year < DateTime.Now.Year; year += 10)
            {
                EvaluateStrategies(history, year);
            }
        }

        private static List<StockData> ReadStockHistory(string csvPath)
        {
            var lines = File.ReadAllLines(csvPath);
            var pattern = new Regex(@"^(?<year>\d+)-(?<month>\d+)-(?<day>\d+),(?<open>\d+\.\d+),(?<close>\d+\.\d+),(?<high>\d+\.\d+),(?<low>\d+\.\d+),(?<adjustedClose>\d+\.\d+),(?<volume>\d+)");
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
                    decimal close = parseDecimal("close");
                    decimal high = parseDecimal("high");
                    decimal low = parseDecimal("low");
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

        private static void EvaluateStrategies(List<StockData> history, int? year = null)
        {
            const decimal initialCash = 100000.0m;
            const decimal orderFees = 10.0m;
            var referenceStrategy = new BuyAndHoldStrategy();
            var strategies = new List<BaseStrategy>
            {
                referenceStrategy
            };
            for (decimal stopLossPercentage = 0.04m; stopLossPercentage <= 0.14m; stopLossPercentage += 0.02m)
            {
                const int daysPerWeek = 7;
                for (int recoveryDays = daysPerWeek; recoveryDays <= 32 * daysPerWeek; recoveryDays *= 2)
                {
                    var stopLossStrategy = new StopLossStrategy(stopLossPercentage, recoveryDays);
                    strategies.Add(stopLossStrategy);
                    var strategy = new TrailingStopStrategy(stopLossPercentage, recoveryDays);
                    strategies.Add(strategy);
                    var mondayStrategy = new TrailingStopMondayStrategy(stopLossPercentage, recoveryDays);
                    strategies.Add(mondayStrategy);
                }
            }
            var adjustedHistory = history;
            if (year.HasValue)
            {
                adjustedHistory = history.Where(stockData => stockData.Date.Year >= year.Value).ToList();
            }
            foreach (var strategy in strategies)
            {
                strategy.Initialize(initialCash, orderFees);
                strategy.Buy(adjustedHistory.First());
                foreach (var stockData in adjustedHistory.Skip(1))
                {
                    strategy.ProcessStockData(stockData);
                }
                strategy.Sell(adjustedHistory.Last(), false);
            }
            strategies.Sort((x, y) => y.Cash.CompareTo(x.Cash));
            WriteLine($"Strategies sorted by returns, starting with {initialCash:C0} on {adjustedHistory.First().Date.ToShortDateString()}:");
            var bestStrategies = strategies.Take(10).ToList();
            if (!bestStrategies.Contains(referenceStrategy))
            {
                bestStrategies.Remove(bestStrategies.Last());
                bestStrategies.Add(referenceStrategy);
            }
            foreach (var strategy in bestStrategies)
            {
                decimal performance = strategy.Cash / referenceStrategy.Cash - 1.0m;
                if (object.ReferenceEquals(strategy, referenceStrategy))
                {
                    WriteLine($"  {strategy.Name}: {strategy.Cash:C2} (reference strategy)", ConsoleColor.White);
                }
                else
                {
                    Write($"  {strategy.Name}: {strategy.Cash:C2} (");
                    var performanceColor = performance >= 0.0m ? ConsoleColor.Green : ConsoleColor.Red;
                    Write($"{performance:+0.##%;-0.##%;0%}", performanceColor);
                    WriteLine(")");
                }
            }
            WriteLine(string.Empty);
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
