using System.IO;
using UnityEngine;

public class Vector3ReplayStream : BaseReplayStream<Vector3>
{
	private const float ZERO_MAGNITUDE = 1E-08f;

	protected override InterpType GetInterpType(float time, Vector3 value, KeyFrame[] buf, int ix, int n, InterpType minType, float epsilon)
	{
		InterpType interpType = InterpType.None;
		KeyFrame keyFrame = buf[ix];
		if (Vector3.SqrMagnitude(keyFrame.value - value) < epsilon * epsilon)
		{
			bool flag = value.sqrMagnitude < 1E-08f;
			if (!(keyFrame.value.sqrMagnitude < 1E-08f) && flag)
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
		if (Vector3.SqrMagnitude(Vector3.Lerp(keyFrame2.value, keyFrame.value, a) - value) < epsilon * epsilon)
		{
			interpType = InterpType.Linear;
		}
		if (n < 3 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		KeyFrame keyFrame3 = buf[--ix];
		CoefsParab(keyFrame3.time, keyFrame2.time, keyFrame.time, time, out a, out var b);
		if (Vector3.SqrMagnitude(Vector3.Lerp(Vector3.Lerp(keyFrame3.value, keyFrame2.value, b / (1f - a)), keyFrame.value, a) - value) < epsilon * epsilon)
		{
			interpType = InterpType.Parab;
		}
		if (n < 4 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		KeyFrame keyFrame4 = buf[--ix];
		CoefsCubic(keyFrame4.time, keyFrame3.time, keyFrame2.time, keyFrame.time, time, out a, out b, out var g);
		if (Vector3.SqrMagnitude(Vector3.Lerp(Vector3.Lerp(Vector3.Lerp(keyFrame4.value, keyFrame3.value, g / (1f - a - b)), keyFrame2.value, b / (1f - a)), keyFrame.value, a) - value) < epsilon * epsilon)
		{
			interpType = InterpType.Cubic;
		}
		return interpType;
	}

	protected override Vector3 Lerp(Vector3 from, Vector3 to, float t)
	{
		return Vector3.Lerp(from, to, t);
	}

	protected override void SerializeValue(BinaryWriter writer, Vector3 value)
	{
		writer.Write(value.x);
		writer.Write(value.y);
		writer.Write(value.z);
	}

	protected override Vector3 DeserializeValue(BinaryReader reader)
	{
		return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
	}
}
