using CarX;
using UnityEngine;

[RequireComponent(typeof(RaceCar))]
public class CarAI : MonoBehaviour
{
	[SerializeField]
	private float m_predTime = 0.65f;

	[SerializeField]
	private float m_accelSmooth = 20f;

	[SerializeField]
	private float m_brakeSmooth = 20f;

	[SerializeField]
	private float m_steerSmooth = 3f;

	private RaceCar m_car;

	private float m_targAccel;

	private float m_targBrake;

	private float m_targSteer;

	private float m_currAccel;

	private float m_currBrake;

	private float m_currSteer;

	private float m_pathOffset;

	private float m_dontShiftTimer;

	private float m_speedIntervalTimer = 2f;

	private Bounds m_intervalBound;

	private RaceCar[] m_allCars;

	private GearShiftType m_gearShiftTypeStored;

	private bool m_tractionControlStored;

	private bool m_autoClutch;

	private const float MAX_SPEED_DIFFERENCE = 15f;

	private const float MIN_RABBIT_DISTANCE = 25f;

	private const float MAX_RABBIT_DISTANCE = 35f;

	private const float PEDAL_DELTA = 0.02f;

	private const float ONE_PER_SPEED = 0.036f;

	private void Awake()
	{
		m_car = GetComponent<RaceCar>();
	}

	private void Start()
	{
		m_allCars = Object.FindObjectsOfType(typeof(RaceCar)) as RaceCar[];
		m_pathOffset = 0f;
	}

	private void OnEnable()
	{
		m_gearShiftTypeStored = m_car.carX.gearShifting;
		m_car.carX.gearShifting = GearShiftType.Auto;
		m_tractionControlStored = m_car.useTC;
		m_autoClutch = m_car.carX.autoClutch;
		m_car.carX.autoClutch = true;
		m_car.useTC = true;
		m_targAccel = (m_currAccel = m_car.carX.accelerate);
		m_targBrake = (m_currBrake = m_car.carX.brake);
		m_targSteer = (m_currSteer = m_car.carX.GetSteer());
		m_intervalBound.SetMinMax(Vector3.zero, Vector3.zero);
	}

	private void OnDisable()
	{
		m_car.carX.autoClutch = m_autoClutch;
		m_car.carX.gearShifting = m_gearShiftTypeStored;
		m_car.useTC = m_tractionControlStored;
	}

	private void FixedUpdate()
	{
		if (Time.deltaTime != 0f && !m_car.IsPhysicsBroken())
		{
			float relativeDistance = CalcRabbitRelDist();
			m_car.positionOnSpline.GetTarget(relativeDistance, out var point, out var _, out var _);
			SteerToTarget3(point);
			SmoothValue(m_accelSmooth, m_targAccel, ref m_currAccel);
			SmoothValue(m_brakeSmooth, m_targBrake, ref m_currBrake);
			SmoothValue(m_steerSmooth, m_targSteer, ref m_currSteer);
			m_car.carX.accelerate = m_currAccel;
			m_car.carX.brake = m_currBrake;
			m_car.carX.handbrake = 0f;
			m_car.carX.SetSteer(m_currSteer, useAssistent: false, Time.deltaTime);
		}
	}

	private float CalcRabbitRelDist()
	{
		return Mathf.Clamp(25f + m_car.carX.speedMPS * 0.036f * 10f, 25f, 35f);
	}

	private void SteerToTarget3(Vector3 targPnt)
	{
		Car carX = m_car.carX;
		Vector3 lhs = targPnt - carX.getTransform.position;
		lhs.y = 0f;
		lhs.Normalize();
		Vector3 forward = carX.getTransform.forward;
		forward.y = 0f;
		forward.Normalize();
		Vector3 vector = Vector3.Cross(lhs, forward);
		float value = ((!(Vector3.Dot(lhs, forward) < 0f)) ? (Mathf.Sign(vector.y) * vector.magnitude) : Mathf.Sign(vector.y));
		m_car.NeedBrake(out var curVel, out var maxVel);
		if (curVel - maxVel > 15f)
		{
			m_targBrake += 0.02f;
			m_targAccel = 0f;
		}
		else
		{
			m_targBrake = 0f;
			m_targAccel += 0.02f;
		}
		m_targSteer = Mathf.Clamp(value, -1f, 1f);
		m_targBrake = Mathf.Clamp01(m_targBrake);
		m_targAccel = Mathf.Clamp01(m_targAccel);
	}

