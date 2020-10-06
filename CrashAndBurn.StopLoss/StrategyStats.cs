using System.Collections.Generic;

namespace CrashAndBurn.StopLoss
{
	class StrategyStats
	{
		public string Name { get; private set; }
		public decimal Cash { get; private set; }

		private List<decimal> results = new List<decimal>();

		public StrategyStats(string name)
		{
			Name = name;
		}

		public void Add(decimal result)
		{
			results.Add(result);
			Cash = GetMedian();
		}

		private decimal GetMedian()
		{
			results.Sort();
			decimal median;
			int offset = results.Count / 2;
			if (results.Count % 2 != 0)
			{
				median = results[offset];
			}
			else
			{
				median = (results[offset - 1] + results[offset]) / 2.0m;
			}
			return median;
		}
	}
}
