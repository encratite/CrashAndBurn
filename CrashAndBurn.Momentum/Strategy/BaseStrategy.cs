using CrashAndBurn.Common;

namespace CrashAndBurn.Momentum.Strategy
{
	abstract class BaseStrategy
	{
		public string Name { get; private set; }

		public decimal? Cash { get; set; }
		public int? MarginCallCount { get; set; }

		protected BaseStrategy(string name)
		{
			Name = name;
		}

		public abstract void Trade(StockMarket stockMarket);
	}
}