	private void SteerToTarget(Vector3 targPnt, Vector3 targDir, float targRad)
	{
		Car carX = m_car.carX;
		Vector3 position = carX.getTransform.position;
		Vector3 velocity = carX.velocity;
		velocity.y = 0f;
		Vector3 forward = carX.getTransform.forward;
		forward.y = 0f;
		forward.Normalize();
		Vector3 right = carX.getTransform.right;
		right.y = 0f;
		right.Normalize();
		float longAccel = carX.longAccel;
		float sideAccel = carX.sideAccel;
		float angAccel = carX.angAccel;
		float curVel = carX.speedMPS;
		float magnitude = carX.angularVelocity.magnitude;
		float num = magnitude + angAccel * Time.deltaTime;
		Quaternion quaternion = Quaternion.AngleAxis((0f - (magnitude + num) * m_predTime * 0.5f) * 57.29578f, Vector3.up);
		Vector3 vector = velocity + forward * longAccel * m_predTime + right * sideAccel * m_predTime;
		Vector3 vector2 = position + (velocity * m_predTime + vector * m_predTime) * 0.5f;
		Vector3 rhs = vector;
		rhs.y = 0f;
		Vector3 vector3 = targDir;
		vector3.y = 0f;
		vector3.Normalize();
		m_car.NeedBrake(out curVel, out var maxVel);
		float num2 = Mathf.Clamp01(Mathf.Abs(targRad) * 0.001f);
		Vector3 vector4 = targPnt + Vector3.Cross(vector3, Vector3.up) * (m_pathOffset * num2 * -1f) - vector2;
		vector4.y = 0f;
		float magnitude2 = vector4.magnitude;
		bool flag = Vector3.Dot(forward, rhs) < 0f;
		if (flag)
		{
			vector3 = -vector3;
		}
		else if (magnitude2 > 2f)
		{
			vector4 *= 2f / magnitude2;
		}
		Vector3 vector5 = vector4 * 2f + vector3 * 2f;
		vector5.y = 0f;
		vector5.Normalize();
		Vector3 vRepulse = Vector3.zero;
		float fAddBrake = 0f;
		GetObstaclesRepulse(out vRepulse, out fAddBrake);
		Vector3 zero = Vector3.zero;
		float num3 = 0f;
		bool flag2 = false;
		float num4 = fAddBrake + num3;
		m_dontShiftTimer -= Time.deltaTime;
		if (m_dontShiftTimer < 0f)
		{
			m_dontShiftTimer = 0f;
		}
		m_speedIntervalTimer -= Time.deltaTime;
		m_intervalBound.Encapsulate(position);
		if (m_speedIntervalTimer < 0f)
		{
			m_speedIntervalTimer = 2f;
			if (carX.clutch != 1f && GetMaxBoundsDim(m_intervalBound) < 0.5f)
			{
				m_dontShiftTimer = 2f;
				carX.gear = ((carX.gear < 1) ? 1 : (-1));
			}
			ref Bounds intervalBound = ref m_intervalBound;
			Vector3 min = (m_intervalBound.max = position);
			intervalBound.min = min;
		}
		if (m_dontShiftTimer == 0f)
		{
			if (!flag2 && carX.gear < 1)
			{
				carX.gear = 1;
				if (flag)
				{
					num4 += 1f;
				}
			}
			if (flag2)
			{
				carX.gear = -1;
			}
		}
		if (zero.magnitude > 1f && vRepulse.magnitude > zero.magnitude * 0.5f)
		{
			vRepulse.Normalize();
			vRepulse *= zero.magnitude * 0.5f;
		}
		Vector3 vector7 = -Vector3.Cross(rhs.normalized, vector5 + vRepulse + zero);
		float num5 = vector7.magnitude;
		if (vector7.y < 0f)
		{
			num5 = 0f - num5;
		}
		Vector3 vector8 = quaternion * forward;
		if (flag)
		{
			vector8 = -vector8;
		}
		float num6 = (-Vector3.Cross(vector8, vector3)).y;
		bool flag3 = num6 > 0.3f || num6 < -0.3f;
		if (!flag3)
		{
			if (targRad > 0f && targRad < 1000f)
			{
				num6 += 0.3f;
			}
			if (targRad < 0f && targRad > -1000f)
			{
				num6 -= 0.3f;
			}
		}
		float num7 = num5 * 4f;
		float targAccel = 0f;
		float num8 = 0f;
		bool flag4 = false;
		bool flag5 = false;
		if (curVel > maxVel)
		{
			num8 = 1f;
			flag5 = true;
			flag4 = true;
		}
		if (num4 > 0.01f)
		{
			flag5 = true;
			num8 += num4;
			if (num8 > 1f)
			{
				num8 = 1f;
			}
		}
		float num9 = Mathf.Min(curVel, maxVel) / targRad;
		float num10 = (num - num9) * 2f;
		if (num10 > 2f)
		{
			num7 -= 0.5f;
		}
		if (num10 < -2f)
		{
			num7 += 0.5f;
		}
		if (!flag5 && !flag4)
		{
			targAccel = 1f;
			if (flag3 && curVel > 5f)
			{
				targAccel = 0.5f;
			}
		}
		if (flag)
		{
			num7 = 0f - num7;
		}
		m_targSteer = Mathf.Clamp(num7, -1f, 1f);
		m_targBrake = num8;
		m_targAccel = targAccel;
	}

