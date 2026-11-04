using UnityEngine;

public static class AccelerationHelper
{
	private static Vector3 acceleration;

	private static Vector3 lastAcceleration;

	private static float accToleranceXAxis01;

	private static float accToleranceYAxis01;

	private static float accToleranceZAxis01;

	private static float accToleranceXAxis02;

	private static float accToleranceYAxis02;

	private static float accToleranceZAxis02;

	public static Vector3 Acceleration => acceleration;

	public static Vector3 LastAcceleration => lastAcceleration;

	static AccelerationHelper()
	{
		accToleranceXAxis01 = 0.03f;
		accToleranceYAxis01 = 0.03f;
		accToleranceZAxis01 = 0.03f;
		accToleranceXAxis02 = 0.06f;
		accToleranceYAxis02 = 0.06f;
		accToleranceZAxis02 = 0.06f;
		acceleration = Vector3.zero;
		lastAcceleration = Vector3.zero;
	}

	public static void OnUpdate()
	{
		lastAcceleration = acceleration;
		acceleration = CorrectRawAcceleration(lastAcceleration, Input.acceleration);
	}

	private static float Norm(float x, float y, float z)
	{
		return Mathf.Sqrt(x * x + y * y + z * z);
	}

	public static Vector3 LowPassFilter(Vector3 lastAcc, Vector3 currentAcc, float lowPassFilteringFactor)
	{
		currentAcc.x = currentAcc.x * lowPassFilteringFactor + lastAcc.x * (1f - lowPassFilteringFactor);
		currentAcc.y = currentAcc.y * lowPassFilteringFactor + lastAcc.y * (1f - lowPassFilteringFactor);
		currentAcc.z = currentAcc.z * lowPassFilteringFactor + lastAcc.z * (1f - lowPassFilteringFactor);
		return currentAcc;
	}

	public static Vector3 LowPassFilterAdaptive(Vector3 lastAcc, Vector3 currentAcc, float frequency, float cutoffFrequency, bool adaptive)
	{
		float num = 1f / frequency;
		float num2 = 1f / cutoffFrequency;
		float num3 = num / (num + num2);
		float num4 = num3;
		if (adaptive)
		{
			float num5 = 0.02f;
			float num6 = 3f;
			float num7 = Mathf.Clamp(Mathf.Abs(Norm(lastAcc.x, lastAcc.y, lastAcc.z) - Norm(currentAcc.x, currentAcc.y, currentAcc.z)) / num5 - 1f, 0f, 1f);
			num4 = (1f - num7) * num3 / num6 + num7 * num3;
		}
		currentAcc.x = currentAcc.x * num4 + lastAcc.x * (1f - num4);
		currentAcc.y = currentAcc.y * num4 + lastAcc.y * (1f - num4);
		currentAcc.z = currentAcc.z * num4 + lastAcc.z * (1f - num4);
		return currentAcc;
	}

	public static Vector3 HighPassFilter(Vector3 lastAcc, Vector3 currentAcc, float highPassFilteringsFactor)
	{
		currentAcc.x = lastAcc.x - (lastAcc.x * highPassFilteringsFactor + currentAcc.x * (1f - highPassFilteringsFactor));
		currentAcc.y = lastAcc.y - (lastAcc.y * highPassFilteringsFactor + currentAcc.y * (1f - highPassFilteringsFactor));
		currentAcc.z = lastAcc.z - (lastAcc.z * highPassFilteringsFactor + currentAcc.z * (1f - highPassFilteringsFactor));
		return currentAcc;
	}

	public static Vector3 HighPassFilterAdaptive(Vector3 lastAcc, Vector3 currentAcc, float frequency, float cutoffFrequency, bool adaptive)
	{
		float num = 1f / frequency;
		float num2 = 1f / cutoffFrequency;
		float num3 = num / (num + num2);
		float num4 = num3;
		if (adaptive)
		{
			float num5 = 0.02f;
			float num6 = 3f;
			float num7 = Mathf.Clamp(Mathf.Abs(Norm(lastAcc.x, lastAcc.y, lastAcc.z) - Norm(currentAcc.x, currentAcc.y, currentAcc.z)) / num5 - 1f, 0f, 1f);
			num4 = num7 * num3 / num6 + (1f - num7) * num3;
		}
		currentAcc.x = num4 * (currentAcc.x - lastAcc.x);
		currentAcc.y = num4 * (currentAcc.y - lastAcc.y);
		currentAcc.z = num4 * (currentAcc.z - lastAcc.z);
		return currentAcc;
	}

	private static Vector3 CorrectRawAcceleration(Vector3 lastAcc, Vector3 currentAcc)
	{
		float num = Mathf.Abs(currentAcc.x - lastAcc.x);
		float num2 = Mathf.Abs(currentAcc.y - lastAcc.y);
		float num3 = Mathf.Abs(currentAcc.z - lastAcc.z);
		accToleranceXAxis01 = ((num > 0f && num <= accToleranceXAxis01) ? num : accToleranceXAxis01);
		accToleranceXAxis02 = ((num > accToleranceXAxis01 && num <= accToleranceXAxis02) ? num : accToleranceXAxis02);
		accToleranceYAxis01 = ((num2 > 0f && num2 <= accToleranceYAxis01) ? num2 : accToleranceYAxis01);
		accToleranceYAxis02 = ((num2 > accToleranceYAxis01 && num2 <= accToleranceYAxis02) ? num2 : accToleranceYAxis02);
		accToleranceZAxis01 = ((num3 > 0f && num3 <= accToleranceZAxis01) ? num3 : accToleranceZAxis01);
		accToleranceZAxis02 = ((num3 > accToleranceZAxis01 && num3 <= accToleranceZAxis02) ? num3 : accToleranceZAxis02);
		if (accToleranceXAxis01 == num)
		{
			currentAcc.x = ((currentAcc.x > 0f) ? (currentAcc.x -= accToleranceXAxis01) : (currentAcc.x += accToleranceXAxis01));
		}
		if (accToleranceXAxis02 == num)
		{
			currentAcc.x = ((currentAcc.x > 0f) ? (currentAcc.x -= accToleranceXAxis02) : (currentAcc.x += accToleranceXAxis02));
		}
		if (accToleranceYAxis01 == num2)
		{
			currentAcc.y = ((currentAcc.y > 0f) ? (currentAcc.y -= accToleranceYAxis01) : (currentAcc.y += accToleranceYAxis01));
		}
		if (accToleranceYAxis02 == num2)
		{
			currentAcc.y = ((currentAcc.y > 0f) ? (currentAcc.y -= accToleranceYAxis02) : (currentAcc.y += accToleranceYAxis02));
		}
		if (accToleranceZAxis01 == num3)
		{
			currentAcc.z = ((currentAcc.z > 0f) ? (currentAcc.z -= accToleranceZAxis01) : (currentAcc.z += accToleranceZAxis01));
		}
		if (accToleranceZAxis02 == num3)
		{
			currentAcc.z = ((currentAcc.z > 0f) ? (currentAcc.z -= accToleranceZAxis02) : (currentAcc.z += accToleranceZAxis02));
		}
		return currentAcc;
	}
}
