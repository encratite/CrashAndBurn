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
					var nodes = document.QuerySelectorAll("#constituents tr");
					var symbolPattern = new Regex("^[A-Z]+$");
					int counter = 1;
					int symbolCount = nodes.Count();
					foreach (var node in nodes)
					{
						var symbolNode = node.QuerySelector("td:first-child a.external");
						var dateFirstAddedNode = node.QuerySelector("td:nth-child(7)");
						if (symbolNode == null || dateFirstAddedNode == null)
							continue;
						string symbol = symbolNode.TextContent;
						string dateFirstAddedString = dateFirstAddedNode.TextContent;
						DateTime dateFirstAdded;
						if (!symbolPattern.IsMatch(symbol) || !DateTime.TryParse(dateFirstAddedString, out dateFirstAdded))
							continue;
						try
						{
							WriteHistory(symbol, counter, symbolCount, outputDirectory, webClient);
							WriteJsonData(symbol, counter, symbolCount, outputDirectory, dateFirstAdded, browsingContext, webClient);
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

		private static void WriteHistory(string symbol, int counter, int symbolCount, string outputDirectory, WebClient webClient)
		{
			string yahooUrl = $"https://query1.finance.yahoo.com/v7/finance/download/{symbol}?period1=0&period2=2000000000&interval=1d&events=history";
			string outputPath = Path.Combine(outputDirectory, $"{symbol}.csv");
			if (ShouldDownloadFile(outputPath))
			{
				webClient.DownloadFile(yahooUrl, outputPath);
				WriteDownloadMessage(counter, symbolCount, outputPath);
			}
			else
			{
				WriteSkipMessage(counter, symbolCount, outputPath);
			}
		}

		private static void WriteDownloadMessage(int counter, int symbolCount, string outputPath)
		{
			Output.WriteLine($"Downloaded {outputPath} ({counter}/{symbolCount})");
		}

		private static void WriteSkipMessage(int counter, int symbolCount, string outputPath)
		{
			Output.WriteLine($"Skipping {outputPath} ({counter}/{symbolCount})");
		}

		private static void WriteJsonData(string symbol, int counter, int symbolCount, string outputDirectory, DateTime dateFirstAdded, IBrowsingContext browsingContext, WebClient webClient)
		{
			string outputPath = Path.Combine(outputDirectory, $"{symbol}.json");
			if (ShouldDownloadFile(outputPath))
			{
				var dividends = new List<DividendData>();
				try
				{
					dividends = DownloadDividends(symbol, browsingContext, webClient);
				}
				catch
				{
				}
				var jsonData = new JsonData(dividends, dateFirstAdded);
				JsonData.Write(outputPath, jsonData);
				WriteDownloadMessage(counter, symbolCount, outputPath);
			}
			else
			{
				WriteSkipMessage(counter, symbolCount, outputPath);
			}
		}

		private static List<DividendData> DownloadDividends(string symbol, IBrowsingContext browsingContext, WebClient webClient)
		{
			string url = $"https://dividata.com/stock/{symbol}/dividend";
			string html = webClient.DownloadString(url);
			var document = browsingContext.OpenAsync(request => request.Content(html)).Result;
			var nodes = document.QuerySelectorAll("table.table tr");
			var amountPattern = new Regex(@"\d+\.\d+");
			var dividends = new List<DividendData>();
			foreach (var node in nodes)
			{
				var dateNode = node.QuerySelector("td.date");
				if (dateNode == null)
					continue;
				if (!DateTime.TryParse(dateNode.TextContent, out DateTime date))
					continue;
				var amountNode = node.QuerySelector("td.money");
				if (amountNode == null)
					continue;
				var match = amountPattern.Match(amountNode.TextContent);
				if (!match.Success)
					continue;
				decimal amount = decimal.Parse(match.ToString());
				var dividendData = new DividendData(date, amount);
				dividends.Add(dividendData);
			}
			return dividends;
		}

		private static bool ShouldDownloadFile(string path)
		{
			return !File.Exists(path) || DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > TimeSpan.FromDays(1);
		}
	}
}
