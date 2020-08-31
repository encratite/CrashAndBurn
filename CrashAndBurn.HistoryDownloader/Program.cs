using CrashAndBurn.Common;
using HtmlAgilityPack;
using ScrapySharp.Extensions;
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
                var document = new HtmlDocument();
                document.LoadHtml(html);
                var nodes = document.DocumentNode.CssSelect("#constituents td:first-child a.external");
                var symbolPattern = new Regex("^[A-Z]+$");
                int counter = 1;
                int symbolCount = nodes.Count();
                foreach (var node in nodes)
                {
                    string symbol = node.InnerText;
                    if (!symbolPattern.IsMatch(symbol))
                    {
                        continue;
                    }
                    string yahooUrl = $"https://query1.finance.yahoo.com/v7/finance/download/{symbol}?period1=0&period2=2000000000&interval=1d&events=history";
                    string outputPath = Path.Combine(outputDirectory, $"{symbol}.csv");
                    webClient.DownloadFile(yahooUrl, outputPath);
                    Output.Write($"Downloaded {outputPath} ({counter}/{symbolCount})");
                    counter++;
                }
            }
        }
    }
}
