using System.Collections.Generic;

namespace CrashAndBurn.Common
{
    public class Stock
    {
        // Symbol or ISIN.
        public string Id { get; private set; }

        public List<StockData> History { get; private set; }

        public Stock(string id, List<StockData> history)
        {
            Id = id;
            History = history;
        }

        public override bool Equals(object obj)
        {
            var stock = obj as Stock;
            if (stock == null)
            {
                return false;
            }
            return Id == stock.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
