using CrashAndBurn.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CrashAndBurn.Momentum.Strategy
{
	class LongShortMomentumStrategy : BaseStrategy
	{
		private const decimal MinFundsPerPosition = 1000.0m;

		private int _stocks;
		private decimal _stopLossThreshold;
		private int _holdDays;
		private int _historyDays;
		private int _ignoreDays;

		private DateTime? lastReevaluation = null;

		public LongShortMomentumStrategy(int stocks, decimal stopLossThreshold, int holdDays, int historyDays, int ignoreDays)
			: base($"Long-short momentum ({stocks} stocks, {stopLossThreshold:P0} stop-loss threshold, hold for {holdDays} days, {historyDays} days of history, ignore past {ignoreDays} days)")
		{
			_stocks = stocks;
			_stopLossThreshold = stopLossThreshold;
			_holdDays = holdDays;
			_historyDays = historyDays;
			_ignoreDays = ignoreDays;
		}

		public override void Trade(StockMarket stockMarket)
		{
			StopLossCheck(stockMarket);
			if
			(
				lastReevaluation == null ||
				stockMarket.Date - lastReevaluation.Value >= TimeSpan.FromDays(_holdDays)
			)
			{
				Reevaluate(stockMarket);
				lastReevaluation = stockMarket.Date;
			}
		}

		private void StopLossCheck(StockMarket stockMarket)
		{
			var positions = stockMarket.Positions.ToList();
			foreach (var position in positions)
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
				if (performance <= -_stopLossThreshold)
				{
					stockMarket.Liquidate(position);
				}
			}
		}

		private void Reevaluate(StockMarket stockMarket)
		{
			int stocksToAcquire = 2 * _stocks - stockMarket.Positions.Count;
			if (stocksToAcquire == 0)
			{
				return;
			}
			var ratedStocks = GetStocksByRating(stockMarket);
			if (ratedStocks.Count < 4 * _stocks)
			{
				return;
			}
			decimal availableFunds = stockMarket.GetAvailableFunds();
			decimal fundsPerPosition = (availableFunds - stocksToAcquire * Constants.OrderFees) / stocksToAcquire;
			fundsPerPosition = Math.Max(fundsPerPosition, MinFundsPerPosition);
			for (; stocksToAcquire > 0 && stockMarket.HasEnoughFunds(MinFundsPerPosition); stocksToAcquire--)
			{
				int longPositions = stockMarket.Positions.Count(p => !p.IsShort);
				bool goShort = longPositions >= _stocks;
				var stock = GetAndRemoveStock(goShort, ratedStocks);
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
			DateTime from = stockMarket.Date - TimeSpan.FromDays(_historyDays);
			DateTime to = stockMarket.Date - TimeSpan.FromDays(_ignoreDays);
			var filteredStocks = stockMarket.Stocks.Where(s => s.MaybeGetPrice(from).HasValue && s.MaybeGetPrice(to).HasValue && !stockMarket.Positions.Any(p => p.Stock == s));
			var orderedStocks = filteredStocks.OrderBy(s => s.GetPrice(to) / s.GetPrice(from));
			var stocks = new LinkedList<Stock>(orderedStocks);
			return stocks;
		}
	}
}
