using MessagePack;

[MessagePackObject(false)]
public struct TextureNetworkData
{
	[Key(0)]
	public byte[] bodyTex;

	[Key(1)]
	public byte[] glassTex;

	[Key(2)]
	public byte[] bodyMat;

	[Key(3)]
	public byte[] glassMat;

	[Key(4)]
	public int bodySize;

	[Key(5)]
	public int glassSize;

	[Key(6)]
	public int bodyFormat;

	[Key(7)]
	public int glassFormat;
}
