using System;
using UnityEngine;

namespace TrackLayout.Style
{
	[Serializable]
	public struct GraphicLineStyle
	{
		public Color color;

		public float width;

		public float bias;

		public bool closed;

		public Texture texture;
	}
}
