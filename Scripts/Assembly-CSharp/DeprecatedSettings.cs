public class DeprecatedSettings
{
	public int seasonDuration = 604800;

	public int seasonEndNoRacesTimeIn = 60;

	public int fuelPer1000Minutes = 1;

	public int lowFuelThreshold;

	public int fuelFullRestoreTimeInMinutes;

	public int maxGhostAge = 86400;

	public int defaultQualiAttempts = 2;

	public int defaultMaxFuel = 20;

	public int attackablePositionsAhead = 30;

	public int attackablePositionsBehind = 10;

	public int battleDelayAfterWinMins = 1440;

	public int raceSameUserThrottleLimit = 72000;

	public Cost qualiReward = new Cost(3000, 8);
}
