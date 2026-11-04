using System.Collections.Generic;
using System.Linq;
using RenderCore.UI;
using SplineLogic;
using TrackLayout.Behaviours;
using TrackLayout.Data;
using TrackLayout.Style;
using UnityEngine;
using UnityEngine.UI;

namespace TrackLayout.Builders
{
	public static class RadarBuilder
	{
		private static readonly Vector3[] s_corners = new Vector3[4];

		private static readonly List<Vector3> leftRoadPoints = new List<Vector3>();

		private static readonly List<Vector3> rightRoadPoints = new List<Vector3>();

		private static readonly List<List<Vector3>> leftSlowZonesPoints = new List<List<Vector3>>();

		private static readonly List<List<Vector3>> rightSlowZonesPoints = new List<List<Vector3>>();

		public static RadarContainer Build(TrackLayoutData trackLayoutData, RadarStyle radarStyle, bool previewAlignment)
		{
			RadarContainer radarContainer = new RadarContainer();
			ISpline spline = trackLayoutData.path.BuildSpline(PathLaneType.Central);
			ISpline leftSpline = trackLayoutData.path.BuildSpline(PathLaneType.Left);
			ISpline rightSpline = trackLayoutData.path.BuildSpline(PathLaneType.Right);
			GameObject gameObject = new GameObject("root", typeof(RectTransform));
			radarContainer.root = gameObject.GetComponent<RectTransform>();
			if (radarStyle.useBackground && trackLayoutData.background != null)
			{
				radarContainer.background = BuildBackground(radarContainer.root, trackLayoutData.background, radarStyle.backgroundStyle);
			}
			float step = radarStyle.step;
			float length = spline.length;
			float num = 0f;
			float num2 = length;
			for (int i = 0; i < trackLayoutData.checkpoints.Length; i++)
			{
				if (trackLayoutData.checkpoints[i].type == CheckpointType.Start)
				{
					num = trackLayoutData.checkpoints[i].distance;
				}
				else if (trackLayoutData.checkpoints[i].type == CheckpointType.Finish)
				{
					num2 = trackLayoutData.checkpoints[i].distance;
				}
			}
			leftRoadPoints.Clear();
			rightRoadPoints.Clear();
			AddRoad(radarStyle.roadColor, num, num2, step, spline, leftSpline, rightSpline, radarStyle.sectorColors, trackLayoutData.sectors.Cast<IRadarSectorRoad>().ToList(), leftRoadPoints, rightRoadPoints, radarContainer, "road", radarStyle.useSectors);
			int count;
			int count2;
			Vector3[] array;
			if (radarStyle.useSlowZones)
			{
				leftSlowZonesPoints.Clear();
				rightSlowZonesPoints.Clear();
				Material[] materials = RadarSlowZoneStyle.SetMaterialProperties(radarStyle.slowZonesMaterial, radarStyle.slowZoneStyles);
				List<IRadarSectorRoad> list = trackLayoutData.speedRules.Cast<IRadarSectorRoad>().ToList();
				AddRoadPart(Color.clear, num, num2, step, spline, leftSpline, rightSpline, list, leftSlowZonesPoints, rightSlowZonesPoints, radarContainer, "slow", drawThis: true, materials);
				int num3 = 0;
				for (int j = 0; j < list.Count; j++)
				{
					if (!list[j].isDisplayed)
					{
						continue;
					}
					count = rightSlowZonesPoints[num3].Count;
					count2 = leftSlowZonesPoints[num3].Count;
					if (count > 1 && count2 > 1)
					{
						array = new Vector3[count2 + count];
						leftSlowZonesPoints[num3].CopyTo(array, 0);
						for (int k = 0; k < count; k++)
						{
							array[count2 + k] = rightSlowZonesPoints[num3][count - k - 1];
						}
						int num4 = ((j < radarStyle.slowZoneStyles.Length) ? j : (radarStyle.slowZoneStyles.Length - 1));
						GameObject gameObject2 = BuildLine(array, radarStyle.slowZoneStyles[num4].styleOutLine);
						radarStyle.slowZoneStyles[num4].SetLast();
						gameObject2.transform.SetParent(radarContainer.root);
					}
					num3++;
				}
			}
			count = rightRoadPoints.Count;
			count2 = leftRoadPoints.Count;
			array = new Vector3[count2 + count];
			leftRoadPoints.CopyTo(array, 0);
			for (int l = 0; l < count; l++)
			{
				array[count2 + l] = rightRoadPoints[count - l - 1];
			}
			BuildLine(array, radarStyle.roadOutlineStyle).transform.SetParent(radarContainer.root);
			if (radarStyle.usePerfectLine && trackLayoutData.perfectLine.points != null && trackLayoutData.perfectLine.points.Length > 1)
			{
				spline.SamplePoint(num, out var pnt);
				spline.SamplePoint(num2, out var pnt2);
				radarContainer.perfectLine = BuildPerfectLine(radarContainer.root, step, pnt, pnt2, trackLayoutData.perfectLine, radarStyle.perfectLineStyle);
			}
			if (radarStyle.useClipZones)
			{
				BuildClipZones(radarContainer, spline, leftSpline, rightSpline, trackLayoutData.clipZones, step, radarStyle.clippingZoneStyles);
			}
			if (radarStyle.useCheckpoints)
			{
				for (int m = 0; m < trackLayoutData.checkpoints.Length; m++)
				{
					CheckpointData checkpointData = trackLayoutData.checkpoints[m];
					for (int n = 0; n < radarStyle.checkpointStyles.Length; n++)
					{
						if (radarStyle.checkpointStyles[n].type == checkpointData.type)
						{
							GameObject gameObject3 = BuildCheckpoint(spline, leftSpline, rightSpline, checkpointData.distance, radarStyle.checkpointStyles[n]);
							gameObject3.transform.SetParent(radarContainer.root);
							if (radarStyle.checkpointStyles[n].aligned)
							{
								radarContainer.alignedMarksList.Add(gameObject3.transform);
							}
							else
							{
								radarContainer.marksList.Add(gameObject3.transform);
							}
							if (checkpointData.type == CheckpointType.Start)
							{
								radarContainer.startPosition = gameObject3.transform.localPosition;
							}
							else if (checkpointData.type == CheckpointType.Finish)
							{
								radarContainer.finishPosition = gameObject3.transform.localPosition;
							}
						}
					}
				}
			}
			if (previewAlignment)
			{
				if ((bool)radarContainer.background)
				{
					radarContainer.background.gameObject.SetActive(value: false);
				}
				Bounds bounds = CalculateRelativeRectTransformBounds(radarContainer.root);
				if ((bool)radarContainer.background)
				{
					radarContainer.background.gameObject.SetActive(value: true);
				}
				radarContainer.root.GetComponent<RectTransform>().sizeDelta = bounds.size;
				Vector3 center = bounds.center;
				for (int num5 = 0; num5 < radarContainer.root.childCount; num5++)
				{
					Transform child = radarContainer.root.GetChild(num5);
					Vector3 localPosition = child.localPosition;
					localPosition.x -= center.x;
					localPosition.y -= center.y;
					child.localPosition = localPosition;
				}
				radarContainer.origin = center;
				float num6 = 0f;
				if (bounds.size.y > bounds.size.x)
				{
					num6 += 90f;
					radarContainer.root.localRotation = Quaternion.AngleAxis(num6, Vector3.forward);
					radarContainer.size.x = bounds.size.y;
					radarContainer.size.y = bounds.size.x;
				}
				else
				{
					radarContainer.size.x = bounds.size.x;
					radarContainer.size.y = bounds.size.y;
				}
				if (radarContainer.alignedMarksList.Count > 1)
				{
					Vector3 position = radarContainer.alignedMarksList[0].position;
					Vector3 position2 = radarContainer.alignedMarksList[radarContainer.alignedMarksList.Count - 1].position;
					Vector2 normalized = new Vector2(position2.x - position.x, position2.y - position.y).normalized;
					Vector2 normalized2 = Vector2.one.normalized;
					if (Vector2.Dot(normalized, normalized2) < 0f)
					{
						num6 += 180f;
						radarContainer.root.localRotation = Quaternion.AngleAxis(num6, Vector3.forward);
					}
				}
			}
			return radarContainer;
		}

