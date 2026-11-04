using CarX;
using UnityEngine;

namespace SyncMultiplayer
{
	public class NetGameCircularInterpolator
	{
		private PosDataFrame[] m_data;

		private int m_current = -1;

		private int m_count;

		private NetworkPlayer m_host;

		public bool lastExtrapolated { get; private set; }

		public NetGameCircularInterpolator(NetworkPlayer host, int capacity)
		{
			m_host = host;
			m_data = new PosDataFrame[capacity];
			for (int i = 0; i < m_data.Length; i++)
			{
				m_data[i] = PosDataFrame.Default;
			}
		}

		public void MarkLastWithFlags(DataFrameFlags flags)
		{
			if (m_count != 0)
			{
				m_data[m_current].flags |= flags;
			}
		}

		public void UnmarkLastWithFlags(DataFrameFlags flags)
		{
			if (m_count != 0)
			{
				m_data[m_current].flags &= (DataFrameFlags)(byte)(~(int)flags);
			}
		}

		public PosDataFrame GetLast()
		{
			if (m_count == 0)
			{
				return PosDataFrame.Default;
			}
			return m_data[m_current];
		}

		public void AddData(ref PosDataFrame dataFrame)
		{
			m_current++;
			if (m_count < m_data.Length - 1)
			{
				m_count++;
			}
			if (m_current >= m_data.Length)
			{
				m_current = 0;
			}
			m_data[m_current] = dataFrame;
		}

		public PosDataFrame GetDataFrame(long modelTime)
		{
			lastExtrapolated = false;
			if (m_count == 0)
			{
				return PosDataFrame.Default;
			}
			if (modelTime > m_data[m_current].modelTime)
			{
				lastExtrapolated = true;
				return ExtrapolateLast(modelTime);
			}
			int num = m_current;
			for (int i = 1; i <= m_count; i++)
			{
				num = m_current - i;
				if (num < 0)
				{
					num = m_data.Length + num;
				}
				if (m_data[num].modelTime < modelTime)
				{
					return Interpolate(modelTime, ref m_data[num], ref m_data[(num + 1) % m_data.Length]);
				}
			}
			return m_data[num % m_data.Length];
		}

		private PosDataFrame ExtrapolateLast(long modelTime)
		{
			PosDataFrame last = m_data[m_current];
			long num = modelTime - last.modelTime;
			if ((float)num > 1000f)
			{
				float num2 = (float)num - 1000f;
				if (num2 > 3000f)
				{
					num2 = 3000f;
					return last;
				}
				float num3 = num2 / 3000f;
				PosDataFrame result = last;
				result.modelTime = modelTime;
				if (m_host.userCar != null)
				{
					GetCurrentFromCar(ref result, m_host.userCar);
				}
				long modelTime2 = MathTools.Lerp(modelTime, last.modelTime, (double)num3);
				return Interpolate(modelTime2, ref last, ref result);
			}
			if (num > 300)
			{
				num = 300L;
			}
			float dt = (float)num / 1000f;
			PosDataFrame extrapolated = PosDataFrame.Default;
			extrapolated.modelTime = modelTime;
			Extrapolate(ref extrapolated, in last, dt);
			return extrapolated;
		}

		private void Extrapolate(ref PosDataFrame extrapolated, in PosDataFrame last, float dt)
		{
			extrapolated.posGeneration = last.posGeneration;
			extrapolated.flags = last.flags;
			extrapolated.accelerate = last.accelerate;
			extrapolated.gear = last.gear;
			extrapolated.brake = last.brake;
			extrapolated.handbrake = last.handbrake;
			extrapolated.flares = last.flares;
			extrapolated.steer = last.steer;
			extrapolated.clutch = last.clutch;
			extrapolated.autoClutch = last.autoClutch;
			extrapolated.clutchKick = last.clutchKick;
			extrapolated.rpm = last.rpm;
			extrapolated.mileage = last.mileage;
			Vector3 vector = Vector3.zero;
			if ((Mathf.Abs(last.sideAccel) > Mathf.Epsilon || Mathf.Abs(last.longAccel) > Mathf.Epsilon) && !last.rotation.IsOneOfNaNOrInfinity())
			{
				Vector3 vector2 = new Vector3(last.sideAccel, 0f, last.longAccel);
				vector = last.rotation * vector2;
			}
			extrapolated.velocity = ExtrapolateVector(last.velocity, vector, dt);
			extrapolated.position = ExtrapolateVector(last.position, last.velocity, vector, dt);
			extrapolated.longAccel = last.longAccel;
			extrapolated.sideAccel = last.sideAccel;
			extrapolated.engineStartTime = last.engineStartTime;
			Car car = ((m_host == null || m_host.userCar == null) ? null : m_host.userCar.carX);
			Vector3 zero = Vector3.zero;
			extrapolated.angAccel = last.angAccel;
			if (car != null && last.angAccel == 0f)
			{
				GameLog.Print(AlertLevel.Notify, "Multiplayer", "Position data frame does not contain accelerations (probably it was sent by older game version). Using old prediction algorithm for rotation.");
				float b = last.angularVelocity.y + car.angAccel * dt;
				extrapolated.angularVelocity = last.angularVelocity;
				zero = new Vector3(last.angularVelocity.x, Mathf.Lerp(last.angularVelocity.y, b, 0.8f), last.angularVelocity.z) * 57.29578f * dt;
			}
			else
			{
				Vector3 signedEulers = (57.29578f * last.angularVelocity).GetSignedEulers();
				Vector3 signedEulers2 = Quaternion.AngleAxis(last.angAccel * 57.29578f, last.rotation * Vector3.up).eulerAngles.GetSignedEulers();
				signedEulers2.x = 0f;
				signedEulers2.z = 0f;
				zero = signedEulers * dt + 0.5f * signedEulers2 * dt * dt;
				extrapolated.angularVelocity = last.angularVelocity;
			}
			extrapolated.processEngineStart = last.processEngineStart;
			extrapolated.ignitionEnabled = last.ignitionEnabled;
			extrapolated.starterEnabled = last.starterEnabled;
			extrapolated.rotation = last.rotation * Quaternion.Euler(zero);
		}

