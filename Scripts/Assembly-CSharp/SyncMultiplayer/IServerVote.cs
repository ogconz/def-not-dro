namespace SyncMultiplayer
{
	public interface IServerVote
	{
		VoteType voteType { get; }

		void OnVoteFinished(VoteResult result);

		string GetTitle();

		string GetTarget();

		string GetAdditionalTarget();
	}
}
