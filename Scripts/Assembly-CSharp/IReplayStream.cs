using System.IO;

public interface IReplayStream
{
	void Clear();

	void Release();

	bool IsEmpty();

	bool HasDataFor(float time);

	void GetTimeInterval(ref float beg, ref float end);

	void Serialize(BinaryWriter writer);

	void Deserialize(BinaryReader reader);
}
