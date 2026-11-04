using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ReplayStream
{
	private enum InterpType : byte
	{
		None = 0,
		Const = 1,
		Linear = 2,
		Parab = 3,
		Cubic = 4,
		Temp = 5
	}

	private struct FloatKey
	{
		public float time;

		public float value;

		public InterpType type;
	}

	private struct Vec3Key
	{
		public float time;

		public Vector3 value;

		public InterpType type;
	}

	private struct QuatKey
	{
		public float time;

		public Quaternion value;

		public InterpType type;
	}

	private Dictionary<int, FastList<FloatKey>> m_floatKeysMap = new Dictionary<int, FastList<FloatKey>>();

	private FastList<Vec3Key> m_vec3Keys = new FastList<Vec3Key>();

	private FastList<QuatKey> m_quatKeys = new FastList<QuatKey>();

	public void Clear()
	{
		m_floatKeysMap.Clear();
		m_vec3Keys.Clear();
		m_quatKeys.Clear();
	}

	public void Release()
	{
		m_floatKeysMap.Clear();
		m_vec3Keys.Release();
		m_quatKeys.Release();
	}

	public bool IsEmpty()
	{
		if (m_floatKeysMap.Count == 0 && m_vec3Keys.size == 0)
		{
			return m_quatKeys.size == 0;
		}
		return false;
	}

	public void WriteFloat(int id, float time, float value, float epsilon = 0.001f)
	{
		if (!m_floatKeysMap.TryGetValue(id, out var value2))
		{
			value2 = new FastList<FloatKey>();
			m_floatKeysMap.Add(id, value2);
		}
		FloatKey[] buffer = value2.buffer;
		int size = value2.size;
		if (size < 1)
		{
			value2.Add(new FloatKey
			{
				time = time,
				value = value,
				type = InterpType.None
			});
			return;
		}
		int num = size - 1;
		if (time <= buffer[num].time + 0.001f)
		{
			return;
		}
		InterpType floatInterpType;
		if (buffer[num].type == InterpType.Temp)
		{
			int num2 = num--;
			floatInterpType = GetFloatInterpType(time, value, buffer, num, size - 1, buffer[num].type, epsilon);
			if ((int)floatInterpType >= (int)buffer[num].type)
			{
				buffer[num2] = new FloatKey
				{
					time = time,
					value = value,
					type = InterpType.Temp
				};
				FloatKey floatKey = buffer[num];
				floatKey.type = floatInterpType;
				buffer[num] = floatKey;
				return;
			}
			FloatKey value3 = buffer[num2];
			value3.type = InterpType.None;
			value2[num2] = value3;
			num = num2;
		}
		floatInterpType = GetFloatInterpType(time, value, buffer, num, size, InterpType.None, epsilon);
		if (floatInterpType == InterpType.None)
		{
			FloatKey floatKey2 = buffer[num];
			floatKey2.type = InterpType.Const;
			buffer[num] = floatKey2;
			value2.Add(new FloatKey
			{
				time = time,
				value = value,
				type = InterpType.None
			});
		}
		else
		{
			FloatKey floatKey3 = buffer[num];
			floatKey3.type = floatInterpType;
			buffer[num] = floatKey3;
			value2.Add(new FloatKey
			{
				time = time,
				value = value,
				type = InterpType.Temp
			});
		}
	}

	public void WriteVector3(float time, Vector3 value, float epsilon = 0.001f)
	{
		Vec3Key[] buffer = m_vec3Keys.buffer;
		int size = m_vec3Keys.size;
		if (size < 1)
		{
			m_vec3Keys.Add(new Vec3Key
			{
				time = time,
				value = value,
				type = InterpType.None
			});
			return;
		}
		int num = size - 1;
		if (time <= buffer[num].time + 0.001f)
		{
			return;
		}
		InterpType vec3InterpType;
		if (buffer[num].type == InterpType.Temp)
		{
			int num2 = num--;
			vec3InterpType = GetVec3InterpType(time, value, buffer, num, size - 1, buffer[num].type, epsilon);
			if ((int)vec3InterpType >= (int)buffer[num].type)
			{
				buffer[num2] = new Vec3Key
				{
					time = time,
					value = value,
					type = InterpType.Temp
				};
				Vec3Key vec3Key = buffer[num];
				vec3Key.type = vec3InterpType;
				buffer[num] = vec3Key;
				return;
			}
			Vec3Key vec3Key2 = buffer[num2];
			vec3Key2.type = InterpType.None;
			buffer[num2] = vec3Key2;
			num = num2;
		}
		vec3InterpType = GetVec3InterpType(time, value, buffer, num, size, InterpType.None, epsilon);
		if (vec3InterpType == InterpType.None)
		{
			Vec3Key vec3Key3 = buffer[num];
			vec3Key3.type = InterpType.Const;
			buffer[num] = vec3Key3;
			m_vec3Keys.Add(new Vec3Key
			{
				time = time,
				value = value,
				type = InterpType.None
			});
		}
		else
		{
			Vec3Key vec3Key4 = buffer[num];
			vec3Key4.type = vec3InterpType;
			buffer[num] = vec3Key4;
			m_vec3Keys.Add(new Vec3Key
			{
				time = time,
				value = value,
				type = InterpType.Temp
			});
		}
	}

	public void WriteQuaternion(float time, Quaternion value, float epsilon = 0.001f)
	{
		QuatKey[] buffer = m_quatKeys.buffer;
		int size = m_quatKeys.size;
		if (size < 1)
		{
			m_quatKeys.Add(new QuatKey
			{
				time = time,
				value = value,
				type = InterpType.None
			});
			return;
		}
		int num = size - 1;
		if (time <= buffer[num].time + 0.001f)
		{
			return;
		}
		InterpType quatInterpType;
		if (buffer[num].type == InterpType.Temp)
		{
			int num2 = num--;
			quatInterpType = GetQuatInterpType(time, value, buffer, num, size - 1, buffer[num].type, epsilon);
			if ((int)quatInterpType >= (int)buffer[num].type)
			{
				buffer[num2] = new QuatKey
				{
					time = time,
					value = value,
					type = InterpType.Temp
				};
				QuatKey quatKey = buffer[num];
				quatKey.type = quatInterpType;
				buffer[num] = quatKey;
				return;
			}
			QuatKey quatKey2 = buffer[num2];
			quatKey2.type = InterpType.None;
			buffer[num2] = quatKey2;
			num = num2;
		}
		quatInterpType = GetQuatInterpType(time, value, buffer, num, size, InterpType.None, epsilon);
		if (quatInterpType == InterpType.None)
		{
			QuatKey quatKey3 = buffer[num];
			quatKey3.type = InterpType.Const;
			buffer[num] = quatKey3;
			m_quatKeys.Add(new QuatKey
			{
				time = time,
				value = value,
				type = InterpType.None
			});
		}
		else
		{
			QuatKey quatKey4 = buffer[num];
			quatKey4.type = quatInterpType;
			buffer[num] = quatKey4;
			m_quatKeys.Add(new QuatKey
			{
				time = time,
				value = value,
				type = InterpType.Temp
			});
		}
	}

	public float ReadFloat(int id, float time, float defaultValue)
	{
		if (!m_floatKeysMap.TryGetValue(id, out var value))
		{
			return defaultValue;
		}
		int num = 0;
		int num2 = value.size - 1;
		FloatKey[] buffer = value.buffer;
		if (time <= buffer[num].time)
		{
			return buffer[num].value;
		}
		if (time >= buffer[num2].time)
		{
			return buffer[num2].value;
		}
		int num3 = 0;
		while (num2 - num > 1)
		{
			num3 = num + num2 >> 1;
			if (time > buffer[num3].time)
			{
				num = num3;
			}
			else
			{
				num2 = num3;
			}
		}
		if (Mathf.Abs(buffer[num].time - time) < 0.001f)
		{
			return buffer[num].value;
		}
		if (Mathf.Abs(buffer[num2].time - time) < 0.001f)
		{
			return buffer[num2].value;
		}
		num3 = num2;
		float a;
		float b;
		switch (buffer[num].type)
		{
		case InterpType.Const:
		case InterpType.Linear:
		{
			FloatKey floatKey = buffer[num3];
			FloatKey floatKey2 = buffer[num3 - 1];
			CoefsLinear(floatKey2.time, floatKey.time, time, out a);
			return Mathf.Lerp(floatKey2.value, floatKey.value, a);
		}
		case InterpType.Parab:
		{
			FloatKey floatKey = buffer[num3];
			FloatKey floatKey2 = buffer[num3 - 1];
			FloatKey floatKey3 = buffer[num3 - 2];
			CoefsParab(floatKey3.time, floatKey2.time, floatKey.time, time, out a, out b);
			return Mathf.Lerp(Mathf.Lerp(floatKey3.value, floatKey2.value, b / (1f - a)), floatKey.value, a);
		}
		case InterpType.Cubic:
		{
			FloatKey floatKey = buffer[num3];
			FloatKey floatKey2 = buffer[num3 - 1];
			FloatKey floatKey3 = buffer[num3 - 2];
			FloatKey floatKey4 = buffer[num3 - 3];
			CoefsCubic(floatKey4.time, floatKey3.time, floatKey2.time, floatKey.time, time, out a, out b, out var g);
			return Mathf.Lerp(Mathf.Lerp(Mathf.Lerp(floatKey4.value, floatKey3.value, g / (1f - a - b)), floatKey2.value, b / (1f - a)), floatKey.value, a);
		}
		default:
			return defaultValue;
		}
	}

	public Vector3 ReadVector3(float time, Vector3 defaultValue)
	{
		int num = 0;
		int num2 = m_vec3Keys.size - 1;
		Vec3Key[] buffer = m_vec3Keys.buffer;
		if (time <= buffer[num].time)
		{
			return buffer[num].value;
		}
		if (time >= buffer[num2].time)
		{
			return buffer[num2].value;
		}
		int num3 = 0;
		while (num2 - num > 1)
		{
			num3 = num + num2 >> 1;
			if (time > buffer[num3].time)
			{
				num = num3;
			}
			else
			{
				num2 = num3;
			}
		}
		if (Mathf.Abs(buffer[num].time - time) <= 0.001f)
		{
			return buffer[num].value;
		}
		if (Mathf.Abs(buffer[num2].time - time) <= 0.001f)
		{
			return buffer[num2].value;
		}
		num3 = num2;
		float a;
		float b;
		switch (buffer[num].type)
		{
		case InterpType.Const:
		case InterpType.Linear:
		{
			Vec3Key vec3Key = buffer[num3];
			Vec3Key vec3Key2 = buffer[num3 - 1];
			CoefsLinear(vec3Key2.time, vec3Key.time, time, out a);
			return Vector3.Lerp(vec3Key2.value, vec3Key.value, a);
		}
		case InterpType.Parab:
		{
			Vec3Key vec3Key = buffer[num3];
			Vec3Key vec3Key2 = buffer[num3 - 1];
			Vec3Key vec3Key3 = buffer[num3 - 2];
			CoefsParab(vec3Key3.time, vec3Key2.time, vec3Key.time, time, out a, out b);
			return Vector3.Lerp(Vector3.Lerp(vec3Key3.value, vec3Key2.value, b / (1f - a)), vec3Key.value, a);
		}
		case InterpType.Cubic:
		{
			Vec3Key vec3Key = buffer[num3];
			Vec3Key vec3Key2 = buffer[num3 - 1];
			Vec3Key vec3Key3 = buffer[num3 - 2];
			Vec3Key vec3Key4 = buffer[num3 - 3];
			CoefsCubic(vec3Key4.time, vec3Key3.time, vec3Key2.time, vec3Key.time, time, out a, out b, out var g);
			return Vector3.Lerp(Vector3.Lerp(Vector3.Lerp(vec3Key4.value, vec3Key3.value, g / (1f - a - b)), vec3Key2.value, b / (1f - a)), vec3Key.value, a);
		}
		default:
			return defaultValue;
		}
	}

	public Quaternion ReadQuaternion(float time, Quaternion defaultValue)
	{
		int num = 0;
		int num2 = m_quatKeys.size - 1;
		QuatKey[] buffer = m_quatKeys.buffer;
		if (time <= buffer[num].time)
		{
			return buffer[num].value;
		}
		if (time >= buffer[num2].time)
		{
			return buffer[num2].value;
		}
		int num3 = 0;
		while (num2 - num > 1)
		{
			num3 = num + num2 >> 1;
			if (time > buffer[num3].time)
			{
				num = num3;
			}
			else
			{
				num2 = num3;
			}
		}
		if (Mathf.Abs(buffer[num].time - time) <= 0.001f)
		{
			return buffer[num].value;
		}
		if (Mathf.Abs(buffer[num2].time - time) <= 0.001f)
		{
			return buffer[num2].value;
		}
		num3 = num2;
		float a;
		float b;
		switch (buffer[num].type)
		{
		case InterpType.Const:
		case InterpType.Linear:
		{
			QuatKey quatKey = buffer[num3];
			QuatKey quatKey2 = buffer[num3 - 1];
			CoefsLinear(quatKey2.time, quatKey.time, time, out a);
			return Quaternion.Slerp(quatKey2.value, quatKey.value, a);
		}
		case InterpType.Parab:
		{
			QuatKey quatKey = buffer[num3];
			QuatKey quatKey2 = buffer[num3 - 1];
			QuatKey quatKey3 = buffer[num3 - 2];
			CoefsParab(quatKey3.time, quatKey2.time, quatKey.time, time, out a, out b);
			return Quaternion.Slerp(Quaternion.Slerp(quatKey3.value, quatKey2.value, b / (1f - a)), quatKey.value, a);
		}
		case InterpType.Cubic:
		{
			QuatKey quatKey = buffer[num3];
			QuatKey quatKey2 = buffer[num3 - 1];
			QuatKey quatKey3 = buffer[num3 - 2];
			QuatKey quatKey4 = buffer[num3 - 3];
			CoefsCubic(quatKey4.time, quatKey3.time, quatKey2.time, quatKey.time, time, out a, out b, out var g);
			return Quaternion.Slerp(Quaternion.Slerp(Quaternion.Slerp(quatKey4.value, quatKey3.value, g / (1f - a - b)), quatKey2.value, b / (1f - a)), quatKey.value, a);
		}
		default:
			return defaultValue;
		}
	}

	public void GetTimeInterval(out float beg, out float end)
	{
		beg = float.MaxValue;
		end = float.MinValue;
		for (int i = 0; i < m_vec3Keys.size; i++)
		{
			Vec3Key vec3Key = m_vec3Keys.buffer[i];
			if (vec3Key.time < beg)
			{
				beg = vec3Key.time;
			}
			if (vec3Key.time > end)
			{
				end = vec3Key.time;
			}
		}
		for (int j = 0; j < m_quatKeys.size; j++)
		{
			QuatKey quatKey = m_quatKeys.buffer[j];
			if (quatKey.time < beg)
			{
				beg = quatKey.time;
			}
			if (quatKey.time > end)
			{
				end = quatKey.time;
			}
		}
		foreach (KeyValuePair<int, FastList<FloatKey>> item in m_floatKeysMap)
		{
			for (int k = 0; k < item.Value.size; k++)
			{
				FloatKey floatKey = item.Value[k];
				if (floatKey.time < beg)
				{
					beg = floatKey.time;
				}
				if (floatKey.time > end)
				{
					end = floatKey.time;
				}
			}
		}
		if (beg > end)
		{
			beg = 0f;
			end = 0f;
		}
	}

	public void Serialize(BinaryWriter writer)
	{
		writer.Write(m_floatKeysMap.Count);
		foreach (KeyValuePair<int, FastList<FloatKey>> item in m_floatKeysMap)
		{
			writer.Write(item.Key);
			writer.Write(item.Value.size);
			foreach (FloatKey item2 in item.Value)
			{
				writer.Write(item2.time);
				writer.Write(item2.value);
				writer.Write((byte)item2.type);
			}
		}
		writer.Write(m_vec3Keys.size);
		foreach (Vec3Key vec3Key in m_vec3Keys)
		{
			writer.Write(vec3Key.time);
			writer.Write(vec3Key.value.x);
			writer.Write(vec3Key.value.y);
			writer.Write(vec3Key.value.z);
			writer.Write((byte)vec3Key.type);
		}
		writer.Write(m_quatKeys.size);
		foreach (QuatKey quatKey in m_quatKeys)
		{
			writer.Write(quatKey.time);
			writer.Write(quatKey.value.x);
			writer.Write(quatKey.value.y);
			writer.Write(quatKey.value.z);
			writer.Write(quatKey.value.w);
			writer.Write((byte)quatKey.type);
		}
	}

	public void Deserialize(BinaryReader reader)
	{
		int num = reader.ReadInt32();
		m_floatKeysMap = new Dictionary<int, FastList<FloatKey>>();
		for (int i = 0; i < num; i++)
		{
			int key = reader.ReadInt32();
			int num2 = reader.ReadInt32();
			FastList<FloatKey> fastList = new FastList<FloatKey>();
			for (int j = 0; j < num2; j++)
			{
				fastList.Add(new FloatKey
				{
					time = reader.ReadSingle(),
					value = reader.ReadSingle(),
					type = (InterpType)reader.ReadByte()
				});
			}
			m_floatKeysMap.Add(key, fastList);
		}
		num = reader.ReadInt32();
		m_vec3Keys = new FastList<Vec3Key>();
		for (int k = 0; k < num; k++)
		{
			Vec3Key item = new Vec3Key
			{
				time = reader.ReadSingle(),
				value = 
				{
					x = reader.ReadSingle(),
					y = reader.ReadSingle(),
					z = reader.ReadSingle()
				},
				type = (InterpType)reader.ReadByte()
			};
			m_vec3Keys.Add(item);
		}
		num = reader.ReadInt32();
		m_quatKeys = new FastList<QuatKey>();
		for (int l = 0; l < num; l++)
		{
			QuatKey item2 = new QuatKey
			{
				time = reader.ReadSingle(),
				value = 
				{
					x = reader.ReadSingle(),
					y = reader.ReadSingle(),
					z = reader.ReadSingle(),
					w = reader.ReadSingle()
				},
				type = (InterpType)reader.ReadByte()
			};
			m_quatKeys.Add(item2);
		}
	}

	private static InterpType GetFloatInterpType(float time, float value, FloatKey[] buf, int ix, int n, InterpType minType, float epsilon)
	{
		InterpType interpType = InterpType.None;
		FloatKey floatKey = buf[ix];
		if (Mathf.Abs(floatKey.value - value) < epsilon)
		{
			interpType = InterpType.Const;
		}
		if (n < 2 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		FloatKey floatKey2 = buf[--ix];
		CoefsLinear(floatKey2.time, floatKey.time, time, out var a);
		if (Mathf.Abs(Mathf.Lerp(floatKey2.value, floatKey.value, a) - value) < epsilon)
		{
			interpType = InterpType.Linear;
		}
		if (n < 3 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		FloatKey floatKey3 = buf[--ix];
		CoefsParab(floatKey3.time, floatKey2.time, floatKey.time, time, out a, out var b);
		if (Mathf.Abs(Mathf.Lerp(Mathf.Lerp(floatKey3.value, floatKey2.value, b / (1f - a)), floatKey.value, a) - value) < epsilon)
		{
			interpType = InterpType.Parab;
		}
		if (n < 4 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		FloatKey floatKey4 = buf[--ix];
		CoefsCubic(floatKey4.time, floatKey3.time, floatKey2.time, floatKey.time, time, out a, out b, out var g);
		if (Mathf.Abs(Mathf.Lerp(Mathf.Lerp(Mathf.Lerp(floatKey4.value, floatKey3.value, g / (1f - a - b)), floatKey2.value, b / (1f - a)), floatKey.value, a) - value) < epsilon)
		{
			interpType = InterpType.Cubic;
		}
		return interpType;
	}

	private static InterpType GetVec3InterpType(float time, Vector3 value, Vec3Key[] buf, int ix, int n, InterpType minType, float epsilon)
	{
		InterpType interpType = InterpType.None;
		Vec3Key vec3Key = buf[ix];
		if (Vector3.SqrMagnitude(vec3Key.value - value) < epsilon * epsilon)
		{
			interpType = InterpType.Const;
		}
		if (n < 2 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		Vec3Key vec3Key2 = buf[--ix];
		CoefsLinear(vec3Key2.time, vec3Key.time, time, out var a);
		if (Vector3.SqrMagnitude(Vector3.Lerp(vec3Key2.value, vec3Key.value, a) - value) < epsilon * epsilon)
		{
			interpType = InterpType.Linear;
		}
		if (n < 3 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		Vec3Key vec3Key3 = buf[--ix];
		CoefsParab(vec3Key3.time, vec3Key2.time, vec3Key.time, time, out a, out var b);
		if (Vector3.SqrMagnitude(Vector3.Lerp(Vector3.Lerp(vec3Key3.value, vec3Key2.value, b / (1f - a)), vec3Key.value, a) - value) < epsilon * epsilon)
		{
			interpType = InterpType.Parab;
		}
		if (n < 4 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		Vec3Key vec3Key4 = buf[--ix];
		CoefsCubic(vec3Key4.time, vec3Key3.time, vec3Key2.time, vec3Key.time, time, out a, out b, out var g);
		if (Vector3.SqrMagnitude(Vector3.Lerp(Vector3.Lerp(Vector3.Lerp(vec3Key4.value, vec3Key3.value, g / (1f - a - b)), vec3Key2.value, b / (1f - a)), vec3Key.value, a) - value) < epsilon * epsilon)
		{
			interpType = InterpType.Cubic;
		}
		return interpType;
	}

	private static InterpType GetQuatInterpType(float time, Quaternion value, QuatKey[] buf, int ix, int n, InterpType minType, float epsilon)
	{
		InterpType interpType = InterpType.None;
		QuatKey quatKey = buf[ix];
		if (QuatDiffSqrMagnitude(quatKey.value, value) < epsilon * epsilon)
		{
			interpType = InterpType.Const;
		}
		if (n < 2 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		QuatKey quatKey2 = buf[--ix];
		CoefsLinear(quatKey2.time, quatKey.time, time, out var a);
		if (QuatDiffSqrMagnitude(Quaternion.Slerp(quatKey2.value, quatKey.value, a), value) < epsilon * epsilon)
		{
			interpType = InterpType.Linear;
		}
		if (n < 3 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		QuatKey quatKey3 = buf[--ix];
		CoefsParab(quatKey3.time, quatKey2.time, quatKey.time, time, out a, out var b);
		if (QuatDiffSqrMagnitude(Quaternion.Slerp(Quaternion.Slerp(quatKey3.value, quatKey2.value, b / (1f - a)), quatKey.value, a), value) < epsilon * epsilon)
		{
			interpType = InterpType.Parab;
		}
		if (n < 4 || (int)interpType >= (int)minType)
		{
			return interpType;
		}
		QuatKey quatKey4 = buf[--ix];
		CoefsCubic(quatKey4.time, quatKey3.time, quatKey2.time, quatKey.time, time, out a, out b, out var g);
		if (QuatDiffSqrMagnitude(Quaternion.Slerp(Quaternion.Slerp(Quaternion.Slerp(quatKey4.value, quatKey3.value, g / (1f - a - b)), quatKey2.value, b / (1f - a)), quatKey.value, a), value) < epsilon * epsilon)
		{
			interpType = InterpType.Cubic;
		}
		return interpType;
	}

	private static float QuatDiffSqrMagnitude(Quaternion q1, Quaternion q2)
	{
		float num = q1.x - q2.x;
		float num2 = q1.y - q2.y;
		float num3 = q1.z - q2.z;
		float num4 = q1.w - q2.w;
		return num * num + num2 * num2 + num3 * num3 + num4 * num4;
	}

	private static void CoefsLinear(float t1, float t0, float t, out float a)
	{
		a = (t - t1) / (t0 - t1);
	}

	private static void CoefsParab(float t2, float t1, float t0, float t, out float a, out float b)
	{
		a = (t - t1) * (t - t2) / ((t0 - t1) * (t0 - t2));
		b = (t - t0) * (t - t2) / ((t1 - t0) * (t1 - t2));
	}

	private static void CoefsCubic(float t3, float t2, float t1, float t0, float t, out float a, out float b, out float g)
	{
		a = (t - t1) * (t - t2) * (t - t3) / ((t0 - t1) * (t0 - t2) * (t0 - t3));
		b = (t - t0) * (t - t2) * (t - t3) / ((t1 - t0) * (t1 - t2) * (t1 - t3));
		g = (t - t0) * (t - t1) * (t - t3) / ((t2 - t0) * (t2 - t1) * (t2 - t3));
	}
}
