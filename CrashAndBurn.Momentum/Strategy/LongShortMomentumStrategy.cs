using CrashAndBurn.Common;
using System;

namespace CrashAndBurn.Momentum.Strategy
{
	class LongShortMomentumStrategy : BaseStrategy
	{
		private int _Stocks;
		private decimal _StopLossThreshold;
		private int _HoldDays;
		private int _IgnoreDays;

		private DateTime? _LastReevaluation = null;

		public LongShortMomentumStrategy(int stocks, decimal stopLossThreshold, int holdDays, int ignoreDays)
			: base($"Long-short momentum ({stocks} stocks, {stopLossThreshold:P0} stop-loss threshold, hold for {holdDays} days, ignore past {ignoreDays} days)")
		{
			_Stocks = stocks;
			_StopLossThreshold = stopLossThreshold;
			_HoldDays = holdDays;
			_IgnoreDays = ignoreDays;
		}

		public override void Trade(StockMarket stockMarket)
		{
			foreach (var position in stockMarket.Positions)
			{
			}
			if
			(
				_LastReevaluation == null ||
				stockMarket.Date - _LastReevaluation.Value >= TimeSpan.FromDays(_HoldDays))
			{
			}
			throw new NotImplementedException();
		}
	}
}
