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

        public IReadOnlyCollection<Stock> Stocks
        {
            get => _Stocks;
        }

        public IReadOnlyCollection<Position> Positions
        {
            get => _Positions;
        }

        public DateTime Time { get; private set; }

        public StockMarket(IEnumerable<Stock> stocks)
        {
            foreach (var stock in stocks)
            {
                _Stocks.Add(stock);
            }
        }

        public void Initialize(decimal cash, decimal orderFees, decimal capitalGainsTax, decimal margin, DateTime time)
        {
            _Cash = cash;
            _OrderFees = orderFees;
            _CapitalGainsTax = capitalGainsTax;
            _Margin = margin;
            Time = time;
        }

        public void NextDay()
        {
            while (Time.DayOfWeek == DayOfWeek.Saturday || Time.DayOfWeek == DayOfWeek.Sunday)
            {
                Time = Time.AddDays(1);
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
            throw new NotImplementedException();
        }

        private decimal GetPricePerStock(Stock stock)
        {
            decimal pricePerStock = stock.GetPrice(Time) + _Spread;
            return pricePerStock;
        }
    }
}
