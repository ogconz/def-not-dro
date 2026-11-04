using System;
using UnityEngine;

namespace TrackLayout.Style
{
	[Serializable]
	public struct MeshLineStyle
	{
		public float width;

		public float bias;

		public float slopeSize;

		public float normalBias;

		public bool closed;

		public Material material;

		public float uvScale;

		public float uvBias;
	}
}
