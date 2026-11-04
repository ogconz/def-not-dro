using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class MinimapData
{
	public int trackId;

	[FormerlySerializedAs("m_boundsCenter")]
	public Vector2 boundsCenter;

	[FormerlySerializedAs("m_boundsSize")]
	public Vector2 boundsSize;

	public MinimapData(int trackId, Vector2 boundsCenter, Vector2 boundsSize)
	{
		this.trackId = trackId;
		this.boundsCenter = boundsCenter;
		this.boundsSize = boundsSize;
	}
}
