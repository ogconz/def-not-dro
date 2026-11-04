using System.IO;
using ArrayExtension;
using UnityEngine;

public abstract class BaseReplayStream<T> : IReplayStream
{
	protected enum InterpType : byte
	{
		None = 0,
		Const = 1,
		Linear = 2,
		Parab = 3,
		Cubic = 4,
		Temp = 5
	}

	protected struct KeyFrame
	{
		public float time;

		public T value;

		public InterpType type;
	}

	private const float TIME_EPSILON = 0.001f;

	private FastList<KeyFrame> m_data = new FastList<KeyFrame>();

	private int lastFound;

	protected abstract InterpType GetInterpType(float time, T value, KeyFrame[] buf, int ix, int n, InterpType minType, float epsilon);

	protected abstract T Lerp(T from, T to, float t);

	protected abstract void SerializeValue(BinaryWriter writer, T value);

	protected abstract T DeserializeValue(BinaryReader reader);

	public void CopyFrom(BaseReplayStream<T> source)
	{
		if (source == null)
		{
			m_data = new FastList<KeyFrame>();
		}
		else
		{
			m_data.CopyFrom(source.m_data);
		}
	}

	public void Clear()
	{
		m_data.Clear();
	}

	public void Release()
	{
		m_data.Release();
	}

	public bool IsEmpty()
	{
		return m_data.size == 0;
	}

	public void WriteBefore(BaseReplayStream<T> parent, float time, float epsilon = 0.001f)
	{
		KeyFrame[] array = m_data.buffer.Filter((KeyFrame e) => e.type != InterpType.None && e.time >= time);
		KeyFrame[] array2 = parent.m_data.buffer.Filter((KeyFrame e) => e.type != InterpType.None && e.time < time);
		m_data.buffer = array2;
		m_data.size = array2.Length;
		KeyFrame[] array3 = array;
		for (int num = 0; num < array3.Length; num++)
		{
			KeyFrame keyFrame = array3[num];
			WriteValue(keyFrame.time, keyFrame.value, epsilon);
		}
	}

	public void WriteValue(float time, T val, float epsilon = 0.001f)
	{
		KeyFrame[] buffer = m_data.buffer;
		int size = m_data.size;
		if (size < 1)
		{
			m_data.Add(new KeyFrame
			{
				time = time,
				value = val,
				type = InterpType.None
			});
			return;
		}
		int num = size - 1;
		if (time <= buffer[num].time + 0.001f)
		{
			return;
		}
		InterpType interpType;
		if (buffer[num].type == InterpType.Temp)
		{
			int num2 = num--;
			interpType = GetInterpType(time, val, buffer, num, size - 1, buffer[num].type, epsilon);
			if ((int)interpType >= (int)buffer[num].type)
			{
				buffer[num2] = new KeyFrame
				{
					time = time,
					value = val,
					type = InterpType.Temp
				};
				KeyFrame keyFrame = buffer[num];
				keyFrame.type = interpType;
				buffer[num] = keyFrame;
				return;
			}
			KeyFrame value = buffer[num2];
			value.type = InterpType.None;
			m_data[num2] = value;
			num = num2;
		}
		interpType = GetInterpType(time, val, buffer, num, size, InterpType.None, epsilon);
		if (interpType == InterpType.None)
		{
			KeyFrame keyFrame2 = buffer[num];
			keyFrame2.type = InterpType.Const;
			buffer[num] = keyFrame2;
			m_data.Add(new KeyFrame
			{
				time = time,
				value = val,
				type = InterpType.None
			});
		}
		else
		{
			KeyFrame keyFrame3 = buffer[num];
			keyFrame3.type = interpType;
			buffer[num] = keyFrame3;
			m_data.Add(new KeyFrame
			{
				time = time,
				value = val,
				type = InterpType.Temp
			});
		}
	}

	private bool CheckTime(float time, int index)
	{
		if (index > 0 && index <= m_data.size - 1)
		{
			KeyFrame[] buffer = m_data.buffer;
			if (time > buffer[index - 1].time)
			{
				return time < buffer[index].time;
			}
			return false;
		}
		return false;
	}

