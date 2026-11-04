using GameOverlay;

namespace SyncMultiplayer
{
	public interface IServerVotePlayer : IServerVote
	{
		PlayerId targetId { get; }
	}
}
