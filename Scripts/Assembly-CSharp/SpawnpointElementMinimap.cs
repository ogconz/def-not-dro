using Binding;
using Binding.Base;
using TrackLayout.Data;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SpawnpointElementMinimap : BaseBindingBehaviourTarget, IElementItem
{
	private StringProperty textSpawnpoint = new StringProperty();

	private BoolProperty selectSpawnpoint = new BoolProperty();

	private RectTransform m_rectTransform;

	public void Construct(string name, RectTransform bound, SpawnPointData spawnPointData, MinimapData minimapData)
	{
		m_rectTransform = GetComponent<RectTransform>();
		Vector2 sizeDelta = bound.sizeDelta;
		textSpawnpoint.value = name;
		m_rectTransform.localPosition = WorldToMap(sizeDelta.x, spawnPointData.position, minimapData.boundsCenter, minimapData.boundsSize);
	}

	private Vector2 WorldToMap(float size, Vector3 worldPos, Vector2 boundCenter, Vector2 boundSize)
	{
		Vector2 vector = new Vector2(worldPos.x, worldPos.z) - boundCenter;
		float num = size / boundSize.x;
		return vector * num;
	}

	public void Select()
	{
		selectSpawnpoint.value = true;
	}

	public void UnSelect()
	{
		selectSpawnpoint.value = false;
	}

	public void UpdateElement()
	{
	}
}