	public T ReadValue(float time, T defaultValue)
	{
		if (m_data.size == 0)
		{
			return defaultValue;
		}
		int num = 0;
		int num2 = m_data.size - 1;
		KeyFrame[] buffer = m_data.buffer;
		if (time <= buffer[num].time)
		{
			return buffer[num].value;
		}
		if (time >= buffer[num2].time)
		{
			return buffer[num2].value;
		}
		int num3 = 0;
		bool flag = true;
		if (CheckTime(time, lastFound))
		{
			flag = false;
			num3 = lastFound;
		}
		else if (CheckTime(time, lastFound + 1))
		{
			flag = false;
			num3 = lastFound + 1;
		}
		else if (CheckTime(time, lastFound - 1))
		{
			flag = false;
			num3 = lastFound - 1;
		}
		if (flag)
		{
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
		}
		else
		{
			num = num3 - 1;
			num2 = num3;
		}
		if (Mathf.Abs(buffer[num].time - time) < 0.001f)
		{
			return buffer[num].value;
		}
		if (Mathf.Abs(buffer[num2].time - time) < 0.001f)
		{
			return buffer[num2].value;
		}
		num3 = (lastFound = num2);
		float a;
		float b;
		switch (buffer[num].type)
		{
		case InterpType.Const:
		case InterpType.Linear:
		{
			KeyFrame keyFrame = buffer[num3];
			KeyFrame keyFrame2 = buffer[num3 - 1];
			if (keyFrame.time - keyFrame2.time > 0.5f)
			{
				if (time < keyFrame.time - Time.fixedDeltaTime - 0.001f)
				{
					return keyFrame2.value;
				}
				CoefsLinear(keyFrame.time - Time.fixedDeltaTime - 0.001f, keyFrame.time, time, out a);
			}
			else
			{
				CoefsLinear(keyFrame2.time, keyFrame.time, time, out a);
			}
			return Lerp(keyFrame2.value, keyFrame.value, a);
		}
		case InterpType.Parab:
		{
			KeyFrame keyFrame = buffer[num3];
			KeyFrame keyFrame2 = buffer[num3 - 1];
			KeyFrame keyFrame3 = buffer[num3 - 2];
			CoefsParab(keyFrame3.time, keyFrame2.time, keyFrame.time, time, out a, out b);
			return Lerp(Lerp(keyFrame3.value, keyFrame2.value, b / (1f - a)), keyFrame.value, a);
		}
		case InterpType.Cubic:
		{
			KeyFrame keyFrame = buffer[num3];
			KeyFrame keyFrame2 = buffer[num3 - 1];
			KeyFrame keyFrame3 = buffer[num3 - 2];
			KeyFrame keyFrame4 = buffer[num3 - 3];
			CoefsCubic(keyFrame4.time, keyFrame3.time, keyFrame2.time, keyFrame.time, time, out a, out b, out var g);
			return Lerp(Lerp(Lerp(keyFrame4.value, keyFrame3.value, g / (1f - a - b)), keyFrame2.value, b / (1f - a)), keyFrame.value, a);
		}
		default:
			return defaultValue;
		}
	}

	public bool HasDataFor(float time)
	{
		if (IsEmpty())
		{
			return false;
		}
		if (time <= m_data.buffer[0].time)
		{
			return false;
		}
		if (time >= m_data.buffer[m_data.size - 1].time)
		{
			return false;
		}
		return true;
	}

	public void GetTimeInterval(ref float beg, ref float end)
	{
		for (int i = 0; i < m_data.size; i++)
		{
			KeyFrame keyFrame = m_data.buffer[i];
			if (keyFrame.time < beg)
			{
				beg = keyFrame.time;
			}
			if (keyFrame.time > end)
			{
				end = keyFrame.time;
			}
		}
	}

	public void Serialize(BinaryWriter writer)
	{
		writer.Write(m_data.size);
		foreach (KeyFrame datum in m_data)
		{
			writer.Write(datum.time);
			SerializeValue(writer, datum.value);
			writer.Write((byte)datum.type);
		}
	}

	public void Deserialize(BinaryReader reader)
	{
		int num = reader.ReadInt32();
		m_data = new FastList<KeyFrame>();
		for (int i = 0; i < num; i++)
		{
			KeyFrame item = new KeyFrame
			{
				time = reader.ReadSingle(),
				value = DeserializeValue(reader),
				type = (InterpType)reader.ReadByte()
			};
			m_data.Add(item);
		}
	}

	protected void CoefsLinear(float t1, float t0, float t, out float a)
	{
		a = (t - t1) / (t0 - t1);
	}

	protected void CoefsParab(float t2, float t1, float t0, float t, out float a, out float b)
	{
		a = (t - t1) * (t - t2) / ((t0 - t1) * (t0 - t2));
		b = (t - t0) * (t - t2) / ((t1 - t0) * (t1 - t2));
	}

	protected void CoefsCubic(float t3, float t2, float t1, float t0, float t, out float a, out float b, out float g)
	{
		a = (t - t1) * (t - t2) * (t - t3) / ((t0 - t1) * (t0 - t2) * (t0 - t3));
		b = (t - t0) * (t - t2) * (t - t3) / ((t1 - t0) * (t1 - t2) * (t1 - t3));
		g = (t - t0) * (t - t1) * (t - t3) / ((t2 - t0) * (t2 - t1) * (t2 - t3));
	}
}
