public abstract class CarCharacteristic : ICarCharacteristic
{
	protected const string FORMAT_N0 = "N0";

	protected RaceCar m_raceCar;

	public virtual float maxFillVal => 0f;

	public virtual void Init(RaceCar raceCar)
	{
		m_raceCar = raceCar;
	}

	public virtual string GetStringValue()
	{
		return string.Empty;
	}

	public virtual float GetValue()
	{
		return 0f;
	}

	public virtual float GetFillValue()
	{
		return 0f;
	}

	public virtual float GetAngleValue()
	{
		return 0f;
	}

	public virtual void Destroy()
	{
	}
}
