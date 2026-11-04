public interface ICarCharacteristic
{
	float maxFillVal { get; }

	void Init(RaceCar raceCar);

	string GetStringValue();

	float GetValue();

	float GetFillValue();

	float GetAngleValue();

	void Destroy();
}
