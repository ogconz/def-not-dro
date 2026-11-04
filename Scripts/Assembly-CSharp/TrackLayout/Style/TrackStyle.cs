using UnityEngine;

namespace TrackLayout.Style
{
	[CreateAssetMenu(fileName = "trackStyle", menuName = "Track Layout/Track Style")]
	public class TrackStyle : ScriptableObject
	{
		public float step = 1f;

		public float triggerDepth = 0.5f;

		public float triggerHeight = 5f;

		public bool useSectors;

		public bool usePerfectLine;

		public bool useCheckpoints;

		public bool useClipZones;

		public MeshLineStyle perfectLineStyle;

		public CheckpointStyle[] checkpoints;

		public ClipZoneStyle[] clipZoneStyles;

		public Transform ClipPoint;

		public Transform ClipzoneFlag;

		public Transform ClipzoneArrows;

		public Transform ClipzoneBillboardBody;

		public Transform ClipzoneBillboardLeg;

		public Transform ClipzoneBillboardRope;
	}
}
