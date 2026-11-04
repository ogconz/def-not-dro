using System;
using System.Collections.Generic;
using UnityEngine;

public class GUICommonNickNames : MonoBehaviour
{
	[SerializeField]
	private UICarPlayerName m_nickNamePrefab;

	public float fadeStart = 200f;

	public float fadeDist = 100f;

	public float heightDist = 50f;

	public float heightBiass = 0.2f;

	private static GUICommonNickNames m_instance;

	private static Dictionary<Transform, UICarPlayerName> m_map = new Dictionary<Transform, UICarPlayerName>();

	private static List<UICarPlayerName> m_playerNicksNames = new List<UICarPlayerName>();

	private Action m_customUpdateAction;

	public static GUICommonNickNames instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_customUpdateAction = CustomUpdate;
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	private void LateUpdate()
	{
		m_playerNicksNames.Sort((UICarPlayerName x1, UICarPlayerName x2) => x2.dist.CompareTo(x1.dist));
		for (int num = 0; num < m_playerNicksNames.Count; num++)
		{
			m_playerNicksNames[num].transform.SetSiblingIndex(num);
		}
		UICustomUpdater.RegisterAction(m_customUpdateAction);
	}

	private void CustomUpdate()
	{
		for (int i = 0; i < m_playerNicksNames.Count; i++)
		{
			m_playerNicksNames[i].CustomUpdate();
		}
	}

	public static void UpdateNickName(RaceCar parent, string name)
	{
		if (!(parent == null) && !(m_instance == null) && m_map.TryGetValue(parent.transform, out var value))
		{
			value.UpdateNickName(name);
		}
	}

	public static void SpawnNickName(RaceCar parent, string name)
	{
		if (!(parent == null) && !(m_instance == null) && !m_map.ContainsKey(parent.transform))
		{
			if ((bool)m_instance.m_nickNamePrefab)
			{
				UICarPlayerName uICarPlayerName = UnityEngine.Object.Instantiate(m_instance.m_nickNamePrefab, m_instance.transform);
				m_map.Add(parent.transform, uICarPlayerName);
				m_playerNicksNames.Add(uICarPlayerName);
				uICarPlayerName.Set(parent, name);
			}
			else
			{
				Debug.LogError("GUICommonNickNames.SpawnNickName() - m_nickNamePrefab == null");
			}
		}
	}

	public static void SetVisibleAllNickNames(bool show)
	{
		foreach (UICarPlayerName playerNicksName in m_playerNicksNames)
		{
			playerNicksName.SetVisible(show);
		}
	}

	public static void SetVisibleNickName(RaceCar parent, bool show)
	{
		if (m_map.TryGetValue(parent.transform, out var value))
		{
			value.SetVisible(show);
		}
	}

	public static void DisposeNick(RaceCar parent)
	{
		if (m_map.TryGetValue(parent.transform, out var value))
		{
			m_playerNicksNames.Remove(value);
			m_map.Remove(parent.transform);
			if ((bool)value)
			{
				UnityEngine.Object.Destroy(value.gameObject);
			}
		}
	}
}
