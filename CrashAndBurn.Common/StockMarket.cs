using System;
using System.Collections.Generic;
using System.Linq;

namespace CrashAndBurn.Common
{
	public class StockMarket
	{
		private const bool EnableLogging = false;

		private HashSet<Stock> _stocks = new HashSet<Stock>();
		private SortedDictionary<DateTime, List<Stock>> _dividends = new SortedDictionary<DateTime, List<Stock>>();
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
				AddDividends(stock);
			}
		}

		public void Initialize(decimal cash, decimal orderFees, decimal capitalGainsTax, decimal initialMargin, decimal maintenanceMargin, decimal stockLendingFee, decimal spread, DateTime date)
		{
			_orderFees = orderFees;
			_capitalGainsTax = capitalGainsTax;
			_stockLendingFee = stockLendingFee;
			_spread = spread;

			_initialMargin = initialMargin;
			_maintenanceMargin = maintenanceMargin;
			_initialMarginReserved = 0.0m;

			Cash = cash;
			Date = date;
			MarginCallCount = 0;

			_gains = 0;
			_losses = 0;

			_outstandingStockLendingFees = 0;

			if (EnableLogging)
			{
				WriteDate();
				Output.WriteLine($"Starting with {Cash:C2}");
			}
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
				ProcessDividends(Date);
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
			if (EnableLogging)
			{
				WriteDate();
				Output.WriteLine($"Bought {count} {stock.Id} shares at {pricePerShare:C2} for {price:C2}");
			}
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
			if (EnableLogging)
			{
				WriteDate();
				Output.WriteLine($"Shorted {count} {stock.Id} shares at {pricePerShare:C2}");
			}
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
				if (EnableLogging)
				{
					WriteDate();
					Output.Write($"Bought back {position.Count} {position.Stock.Id} shares at {currentPrice:C2}");
					WriteProfit(capitalGains);
				}
			}
			else
			{
				Cash += position.Count * currentPrice - _orderFees;
				BookCapitalGains(capitalGains);
				if (EnableLogging)
				{
					WriteDate();
					Output.Write($"Sold {position.Count} {position.Stock.Id} shares at {currentPrice:C2}");
					WriteProfit(capitalGains);
				}
			}
			_positions.Remove(position);
		}

		public void LiquidateAll()
		{
			foreach (var position in _positions.ToList())
				Liquidate(position);
		}

		public void CashOut()
		{
			LiquidateAll();
			ProcessTaxReturn();
			ProcessStockLendingFees();
			if (EnableLogging)
			{
				WriteDate();
				Output.WriteLine($"Cashed out with {Cash:C2}");
				Output.NewLine();
			}
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

		private void AddDividends(Stock stock)
		{
			foreach (var pair in stock.Dividends)
			{
				List<Stock> dividendStocks;
				if (!_dividends.TryGetValue(pair.Key, out dividendStocks))
				{
					dividendStocks = new List<Stock>
					{
						stock
					};
					_dividends.Add(pair.Key, dividendStocks);
				}
				else
				{
					dividendStocks.Add(stock);
				}
			}
		}

		private void ProcessDividends(DateTime date)
		{
			if (!_dividends.TryGetValue(date, out List<Stock> stocks))
				return;
			foreach (var position in _positions)
			{
				if (stocks.Contains(position.Stock))
				{
					decimal dividendsPerShare = position.Stock.Dividends[date];
					decimal dividends = position.Count * dividendsPerShare;
					if (position.IsShort)
					{
						Cash -= dividends;
						if (EnableLogging)
						{
							WriteDate();
							Output.Write($"Paid dividends for {position.Count} {position.Stock.Id} shares");
							WriteProfit(-dividends);
						}
					}
					else
					{
						Cash += dividends;
						BookCapitalGains(dividends);
						if (EnableLogging)
						{
							WriteDate();
							Output.Write($"Received dividends for {position.Count} {position.Stock.Id} shares");
							WriteProfit(dividends);
						}
					}
				}
			}
		}

		private void WriteDate()
		{
			Output.Write($"{Date.ToShortDateString()} ");
		}

		private void WriteProfit(decimal capitalGains)
		{
			var color = capitalGains >= 0.0m ? ConsoleColor.Green : ConsoleColor.Red;
			Output.Write(" (");
			Output.Write($"{capitalGains:+$##,#.##;-$##,#.##;$##,#.##}", color);
			Output.WriteLine(")");
		}
	}
}
