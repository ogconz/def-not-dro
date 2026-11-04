using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerCarState : MonoBehaviour
{
	[Serializable]
	public struct PlayerStatePanel
	{
		public PlayerCarState state;

		public GameObject panel;
	}

	[SerializeField]
	private List<PlayerStatePanel> m_panels = new List<PlayerStatePanel>();

	[SerializeField]
	private RectTransform m_rootLayout;

	private List<RectTransform> m_layoutGroups = new List<RectTransform>();

	private PlayerCarState m_current;

	public PlayerCarState current
	{
		get
		{
			return m_current;
		}
		set
		{
			if (!m_current.Equals(value))
			{
				m_current = value;
				RefreshPanels();
			}
		}
	}

	private void Awake()
	{
		CacheLayoutGroups();
	}

	private void Start()
	{
		RefreshPanels();
	}

	private void RefreshPanels()
	{
		foreach (PlayerStatePanel panel in m_panels)
		{
			panel.panel.SetActive(current.HasFlag(panel.state));
		}
		RefreshLayoutGroups();
	}

	private void RefreshLayoutGroups()
	{
		foreach (RectTransform layoutGroup in m_layoutGroups)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup);
		}
		if ((bool)m_rootLayout)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(m_rootLayout);
		}
	}

	private void CacheLayoutGroups()
	{
		m_layoutGroups.Clear();
		LayoutGroup[] componentsInChildren = GetComponentsInChildren<LayoutGroup>(includeInactive: true);
		foreach (LayoutGroup layoutGroup in componentsInChildren)
		{
			m_layoutGroups.Add(layoutGroup.GetComponent<RectTransform>());
		}
	}
}
