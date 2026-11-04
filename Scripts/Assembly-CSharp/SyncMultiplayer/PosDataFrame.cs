using UnityEngine;

namespace SyncMultiplayer
{
	public struct PosDataFrame
	{
		public long modelTime;

		public byte posGeneration;

		public DataFrameFlags flags;

		public float accelerate;

		public float brake;

		public byte gear;

		public bool flares;

		public float handbrake;

		public float steer;

		public Vector3 position;

		public Quaternion rotation;

		public Vector3 velocity;

		public Vector3 angularVelocity;

		public float longAccel;

		public float sideAccel;

		public float angAccel;

		public bool processEngineStart;

		public bool ignitionEnabled;

		public bool starterEnabled;

		public float engineStartTime;

		public float clutch;

		public bool autoClutch;

		public bool clutchKick;

		public float rpm;

		public float mileage;

		public static PosDataFrame Default { get; } = new PosDataFrame
		{
			position = Vector3.zero,
			velocity = Vector3.zero,
			angularVelocity = Vector3.zero,
			rotation = Quaternion.identity
		};

		public bool IsFlaged(DataFrameFlags flag)
		{
			return (flags & flag) != 0;
		}
	}
}