	private void SteerToTarget2(float targDst, Vector3 targPnt, Vector3 targDir, float targRad)
	{
		Car carX = m_car.carX;
		Vector3 position = carX.getTransform.position;
		_ = carX.velocity;
		Vector3 forward = carX.getTransform.forward;
		Vector3 right = carX.getTransform.right;
		m_car.NeedBrake(out var curVel, out var maxVel);
		Vector3 vector = targPnt + targDir - position;
		vector.Normalize();
		float value = (0f - Vector3.Project(vector, right).magnitude) * Sign2D(vector, forward);
		float targAccel = 0f;
		float targBrake = 0f;
		bool flag = false;
		bool flag2 = false;
		if (curVel > maxVel)
		{
			targBrake = 1f;
			flag2 = true;
			flag = true;
		}
		if (!flag2 && !flag)
		{
			targAccel = 1f;
		}
		m_targSteer = Mathf.Clamp(value, -1f, 1f);
		m_targBrake = targBrake;
		m_targAccel = targAccel;
	}

	private float Sign2D(Vector3 v1, Vector3 v2)
	{
		if (!(v1.z * v2.x > v1.x * v2.z))
		{
			return 1f;
		}
		return -1f;
	}

	private void GetBordersRepulse(Vector3 vTargPnt, float fTargRad, out Vector3 vRepulse, out float fBrake, out bool bNeedRev)
	{
		Car carX = m_car.carX;
		bNeedRev = false;
		fBrake = 0f;
		vRepulse = Vector3.zero;
		float longAccel = carX.longAccel;
		float sideAccel = carX.sideAccel;
		Vector3 position = carX.getTransform.position;
		Vector3 forward = carX.getTransform.forward;
		Vector3 right = carX.getTransform.right;
		Vector3 velocity = carX.velocity;
		float num = Vector3.Dot(forward, velocity);
		Vector3 vector = velocity + forward * longAccel * 0.5f + right * sideAccel * 0.5f;
		Vector3 vector2 = position + velocity * 0.1f + vector * 0.1f;
		bool flag = fTargRad > 0f && fTargRad < 1500f;
		bool flag2 = fTargRad < 0f && fTargRad > -1500f;
		float num2 = 0f;
		if (m_car.positionOnSpline.GetLeftBorderPointAndDirection(vTargPnt, out var borderPoint, out var borderDirection))
		{
			Vector3 vector3 = -Vector3.Cross(borderDirection, Vector3.up);
			bool flag3 = Vector3.Dot(forward, vector3) > 0f;
			bool flag4 = Mathf.Abs(Vector3.Dot(forward, vector3)) > 0.7f;
			Vector3 lhs = (flag3 ? (vector2 - forward) : (vector2 + forward)) - borderPoint;
			lhs.y = 0f;
			float num3 = Vector3.Dot(lhs, vector3) * (2f / 3f);
			if (carX.gear == -1 && !flag3)
			{
				num2 = 1f;
			}
			if (num3 < num2)
			{
				if (flag3)
				{
					if (num < -1f)
					{
						fBrake = 1f;
					}
					else if (carX.gear == -1)
					{
						carX.gear = 1;
					}
				}
				else
				{
					if (carX.gear > 0 && num > 0.5f && flag4)
					{
						fBrake = 1f;
					}
					if ((num < 0.6f && flag4) || (carX.gear == -1 && !flag4))
					{
						bNeedRev = true;
					}
				}
			}
			if (flag && num3 < 0.2f)
			{
				num3 = 0.8f;
				flag = false;
			}
			if (!flag && num3 < 1f)
			{
				vRepulse += vector3 * (1f - num3) * 5f;
			}
		}
		if (m_car.positionOnSpline.GetRightBorderPointAndDirection(vTargPnt, out borderPoint, out borderDirection))
		{
			Vector3 vector4 = Vector3.Cross(borderDirection, Vector3.up);
			bool flag5 = Vector3.Dot(forward, vector4) > 0f;
			bool flag6 = Mathf.Abs(Vector3.Dot(forward, vector4)) > 0.7f;
			Vector3 lhs2 = (flag5 ? (vector2 - forward) : (vector2 + forward)) - borderPoint;
			lhs2.y = 0f;
			float num4 = Vector3.Dot(lhs2, vector4) * (2f / 3f);
			if (carX.gear == -1 && !flag5)
			{
				num2 = 1f;
			}
			if (num4 < num2)
			{
				if (flag5)
				{
					if (num < -1f)
					{
						fBrake = 1f;
					}
					else if (carX.gear == -1)
					{
						carX.gear = 1;
					}
				}
				else
				{
					if (carX.gear > 0 && num > 0.5f && flag6)
					{
						fBrake = 1f;
					}
					if ((num < 0.6f && flag6) || (carX.gear == -1 && !flag6))
					{
						bNeedRev = true;
					}
				}
			}
			if (flag2 && num4 < 0.2f)
			{
				num4 = 0.8f;
				flag2 = false;
			}
			if (!flag2 && num4 < 1f)
			{
				vRepulse += vector4 * (1f - num4) * 5f;
			}
		}
		if (fBrake > 1f)
		{
			fBrake = 1f;
		}
	}

