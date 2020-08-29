using CrashAndBurn.Strategy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
                Console.WriteLine($"{name.Name} <path to .csv file containing Yahoo! Finance dump>");
                return;
            }
            string csvPath = arguments[0];
            var history = ReadStockHistory(csvPath);
            RunTest(history);
            for (int year = 1970; year < DateTime.Now.Year; year += 10)
            {
                RunTest(history, year);
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

        private static void RunTest(List<StockData> history, int? year = null)
        {
            const decimal initialCash = 100000.0m;
            const decimal orderFees = 10.0m;
            var strategies = new List<BaseStrategy>
            {
                new BuyAndHoldStrategy()
            };
            for (decimal trailingStopPercentage = 0.04m; trailingStopPercentage <= 0.14m; trailingStopPercentage += 0.02m)
            {
                const int daysPerWeek = 7;
                for (int recoveryDays = 2 * daysPerWeek; recoveryDays <= 12 * daysPerWeek; recoveryDays += 2 * daysPerWeek)
                {
                    var strategy = new TrailingStopStrategy(trailingStopPercentage, recoveryDays);
                    strategies.Add(strategy);
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
            Console.WriteLine($"Strategies sorted by returns, starting with {initialCash:C0} on {adjustedHistory.First().Date.ToShortDateString()}:");
            foreach (var strategy in strategies.Take(10))
            {
                Console.WriteLine($"  {strategy.Name}: {strategy.Cash:C2}");
            }
        }
    }
}
