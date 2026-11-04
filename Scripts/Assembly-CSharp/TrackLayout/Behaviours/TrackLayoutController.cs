using System.Collections.Generic;
using AssetCore;
using DI;
using RenderCore.UI;
using TrackLayout.Builders;
using TrackLayout.Data;
using TrackLayout.Style;
using UnityEngine;
using Utils.SceneManagement;

namespace TrackLayout.Behaviours
{
	public class TrackLayoutController : Singleton<TrackLayoutController>
	{
		private const string FIRST_RACE_CONFIG = "xds_firstRace";

		private const string SECOND_RACE_CONFIG = "xds_secondRace";

		private const string SECOND_SNOW_RACE_CONFIG = "xds_secondSnowRace";

		private IgnoreFlags m_trackDisplayIgnoreFlags;

		private TrackLayoutData m_data;

		private string m_previousSceneName;

		private bool m_placementCached;

		private IList<RadarStyle> m_radarStyles;

		private IList<TrackStyle> m_trackStyles;

		private RadarContainer m_previewContainer;

		private LegacyTrackContainer m_trackContainer;

		private List<int> m_minimapPlayerMarkIndices;

		private Transform m_previewWorkaroundParent;

		private Transform m_previewParent;

		private Transform m_trackParent;

		private string m_previewStyleName;

		private string m_trackStyleName;

		private AdditiveSceneController m_additiveSceneLoadController = new AdditiveSceneController();

		private bool m_needDelayedPlacementReset;

		private bool m_isSameScene => m_previousSceneName == m_additiveSceneLoadController.lastSceneName;

		public LegacyTrackContainer legacyTrackContainer
		{
			get
			{
				return m_trackContainer;
			}
			set
			{
				m_trackContainer = value;
			}
		}

		protected override void Initialization()
		{
			base.Initialization();
			m_radarStyles = ResourcesWrapper.LoadAll<RadarStyle>("RadarStyle");
			m_trackStyles = ResourcesWrapper.LoadAll<TrackStyle>("TrackStyle");
			m_minimapPlayerMarkIndices = new List<int>();
		}

		protected override void Finalization()
		{
			m_additiveSceneLoadController.Unload();
		}

		public RadarStyle GetRadarStyle()
		{
			return GetRadarStyle(m_previewStyleName);
		}

		public RadarStyle GetRadarStyle(string name)
		{
			RadarStyle radarStyle = null;
			for (int i = 0; i < m_radarStyles.Count; i++)
			{
				if (m_radarStyles[i].name == name)
				{
					radarStyle = m_radarStyles[i];
					break;
				}
				if (radarStyle == null && m_radarStyles[i].name == "default")
				{
					radarStyle = m_radarStyles[i];
				}
			}
			return radarStyle;
		}

		private TrackStyle GetTrackStyle(string name)
		{
			TrackStyle trackStyle = null;
			for (int i = 0; i < m_trackStyles.Count; i++)
			{
				if (m_trackStyles[i].name == name)
				{
					trackStyle = m_trackStyles[i];
					break;
				}
				if (trackStyle == null && m_trackStyles[i].name == "default")
				{
					trackStyle = m_trackStyles[i];
				}
			}
			return trackStyle;
		}

		public void SetTrackDisplayIgnoreFlag(IgnoreFlags ignoreFlag, bool on)
		{
			if (on)
			{
				m_trackDisplayIgnoreFlags |= ignoreFlag;
			}
			else
			{
				m_trackDisplayIgnoreFlags &= ~ignoreFlag;
			}
			UpdateTrackVisibility();
		}

		public void SetDataRAW(TrackLayoutData data, bool destroyActive = false)
		{
			if (!(m_data == data))
			{
				m_data = data;
				if (destroyActive)
				{
					DestroyPreview();
					DestroyTrack();
					m_previewParent = null;
					m_trackParent = null;
				}
			}
		}

		public void SetData(TrackLayoutData data)
		{
			if (!(m_data == data))
			{
				m_data = data;
				DestroyPreview();
				DestroyTrack();
				BuildPreview();
				BuildTrack();
				BindPreview(m_previewParent);
				BindTrack(m_trackParent);
			}
		}

		public TrackLayoutData GetData()
		{
			return m_data;
		}

		public void LoadAdditionalScene(string name)
		{
			m_additiveSceneLoadController.LoadAdditionalScene(name);
		}

		public void PreparePlacement()
		{
			if (m_additiveSceneLoadController == null || !m_additiveSceneLoadController.lastScene.IsValid())
			{
				return;
			}
			GameObject[] rootGameObjects = m_additiveSceneLoadController.lastScene.GetRootGameObjects();
			for (int i = 0; i < rootGameObjects.Length; i++)
			{
				if (rootGameObjects[i].TryGetComponent<Placement>(out var component))
				{
					RestoreManager.instance.PrepareCacheByPlacement(component);
				}
			}
		}

