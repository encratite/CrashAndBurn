namespace CrashAndBurn.Momentum.Strategy
{
	class LongMomentumStrategy : LongShortMomentumStrategy
	{
		public LongMomentumStrategy(int stocks, decimal stopLossThreshold, int holdDays, int historyDays, int ignoreDays)
			: base(stocks, stopLossThreshold, holdDays, historyDays, ignoreDays, false)
		{
		}
	}
}
