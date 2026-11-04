using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace TrackLayout.Style
{
	[CreateAssetMenu(fileName = "minimapStyle", menuName = "Track Layout/Minimap Style")]
	public class RadarStyle : ScriptableObject
	{
		public float step = 1f;

		public BackgroundStyle backgroundStyle;

		[FormerlySerializedAs("layoutColor")]
		public Color roadColor = Color.white;

		public Color[] sectorColors;

		public GraphicLineStyle roadOutlineStyle;

		[Space]
		public Material slowZonesMaterial;

		public RadarSlowZoneStyle[] slowZoneStyles = Array.Empty<RadarSlowZoneStyle>();

		[Space]
		public RadarCheckpointStyle[] checkpointStyles = new RadarCheckpointStyle[0];

		public RadarClipZoneStyle[] clippingZoneStyles = new RadarClipZoneStyle[0];

		public RadarBlipStyle[] blipStyles = new RadarBlipStyle[0];

		public GraphicLineStyle perfectLineStyle;

		public float blipCrossFadeDuration;

		[Space]
		public bool useBackground;

		public bool useSectors;

		public bool useSlowZones;

		public bool useCheckpoints;

		public bool useClipZones;

		public bool usePerfectLine;

		public bool useBlips;
	}
}