		public static void AddRoad(Color defColor, float startDistance, float finishDistance, float step, ISpline centralSpline, ISpline leftSpline, ISpline rightSpline, Color[] sectorColor, List<IRadarSectorRoad> sectorRoads, List<Vector3> leftRoadPoints, List<Vector3> rightRoadPoints, RadarContainer container, string name, bool drawThis, Material material = null)
		{
			bool dontAddFirstPoints = false;
			float num = startDistance;
			int num2 = 0;
			float num3;
			Vector2 min = default(Vector2);
			Vector2 max = default(Vector2);
			Vector3 vector = default(Vector3);
			for (; num < finishDistance; num += num3)
			{
				num3 = finishDistance - num;
				Color color = defColor;
				if (drawThis && num2 < sectorRoads.Count)
				{
					IRadarSectorRoad radarSectorRoad = sectorRoads[num2];
					if (radarSectorRoad.distanceRoad > num)
					{
						num3 = radarSectorRoad.distanceRoad - num;
					}
					else
					{
						num3 = radarSectorRoad.lengthRoad;
						if (sectorRoads[num2].isDisplayed)
						{
							color = ((num2 < sectorColor.Length) ? sectorColor[num2] : sectorColor[^1]);
						}
						num2++;
					}
				}
				BuildRoadGeometry(centralSpline, leftSpline, rightSpline, num, num3, step, out var vertices, out var triangles, leftRoadPoints, rightRoadPoints, dontAddFirstPoints);
				dontAddFirstPoints = true;
				ConvertYUpToZUpAxis(ref vertices);
				min.x = float.MaxValue;
				min.y = float.MaxValue;
				max.x = float.MinValue;
				max.y = float.MinValue;
				EncapsulateBounds(vertices, ref min, ref max);
				TryForceSetZPositionToZero(vertices);
				vector.x = (max.x + min.x) * 0.5f;
				vector.y = (max.y + min.y) * 0.5f;
				vector.z = 0f;
				for (int i = 0; i < vertices.Length; i++)
				{
					vertices[i].x -= vector.x;
					vertices[i].y -= vector.y;
				}
				GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(GraphicMesh));
				RectTransform component = gameObject.GetComponent<RectTransform>();
				component.anchoredPosition = vector;
				component.sizeDelta = max - min;
				component.SetParent(container.root);
				GraphicMesh component2 = gameObject.GetComponent<GraphicMesh>();
				component2.color = color;
				component2.vertices = vertices;
				component2.triangles = triangles;
				component2.SetVerticesDirty();
				component2.material = material;
				container.roadsList.Add(component);
			}
		}

