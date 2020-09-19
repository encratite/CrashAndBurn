using AngleSharp;
using CrashAndBurn.Common;
using System;
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
			DownloadHistory(outputDirectory);
		}

		private static void DownloadHistory(string outputDirectory)
		{
			string symbolUrl = "https://en.wikipedia.org/wiki/List_of_S%26P_500_companies";
			using (var webClient = new WebClient())
			{
				string html = webClient.DownloadString(symbolUrl);
				var context = BrowsingContext.New(Configuration.Default);
				var document = context.OpenAsync(request => request.Content(html)).Result;
				var nodes = document.QuerySelectorAll("#constituents td:first-child a.external");
				var symbolPattern = new Regex("^[A-Z]+$");
				int counter = 1;
				int symbolCount = nodes.Count();
				foreach (var node in nodes)
				{
					string symbol = node.TextContent;
					string outputPath = Path.Combine(outputDirectory, $"{symbol}.csv");
					try
					{
						if (!symbolPattern.IsMatch(symbol))
						{
							continue;
						}
						string yahooUrl = $"https://query1.finance.yahoo.com/v7/finance/download/{symbol}?period1=0&period2=2000000000&interval=1d&events=history";
						webClient.DownloadFile(yahooUrl, outputPath);
						Output.WriteLine($"Downloaded {outputPath} ({counter}/{symbolCount})");
					}
					catch (Exception exception)
					{
						Output.Write($"Failed to download {outputPath} ({counter}/{symbolCount}): ");
						Output.WriteLine(exception.Message, ConsoleColor.Red);
					}
					counter++;
				}
			}
		}
	}
}
