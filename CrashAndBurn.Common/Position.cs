namespace CrashAndBurn.Common
{
    public class Position
    {
        public Stock Stock { get; private set; }

        public int Count { get; private set; }

        public decimal OriginalPrice { get; private set; }

        public bool IsShort { get; private set; }

        public Position(Stock stock, int count, decimal originalPrice, bool isShort = false)
        {
            Stock = stock;
            Count = count;
            OriginalPrice = originalPrice;
            IsShort = isShort;
        }
    }
}