	private void GetObstaclesRepulse(out Vector3 vRepulse, out float fAddBrake)
	{
		fAddBrake = 0f;
		vRepulse = Vector3.zero;
		Vector3 forward = m_car.getTransform.forward;
		GetCarCapsule(m_car, m_predTime, out var p, out var p2, out var rad, 1f, 1f);
		RaceCar[] allCars = m_allCars;
		foreach (RaceCar raceCar in allCars)
		{
			if (raceCar == null || raceCar == m_car)
			{
				continue;
			}
			GetCarCapsule(raceCar, m_predTime, out var p3, out var p4, out var rad2, 1f, 1f);
			float mind = float.MinValue;
			GetEdgeDist(p, p2, p3, p4, out var mina, out var minb, out mind);
			Vector3 vector = mina - minb;
			float num = vector.magnitude / (rad + rad2 + 1.2f) + 0.01f;
			if (num < 1f)
			{
				vector.Normalize();
				Vector3 vector2 = vector / (num * num) * 2f;
				float num2 = Vector3.Dot(vector2, forward);
				vector2 -= forward * num2;
				Vector3 rhs = p4 - p3;
				float magnitude = rhs.magnitude;
				rhs.Normalize();
				if (Vector3.Dot(p2 - p3, rhs) > magnitude)
				{
					vector2 *= 0.1f;
				}
				vRepulse += vector2;
			}
		}
		if (fAddBrake > 1f)
		{
			fAddBrake = 1f;
		}
	}

