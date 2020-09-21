using CrashAndBurn.Common;

namespace CrashAndBurn.Momentum.Strategy
{
	abstract class BaseStrategy
	{
		public string Name { get; private set; }

		protected BaseStrategy(string name)
		{
			Name = name;
		}

		public abstract void Trade(StockMarket stockMarket);
	}
}
