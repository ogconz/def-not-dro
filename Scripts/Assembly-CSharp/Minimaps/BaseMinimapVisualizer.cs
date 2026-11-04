using System.Collections.Generic;
using Binding;
using HUD;
using UnityEngine;
using UnityEngine.UI;

namespace Minimaps
{
	public class BaseMinimapVisualizer : MonoBehaviour
	{
		[SerializeField]
		protected RawImage m_mapImage;

		[SerializeField]
		private Transform m_poolIcons;

		[SerializeField]
		protected UIMinimapItem m_itemPrefab;

		[SerializeField]
		protected RectTransform m_mapRoot;

		protected StageMaterial m_stageMaterial1;

		protected List<(RaceCar, string)> m_carsList = new List<(RaceCar, string)>();

		protected List<UIMinimapItem> m_iconsList = new List<UIMinimapItem>(32);

		protected float m_carDist;

		protected bool m_dirtyList = true;

		private MinimapZone m_currentZone;

		private BoolProperty m_visible = new BoolProperty(startValue: true);

		private void Awake()
		{
			m_stageMaterial1 = new StageMaterial(m_mapImage);
			m_stageMaterial1.zoneId = -1;
		}

		protected virtual void OnEnable()
		{
			m_carsList = MinimapProvider.instance.cars;
			m_dirtyList = true;
			ReinitIconsIfDirtyList();
			MinimapProvider.instance.OnSwitchCar += OnSwitchCarHandler;
			MinimapProvider.OnRefreshMinimap += SetDirty;
		}

		protected virtual void OnDisable()
		{
			m_stageMaterial1.Restore();
			m_carsList = null;
			MinimapProvider.instance.OnSwitchCar -= OnSwitchCarHandler;
			MinimapProvider.OnRefreshMinimap -= SetDirty;
		}

		private void OnSwitchCarHandler()
		{
			m_dirtyList = true;
		}

		protected void UpdateIcons()
		{
			UpdateAllIcons();
			ReinitIconsIfDirtyList();
		}

		private void UpdateAllIcons()
		{
			for (int i = 0; i < m_iconsList.Count; i++)
			{
				UpdateIcon(m_iconsList[i]);
			}
		}

		protected void ReinitIconsIfDirtyList()
		{
			if (!m_dirtyList)
			{
				return;
			}
			m_dirtyList = false;
			MinimapProvider.SortList();
			m_iconsList.Clear();
			int count = MinimapProvider.itemList.Count;
			int num = Mathf.Max(count, m_poolIcons.childCount);
			for (int i = 0; i < num; i++)
			{
				UIMinimapItem uIMinimapItem = ((m_poolIcons.childCount <= i) ? Object.Instantiate(m_itemPrefab, m_poolIcons) : m_poolIcons.GetChild(i).GetComponent<UIMinimapItem>());
				if (i < count)
				{
					InitializeIcon(uIMinimapItem, MinimapProvider.itemList[i]);
				}
				MinimapItem item = uIMinimapItem.item;
				uIMinimapItem.gameObject.SetActive(item != null && IsNeedActivate(item, i < count));
			}
			OnItemListUpdated();
		}

		protected virtual void OnItemListUpdated()
		{
		}

		protected virtual bool IsNeedActivate(MinimapItem item, bool defaultShowValue)
		{
			return defaultShowValue;
		}

		private void InitializeIcon(UIMinimapItem icon, MinimapItem item)
		{
			icon.Init(item);
			icon.gameObject.SetActive(value: true);
			icon.size = MinimapHelper.defaultIconSize;
			if (item.isCheckpoint)
			{
				icon.size = new Vector2(item.size, 6f);
				icon.scale = Vector2.one;
			}
			else
			{
				icon.size = new Vector2(item.size, item.size);
				icon.scale = Vector2.one;
			}
			m_iconsList.Add(icon);
		}

		private void UpdateIcon(UIMinimapItem uiIcon)
		{
			if (uiIcon.rectTransform == null)
			{
				return;
			}
			MinimapItem item = uiIcon.item;
			if (!item)
			{
				return;
			}
			Vector2 vector = (uiIcon.position = MinimapHelper.WorldToMap(item.cachedTransform.position, m_mapImage.rectTransform));
			Vector2 vector3 = m_mapImage.rectTransform.sizeDelta * 0.5f;
			if (!(vector.x >= 0f - vector3.x) || !(vector.x <= vector3.x) || !(vector.y >= 0f - vector3.y) || !(vector.y <= vector3.y))
			{
				return;
			}
			uiIcon.color = item.color;
			if (item.isCheckpoint)
			{
				UpdateCheckpointItem(uiIcon);
				return;
			}
			uiIcon.angle = 0f - item.cachedTransform.eulerAngles.y;
			if (item is WaypointMinimapItem)
			{
				UpdateWaypointItem(uiIcon);
			}
		}

		protected virtual void UpdateCheckpointItem(UIMinimapItem uiCheckpointItem)
		{
		}

		protected virtual void UpdateWaypointItem(UIMinimapItem uiWaypointItem)
		{
			uiWaypointItem.transform.localRotation = Quaternion.identity;
			uiWaypointItem.gameObject.SetActive(uiWaypointItem.item.transform.position != Vector3.zero);
		}

		protected void UpdateMinimapTexture()
		{
			if (!MinimapProvider.instance.playerCar)
			{
				return;
			}
			m_carDist = MinimapProvider.instance.checkpointManager.GetForwardDistanceFromStart(MinimapProvider.instance.playerCar);
			if (MinimapProvider.instance.data.zoneList == null)
			{
				if (!m_stageMaterial1.IsApplied)
				{
					m_stageMaterial1.zoneId = 0;
					m_stageMaterial1.ApplyStage();
				}
				return;
			}
			if (m_currentZone == null || !m_currentZone.Exists(m_carDist))
			{
				m_currentZone = FindCheckpointRangeByDist(m_carDist);
			}
			if (m_currentZone == null)
			{
				m_stageMaterial1.Restore();
				return;
			}
			if (m_stageMaterial1.zoneId != m_currentZone.zoneId)
			{
				m_stageMaterial1.zoneId = (byte)m_currentZone.zoneId;
				m_stageMaterial1.ApplyStage();
			}
			if (!m_currentZone.fromZoneId.HasValue)
			{
				m_stageMaterial1.SetAlpha(1f);
				return;
			}
			float progress = m_currentZone.GetProgress(m_carDist);
			m_stageMaterial1.SetAlpha(progress);
		}

		private MinimapZone FindCheckpointRangeByDist(float dist)
		{
			return MinimapProvider.instance.data.zoneList.Find((MinimapZone zone) => zone.Exists(dist));
		}

		public void ResetZones()
		{
			m_stageMaterial1.Restore();
			m_stageMaterial1.zoneId = -1;
			m_currentZone = null;
			MinimapProvider.instance.ResetCheckpoints();
		}

		protected void SetupVisibility(bool isVisible)
		{
			m_visible.value = isVisible;
		}

		private void SetDirty()
		{
			m_dirtyList = true;
		}
	}
}
