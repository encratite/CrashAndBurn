using System;
using System.Collections.Generic;
using System.Linq;

namespace CrashAndBurn.Common
{
	public class StockMarket
	{
		private HashSet<Stock> _Stocks = new HashSet<Stock>();
		private List<Position> _Positions = new List<Position>();

		private decimal _Cash;
		private decimal _OrderFees;
		private decimal _CapitalGainsTax;

		private decimal _InitialMargin;
		private decimal _MaintenanceMargin;
		private decimal _InitialMarginReserved;

		private bool _MarginCallSellAllPositions = true;

		private decimal _Spread = 0.01m;

		private decimal _Gains = 0;
		private decimal _Losses = 0;

		public static decimal GetPerformance(decimal now, decimal then)
		{
			return now / then - 1.0m;
		}

		public IReadOnlyCollection<Stock> Stocks
		{
			get => _Stocks;
		}

		public IReadOnlyCollection<Position> Positions
		{
			get => _Positions;
		}

		public DateTime Date { get; private set; }

		public int MarginCallCount { get; private set; }

		public StockMarket(IEnumerable<Stock> stocks)
		{
			foreach (var stock in stocks)
			{
				_Stocks.Add(stock);
			}
		}

		public void Initialize(decimal cash, decimal orderFees, decimal capitalGainsTax, decimal initialMargin, decimal maintenanceMargin, DateTime date)
		{
			_Cash = cash;
			_OrderFees = orderFees;
			_CapitalGainsTax = capitalGainsTax;

			_InitialMargin = initialMargin;
			_MaintenanceMargin = maintenanceMargin;
			_InitialMarginReserved = 0.0m;

			MarginCallCount = 0;

			Date = date;
		}

		public void NextDay()
		{
			int lastMonth = Date.Month;
			do
			{
				Date = Date.AddDays(1);
			}
			while (Date.DayOfWeek == DayOfWeek.Saturday || Date.DayOfWeek == DayOfWeek.Sunday);
			if (BelowMaintenanceMargin())
			{
				MarginCall();
			}
			if (Date.Month != lastMonth)
			{
				decimal minimum = Math.Min(_Gains, _Losses);
				decimal taxReturn = _CapitalGainsTax * minimum;
				_Cash += taxReturn;
				_Gains -= minimum;
				_Losses -= minimum;
			}
		}

		public Position Buy(Stock stock, int count)
		{
			decimal pricePerShare = GetPricePerShare(stock);
			decimal price = count * pricePerShare + _OrderFees;
			if (HasEnoughFunds(price))
			{
				return null;
			}
			_Cash -= price;
			var position = new Position(stock, count, pricePerShare, false);
			_Positions.Add(position);
			return position;
		}

		public Position Short(Stock stock, int count)
		{
			decimal pricePerShare = GetPricePerShare(stock);
			decimal initialMargin = GetInitialMargin(count, pricePerShare);
			decimal cashRequired = initialMargin + _OrderFees;
			if (HasEnoughFunds(cashRequired))
			{
				return null;
			}
			_Cash -= _OrderFees;
			_InitialMarginReserved += initialMargin;
			var position = new Position(stock, count, pricePerShare, true);
			_Positions.Add(position);
			return position;
		}

		public void Liquidate(Position position)
		{
			decimal currentPrice = position.Stock.GetPrice(Date);
			decimal priceDelta = currentPrice - position.OriginalPrice;
			decimal capitalGains = position.Count * priceDelta;
			if (position.IsShort)
			{
				capitalGains = -capitalGains;
				_Cash += capitalGains - _OrderFees;
				BookCapitalGains(capitalGains);
				decimal initialMargin = GetInitialMargin(position.Count, position.OriginalPrice);
				_InitialMargin -= initialMargin;
			}
			else
			{
				_Cash += position.Count * currentPrice - _OrderFees;
				BookCapitalGains(capitalGains);
			}
			_Positions.Remove(position);
		}

		public void LiquidateAll()
		{
			foreach (var position in _Positions)
			{
				Liquidate(position);
			}
		}

		public DateRange GetDateRange()
		{
			var dateRange = new DateRange();
			foreach (var stock in Stocks)
			{
				var history = stock.History;
				if (history.Any())
				{
					dateRange.Process(history.First().Key);
					dateRange.Process(history.Last().Key);
				}
			}
			return dateRange;
		}

		public decimal GetAvailableFunds()
		{
			return _Cash - _InitialMarginReserved;
		}

		public bool HasEnoughFunds(decimal price)
		{
			decimal availableFunds = GetAvailableFunds();
			return price <= availableFunds;
		}

		private decimal GetInitialMargin(int count, decimal pricePerShare)
		{
			decimal initialMargin = _InitialMargin * count * pricePerShare;
			return initialMargin;
		}

		private void BookCapitalGains(decimal capitalGains)
		{
			if (capitalGains > 0)
			{
				_Cash -= _CapitalGainsTax * capitalGains;
				_Gains += capitalGains;
			}
			else
			{
				_Losses -= capitalGains;
			}
		}

		private decimal GetPricePerShare(Stock stock)
		{
			decimal pricePerStock = stock.GetPrice(Date) + _Spread;
			return pricePerStock;
		}

		private bool BelowMaintenanceMargin()
		{
			decimal equity = _Cash;
			decimal shortMarketValue = 0.0m;
			foreach (var position in _Positions)
			{
				decimal currentPrice = position.Stock.GetPrice(Date);
				decimal value = position.Count * currentPrice;
				if (position.IsShort)
				{
					equity += position.Count * position.OriginalPrice - value;
					shortMarketValue += value;
				}
				else
				{
					equity += value;
				}
			}
			if (shortMarketValue == 0.0m)
			{
				return false;
			}
			decimal margin = equity / shortMarketValue;
			return margin < _MaintenanceMargin;
		}

		private void MarginCall()
		{
			if (_MarginCallSellAllPositions)
			{
				LiquidateAll();
			}
			else
			{
				while (BelowMaintenanceMargin() && _Positions.Any())
				{
					var position = _Positions.First();
					Liquidate(position);
				}
			}
			MarginCallCount++;
		}
	}
}
