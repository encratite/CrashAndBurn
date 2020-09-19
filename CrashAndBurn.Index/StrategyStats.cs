using System.Collections.Generic;

namespace CrashAndBurn
{
	class StrategyStats
	{
		public string Name { get; private set; }
		public decimal Cash { get; private set; }

		private List<decimal> _Results = new List<decimal>();

		public StrategyStats(string name)
		{
			Name = name;
		}

		public void Add(decimal result)
		{
			_Results.Add(result);
			Cash = GetMedian();
		}

		private decimal GetMedian()
		{
			_Results.Sort();
			decimal median;
			int offset = _Results.Count / 2;
			if (_Results.Count % 2 != 0)
			{
				median = _Results[offset];
			}
			else
			{
				median = (_Results[offset - 1] + _Results[offset]) / 2.0m;
			}
			return median;
		}
	}
}
