public struct StarterStatesReplay
{
	public bool isStarterPressed;

	public bool isIgnitionOn;

	public bool Equals(StarterStatesReplay obj)
	{
		if (isStarterPressed == obj.isStarterPressed)
		{
			return isIgnitionOn == obj.isIgnitionOn;
		}
		return false;
	}
}