		public void ResetPlacementOnAdditionalScene(bool forceDelayToReset = false)
		{
			if (string.IsNullOrEmpty(m_additiveSceneLoadController.lastSceneName) || !m_additiveSceneLoadController.lastScene.IsValid())
			{
				m_previousSceneName = m_additiveSceneLoadController.lastSceneName;
				RestoreManager.instance.ClearCachedPlacement();
				return;
			}
			if (!m_isSameScene)
			{
				RestoreManager.instance.ClearCachedPlacement();
			}
			if (!forceDelayToReset && m_additiveSceneLoadController.lastScene.isLoaded)
			{
				if (!m_isSameScene)
				{
					PreparePlacement();
				}
				m_needDelayedPlacementReset = false;
				m_previousSceneName = m_additiveSceneLoadController.lastSceneName;
			}
			else
			{
				m_needDelayedPlacementReset = true;
			}
		}

		private void SetRenderersEnabled(Renderer[] renderers, bool enabled)
		{
			for (int i = 0; i < renderers.Length; i++)
			{
				renderers[i].enabled = enabled;
			}
		}

		private void UpdateTrackVisibility()
		{
			if (m_trackContainer != null)
			{
				if (m_trackContainer.perfectLine != null)
				{
					SetRenderersEnabled(m_trackContainer.perfectLine.GetComponentsInChildren<Renderer>(), (m_trackDisplayIgnoreFlags & IgnoreFlags.PerfectLine) == 0);
				}
				for (int i = 0; i < m_trackContainer.clipZones.Count; i++)
				{
					SetRenderersEnabled(m_trackContainer.clipZones[i].GetComponentsInChildren<Renderer>(), (m_trackDisplayIgnoreFlags & IgnoreFlags.ClipZones) == 0);
				}
				for (int j = 0; j < m_trackContainer.checkpoints.Count; j++)
				{
					SetRenderersEnabled(m_trackContainer.checkpoints[j].GetComponentsInChildren<Renderer>(), (m_trackDisplayIgnoreFlags & IgnoreFlags.Checkpoints) == 0);
				}
			}
		}

		private void DestroyPreview()
		{
			if (m_previewContainer != null && m_previewWorkaroundParent != null)
			{
				GameObject gameObject = ((m_previewWorkaroundParent == null) ? null : m_previewWorkaroundParent.gameObject);
				if ((bool)gameObject)
				{
					Object.Destroy(gameObject);
				}
				m_previewContainer = null;
			}
		}

		private void DestroyTrack()
		{
			if (m_trackContainer != null && m_trackContainer.root != null)
			{
				Object.Destroy(m_trackContainer.root.gameObject);
				m_trackContainer = null;
			}
		}

		private void BuildPreview()
		{
			if (m_data != null && m_previewParent != null)
			{
				m_previewContainer = RadarBuilder.Build(m_data, GetRadarStyle(m_previewStyleName), previewAlignment: true);
				for (int i = 0; i < m_previewContainer.root.childCount; i++)
				{
					m_previewContainer.root.GetChild(i).gameObject.AddComponent<CustomMaskable>();
				}
				GameObject obj = new GameObject("workaroundRoot");
				RectTransform rectTransform = (RectTransform)(m_previewWorkaroundParent = obj.AddComponent<RectTransform>());
				rectTransform.sizeDelta = m_previewContainer.size;
				rectTransform.SetParent(m_previewParent);
				rectTransform.localScale = Vector3.one;
				rectTransform.localPosition = Vector3.zero;
				rectTransform.localRotation = Quaternion.identity;
				SetParent(rectTransform, m_previewContainer.root);
				rectTransform.anchorMin = new Vector2(0f, 0f);
				rectTransform.anchorMax = new Vector2(1f, 1f);
				obj.gameObject.AddComponent<TrackPreviewFitter>().referenceSize = m_previewContainer.size;
			}
		}

		private void BuildTrack()
		{
			if (m_data != null && m_trackParent != null)
			{
				int lastTrackId = DependencyInjector.Resolve<GamePrefs>().globals.lastTrackId;
				string text = ((m_trackStyleName == "xds_secondRace" && TrackMetaDB.IsWinterTrackWithIce(lastTrackId)) ? "xds_secondSnowRace" : m_trackStyleName);
				m_trackContainer = TrackBuilder.BuildLegacy(m_data, GetTrackStyle(text));
				SetParent(m_trackParent, m_trackContainer.root);
				m_trackContainer.root.gameObject.SetActive(value: true);
				UpdateTrackVisibility();
			}
		}

