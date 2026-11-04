using System.IO;
using UnityEngine;

public class FloatReplayStream : BaseReplayStream<float>
{
	private const float ZERO_MAGNITUDE = 0.0001f;

	protected override InterpType GetInterpType(float time, float value, KeyFrame[] buf, int ix, int n, InterpType minType, float epsilon)
	{
		InterpType interpType = InterpType.None;
		KeyFrame keyFrame = buf[ix];
		if (Mathf.Abs(keyFrame.value - value) < epsilon)
		{
			if (Mathf.Abs(keyFrame.value) > 0.0001f && Mathf.Abs(value) < 0.0001f)
			{
				return interpType;
			}
			interpType = InterpType.Const;
		}
		if (n < 2 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		KeyFrame keyFrame2 = buf[--ix];
		CoefsLinear(keyFrame2.time, keyFrame.time, time, out var a);
		if (Mathf.Abs(Mathf.Lerp(keyFrame2.value, keyFrame.value, a) - value) < epsilon)
		{
			interpType = InterpType.Linear;
		}
		if (n < 3 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		KeyFrame keyFrame3 = buf[--ix];
		CoefsParab(keyFrame3.time, keyFrame2.time, keyFrame.time, time, out a, out var b);
		if (Mathf.Abs(Mathf.Lerp(Mathf.Lerp(keyFrame3.value, keyFrame2.value, b / (1f - a)), keyFrame.value, a) - value) < epsilon)
		{
			interpType = InterpType.Parab;
		}
		if (n < 4 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		KeyFrame keyFrame4 = buf[--ix];
		CoefsCubic(keyFrame4.time, keyFrame3.time, keyFrame2.time, keyFrame.time, time, out a, out b, out var g);
		if (Mathf.Abs(Mathf.Lerp(Mathf.Lerp(Mathf.Lerp(keyFrame4.value, keyFrame3.value, g / (1f - a - b)), keyFrame2.value, b / (1f - a)), keyFrame.value, a) - value) < epsilon)
		{
			interpType = InterpType.Cubic;
		}
		return interpType;
	}

	protected override float Lerp(float from, float to, float t)
	{
		return Mathf.Lerp(from, to, t);
	}

	protected override void SerializeValue(BinaryWriter writer, float value)
	{
		writer.Write(value);
	}

	protected override float DeserializeValue(BinaryReader reader)
	{
		return reader.ReadSingle();
	}
}
