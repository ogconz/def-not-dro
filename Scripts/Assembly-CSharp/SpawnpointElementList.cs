using System;
using TrackLayout.Data;
using UnityEngine;

[Serializable]
public class SpawnpointElementList : ElementList<SpawnpointElementMinimap>
{
	[SerializeField]
	private SpawnpointElementMinimap m_element;

	[SerializeField]
	private RectTransform m_bound;

	[SerializeField]
	private RectTransform m_content;

	private PoolObject m_pool = new PoolObject();

	public void Add(string name, SpawnPointData pointData, MinimapData minimapData)
	{
		if (m_pool != null && !(m_content == null))
		{
			SpawnpointElementMinimap spawnpointElementMinimap = m_pool.Pop(m_element);
			spawnpointElementMinimap.UnSelect();
			spawnpointElementMinimap.transform.SetParent(m_content);
			spawnpointElementMinimap.Construct(name, m_bound, pointData, minimapData);
			m_subItems.Add(spawnpointElementMinimap);
			GetSelectedItem()?.Select();
			RectTransform component = spawnpointElementMinimap.GetComponent<RectTransform>();
			if (component != null)
			{
				component.localRotation = Quaternion.identity;
				component.localScale = Vector3.one;
			}
		}
	}

	public void Clear()
	{
		foreach (SpawnpointElementMinimap subItem in m_subItems)
		{
			m_pool.Push(subItem);
		}
		m_subItems.Clear();
	}

	public void ClearForce()
	{
		m_pool.Dispose();
		m_subItems.Clear();
	}
}
