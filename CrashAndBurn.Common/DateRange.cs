using System;

namespace CrashAndBurn.Common
{
	public class DateRange
	{
		public DateTime? Min { get; set; }
		public DateTime? Max { get; set; }

		public void Process(DateTime date)
		{
			if (!Min.HasValue || date < Min.Value)
			{
				Min = date;
			}
			if (!Max.HasValue || date > Max.Value)
			{
				Max = date;
			}
		}
	}
}
