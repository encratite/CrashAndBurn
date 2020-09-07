using System;
using System.Collections.Generic;

namespace CrashAndBurn.Common
{
    public class StockMarket
    {
        private HashSet<Stock> _Stocks = new HashSet<Stock>();
        private List<Position> _Positions = new List<Position>();

        private decimal _Cash;
        private decimal _OrderFees;
        private decimal _CapitalGainsTax;
        private decimal _Margin;
        private decimal _Spread = 0.01m;

        private decimal _Gains = 0.0m;
        private decimal _Losses = 0.0m;

        public IReadOnlyCollection<Stock> Stocks
        {
            get => _Stocks;
        }

        public IReadOnlyCollection<Position> Positions
        {
            get => _Positions;
        }

        public DateTime Date { get; private set; }

        public StockMarket(IEnumerable<Stock> stocks)
        {
            foreach (var stock in stocks)
            {
                _Stocks.Add(stock);
            }
        }

        public void Initialize(decimal cash, decimal orderFees, decimal capitalGainsTax, decimal margin, DateTime date)
        {
            _Cash = cash;
            _OrderFees = orderFees;
            _CapitalGainsTax = capitalGainsTax;
            _Margin = margin;
            Date = date;
        }

        public void NextDay()
        {
            int lastMonth = Date.Month;
            while (Date.DayOfWeek == DayOfWeek.Saturday || Date.DayOfWeek == DayOfWeek.Sunday)
            {
                Date = Date.AddDays(1);
            }
            if (Date.Month != lastMonth)
            {
                decimal taxReturn = _CapitalGainsTax * Math.Min(_Gains, _Losses);
                _Cash += taxReturn;
                _Gains = 0;
                _Losses = 0;
            }
        }

        public Position Buy(Stock stock, int count)
        {
            decimal pricePerStock = GetPricePerStock(stock);
            decimal price = count * pricePerStock + _OrderFees;
            if (price > _Cash)
            {
                throw new ApplicationException("Unable to buy stock, not enough funds.");
            }
            var position = new Position(stock, count, pricePerStock, false);
            _Positions.Add(position);
            return position;
        }

        public Position Short(Stock stock, int count)
        {
            decimal pricePerStock = GetPricePerStock(stock);
            decimal priceWithMargin = _Margin * count * pricePerStock + _OrderFees;
            if (priceWithMargin > _Cash)
            {
                throw new ApplicationException("Unable to short stock, required margin exceeds funds.");
            }
            var position = new Position(stock, count, pricePerStock, true);
            _Positions.Add(position);
            return position;
        }

        public void Sell(Position position)
        {
            decimal currentPrice = position.Stock.GetPrice(Date);
            decimal priceDelta = currentPrice - position.OriginalPrice;
            decimal capitalGains = position.Count * priceDelta;
            if (position.IsShort)
            {
                capitalGains = -capitalGains;
                _Cash += capitalGains;
                BookCapitalGains(capitalGains);
            }
            else
            {
                _Cash += position.Count * currentPrice;
                BookCapitalGains(capitalGains);
            }
            _Positions.Remove(position);
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

        private decimal GetPricePerStock(Stock stock)
        {
            decimal pricePerStock = stock.GetPrice(Date) + _Spread;
            return pricePerStock;
        }
    }
}
