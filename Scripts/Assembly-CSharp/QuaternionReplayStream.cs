using System.IO;
using UnityEngine;

public class QuaternionReplayStream : BaseReplayStream<Quaternion>
{
	protected override InterpType GetInterpType(float time, Quaternion value, KeyFrame[] buf, int ix, int n, InterpType minType, float epsilon)
	{
		InterpType interpType = InterpType.None;
		KeyFrame keyFrame = buf[ix];
		if (QuatDiffSqrMagnitude(keyFrame.value, value) < epsilon * epsilon)
		{
			interpType = InterpType.Const;
		}
		if (n < 2 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		KeyFrame keyFrame2 = buf[--ix];
		CoefsLinear(keyFrame2.time, keyFrame.time, time, out var a);
		if (QuatDiffSqrMagnitude(Quaternion.Slerp(keyFrame2.value, keyFrame.value, a), value) < epsilon * epsilon)
		{
			interpType = InterpType.Linear;
		}
		if (n < 3 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		KeyFrame keyFrame3 = buf[--ix];
		CoefsParab(keyFrame3.time, keyFrame2.time, keyFrame.time, time, out a, out var b);
		if (QuatDiffSqrMagnitude(Quaternion.Slerp(Quaternion.Slerp(keyFrame3.value, keyFrame2.value, b / (1f - a)), keyFrame.value, a), value) < epsilon * epsilon)
		{
			interpType = InterpType.Parab;
		}
		if (n < 4 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		KeyFrame keyFrame4 = buf[--ix];
		CoefsCubic(keyFrame4.time, keyFrame3.time, keyFrame2.time, keyFrame.time, time, out a, out b, out var g);
		if (QuatDiffSqrMagnitude(Quaternion.Slerp(Quaternion.Slerp(Quaternion.Slerp(keyFrame4.value, keyFrame3.value, g / (1f - a - b)), keyFrame2.value, b / (1f - a)), keyFrame.value, a), value) < epsilon * epsilon)
		{
			interpType = InterpType.Cubic;
		}
		return interpType;
	}

	protected override Quaternion Lerp(Quaternion from, Quaternion to, float t)
	{
		return Quaternion.Slerp(from, to, t);
	}

	protected override void SerializeValue(BinaryWriter writer, Quaternion value)
	{
		writer.Write(value.x);
		writer.Write(value.y);
		writer.Write(value.z);
		writer.Write(value.w);
	}

	protected override Quaternion DeserializeValue(BinaryReader reader)
	{
		return new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
	}

	private static float QuatDiffSqrMagnitude(Quaternion q1, Quaternion q2)
	{
		float num = q1.x - q2.x;
		float num2 = q1.y - q2.y;
		float num3 = q1.z - q2.z;
		float num4 = q1.w - q2.w;
		return num * num + num2 * num2 + num3 * num3 + num4 * num4;
	}
}
