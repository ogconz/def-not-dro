using SyncMultiplayer;

namespace HUD
{
	public interface IPostCombatNetworkContext
	{
		void Fill(SubroomInfo info, bool showRaceHints = true, bool top32 = false);

		void Hide();
	}
}