		public void BindPreview(Transform parent, string styleName = null)
		{
			if (!(m_previewParent == parent) || !(m_previewStyleName == styleName))
			{
				DestroyPreview();
				m_previewParent = parent;
				m_previewStyleName = styleName;
				BuildPreview();
			}
		}

		public void BindTrack(Transform parent, string styleName = null)
		{
			if (!(m_trackParent == parent) || !(m_trackStyleName == styleName))
			{
				DestroyTrack();
				m_trackParent = parent;
				m_trackStyleName = styleName;
				BuildTrack();
			}
		}

		public IMovable CreateBlip(BlipType type, bool visible)
		{
			if (m_previewContainer != null)
			{
				return RadarBuilder.CreateBlip(type, m_previewContainer, GetRadarStyle("normal"), visible);
			}
			return null;
		}

		public void DestroyBlip(IMovable movable)
		{
			if (m_previewContainer == null || !(movable is BlipData blipData))
			{
				return;
			}
			int num = m_previewContainer.blips.IndexOf(blipData);
			if (num >= 0)
			{
				if (blipData.transform != null)
				{
					Object.Destroy(blipData.transform.gameObject);
				}
				m_previewContainer.blips.RemoveAt(num);
			}
		}

		public Vector3 GetPreviewStartWorldPosition()
		{
			if (m_previewContainer != null)
			{
				return new Vector3(m_previewContainer.startPosition.x, m_previewContainer.startPosition.z, m_previewContainer.startPosition.y);
			}
			return Vector3.zero;
		}

		public Vector3 GetPreviewFinishWorldPosition()
		{
			if (m_previewContainer != null)
			{
				return new Vector3(m_previewContainer.finishPosition.x, m_previewContainer.finishPosition.z, m_previewContainer.finishPosition.y);
			}
			return Vector3.zero;
		}

		private void Update()
		{
			if (m_previewContainer != null)
			{
				Vector3 vector = default(Vector3);
				for (int i = 0; i < m_previewContainer.blips.Count; i++)
				{
					BlipData blipData = m_previewContainer.blips[i];
					if (!(blipData.transform != null))
					{
						continue;
					}
					if (blipData.wasPositionChanged)
					{
						vector.x = blipData.position.x;
						vector.y = blipData.position.z;
						vector.z = blipData.position.y;
						float y = blipData.rotation.eulerAngles.y;
						Quaternion localRotation = Quaternion.Euler(0f, 0f, 0f - y);
						blipData.transform.localPosition = vector - m_previewContainer.origin;
						blipData.transform.localRotation = localRotation;
						blipData.wasPositionChanged = false;
					}
					if (blipData.wasVisibilityChanged)
					{
						if (blipData.visible && blipData.crossFadeElapsedTime < blipData.crossFadeDuration)
						{
							blipData.crossFadeElapsedTime += Time.unscaledDeltaTime;
							blipData.crossFadeElapsedTime = Mathf.Min(blipData.crossFadeElapsedTime, blipData.crossFadeDuration);
						}
						else if (!blipData.visible && blipData.crossFadeElapsedTime > 0f)
						{
							blipData.crossFadeElapsedTime -= Time.unscaledDeltaTime;
							blipData.crossFadeElapsedTime = Mathf.Max(blipData.crossFadeElapsedTime, 0f);
						}
						float num = blipData.crossFadeElapsedTime / blipData.crossFadeDuration;
						Color color = blipData.image.color;
						color.a = num;
						blipData.image.color = color;
						if (num == 0f || num == 1f)
						{
							blipData.wasVisibilityChanged = false;
						}
					}
				}
			}
			if (m_needDelayedPlacementReset && !string.IsNullOrEmpty(m_additiveSceneLoadController.lastSceneName) && m_additiveSceneLoadController.lastScene.IsValid() && m_additiveSceneLoadController.lastScene.isLoaded)
			{
				if (!m_isSameScene)
				{
					PreparePlacement();
				}
				m_needDelayedPlacementReset = false;
				m_previousSceneName = m_additiveSceneLoadController.lastSceneName;
			}
			m_additiveSceneLoadController.Update();
		}

		private static void SetParent(Transform parent, Transform root)
		{
			root.SetParent(parent, worldPositionStays: false);
			root.localPosition = Vector3.zero;
			root.localScale = Vector3.one;
		}

		private static void Destroy(LegacyTrackContainer container)
		{
			if (container != null && !(container.root == null))
			{
				for (int i = 0; i < container.root.childCount; i++)
				{
					Object.Destroy(container.root.GetChild(i).gameObject);
				}
				Object.DestroyImmediate(container.root.gameObject);
			}
		}
	}
}
