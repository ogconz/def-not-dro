using System;

namespace LocalCaching.Model
{
	[Serializable]
	public class CarProfileCache
	{
		public int id;

		public float topSpeedMpH;

		public float topSpeedKmH;

		public float accelMpH;

		public float accelKmH;

		public int maxPower;

		public int maxPowerRPM;

		public int maxTorque;

		public int maxTorqueRPM;

		public int weight;

		public bool traction_control;

		public string transmission;

		public string tyres;

		public float powerDensity;

		public CarProfileCache()
		{
		}

		public CarProfileCache(int id)
		{
			this.id = id;
		}
	}
}
