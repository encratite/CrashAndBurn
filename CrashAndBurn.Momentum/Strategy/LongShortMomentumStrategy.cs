using CrashAndBurn.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CrashAndBurn.Momentum.Strategy
{
	enum LongShortMode
	{
		LongShort,
		ShortOnly,
		LongOnly
	}

	class LongShortMomentumStrategy : BaseStrategy
	{
		private const decimal MinFundsPerPosition = 1000.0m;

		private int _stocks;
		private decimal _stopLossThreshold;
		private int _holdDays;
		private int _historyDays;
		private int _ignoreDays;
		private LongShortMode _mode;

		private DateTime? _lastReevaluation = null;

		private static string GetDescription(LongShortMode mode)
		{
			switch (mode)
			{
				case LongShortMode.LongShort:
					return "Long-short";
				case LongShortMode.LongOnly:
					return "Long";
				case LongShortMode.ShortOnly:
					return "Short";
			}
			throw new ApplicationException("Invalid mode.");
		}

		public LongShortMomentumStrategy(int stocks, decimal stopLossThreshold, int holdDays, int historyDays, int ignoreDays, LongShortMode mode)
			: base($"{GetDescription(mode)} momentum ({stocks} stocks, {stopLossThreshold:P0} stop-loss threshold, hold for {holdDays} days, {historyDays} days of history, ignore past {ignoreDays} days)")
		{
			_stocks = stocks;
			_stopLossThreshold = stopLossThreshold;
			_holdDays = holdDays;
			_historyDays = historyDays;
			_ignoreDays = ignoreDays;
			_mode = mode;
		}

		public override void Trade(StockMarket stockMarket)
		{
			StopLossCheck(stockMarket);
			if
			(
				_lastReevaluation == null ||
				stockMarket.Date - _lastReevaluation.Value >= TimeSpan.FromDays(_holdDays)
			)
			{
				Reevaluate(stockMarket);
				_lastReevaluation = stockMarket.Date;
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
			int stocksToAcquire = (_mode == LongShortMode.LongShort ? 2 * _stocks : _stocks) - stockMarket.Positions.Count;
			if (stocksToAcquire == 0)
			{
				return;
			}
			var ratedStocks = GetStocksByRating(stockMarket);
			if (ratedStocks.Count < 2 * stocksToAcquire)
			{
				return;
			}
			decimal availableFunds = stockMarket.GetAvailableFunds();
			decimal fundsPerPosition = (availableFunds - stocksToAcquire * Constants.OrderFees) / stocksToAcquire;
			fundsPerPosition = Math.Max(fundsPerPosition, MinFundsPerPosition);
			for (; stocksToAcquire > 0 && stockMarket.HasEnoughFunds(MinFundsPerPosition); stocksToAcquire--)
			{
				int longPositions = stockMarket.Positions.Count(p => !p.IsShort);
				bool goShort =
					(_mode == LongShortMode.LongShort && longPositions >= _stocks) ||
					_mode == LongShortMode.ShortOnly;
				var stock = GetAndRemoveStock(goShort, ratedStocks);
				decimal currentPrice = stock.GetPrice(stockMarket.Date) + Constants.Spread;
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
