using System;

namespace CrashAndBurn.Common
{
	public static class Output
	{
		public static void WriteLine(string text, ConsoleColor? color = null)
		{
			WithColor(color, () =>
			{
				Console.WriteLine(text);
			});
		}

		public static void NewLine()
		{
			WriteLine(string.Empty);
		}

		public static void Write(string text, ConsoleColor? color = null)
		{
			WithColor(color, () =>
			{
				Console.Write(text);
			});
		}

		public static void WritePerformance(decimal cash, decimal referenceCash)
		{
			decimal performance = StockMarket.GetPerformance(cash, referenceCash);
			var performanceColor = performance >= 0.0m ? ConsoleColor.Green : ConsoleColor.Red;
			Write(" (");
			Write($"{performance:+0.##%;-0.##%;0%}", performanceColor);
			WriteLine(")");
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
	}
}
