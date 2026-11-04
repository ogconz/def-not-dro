using UnityEngine;

public abstract class FlareData : ScriptableObject
{
	public abstract Material Material { get; }

	public abstract Mesh Mesh { get; }
}