	private static void GetCarCapsule(RaceCar car, float predTime, out Vector3 p1, out Vector3 p2, out float rad, float mult1, float mult2)
	{
		Vector3 position = car.getTransform.position;
		Vector3 forward = car.getTransform.forward;
		car.CalcPrediction(predTime, out var delta);
		rad = car.width * 0.5f;
		p1 = position - forward * (car.lengthRear + 0.5f) + delta * mult1;
		p2 = position + forward * (car.lengthFront + 0.5f) + delta * mult2;
		p1.y = 0f;
		p2.y = 0f;
	}

	private static void GetEdgeDist(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2, out Vector3 mina, out Vector3 minb, out float mind)
	{
		Vector3 vector = a2 - a1;
		Vector3 vector2 = b2 - b1;
		float magnitude = vector.magnitude;
		float magnitude2 = vector2.magnitude;
		if (magnitude > 1E-05f)
		{
			vector /= magnitude;
		}
		if (magnitude2 > 1E-05f)
		{
			vector2 /= magnitude2;
		}
		Vector3 vector3 = a1;
		float num = Vector3.Dot(a1 - b1, vector2);
		Vector3 vector4 = ((num < 0f) ? b1 : ((!(num > magnitude2)) ? (b1 + vector2 * num) : b2));
		mind = Vector3.SqrMagnitude(vector3 - vector4);
		mina = vector3;
		minb = vector4;
		vector3 = a2;
		num = Vector3.Dot(a2 - b1, vector2);
		vector4 = ((num < 0f) ? b1 : ((!(num > magnitude2)) ? (b1 + vector2 * num) : b2));
		float num2 = Vector3.SqrMagnitude(vector3 - vector4);
		if (num2 < mind)
		{
			mind = num2;
			mina = vector3;
			minb = vector4;
		}
		vector3 = b1;
		num = Vector3.Dot(b1 - a1, vector);
		vector4 = ((num < 0f) ? a1 : ((!(num > magnitude)) ? (a1 + vector * num) : a2));
		num2 = Vector3.SqrMagnitude(vector3 - vector4);
		if (num2 < mind)
		{
			mind = num2;
			mina = vector4;
			minb = vector3;
		}
		vector3 = b2;
		num = Vector3.Dot(b2 - a1, vector);
		vector4 = ((num < 0f) ? a1 : ((!(num > magnitude)) ? (a1 + vector * num) : a2));
		num2 = Vector3.SqrMagnitude(vector3 - vector4);
		if (num2 < mind)
		{
			mind = num2;
			mina = vector4;
			minb = vector3;
		}
		float num3 = Vector3.Dot(vector, vector2);
		float num4 = num3 * num3;
		if (Mathf.Abs(num4 - 1f) > 0.01f)
		{
			num = Vector3.Dot(b1 - a1, vector - vector2 * num3) / (1f - num4);
			if (num > 0f && num < magnitude)
			{
				vector3 = a1 + vector * num;
				float num5 = Vector3.Dot(vector3 - b1, vector2);
				if (num5 > 0f && num5 < magnitude2)
				{
					vector4 = b1 + vector2 * num5;
					num2 = Vector3.SqrMagnitude(vector3 - vector4);
					if (num2 < mind)
					{
						mind = num2;
						mina = vector3;
						minb = vector4;
					}
				}
			}
		}
		mind = Mathf.Sqrt(mind);
	}

	private static void SmoothValue(float vel, float target, ref float current)
	{
		if (current < target)
		{
			current += vel * Time.deltaTime;
			if (current > target)
			{
				current = target;
			}
		}
		else if (current > target)
		{
			current -= vel * Time.deltaTime;
			if (current < target)
			{
				current = target;
			}
		}
	}

	private static float GetMaxBoundsDim(Bounds bounds)
	{
		Vector3 size = bounds.size;
		if (size.x > size.y)
		{
			if (size.x > size.z)
			{
				return size.x;
			}
		}
		else if (size.y > size.z)
		{
			return size.y;
		}
		return size.z;
	}
}
