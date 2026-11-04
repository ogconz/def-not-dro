using System;
using System.Collections.Generic;
using CarX.Gameplay.ClippingZone;
using CarX.XDSPoints;
using SplineLogic;
using TrackLayout.Behaviours;
using TrackLayout.Data;
using TrackLayout.Style;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrackLayout.Builders
{
	public static class TrackBuilder
	{
		private static TrackLayoutData m_lastLayout;

		public static bool debugSlowZones;

		private static StartPosition AddLegacyStartPosition(GameObject gameObject, SpawnPointData data)
		{
			return gameObject.GetComponent<StartPosition>();
		}

		private static StartLine AddLegacyStartLine(ISpline centralSpline, GameObject gameObject, CheckpointData data)
		{
			StartLine startLine = gameObject.AddComponent<StartLine>();
			startLine.spline = centralSpline;
			startLine.distance = data.distance;
			return startLine;
		}

		private static Checkpoint AddLegacyCheckpoint(ISpline centralSpline, GameObject gameObject, CheckpointData data, int id)
		{
			Checkpoint checkpoint = gameObject.AddComponent<Checkpoint>();
			checkpoint.spline = centralSpline;
			checkpoint.distance = data.distance;
			checkpoint.zone = data.zoneID;
			return checkpoint;
		}

		private static XDSDriftSideRule AddLegacyRule(GameObject gameObject, RuleData data)
		{
			XDSDriftSideRule xDSDriftSideRule = gameObject.AddComponent<XDSDriftSideRule>();
			xDSDriftSideRule.distance = data.distance;
			xDSDriftSideRule.targetSide = (DriftControllerXDS.TargetSide)data.targetSide;
			return xDSDriftSideRule;
		}

		private static XDSDriftSpeedRule AddSpeedRule(int index, GameObject gameObject, SpeedRuleData data, GameObject beginGO, GameObject endGO)
		{
			XDSDriftSpeedRule speedRuleBehaviour = gameObject.AddComponent<XDSDriftSpeedRule>();
			speedRuleBehaviour.distance = data.distance;
			speedRuleBehaviour.length = data.length;
			speedRuleBehaviour.deltaV = data.deltaV;
			speedRuleBehaviour.collTracking = data.collisionOnly;
			speedRuleBehaviour.beginGO = beginGO;
			speedRuleBehaviour.endGO = endGO;
			TriggerZone triggerZone = beginGO.AddComponent<TriggerZone>();
			triggerZone.onTriggerEnter = (Action<Collider>)Delegate.Combine(triggerZone.onTriggerEnter, (Action<Collider>)delegate(Collider other)
			{
				speedRuleBehaviour.OnZoneLineCross(other, contact: true);
			});
			TriggerZone triggerZone2 = endGO.AddComponent<TriggerZone>();
			triggerZone2.onTriggerEnter = (Action<Collider>)Delegate.Combine(triggerZone2.onTriggerEnter, (Action<Collider>)delegate(Collider other)
			{
				speedRuleBehaviour.OnZoneLineCross(other, contact: false);
			});
			if (debugSlowZones && data.display)
			{
				triggerZone.editorColor = Color.red;
				triggerZone.name = "Begin Slow zone №" + index;
				triggerZone.labelOffset += Vector3.up;
				triggerZone2.editorColor = Color.yellow;
				triggerZone2.name = "End Slow zone №" + index;
				triggerZone2.labelOffset += Vector3.up * 1.5f;
			}
			return speedRuleBehaviour;
		}

		private static ClippingZoneSpline AddLegacyClipZone(GameObject gameObject, GameObject failGateZoneGameObject, ClipZoneData data)
		{
			ClippingZoneSpline clippingZoneSpline = gameObject.AddComponent<ClippingZoneSpline>();
			clippingZoneSpline.zoneType = data.type;
			clippingZoneSpline.pointsFactor = data.factor;
			TriggerZone triggerZone = failGateZoneGameObject.AddComponent<TriggerZone>();
			triggerZone.onTriggerExit = (Action<Collider>)Delegate.Combine(triggerZone.onTriggerExit, new Action<Collider>(clippingZoneSpline.OnFailGate));
			return clippingZoneSpline;
		}

		public static LegacyTrackContainer BuildLegacy(TrackLayoutData trackLayoutData, TrackStyle trackStyle)
		{
			m_lastLayout = trackLayoutData;
			LegacyTrackContainer legacyTrackContainer = new LegacyTrackContainer();
			ISpline spline = trackLayoutData.path.BuildSpline(PathLaneType.Central);
			ISpline spline2 = trackLayoutData.path.BuildSpline(PathLaneType.Left);
			ISpline spline3 = trackLayoutData.path.BuildSpline(PathLaneType.Right);
			ISpline racingLine = trackLayoutData.path.BuildSpline(PathLaneType.RaceAI);
			GameObject gameObject = new GameObject("root");
			gameObject.SetActive(value: false);
			legacyTrackContainer.root = gameObject.transform;
			if (trackLayoutData.networkSubraceTime > 1f)
			{
				gameObject.GetOrAddComponent<NetworkSubraceDescriptor>().descriptor.raceTime = trackLayoutData.networkSubraceTime;
			}
			GameObject gameObject2 = new GameObject("Track");
			gameObject2.transform.SetParent(legacyTrackContainer.root, worldPositionStays: false);
			legacyTrackContainer.track = gameObject2.AddComponent<Track>();
			legacyTrackContainer.track.centerLine = spline;
			legacyTrackContainer.track.racingLine = racingLine;
			legacyTrackContainer.track.leftBorderLine = spline2;
			legacyTrackContainer.track.rightBorderLine = spline3;
			legacyTrackContainer.track.transform.parent = legacyTrackContainer.root;
			for (int i = 0; i < trackLayoutData.spawnPoints.Length; i++)
			{
				GameObject gameObject3 = CreateSpawnPoint(trackLayoutData.spawnPoints[i]);
				legacyTrackContainer.startPositions.Add(AddLegacyStartPosition(gameObject3, trackLayoutData.spawnPoints[i]));
				gameObject3.transform.parent = legacyTrackContainer.root;
			}
			if (trackStyle.useCheckpoints)
			{
				for (int j = 0; j < trackLayoutData.checkpoints.Length; j++)
				{
					CheckpointData data = trackLayoutData.checkpoints[j];
					GameObject gameObject4 = CreateCheckpoint(spline, spline2, spline3, data, trackStyle);
					gameObject4.transform.parent = legacyTrackContainer.root;
					if (data.type == CheckpointType.Start)
					{
						legacyTrackContainer.startLine = AddLegacyStartLine(spline, gameObject4, data);
					}
					else
					{
						legacyTrackContainer.checkpoints.Add(AddLegacyCheckpoint(spline, gameObject4, data, j));
					}
				}
			}
			for (int k = 0; k < trackLayoutData.rules.Length; k++)
			{
				RuleData data2 = trackLayoutData.rules[k];
				GameObject gameObject5 = CreateGateZone(spline, spline2, spline3, data2.distance, trackStyle);
				gameObject5.transform.parent = legacyTrackContainer.root;
				legacyTrackContainer.driftSideRules.Add(AddLegacyRule(gameObject5, data2));
			}
			for (int l = 0; l < trackLayoutData.speedRules.Length; l++)
			{
				GameObject gameObject6 = new GameObject("speedRule");
				gameObject6.transform.parent = legacyTrackContainer.root;
				SpeedRuleData data3 = trackLayoutData.speedRules[l];
				GameObject gameObject7 = CreateGateZone(spline, spline2, spline3, data3.distance, trackStyle);
				gameObject7.transform.parent = gameObject6.transform;
				GameObject gameObject8 = CreateGateZone(spline, spline2, spline3, data3.distance + data3.length, trackStyle);
				gameObject8.transform.parent = gameObject6.transform;
				legacyTrackContainer.driftSpeedRules.Add(AddSpeedRule(l, gameObject6, data3, gameObject7, gameObject8));
			}
			if (trackStyle.useClipZones)
			{
				for (int m = 0; m < trackLayoutData.clipZones.Length; m++)
				{
					ClipZoneData data4 = trackLayoutData.clipZones[m];
					GameObject gameObject9 = CreateClipZone(spline, spline2, spline3, data4, trackStyle);
					gameObject9.transform.parent = legacyTrackContainer.root;
					GameObject gameObject10 = CreateGateZone(spline, spline2, spline3, data4, trackStyle);
					gameObject10.transform.parent = gameObject9.transform;
					legacyTrackContainer.clipZones.Add(AddLegacyClipZone(gameObject9, gameObject10, data4));
				}
			}
			if (trackStyle.useSectors)
			{
				for (int n = 0; n < trackLayoutData.sectors.Length; n++)
				{
					SectorData sectorData = trackLayoutData.sectors[n];
					XDSSector xDSSector = CreateSector(spline, spline2, spline3, sectorData);
					legacyTrackContainer.sectors.Add(xDSSector);
					if (trackStyle.useClipZones)
					{
						xDSSector.clipZoneIndices = new List<int>();
						for (int num = 0; num < trackLayoutData.clipZones.Length; num++)
						{
							if (trackLayoutData.clipZones[num].distance >= sectorData.distance && trackLayoutData.clipZones[num].distance < sectorData.distance + sectorData.length)
							{
								xDSSector.clipZoneIndices.Add(num);
								legacyTrackContainer.clipZones[num].sector = legacyTrackContainer.sectors.Count - 1;
							}
						}
					}
					if (n == trackLayoutData.sectors.Length - 1 || Math.Abs(sectorData.distance + sectorData.length - trackLayoutData.sectors[n + 1].distance) > 0.01f)
					{
						SectorData data5 = sectorData;
						data5.distance += sectorData.length;
						XDSSector xDSSector2 = CreateSector(spline, spline2, spline3, data5);
						xDSSector2.isBlank = true;
						legacyTrackContainer.sectors.Add(xDSSector2);
					}
				}
			}
			if (trackStyle.usePerfectLine && trackLayoutData.perfectLine.points != null && trackLayoutData.perfectLine.points.Length > 1)
			{
				GameObject gameObject11 = CreatePerfectLine(trackLayoutData.perfectLine.BuildSpline(), trackStyle);
				gameObject11.transform.parent = legacyTrackContainer.root;
				legacyTrackContainer.perfectLine = gameObject11;
			}
			if (trackLayoutData.extras != null)
			{
				for (int num2 = 0; num2 < trackLayoutData.extras.Length; num2++)
				{
					if ((bool)trackLayoutData.extras[num2])
					{
						legacyTrackContainer.extras.Add(UnityEngine.Object.Instantiate(trackLayoutData.extras[num2], Vector3.zero, Quaternion.identity, legacyTrackContainer.root));
					}
				}
			}
			gameObject.AddComponent<CheckpointsData>();
			gameObject.AddComponent<StartPointsData>();
			m_lastLayout = null;
			return legacyTrackContainer;
		}

		private static GameObject CreateSpawnPoint(SpawnPointData data)
		{
			GameObject gameObject = new GameObject("spawnPoint", typeof(StartPosition));
			Transform transform = gameObject.transform;
			transform.position = data.position;
			transform.rotation = data.rotation;
			return gameObject;
		}

		private static GameObject CreatePerfectLine(ISpline spline, TrackStyle trackStyle)
		{
			int num = Mathf.CeilToInt(spline.length / trackStyle.step);
			float num2 = spline.length / (float)num;
			int num3 = num + 1;
			Vector3[] array = new Vector3[num3];
			for (int i = 0; i < num3; i++)
			{
				spline.SamplePoint((float)i * num2, out var pnt);
				array[i] = pnt;
			}
			MeshData mesh = default(MeshData);
			GeometryBuilder.BuildMeshLine(array, trackStyle.perfectLineStyle, Vector3.up, ref mesh);
			Mesh sharedMesh = new Mesh
			{
				vertices = mesh.vertices,
				triangles = mesh.triangles,
				normals = mesh.normals,
				uv = mesh.uv
			};
			GameObject gameObject = new GameObject("perfectLine");
			MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
			meshFilter.sharedMesh = sharedMesh;
			meshRenderer.sharedMaterial = trackStyle.perfectLineStyle.material;
			meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			return gameObject;
		}

		private static GameObject CreateClipZone(ISpline centralSpline, ISpline leftSpline, ISpline rightSpline, ClipZoneData data, TrackStyle trackStyle)
		{
			GameObject gameObject = new GameObject("clipZone");
			Transform transform = gameObject.transform;
			gameObject.transform.position += gameObject.transform.up * data.height;
			CompositeTriggerZone compositeTriggerZone = gameObject.AddComponent<CompositeTriggerZone>();
			compositeTriggerZone.onlyRigidbody = true;
			Vector3[] array = BuilderUtils.SampleClipZone(centralSpline, leftSpline, rightSpline, data, trackStyle.step * 2f);
			int num = array.Length;
			int num2 = num / 2;
			if (data.flag != ClipZoneFlagType.Point)
			{
				for (int i = 0; i < num2 - 1; i++)
				{
					Vector3 vector = array[i];
					Vector3 vector2 = array[num - 1 - i];
					Vector3 vector3 = array[i + 1];
					Vector3 vector4 = array[num - 1 - (i + 1)];
					Vector3 vector5 = vector3 - vector;
					Vector3 vector6 = vector4 - vector2;
					float magnitude = vector5.magnitude;
					float magnitude2 = vector6.magnitude;
					Vector3 vector7 = vector - vector2;
					Vector3 vector8 = vector3 - vector4;
					float magnitude3 = vector7.magnitude;
					float magnitude4 = vector8.magnitude;
					float depth;
					Vector3 vector9;
					if (magnitude2 > magnitude)
					{
						depth = magnitude2;
						vector9 = vector6 * 0.5f;
					}
					else
					{
						depth = magnitude;
						vector9 = vector5 * 0.5f;
					}
					Vector3 position;
					Vector3 direction;
					float length;
					if (i == num2 - 2)
					{
						position = (vector3 + vector4) * 0.5f - vector9;
						direction = vector8 / magnitude4;
						length = magnitude4;
					}
					else
					{
						position = (vector + vector2) * 0.5f + vector9;
						direction = vector7 / magnitude3;
						length = magnitude3;
					}
					GameObject gameObject2 = CreateTriggerZone(position, direction, length, depth, 10f);
					gameObject2.transform.parent = transform;
					TriggerZone triggerZone = gameObject2.AddComponent<TriggerZone>();
					triggerZone.onTriggerEnter = (Action<Collider>)Delegate.Combine(triggerZone.onTriggerEnter, new Action<Collider>(compositeTriggerZone.OnTriggerEnter));
					triggerZone.onTriggerStay = (Action<Collider>)Delegate.Combine(triggerZone.onTriggerStay, new Action<Collider>(compositeTriggerZone.OnTriggerStay));
					triggerZone.onTriggerExit = (Action<Collider>)Delegate.Combine(triggerZone.onTriggerExit, new Action<Collider>(compositeTriggerZone.OnTriggerExit));
				}
			}
			if (data.flag != ClipZoneFlagType.None && num2 > 0 && data.type != ClipZoneType.Bad)
			{
				float distance = data.distance + 0.5f * data.length;
				GetLeftRightPoints(centralSpline, leftSpline, rightSpline, distance, out var leftPoint, out var rightPoint);
				BuilderUtils.GetLeftRightPoints(centralSpline, leftSpline, rightSpline, data, distance, out var leftPoint2, out var rightPoint2);
				Vector3 direction2 = (leftPoint2 - rightPoint2) / (leftPoint2 - rightPoint2).magnitude;
				float magnitude5 = (leftPoint2 - leftPoint).magnitude;
				float magnitude6 = (rightPoint2 - rightPoint).magnitude;
				FlagPlacer.Setup(trackStyle, transform, direction2, leftPoint, magnitude5, magnitude6, rightPoint);
				if (data.flag == ClipZoneFlagType.Point)
				{
					FlagPlacer.mode = ((!(data.bias > 0.5f)) ? FlagPlacer.PlaceMode.Left : FlagPlacer.PlaceMode.Right);
					FlagPlacer.PlaceClipPoint(data.width / 10f);
				}
				else
				{
					FlagPlacer.mode = ((!(data.bias > 0.5f)) ? FlagPlacer.PlaceMode.Left : FlagPlacer.PlaceMode.Right);
					FlagPlacer.PlaceFlagSingle();
				}
			}
			if (data.flag != ClipZoneFlagType.Point)
			{
				array = BuilderUtils.SampleClipZone(centralSpline, leftSpline, rightSpline, data, trackStyle.step);
				for (int j = 0; j < trackStyle.clipZoneStyles.Length; j++)
				{
					ClipZoneStyle clipZoneStyle = trackStyle.clipZoneStyles[j];
					if (clipZoneStyle.type == data.type)
					{
						MeshLineStyle style = new MeshLineStyle
						{
							width = clipZoneStyle.width,
							bias = clipZoneStyle.bias,
							slopeSize = clipZoneStyle.slopeSize,
							normalBias = clipZoneStyle.normalBias,
							closed = true,
							material = clipZoneStyle.material,
							uvScale = clipZoneStyle.uvScale,
							uvBias = clipZoneStyle.uvBias
						};
						MeshData mesh = default(MeshData);
						GeometryBuilder.BuildMeshLine(array, style, Vector3.up, ref mesh);
						Mesh sharedMesh = new Mesh
						{
							vertices = mesh.vertices,
							triangles = mesh.triangles,
							normals = mesh.normals,
							uv = mesh.uv
						};
						MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
						MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
						meshFilter.sharedMesh = sharedMesh;
						meshRenderer.sharedMaterial = clipZoneStyle.material;
						meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
						break;
					}
				}
			}
			return gameObject;
		}

		private static GameObject CreateTriggerZone(Vector3 position, Vector3 direction, float length, float depth, float height)
		{
			GameObject obj = new GameObject("triggerZone")
			{
				layer = 2
			};
			Transform transform = obj.transform;
			transform.position = position;
			transform.localRotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);
			BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
			boxCollider.isTrigger = true;
			boxCollider.size = new Vector3(length, height, depth);
			boxCollider.center = new Vector3(0f, height * 0.5f, 0f);
			return obj;
		}

		private static void GetLeftRightPoints(ISpline centralSpline, ISpline leftSpline, ISpline rightSpline, float distance, out Vector3 leftPoint, out Vector3 rightPoint)
		{
			centralSpline.SamplePoint(distance, out var pnt);
			leftSpline.ProjectPoint(pnt, out leftPoint, out Vector3 dir);
			rightSpline.ProjectPoint(pnt, out rightPoint, out dir);
		}

		private static GameObject CreateGateZone(ISpline centralSpline, ISpline leftSpline, ISpline rightSpline, float distance, TrackStyle trackStyle)
		{
			GetLeftRightPoints(centralSpline, leftSpline, rightSpline, distance, out var leftPoint, out var rightPoint);
			Vector3 vector = leftPoint - rightPoint;
			float magnitude = vector.magnitude;
			Vector3 direction = vector / magnitude;
			return CreateTriggerZone((leftPoint + rightPoint) * 0.5f, direction, magnitude + 2f * m_lastLayout.triggerAdditionalWidth, trackStyle.triggerDepth, trackStyle.triggerHeight);
		}

		private static GameObject CreateGateZone(ISpline centralSpline, ISpline leftSpline, ISpline rightSpline, ClipZoneData data, TrackStyle trackStyle)
		{
			float distance = data.distance + data.length + 10f;
			GetLeftRightPoints(centralSpline, leftSpline, rightSpline, distance, out var leftPoint, out var rightPoint);
			Vector3 vector = leftPoint - rightPoint;
			float magnitude = vector.magnitude;
			Vector3 direction = vector / magnitude;
			return CreateTriggerZone((leftPoint + rightPoint) * 0.5f, direction, magnitude + 2f * m_lastLayout.triggerAdditionalWidth + data.triggerWidth, trackStyle.triggerDepth, trackStyle.triggerHeight);
		}

		private static XDSSector CreateSector(ISpline centralSpline, ISpline leftSpline, ISpline rightSpline, SectorData data)
		{
			GetLeftRightPoints(centralSpline, leftSpline, rightSpline, data.distance, out var leftPoint, out var rightPoint);
			Vector3 vector = leftPoint - rightPoint;
			float magnitude = vector.magnitude;
			Vector3 vector2 = vector / magnitude;
			leftPoint += data.width * vector2;
			rightPoint += data.width * -vector2;
			return new XDSSector
			{
				leftPoint = leftPoint,
				rightPoint = rightPoint,
				weight = data.weight,
				sideChangeFactor = data.sideChangeFactor,
				minSpeed = data.minSpeed,
				isBlank = data.isBlank
			};
		}

		private static GameObject CreateCheckpoint(ISpline centralSpline, ISpline leftSpline, ISpline rightSpline, CheckpointData data, TrackStyle trackStyle)
		{
			GetLeftRightPoints(centralSpline, leftSpline, rightSpline, data.distance, out var leftPoint, out var rightPoint);
			Vector3 vector = leftPoint - rightPoint;
			float magnitude = vector.magnitude;
			Vector3 vector2 = vector / magnitude;
			Vector3 vector3 = (leftPoint + rightPoint) * 0.5f;
			GameObject gameObject = CreateTriggerZone(vector3, vector2, magnitude + 2f * m_lastLayout.triggerAdditionalWidth, trackStyle.triggerDepth, trackStyle.triggerHeight);
			gameObject.layer = 16;
			Transform transform = gameObject.transform;
			if (States.mainCurrent is LoadRaceState loadRaceState)
			{
				LoadRaceState.StartArgs currentStartArgs = loadRaceState.GetCurrentStartArgs();
				if (currentStartArgs != null && currentStartArgs.raceMode == RaceMode.Freeride)
				{
					return gameObject;
				}
			}
			for (int i = 0; i < trackStyle.checkpoints.Length; i++)
			{
				if (trackStyle.checkpoints[i].type == data.type)
				{
					float bias = trackStyle.checkpoints[i].bias;
					Vector3 zero = Vector3.zero;
					ResourcesWrapper.InstantiateAsync(position: (bias > 0f) ? (rightPoint + vector2 * bias) : ((!(bias < 0f)) ? vector3 : (leftPoint + vector2 * bias)), path: trackStyle.checkpoints[i].referenceName, rotation: Quaternion.identity, parent: transform);
				}
			}
			return gameObject;
		}
	}
}
