using System.Collections.Generic;

namespace CrashAndBurn.StopLoss
{
	class StrategyStats
	{
		public string Name { get; private set; }
		public decimal Cash { get; private set; }

		private List<decimal> _results = new List<decimal>();

		public StrategyStats(string name)
		{
			Name = name;
		}

		public void Add(decimal result)
		{
			_results.Add(result);
			Cash = GetMedian();
		}

		private decimal GetMedian()
		{
			_results.Sort();
			decimal median;
			int offset = _results.Count / 2;
			if (_results.Count % 2 != 0)
			{
				median = _results[offset];
			}
			else
			{
				median = (_results[offset - 1] + _results[offset]) / 2.0m;
			}
			return median;
		}
	}
}
