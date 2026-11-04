public struct InputStatesReplay
{
	public byte gear;

	public bool isHandbrake;

	public bool isBrake;

	public bool isFlares;

	public bool Equals(InputStatesReplay obj)
	{
		if (gear == obj.gear && isHandbrake == obj.isHandbrake && isBrake == obj.isBrake)
		{
			return isFlares == obj.isFlares;
		}
		return false;
	}
}
