namespace Minimaps
{
	public class UIMinimapPauseMenuVisualizer : BaseMinimapVisualizer
	{
		private void Update()
		{
			if (!(Minimap.instance == null))
			{
				UpdateIcons();
				UpdateMinimapTexture();
			}
		}

		protected override bool IsNeedActivate(MinimapItem item, bool defaultShowValue)
		{
			if (item.isCheckpoint)
			{
				if (States.current is BaseRaceModeState baseRaceModeState)
				{
					return baseRaceModeState.raceMode != RaceMode.Freeride;
				}
				return false;
			}
			return defaultShowValue;
		}
	}
}
