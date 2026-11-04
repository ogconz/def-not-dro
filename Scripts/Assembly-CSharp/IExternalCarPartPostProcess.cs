using UnityEngine;

public interface IExternalCarPartPostProcess
{
	void Process(RaceCar raceCar, GameObject gameObject, string format);
}
