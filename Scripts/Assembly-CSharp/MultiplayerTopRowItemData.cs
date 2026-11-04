public class MultiplayerTopRowItemData
{
	public enum RowType
	{
		Header = 0,
		Replays = 1
	}

	public RowType rowType;

	public long calculationDate;

	public TopReplay topReplay1;

	public TopReplay topReplay2;

	public MultiplayerTopRowItemData(long calculationDate)
	{
		rowType = RowType.Header;
		this.calculationDate = calculationDate;
	}

	public MultiplayerTopRowItemData(TopReplay topReplay1, TopReplay topReplay2)
	{
		rowType = RowType.Replays;
		this.topReplay1 = topReplay1;
		this.topReplay2 = topReplay2;
	}
}
