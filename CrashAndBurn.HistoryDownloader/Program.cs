using AngleSharp;
using CrashAndBurn.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CrashAndBurn.HistoryDownloader
{
	class Program
	{
		static void Main(string[] arguments)
		{
			if (arguments.Length != 1)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Output.WriteLine($"{name.Name} <output directory>");
				return;
			}
			string outputDirectory = arguments[0];
			DownloadStockData(outputDirectory);
		}

		private static void DownloadStockData(string outputDirectory)
		{
			string symbolUrl = "https://en.wikipedia.org/wiki/List_of_S%26P_500_companies";
			using (var webClient = new WebClient())
			{
				string html = webClient.DownloadString(symbolUrl);
				using (var browsingContext = BrowsingContext.New(Configuration.Default))
				{
					var document = browsingContext.OpenAsync(request => request.Content(html)).Result;
					var nodes = document.QuerySelectorAll("#constituents td:first-child a.external");
					var symbolPattern = new Regex("^[A-Z]+$");
					int counter = 1;
					int symbolCount = nodes.Count();
					foreach (var node in nodes)
					{
						string symbol = node.TextContent;
						if (!symbolPattern.IsMatch(symbol))
							continue;
						try
						{
							DownloadHistory(symbol, counter, symbolCount, outputDirectory, webClient);
							DownloadDividends(symbol, counter, symbolCount, outputDirectory, browsingContext, webClient);
						}
						catch (Exception exception)
						{
							Output.Write($"Failed to download {symbol} ({counter}/{symbolCount}): ");
							Output.WriteLine(exception.Message, ConsoleColor.Red);
						}
						counter++;
					}
				}
			}
		}

		private static void DownloadHistory(string symbol, int counter, int symbolCount, string outputDirectory, WebClient webClient)
		{
			string yahooUrl = $"https://query1.finance.yahoo.com/v7/finance/download/{symbol}?period1=0&period2=2000000000&interval=1d&events=history";
			string outputPath = Path.Combine(outputDirectory, $"{symbol}.csv");
			webClient.DownloadFile(yahooUrl, outputPath);
			Output.WriteLine($"Downloaded {outputPath} ({counter}/{symbolCount})");
		}

		private static void DownloadDividends(string symbol, int counter, int symbolCount, string outputDirectory, IBrowsingContext browsingContext, WebClient webClient)
		{
			string url = $"https://www.nasdaq.com/market-activity/stocks/{symbol.ToLower()}/dividend-history";
			string html = webClient.DownloadString(url);
			var document = browsingContext.OpenAsync(request => request.Content(html)).Result;
			var nodes = document.QuerySelectorAll(".dividend-history__row--data");
			var amountPattern = new Regex(@"\d+\.\d+");
			var dividends = new List<DividendData>();
			foreach (var node in nodes)
			{
				var amountNode = node.QuerySelector(".dividend-history__cell--amount");
				if (amountNode == null)
					continue;
				var recordDateNode = node.QuerySelector(".dividend-history__cell--recordDate");
				if (recordDateNode == null)
					continue;
				var match = amountPattern.Match(amountNode.TextContent);
				if (!match.Success)
					continue;
				decimal amount = decimal.Parse(match.ToString());
				if (!DateTime.TryParse(recordDateNode.TextContent, out DateTime recordDate))
					continue;
				var dividendData = new DividendData(recordDate, amount);
				dividends.Add(dividendData);
			}
			string outputPath = Path.Combine(outputDirectory, $"{symbol}.json");
			DividendData.Write(outputPath, dividends);
			Output.WriteLine($"Downloaded {outputPath} ({counter}/{symbolCount})");

		}
	}
}
