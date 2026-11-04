using System.Collections.Generic;
using CarX.Gameplay.ClippingZone;
using CarX.XDSPoints;
using UnityEngine;

namespace TrackLayout
{
	public class LegacyTrackContainer
	{
		public Transform root;

		public Track track;

		public List<StartPosition> startPositions = new List<StartPosition>();

		public StartLine startLine;

		public List<Checkpoint> checkpoints = new List<Checkpoint>();

		public List<ClippingZoneSpline> clipZones = new List<ClippingZoneSpline>();

		public List<XDSDriftSideRule> driftSideRules = new List<XDSDriftSideRule>();

		public List<XDSDriftSpeedRule> driftSpeedRules = new List<XDSDriftSpeedRule>();

		public List<XDSSector> sectors = new List<XDSSector>();

		public GameObject perfectLine;

		public List<GameObject> extras = new List<GameObject>();
	}
}