		private PosDataFrame GetCurrentFromCar(ref PosDataFrame result, RaceCar car)
		{
			Car carX = car.carX;
			result.accelerate = carX.accelerate;
			result.gear = (byte)carX.gear;
			result.brake = carX.brake;
			result.handbrake = carX.handbrake;
			result.steer = carX.GetSteer();
			result.clutch = carX.clutch;
			result.autoClutch = carX.autoClutch;
			result.clutchKick = car.isClutchKick;
			result.rpm = carX.rpm;
			result.mileage = car.mileageMoreZero;
			result.velocity = carX.velocity;
			result.angularVelocity = carX.angularVelocity;
			result.position = carX.getTransform.position;
			result.rotation = carX.getTransform.rotation;
			result.angAccel = carX.angAccel;
			result.longAccel = carX.longAccel;
			result.sideAccel = carX.sideAccel;
			return result;
		}

		private PosDataFrame Interpolate(long modelTime, ref PosDataFrame left, ref PosDataFrame right)
		{
			modelTime = MathTools.Clamp(modelTime, left.modelTime, right.modelTime);
			long num = (modelTime - left.modelTime) / (right.modelTime - left.modelTime);
			PosDataFrame result = PosDataFrame.Default;
			result.modelTime = modelTime;
			result.posGeneration = left.posGeneration;
			result.flags = left.flags;
			result.accelerate = Mathf.Lerp(left.accelerate, right.accelerate, num);
			result.gear = (((double)num < 0.5) ? left.gear : right.gear);
			result.brake = Mathf.Lerp(left.brake, right.brake, num);
			result.handbrake = Mathf.Lerp(left.handbrake, right.handbrake, num);
			result.flares = (((double)num < 0.5) ? left.flares : right.flares);
			result.steer = Mathf.Lerp(left.steer, right.steer, num);
			result.clutch = Mathf.Lerp(left.clutch, right.clutch, num);
			result.autoClutch = (((float)num < 0.5f) ? left.autoClutch : right.autoClutch);
			result.clutchKick = (((float)num < 0.5f) ? left.clutchKick : right.clutchKick);
			result.rpm = Mathf.Lerp(left.rpm, right.rpm, num);
			result.mileage = Mathf.Lerp(left.mileage, right.mileage, num);
			result.velocity = Vector3.Lerp(left.velocity, right.velocity, num);
			result.angularVelocity = Vector3.Lerp(left.angularVelocity, right.angularVelocity, num);
			result.position = Vector3.Lerp(left.position, right.position, num);
			result.rotation = Quaternion.Lerp(left.rotation, right.rotation, num);
			result.angAccel = Mathf.Lerp(left.angAccel, right.angAccel, num);
			result.longAccel = Mathf.Lerp(left.longAccel, right.longAccel, num);
			result.sideAccel = Mathf.Lerp(left.sideAccel, right.sideAccel, num);
			result.processEngineStart = (((float)num < 0.5f) ? left.processEngineStart : right.processEngineStart);
			result.ignitionEnabled = (((float)num < 0.5f) ? left.ignitionEnabled : right.ignitionEnabled);
			result.starterEnabled = (((float)num < 0.5f) ? left.starterEnabled : right.starterEnabled);
			return result;
		}

		public static Vector3 ExtrapolateVector(Vector3 value, Vector3 velocity, Vector3 accel, float time)
		{
			value += velocity * time + accel * time * time / 2f;
			return value;
		}

		public static Vector3 ExtrapolateVector(Vector3 value, Vector3 velocity, float time)
		{
			return value + velocity * time;
		}
	}
}
