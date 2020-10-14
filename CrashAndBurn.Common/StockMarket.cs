using System;
using System.Collections.Generic;
using System.Linq;

namespace CrashAndBurn.Common
{
	public class StockMarket
	{
		private HashSet<Stock> _stocks = new HashSet<Stock>();
		private List<Position> _positions = new List<Position>();

		private decimal _orderFees;
		private decimal _capitalGainsTax;
		private decimal _stockLendingFee;

		private decimal _initialMargin;
		private decimal _maintenanceMargin;
		private decimal _initialMarginReserved;

		private bool _marginCallSellAllPositions = true;

		private decimal _spread = 0.01m;

		private decimal _gains = 0;
		private decimal _losses = 0;

		private decimal _outstandingStockLendingFees = 0.0m;

		public static decimal GetPerformance(decimal now, decimal then)
		{
			return now / then - 1.0m;
		}

		public IReadOnlyCollection<Stock> Stocks
		{
			get => _stocks;
		}

		public IReadOnlyCollection<Position> Positions
		{
			get => _positions;
		}

		public decimal Cash { get; private set; }
		public DateTime Date { get; private set; }
		public int MarginCallCount { get; private set; }

		public StockMarket(IEnumerable<Stock> stocks)
		{
			foreach (var stock in stocks)
			{
				_stocks.Add(stock);
			}
		}

		public void Initialize(decimal cash, decimal orderFees, decimal capitalGainsTax, decimal initialMargin, decimal maintenanceMargin, decimal stockLendingFee, DateTime date)
		{
			_orderFees = orderFees;
			_capitalGainsTax = capitalGainsTax;
			_stockLendingFee = stockLendingFee;

			_initialMargin = initialMargin;
			_maintenanceMargin = maintenanceMargin;
			_initialMarginReserved = 0.0m;

			Cash = cash;
			Date = date;
			MarginCallCount = 0;

			_gains = 0;
			_losses = 0;

			_outstandingStockLendingFees = 0;
		}

		public void NextDay()
		{
			int lastMonth = Date.Month;
			do
			{
				foreach (var position in _positions)
				{
					if (position.IsShort)
						_outstandingStockLendingFees += _stockLendingFee * position.Count * position.OriginalPrice / 365.0m;
				}
				Date = Date.AddDays(1);
			}
			while (Date.DayOfWeek == DayOfWeek.Saturday || Date.DayOfWeek == DayOfWeek.Sunday);
			bool newMonth = Date.Month != lastMonth;
			if (newMonth)
				ProcessStockLendingFees();
			if (BelowMaintenanceMargin())
				MarginCall();
			if (newMonth)
				ProcessTaxReturn();
		}

		public Position Buy(Stock stock, int count)
		{
			decimal pricePerShare = GetPricePerShare(stock);
			decimal price = count * pricePerShare + _orderFees;
			if (!HasEnoughFunds(price))
			{
				return null;
			}
			Cash -= price;
			var position = new Position(stock, count, pricePerShare, false);
			_positions.Add(position);
			return position;
		}

		public Position Short(Stock stock, int count)
		{
			decimal pricePerShare = GetPricePerShare(stock);
			decimal initialMargin = GetInitialMargin(count, pricePerShare);
			decimal cashRequired = initialMargin + _orderFees;
			if (!HasEnoughFunds(cashRequired))
			{
				return null;
			}
			Cash -= _orderFees;
			_initialMarginReserved += initialMargin;
			var position = new Position(stock, count, pricePerShare, true);
			_positions.Add(position);
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
				Cash += capitalGains - _orderFees;
				BookCapitalGains(capitalGains);
				decimal initialMargin = GetInitialMargin(position.Count, position.OriginalPrice);
				_initialMarginReserved -= initialMargin;
			}
			else
			{
				Cash += position.Count * currentPrice - _orderFees;
				BookCapitalGains(capitalGains);
			}
			_positions.Remove(position);
		}

		public void LiquidateAll()
		{
			foreach (var position in _positions.ToList())
			{
				Liquidate(position);
			}
		}

		public void CashOut()
		{
			LiquidateAll();
			ProcessTaxReturn();
			ProcessStockLendingFees();
		}

		public decimal GetAvailableFunds()
		{
			return Cash - _initialMarginReserved;
		}

		public bool HasEnoughFunds(decimal price)
		{
			decimal availableFunds = GetAvailableFunds();
			return price <= availableFunds;
		}

		private decimal GetInitialMargin(int count, decimal pricePerShare)
		{
			decimal initialMargin = _initialMargin * count * pricePerShare;
			return initialMargin;
		}

		private void BookCapitalGains(decimal capitalGains)
		{
			if (capitalGains > 0)
			{
				Cash -= _capitalGainsTax * capitalGains;
				_gains += capitalGains;
			}
			else
			{
				_losses -= capitalGains;
			}
		}

		private decimal GetPricePerShare(Stock stock)
		{
			decimal pricePerStock = stock.GetPrice(Date) + _spread;
			return pricePerStock;
		}

		private bool BelowMaintenanceMargin()
		{
			decimal equity = Cash;
			decimal shortMarketValue = 0.0m;
			foreach (var position in _positions)
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
			return margin < _maintenanceMargin;
		}

		private void MarginCall()
		{
			if (_marginCallSellAllPositions)
			{
				LiquidateAll();
			}
			else
			{
				while (BelowMaintenanceMargin() && _positions.Any())
				{
					var position = _positions.First();
					Liquidate(position);
				}
			}
			MarginCallCount++;
		}

		private void ProcessTaxReturn()
		{
			decimal minimum = Math.Min(_gains, _losses);
			decimal taxReturn = _capitalGainsTax * minimum;
			Cash += taxReturn;
			_gains -= minimum;
			_losses -= minimum;
		}

		private void ProcessStockLendingFees()
		{
			Cash -= _outstandingStockLendingFees;
			_outstandingStockLendingFees = 0.0m;
		}
	}
}
