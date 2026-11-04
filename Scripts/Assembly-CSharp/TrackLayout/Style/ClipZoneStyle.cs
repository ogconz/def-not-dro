using System;
using UnityEngine;

namespace TrackLayout.Style
{
	[Serializable]
	public struct ClipZoneStyle
	{
		public ClipZoneType type;

		public float width;

		public float bias;

		public float slopeSize;

		public float normalBias;

		public Material material;

		public float uvScale;

		public float uvBias;
	}
}
