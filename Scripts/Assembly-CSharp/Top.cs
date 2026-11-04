using System.Collections.Generic;

public class Top
{
	public long nextCallculationDate { get; set; }

	public int league { get; set; }

	public int nextPageAccessor { get; set; }

	public int currentPeriod { get; set; }

	public List<TopReplay> topRaces { get; set; }
}
