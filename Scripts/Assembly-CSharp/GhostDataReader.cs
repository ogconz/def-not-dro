using System;
using System.IO;

public abstract class GhostDataReader
{
	public abstract void Load(BinaryReader reader, GhostData result);

	protected void ReadSingleStream(BinaryReader reader, out GhostData.ReplayStreamType streamType, out byte[] streamData)
	{
		byte b = reader.ReadByte();
		int count = reader.ReadInt32();
		if (!Enum.IsDefined(typeof(GhostData.ReplayStreamType), b))
		{
			throw new Exception("Wrong stream type " + b);
		}
		streamType = (GhostData.ReplayStreamType)b;
		streamData = reader.BaseStream.ForcedReadBytes(count);
	}
}
