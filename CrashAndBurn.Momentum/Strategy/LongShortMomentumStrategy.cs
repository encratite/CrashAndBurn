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
			StopLossCheck(stockMarket);
			if
			(
				_LastReevaluation == null ||
				stockMarket.Date - _LastReevaluation.Value >= TimeSpan.FromDays(_HoldDays)
			)
			{
				Reevaluate(stockMarket);
				_LastReevaluation = stockMarket.Date;
			}
		}

		private void StopLossCheck(StockMarket stockMarket)
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
		}

		private void Reevaluate(StockMarket stockMarket)
		{
			int stocksToAcquire = 2 * _Stocks - stockMarket.Positions.Count;
			if (stocksToAcquire == 0)
			{
				return;
			}
			var stocks = GetStocksByRating(stockMarket);
			decimal fundsPerPosition = (stockMarket.GetAvailableFunds() - stocksToAcquire * Constants.OrderFees) / stocksToAcquire;
			fundsPerPosition = Math.Max(fundsPerPosition, _MinFundsPerPosition);
			for (; stocksToAcquire > 0 && stockMarket.HasEnoughFunds(_MinFundsPerPosition); stocksToAcquire--)
			{
				int longPositions = stockMarket.Positions.Count(p => !p.IsShort);
				bool goShort = longPositions >= _Stocks;
				var stock = GetAndRemoveStock(goShort, stocks);
				decimal currentPrice = stock.GetPrice(stockMarket.Date);
				int shares = (int)Math.Floor(fundsPerPosition / currentPrice);
				if (shares > 0)
				{
					if (goShort)
					{
						stockMarket.Short(stock, shares);
					}
					else
					{
						stockMarket.Buy(stock, shares);
					}
				}
			}
		}

		private Stock GetAndRemoveStock(bool goShort, LinkedList<Stock> stocks)
		{
			Stock stock;
			if (goShort)
			{
				stock = stocks.First();
				stocks.RemoveFirst();
			}
			else
			{
				stock = stocks.Last();
				stocks.RemoveLast();
			}
			return stock;
		}

		private LinkedList<Stock> GetStocksByRating(StockMarket stockMarket)
		{
			DateTime from = stockMarket.Date - TimeSpan.FromDays(_HistoryDays);
			DateTime to = stockMarket.Date - TimeSpan.FromDays(_IgnoreDays);
			var filteredStocks = stockMarket.Stocks.Where(s => s.MaybeGetPrice(from).HasValue && !stockMarket.Positions.Any(p => p.Stock == s));
			var orderedStocks = filteredStocks.OrderBy(s => s.GetPrice(to) / s.GetPrice(from));
			var stocks = new LinkedList<Stock>(orderedStocks);
			return stocks;
		}
	}
}
