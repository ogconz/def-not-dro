using System;
using System.Collections.Generic;
using System.IO;
using SyncMultiplayer;

public class GhostDataReaderV6 : GhostDataReader
{
	public override void Load(BinaryReader reader, GhostData result)
	{
		result.carId = reader.ReadInt32();
		result.profileId = reader.ReadInt32();
		result.trackId = reader.ReadInt32();
		result.greenLightTime = reader.ReadSingle();
		int num = reader.ReadInt32();
		result.visual = ((num == 0) ? null : CustomTypes.DeserializeCarVS(reader.ReadBytes(num)));
		if (result.visual != null)
		{
			result.visual = result.visual.MutateToLocalParts();
		}
		int num2 = reader.ReadInt32();
		result.profile = ((num2 == 0) ? null : CustomTypes.DeserializeCarProfile(reader.ReadBytes(num2)));
		DeserializeStreams(reader, result);
	}

	private void DeserializeStreams(BinaryReader reader, GhostData target)
	{
		List<GhostData.ReplayStreamType> list = new List<GhostData.ReplayStreamType>
		{
			GhostData.ReplayStreamType.Position,
			GhostData.ReplayStreamType.Rotation,
			GhostData.ReplayStreamType.Steer,
			GhostData.ReplayStreamType.RPM,
			GhostData.ReplayStreamType.Velocity,
			GhostData.ReplayStreamType.AngVelocity,
			GhostData.ReplayStreamType.Lights,
			GhostData.ReplayStreamType.Ignition,
			GhostData.ReplayStreamType.Accel
		};
		while (reader.PeekChar() >= 0)
		{
			ReadSingleStream(reader, out var streamType, out var streamData);
			IReplayStream streamOfType = target.GetStreamOfType(streamType);
			if (streamOfType == null)
			{
				continue;
			}
			list.Remove(streamType);
			using MemoryStream input = new MemoryStream(streamData);
			using BinaryReader reader2 = new BinaryReader(input);
			streamOfType.Deserialize(reader2);
		}
		if (list.Count <= 0)
		{
			return;
		}
		string text = "";
		foreach (GhostData.ReplayStreamType item in list)
		{
			text += $"{item}, ";
		}
		throw new Exception("Replay data is incomplete, missing streams: " + text);
	}
}
