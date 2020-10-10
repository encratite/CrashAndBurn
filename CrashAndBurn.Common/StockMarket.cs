using System;
using System.Collections.Generic;
using System.Linq;

namespace CrashAndBurn.Common
{
	public class StockMarket
	{
		private HashSet<Stock> stocks = new HashSet<Stock>();
		private List<Position> positions = new List<Position>();

		private decimal orderFees;
		private decimal capitalGainsTax;
		private decimal stockLendingFee;

		private decimal initialMargin;
		private decimal maintenanceMargin;
		private decimal initialMarginReserved;

		private bool marginCallSellAllPositions = true;

		private decimal spread = 0.01m;

		private decimal gains = 0;
		private decimal losses = 0;

		private decimal outstandingStockLendingFees = 0.0m;

		public static decimal GetPerformance(decimal now, decimal then)
		{
			return now / then - 1.0m;
		}

		public IReadOnlyCollection<Stock> Stocks
		{
			get => stocks;
		}

		public IReadOnlyCollection<Position> Positions
		{
			get => positions;
		}

		public decimal Cash { get; private set; }
		public DateTime Date { get; private set; }
		public int MarginCallCount { get; private set; }

		public StockMarket(IEnumerable<Stock> stocks)
		{
			foreach (var stock in stocks)
			{
				this.stocks.Add(stock);
			}
		}

		public void Initialize(decimal cash, decimal orderFees, decimal capitalGainsTax, decimal initialMargin, decimal maintenanceMargin, decimal stockLendingFee, DateTime date)
		{
			this.orderFees = orderFees;
			this.capitalGainsTax = capitalGainsTax;
			this.stockLendingFee = stockLendingFee;

			this.initialMargin = initialMargin;
			this.maintenanceMargin = maintenanceMargin;
			initialMarginReserved = 0.0m;

			Cash = cash;
			Date = date;
			MarginCallCount = 0;

			gains = 0;
			losses = 0;

			outstandingStockLendingFees = 0;
		}

		public void NextDay()
		{
			int lastMonth = Date.Month;
			do
			{
				foreach (var position in positions)
				{
					if (position.IsShort)
						outstandingStockLendingFees += stockLendingFee * position.Count * position.OriginalPrice / 365.0m;
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
			decimal price = count * pricePerShare + orderFees;
			if (!HasEnoughFunds(price))
			{
				return null;
			}
			Cash -= price;
			var position = new Position(stock, count, pricePerShare, false);
			positions.Add(position);
			return position;
		}

		public Position Short(Stock stock, int count)
		{
			decimal pricePerShare = GetPricePerShare(stock);
			decimal initialMargin = GetInitialMargin(count, pricePerShare);
			decimal cashRequired = initialMargin + orderFees;
			if (!HasEnoughFunds(cashRequired))
			{
				return null;
			}
			Cash -= orderFees;
			initialMarginReserved += initialMargin;
			var position = new Position(stock, count, pricePerShare, true);
			positions.Add(position);
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
				Cash += capitalGains - orderFees;
				BookCapitalGains(capitalGains);
				decimal initialMargin = GetInitialMargin(position.Count, position.OriginalPrice);
				initialMarginReserved -= initialMargin;
			}
			else
			{
				Cash += position.Count * currentPrice - orderFees;
				BookCapitalGains(capitalGains);
			}
			positions.Remove(position);
		}

		public void LiquidateAll()
		{
			foreach (var position in positions.ToList())
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

		public DateRange GetDateRange()
		{
			var dateRange = new DateRange();
			foreach (var stock in Stocks)
				stock.UpdateDateRange(dateRange);
			return dateRange;
		}

		public decimal GetAvailableFunds()
		{
			return Cash - initialMarginReserved;
		}

		public bool HasEnoughFunds(decimal price)
		{
			decimal availableFunds = GetAvailableFunds();
			return price <= availableFunds;
		}

		private decimal GetInitialMargin(int count, decimal pricePerShare)
		{
			decimal initialMargin = this.initialMargin * count * pricePerShare;
			return initialMargin;
		}

		private void BookCapitalGains(decimal capitalGains)
		{
			if (capitalGains > 0)
			{
				Cash -= capitalGainsTax * capitalGains;
				gains += capitalGains;
			}
			else
			{
				losses -= capitalGains;
			}
		}

		private decimal GetPricePerShare(Stock stock)
		{
			decimal pricePerStock = stock.GetPrice(Date) + spread;
			return pricePerStock;
		}

		private bool BelowMaintenanceMargin()
		{
			decimal equity = Cash;
			decimal shortMarketValue = 0.0m;
			foreach (var position in positions)
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
			return margin < maintenanceMargin;
		}

		private void MarginCall()
		{
			if (marginCallSellAllPositions)
			{
				LiquidateAll();
			}
			else
			{
				while (BelowMaintenanceMargin() && positions.Any())
				{
					var position = positions.First();
					Liquidate(position);
				}
			}
			MarginCallCount++;
		}

		private void ProcessTaxReturn()
		{
			decimal minimum = Math.Min(gains, losses);
			decimal taxReturn = capitalGainsTax * minimum;
			Cash += taxReturn;
			gains -= minimum;
			losses -= minimum;
		}

		private void ProcessStockLendingFees()
		{
			Cash -= outstandingStockLendingFees;
			outstandingStockLendingFees = 0.0m;
		}
	}
}
