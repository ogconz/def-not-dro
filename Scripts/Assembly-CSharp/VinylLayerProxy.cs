using MessagePack;
using UnityEngine;

[MessagePackObject(false)]
public struct VinylLayerProxy
{
	[Key(0)]
	public byte version;

	[Key(1)]
	public int rawId;

	[Key(2)]
	public int groupMask;

	[Key(3)]
	public bool isSymmetry;

	[Key(4)]
	public bool isHorizontalFlip;

	[Key(5)]
	public bool isVerticalFlip;

	[Key(6)]
	public bool isPassThrough;

	[Key(7)]
	public float angle;

	[Key(8)]
	public Vector3 position;

	[Key(9)]
	public Quaternion rotation;

	[Key(10)]
	public Vector2 scale;

	[Key(11)]
	public float clipMaskShift;

	[Key(12)]
	public Matrix4x4 projectionMatrix;

	[Key(13)]
	public int[] materialProps;

	[Key(14)]
	public Color[] colors;
}