		private static void TryForceSetZPositionToZero(Vector3[] vertices)
		{
			if (XRModeManager.IsXRActive())
			{
				for (int i = 0; i < vertices.Length; i++)
				{
					vertices[i].z = 0f;
				}
			}
		}

		private static void AddRoadPart(Color defColor, float startDistance, float finishDistance, float step, ISpline centralSpline, ISpline leftSpline, ISpline rightSpline, List<IRadarSectorRoad> sectorRoads, List<List<Vector3>> leftRoadPoints, List<List<Vector3>> rightRoadPoints, RadarContainer container, string name, bool drawThis, Material[] materials)
		{
			float num = startDistance;
			int num2 = 0;
			float num3;
			Vector2 min = default(Vector2);
			Vector2 max = default(Vector2);
			Vector3 vector = default(Vector3);
			for (; num < finishDistance; num += num3)
			{
				num3 = finishDistance - num;
				Color color = defColor;
				Material material = null;
				if (drawThis && num2 < sectorRoads.Count)
				{
					IRadarSectorRoad radarSectorRoad = sectorRoads[num2];
					if (radarSectorRoad.distanceRoad > num)
					{
						num3 = radarSectorRoad.distanceRoad - num;
					}
					else
					{
						num3 = radarSectorRoad.lengthRoad;
						if (sectorRoads[num2].isDisplayed)
						{
							color = Color.white;
							material = ((num2 < materials.Length) ? materials[num2] : materials[^1]);
						}
						num2++;
					}
				}
				if (color != Color.clear)
				{
					leftRoadPoints.Add(new List<Vector3>());
					rightRoadPoints.Add(new List<Vector3>());
				}
				BuildRoadGeometry(centralSpline, leftSpline, rightSpline, num, num3, step, out var vertices, out var triangles, (leftRoadPoints.Count > 0) ? leftRoadPoints[leftRoadPoints.Count - 1] : null, (rightRoadPoints.Count > 0) ? rightRoadPoints[rightRoadPoints.Count - 1] : null, dontAddFirstPoints: false, color != Color.clear);
				ConvertYUpToZUpAxis(ref vertices);
				min.x = float.MaxValue;
				min.y = float.MaxValue;
				max.x = float.MinValue;
				max.y = float.MinValue;
				EncapsulateBounds(vertices, ref min, ref max);
				TryForceSetZPositionToZero(vertices);
				vector.x = (max.x + min.x) * 0.5f;
				vector.y = (max.y + min.y) * 0.5f;
				vector.z = 0f;
				for (int i = 0; i < vertices.Length; i++)
				{
					vertices[i].x -= vector.x;
					vertices[i].y -= vector.y;
				}
				if (color != Color.clear)
				{
					min = vector;
					max = vector;
					GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(GraphicMesh));
					RectTransform component = gameObject.GetComponent<RectTransform>();
					component.anchoredPosition = vector;
					component.sizeDelta = max - min;
					component.SetParent(container.root);
					GraphicMesh component2 = gameObject.GetComponent<GraphicMesh>();
					component2.color = color;
					component2.vertices = vertices;
					component2.triangles = triangles;
					component2.SetVerticesDirty();
					component2.material = material;
					container.roadsList.Add(component);
				}
			}
		}

		public static BlipData CreateBlip(BlipType type, RadarContainer radarContainer, RadarStyle radarStyle, bool visible)
		{
			for (int i = 0; i < radarStyle.blipStyles.Length; i++)
			{
				if (radarStyle.blipStyles[i].type == type)
				{
					RadarBlipStyle radarBlipStyle = radarStyle.blipStyles[i];
					Vector2 sizeDelta = ((!(radarBlipStyle.sprite != null)) ? new Vector2(radarBlipStyle.size, radarBlipStyle.size) : ((!(radarBlipStyle.sprite.rect.width > radarBlipStyle.sprite.rect.height)) ? new Vector2(radarBlipStyle.size * radarBlipStyle.sprite.rect.width / radarBlipStyle.sprite.rect.height, radarBlipStyle.size) : new Vector2(radarBlipStyle.size, radarBlipStyle.size * radarBlipStyle.sprite.rect.height / radarBlipStyle.sprite.rect.width)));
					GameObject gameObject = new GameObject("blip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
					RectTransform component = gameObject.GetComponent<RectTransform>();
					component.SetParent(radarContainer.root);
					component.anchoredPosition = Vector2.zero;
					component.localRotation = Quaternion.identity;
					component.sizeDelta = sizeDelta;
					component.localScale = Vector3.one;
					Color color = radarBlipStyle.color;
					color.a = (visible ? 1f : 0f);
					Image component2 = gameObject.GetComponent<Image>();
					component2.color = color;
					component2.sprite = radarBlipStyle.sprite;
					BlipData blipData = new BlipData(component, component2);
					blipData.crossFadeDuration = radarStyle.blipCrossFadeDuration;
					blipData.visible = visible;
					blipData.wasVisibilityChanged = false;
					radarContainer.blips.Add(blipData);
					return blipData;
				}
			}
			return null;
		}

		private static Transform BuildBackground(Transform parent, BackgroundData backgroundData, BackgroundStyle style)
		{
			GameObject gameObject = new GameObject("background", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(ColorMapping));
			RectTransform component = gameObject.GetComponent<RectTransform>();
			component.SetParent(parent);
			component.sizeDelta = backgroundData.maxPosition - backgroundData.minPosition;
			component.anchoredPosition = (backgroundData.maxPosition + backgroundData.minPosition) * 0.5f;
			RawImage component2 = gameObject.GetComponent<RawImage>();
			component2.texture = backgroundData.texture;
			component2.color = style.primaryColor;
			gameObject.GetComponent<ColorMapping>().secondaryColor = style.secondaryColor;
			return component;
		}

		private static Transform BuildPerfectLine(Transform parent, float step, Vector3 startPoint, Vector3 finishPoint, PerfectLineData perfectLineData, GraphicLineStyle perfectLineStyle)
		{
			ISpline spline = perfectLineData.BuildSpline();
			if (spline.pointCount == 0)
			{
				return null;
			}
			spline.ProjectPoint(startPoint, out float along, out float across);
			spline.ProjectPoint(finishPoint, out float along2, out across);
			if (along >= along2)
			{
				along -= along2;
				along2 += spline.length;
			}
			if (along2 - along < 2f)
			{
				along2 += spline.length;
			}
			Transform transform = BuildLine(spline, along, along2 - along, step, perfectLineStyle).transform;
			transform.SetParent(parent);
			return transform;
		}

		private static GameObject BuildCheckpoint(ISpline centralSpline, ISpline leftSpline, ISpline rightSpline, float distance, RadarCheckpointStyle style)
		{
			centralSpline.SamplePoint(distance, out var pnt);
			leftSpline.ProjectPoint(pnt, out Vector3 pos, out Vector3 _);
			rightSpline.ProjectPoint(pnt, out Vector3 pos2, out Vector3 _);
			Vector3 vector = pos - pos2;
			float magnitude = vector.magnitude;
			Vector3 normalized = new Vector3(vector.x, vector.z, 0f).normalized;
			Vector2 anchoredPosition = new Vector2((pos.x + pos2.x) * 0.5f, (pos.z + pos2.z) * 0.5f);
			Vector2 sizeDelta = ((!(style.texture != null)) ? new Vector2(magnitude + style.size * 2f, style.size) : ((style.texture.width <= style.texture.height) ? new Vector2(style.size * (float)style.texture.width / (float)style.texture.height, style.size) : new Vector2(style.size, style.size * (float)style.texture.height / (float)style.texture.width)));
			Quaternion localRotation = ((!style.aligned) ? Quaternion.identity : Quaternion.AngleAxis(Mathf.Atan2(normalized.y, normalized.x) * 57.29578f, Vector3.forward));
			GameObject gameObject = new GameObject("checkpoint", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
			RectTransform component = gameObject.GetComponent<RectTransform>();
			component.anchoredPosition = anchoredPosition;
			component.localRotation = localRotation;
			component.sizeDelta = sizeDelta;
			RawImage component2 = gameObject.GetComponent<RawImage>();
			component2.color = style.color;
			component2.texture = style.texture;
			return gameObject;
		}

		private static void BuildClipZones(RadarContainer radarContainer, ISpline centralSpline, ISpline leftSpline, ISpline rightSpline, ClipZoneData[] clipZones, float step, RadarClipZoneStyle[] styles)
		{
			Vector2 min = default(Vector2);
			Vector2 max = default(Vector2);
			Vector3 vector = default(Vector3);
			for (int i = 0; i < clipZones.Length; i++)
			{
				ClipZoneData data = clipZones[i];
				Vector3[] points = BuilderUtils.SampleClipZone(centralSpline, leftSpline, rightSpline, data, step);
				for (int j = 0; j < styles.Length; j++)
				{
					if (styles[j].type == data.type)
					{
						MeshData mesh = default(MeshData);
						GeometryBuilder.BuildLine(points, styles[j].style, Vector3.up, ref mesh);
						ConvertYUpToZUpAxis(ref mesh.vertices);
						min.x = float.MaxValue;
						min.y = float.MaxValue;
						max.x = float.MinValue;
						max.y = float.MinValue;
						EncapsulateBounds(mesh.vertices, ref min, ref max);
						TryForceSetZPositionToZero(mesh.vertices);
						vector.x = (max.x + min.x) * 0.5f;
						vector.y = (max.y + min.y) * 0.5f;
						vector.z = 0f;
						for (int k = 0; k < mesh.vertices.Length; k++)
						{
							mesh.vertices[k].x -= vector.x;
							mesh.vertices[k].y -= vector.y;
						}
						GameObject gameObject = new GameObject("clippingZone", typeof(RectTransform), typeof(CanvasRenderer), typeof(GraphicMesh));
						RectTransform component = gameObject.GetComponent<RectTransform>();
						component.anchoredPosition = vector;
						component.sizeDelta = max - min;
						component.SetParent(radarContainer.root);
						GraphicMesh component2 = gameObject.GetComponent<GraphicMesh>();
						component2.color = styles[j].style.color;
						component2.texture = styles[j].style.texture;
						component2.vertices = mesh.vertices;
						component2.triangles = mesh.triangles;
						component2.uv = mesh.uv;
						component2.SetVerticesDirty();
						radarContainer.clippingZonesList.Add(component);
					}
				}
			}
		}

		private static void ConvertYUpToZUpAxis(ref Vector3[] vertices)
		{
			for (int i = 0; i < vertices.Length; i++)
			{
				float y = vertices[i].y;
				vertices[i].y = vertices[i].z;
				vertices[i].z = y;
			}
		}

		private static void EncapsulateBounds(Vector3[] vertices, ref Vector2 min, ref Vector2 max)
		{
			for (int i = 0; i < vertices.Length; i++)
			{
				Vector3 vector = vertices[i];
				if (min.x > vector.x)
				{
					min.x = vector.x;
				}
				if (min.y > vector.y)
				{
					min.y = vector.y;
				}
				if (max.x < vector.x)
				{
					max.x = vector.x;
				}
				if (max.y < vector.y)
				{
					max.y = vector.y;
				}
			}
		}

		public static Bounds CalculateRelativeRectTransformBounds(Transform root)
		{
			RectTransform[] componentsInChildren = root.GetComponentsInChildren<RectTransform>(includeInactive: false);
			if (componentsInChildren.Length == 0 || (componentsInChildren.Length == 1 && componentsInChildren[0] == root))
			{
				return new Bounds(Vector3.zero, Vector3.zero);
			}
			Vector3 vector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			Vector3 vector2 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			Matrix4x4 worldToLocalMatrix = root.worldToLocalMatrix;
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if (!(componentsInChildren[i] == root))
				{
					componentsInChildren[i].GetWorldCorners(s_corners);
					for (int j = 0; j < 4; j++)
					{
						Vector3 lhs = worldToLocalMatrix.MultiplyPoint3x4(s_corners[j]);
						vector = Vector3.Min(lhs, vector);
						vector2 = Vector3.Max(lhs, vector2);
					}
				}
			}
			return new Bounds((vector + vector2) * 0.5f, vector2 - vector);
		}

		private static GameObject BuildLine(Vector3[] points, GraphicLineStyle style)
		{
			MeshData mesh = default(MeshData);
			GeometryBuilder.BuildLine(points, style, Vector3.up, ref mesh);
			ConvertYUpToZUpAxis(ref mesh.vertices);
			Vector2 min = default(Vector2);
			min.x = float.MaxValue;
			min.y = float.MaxValue;
			Vector2 max = default(Vector2);
			max.x = float.MinValue;
			max.y = float.MinValue;
			EncapsulateBounds(mesh.vertices, ref min, ref max);
			TryForceSetZPositionToZero(mesh.vertices);
			Vector3 vector = default(Vector3);
			vector.x = (max.x + min.x) * 0.5f;
			vector.y = (max.y + min.y) * 0.5f;
			vector.z = 0f;
			for (int i = 0; i < mesh.vertices.Length; i++)
			{
				mesh.vertices[i].x -= vector.x;
				mesh.vertices[i].y -= vector.y;
			}
			GameObject gameObject = new GameObject("line", typeof(RectTransform), typeof(CanvasRenderer), typeof(GraphicMesh));
			RectTransform component = gameObject.GetComponent<RectTransform>();
			component.anchoredPosition = vector;
			component.sizeDelta = max - min;
			GraphicMesh component2 = gameObject.GetComponent<GraphicMesh>();
			component2.color = style.color;
			component2.texture = style.texture;
			component2.vertices = mesh.vertices;
			component2.triangles = mesh.triangles;
			component2.uv = mesh.uv;
			component2.SetVerticesDirty();
			return gameObject;
		}

		private static GameObject BuildLine(ISpline spline, float distance, float length, float step, GraphicLineStyle style)
		{
			BuilderUtils.CalcStep(length, step, out step, out var samplesCount);
			Vector3[] array = new Vector3[samplesCount];
			for (int i = 0; i < samplesCount; i++)
			{
				spline.SamplePoint(distance + (float)i * step, out array[i]);
			}
			return BuildLine(array, style);
		}

		private static void BuildRoadGeometry(ISpline centralSpline, ISpline leftSpline, ISpline rightSpline, float distance, float length, float step, out Vector3[] vertices, out int[] triangles, ICollection<Vector3> leftRoadPoints, ICollection<Vector3> rightRoadPoints, bool dontAddFirstPoints, bool fullRoad = true)
		{
			centralSpline.SamplePoint(distance, out var pnt);
			leftSpline.ProjectPoint(pnt, out float along, out float across);
			rightSpline.ProjectPoint(pnt, out float along2, out across);
			centralSpline.SamplePoint(distance + length, out var pnt2);
			leftSpline.ProjectPoint(pnt2, out float along3, out across);
			rightSpline.ProjectPoint(pnt2, out float along4, out across);
			if (along >= along3)
			{
				along -= leftSpline.length;
			}
			if (along2 >= along4)
			{
				along2 -= rightSpline.length;
			}
			if (along3 - along < 1f)
			{
				along3 += leftSpline.length;
			}
			if (along4 - along2 < 1f)
			{
				along4 += rightSpline.length;
			}
			BuilderUtils.CalcStep(along3 - along, step, out var step2, out var samplesCount);
			BuilderUtils.CalcStep(along4 - along2, step, out var step3, out var samplesCount2);
			vertices = new Vector3[samplesCount + samplesCount2];
			for (int i = 0; i < samplesCount; i++)
			{
				leftSpline.SamplePoint(along + (float)i * step2, out var pnt3);
				vertices[i] = pnt3;
				if ((!dontAddFirstPoints || i != 0) && fullRoad)
				{
					leftRoadPoints.Add(pnt3);
				}
			}
			for (int j = 0; j < samplesCount2; j++)
			{
				rightSpline.SamplePoint(along2 + (float)j * step3, out var pnt4);
				vertices[samplesCount + j] = pnt4;
				if ((!dontAddFirstPoints || j != 0) && fullRoad)
				{
					rightRoadPoints.Add(pnt4);
				}
			}
			List<int> list = new List<int>();
			int num = 0;
			int num2 = samplesCount;
			while (num < samplesCount - 1 && num2 < samplesCount + samplesCount2 - 1)
			{
				float sqrMagnitude = (vertices[num + 1] - vertices[num2]).sqrMagnitude;
				float sqrMagnitude2 = (vertices[num] - vertices[num2 + 1]).sqrMagnitude;
				if (sqrMagnitude < sqrMagnitude2)
				{
					list.Add(num);
					list.Add(num + 1);
					list.Add(num2);
					num++;
				}
				else
				{
					list.Add(num2);
					list.Add(num);
					list.Add(num2 + 1);
					num2++;
				}
			}
			if (num2 == samplesCount + samplesCount2 - 1)
			{
				for (int k = num; k < samplesCount - 1; k++)
				{
					list.Add(k);
					list.Add(k + 1);
					list.Add(num2);
				}
			}
			else
			{
				for (int l = num2; l < samplesCount + samplesCount2 - 1; l++)
				{
					list.Add(l);
					list.Add(num);
					list.Add(l + 1);
				}
			}
			triangles = list.ToArray();
		}
	}
}
