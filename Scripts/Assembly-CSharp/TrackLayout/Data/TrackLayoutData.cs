using System;
using UnityEngine;

namespace TrackLayout.Data
{
	public class TrackLayoutData : ScriptableObject
	{
		public float triggerAdditionalWidth;

		public float networkSubraceTime;

		public PathData path;

		public BackgroundData background;

		public SpawnPointData[] spawnPoints;

		public PerfectLineData perfectLine;

		public CheckpointData[] checkpoints = Array.Empty<CheckpointData>();

		public ClipZoneData[] clipZones = Array.Empty<ClipZoneData>();

		public RuleData[] rules = Array.Empty<RuleData>();

		public SpeedRuleData[] speedRules = Array.Empty<SpeedRuleData>();

		public float initialMinSpeed = 40f;

		public SectorData[] sectors = Array.Empty<SectorData>();

		public GameObject[] extras;

		public string additionalScene;
	}
}
