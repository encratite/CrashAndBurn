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
			EvaluateStrategies(referenceIndex, stockMarket, null, null);
		}

		private static void EvaluateStrategies(Stock referenceIndex, StockMarket stockMarket, int? firstYear, int? lastYear)
		{
			var dateRange = stockMarket.GetDateRange();
			DateTime startDate = GetStartEndDate(firstYear, false, dateRange);
			DateTime endDate = GetStartEndDate(lastYear, true, dateRange);
			var strategies = new BaseStrategy[] { };
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
	}
}
