using MessagePack;
using UnityEngine;

[MessagePackObject(false)]
public struct CarPaintHolderProxy
{
	[Key(0)]
	public CarVisualManager.CustomizationFlags type;

	[Key(1)]
	public int id;

	[Key(2)]
	public Color color;

	[Key(3)]
	public Color specColor;

	[Key(4)]
	public Color duoColor;

	[Key(5)]
	public Color customSecColor;

	[Key(6)]
	public int indexConfig;
}
