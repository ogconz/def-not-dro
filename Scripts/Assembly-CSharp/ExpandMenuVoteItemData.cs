public class ExpandMenuVoteItemData : ExpandMenuItemData
{
	public enum VoteRate
	{
		Up = 1,
		Down = -1
	}

	public int rate => m_voteRate.value;

	public ExpandMenuVoteItemData(string title, VoteRate voteRate)
		: base(title)
	{
		m_voteRate.value = (int)voteRate;
	}
}
