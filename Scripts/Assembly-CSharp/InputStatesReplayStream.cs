using System.IO;

public class InputStatesReplayStream : BaseReplayStream<InputStatesReplay>
{
	protected override InterpType GetInterpType(float time, InputStatesReplay value, KeyFrame[] buf, int ix, int n, InterpType minType, float epsilon)
	{
		if (buf[ix].value.Equals(value))
		{
			return InterpType.Const;
		}
		return InterpType.None;
	}

	protected override InputStatesReplay Lerp(InputStatesReplay from, InputStatesReplay to, float t)
	{
		if (!(t > 0.5f))
		{
			return from;
		}
		return to;
	}

	protected override void SerializeValue(BinaryWriter writer, InputStatesReplay value)
	{
		byte b = (byte)(value.gear & 0xF);
		b |= (byte)((value.isHandbrake ? 1 : 0) << 4);
		b |= (byte)((value.isBrake ? 1 : 0) << 5);
		b |= (byte)((value.isFlares ? 1 : 0) << 6);
		writer.Write(b);
	}

	protected override InputStatesReplay DeserializeValue(BinaryReader reader)
	{
		byte b = reader.ReadByte();
		InputStatesReplay result = default(InputStatesReplay);
		result.gear = (byte)(b & 0xF);
		if (result.gear == 15)
		{
			result.gear = byte.MaxValue;
		}
		result.isHandbrake = ((b >> 4) & 1) > 0;
		result.isBrake = ((b >> 5) & 1) > 0;
		result.isFlares = ((b >> 6) & 1) > 0;
		return result;
	}
}
