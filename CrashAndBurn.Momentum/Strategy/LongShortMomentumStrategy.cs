using CrashAndBurn.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CrashAndBurn.Momentum.Strategy
{
	class LongShortMomentumStrategy : BaseStrategy
	{
		private const decimal _MinFundsPerPosition = 1000.0m;

		private int _Stocks;
		private decimal _StopLossThreshold;
		private int _HoldDays;
		private int _HistoryDays;
		private int _IgnoreDays;

		private DateTime? _LastReevaluation = null;

		public LongShortMomentumStrategy(int stocks, decimal stopLossThreshold, int holdDays, int historyDays, int ignoreDays)
			: base($"Long-short momentum ({stocks} stocks, {stopLossThreshold:P0} stop-loss threshold, hold for {holdDays} days, {historyDays} days of history, ignore past {ignoreDays} days)")
		{
			_Stocks = stocks;
			_StopLossThreshold = stopLossThreshold;
			_HoldDays = holdDays;
			_HistoryDays = historyDays;
			_IgnoreDays = ignoreDays;
		}

		public override void Trade(StockMarket stockMarket)
		{
			foreach (var position in stockMarket.Positions)
			{
				decimal currentPrice = position.Stock.GetPrice(stockMarket.Date);
				decimal performance;
				if (position.IsShort)
				{
					performance = StockMarket.GetPerformance(position.OriginalPrice, currentPrice);
				}
				else
				{
					performance = StockMarket.GetPerformance(currentPrice, position.OriginalPrice);
				}
				if (performance <= -_StopLossThreshold)
				{
					stockMarket.Liquidate(position);
				}
			}
			if
			(
				_LastReevaluation == null ||
				stockMarket.Date - _LastReevaluation.Value >= TimeSpan.FromDays(_HoldDays)
			)
			{
				int stocksToAcquire = 2 * _Stocks - stockMarket.Positions.Count;
				if (stocksToAcquire > 0)
				{
					var stocks = GetStocksByRating(stockMarket);
					decimal fundsPerPosition = Math.Max((stockMarket.GetAvailableFunds() - stocksToAcquire * Constants.OrderFees) / stocksToAcquire, _MinFundsPerPosition);
					for (;  stocksToAcquire > 0 && stockMarket.HasEnoughFunds(_MinFundsPerPosition); stocksToAcquire--)
					{
						int longPositions = stockMarket.Positions.Count(p => !p.IsShort);
					}
				}
				_LastReevaluation = stockMarket.Date;
			}
			throw new NotImplementedException();
		}

		private List<Stock> GetStocksByRating(StockMarket stockMarket)
		{
			DateTime from = stockMarket.Date - TimeSpan.FromDays(_HistoryDays);
			DateTime to = stockMarket.Date - TimeSpan.FromDays(_IgnoreDays);
			var stocks = stockMarket.Stocks.Where(s => s.MaybeGetPrice(from).HasValue && !stockMarket.Positions.Any(p => p.Stock == s)).ToList();
			Func<Stock, decimal> getPerformance = s => s.GetPrice(to) / s.GetPrice(from);
			stocks.Sort((x, y) => getPerformance(x).CompareTo(getPerformance(y)));
			return stocks;
		}
	}
}
