using System.IO;

public class StarterStatesReplayStream : BaseReplayStream<StarterStatesReplay>
{
	protected override StarterStatesReplay DeserializeValue(BinaryReader reader)
	{
		byte b = reader.ReadByte();
		StarterStatesReplay result = default(StarterStatesReplay);
		result.isStarterPressed = ((b >> 1) & 1) > 0;
		result.isIgnitionOn = ((b >> 2) & 1) > 0;
		return result;
	}

	protected override InterpType GetInterpType(float time, StarterStatesReplay value, KeyFrame[] buf, int ix, int n, InterpType minType, float epsilon)
	{
		if (buf[ix].value.Equals(value))
		{
			return InterpType.Const;
		}
		return InterpType.None;
	}

	protected override StarterStatesReplay Lerp(StarterStatesReplay from, StarterStatesReplay to, float t)
	{
		if (!(t > 0.5f))
		{
			return from;
		}
		return to;
	}

	protected override void SerializeValue(BinaryWriter writer, StarterStatesReplay value)
	{
		byte b = (byte)((value.isStarterPressed ? 1 : 0) << 1);
		b |= (byte)((value.isIgnitionOn ? 1 : 0) << 2);
		writer.Write(b);
	}
}
