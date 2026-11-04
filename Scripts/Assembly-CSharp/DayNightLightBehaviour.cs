using System.Collections.Generic;
using UnityEngine;

public class DayNightLightBehaviour : IDayNightBehaviour
{
	[SerializeField]
	private List<GameObject> lightGameObjects = new List<GameObject>();

	protected override void Start()
	{
		base.Start();
		if (lightGameObjects.Count == 0)
		{
			PickAllLights();
		}
	}

	public override void ChangeToDay()
	{
		for (int i = 0; i < lightGameObjects.Count; i++)
		{
			lightGameObjects[i].SetActive(value: false);
		}
	}

	public override void ChangeToNight(bool force = false)
	{
		for (int i = 0; i < lightGameObjects.Count; i++)
		{
			lightGameObjects[i].SetActive(value: true);
		}
	}

	[ContextMenu("Pick All Lights")]
	public void PickAllLights()
	{
		List<Light> list = new List<Light>();
		GetComponentsInChildren(list);
		lightGameObjects.Clear();
		for (int i = 0; i < list.Count; i++)
		{
			lightGameObjects.Add(list[i].gameObject);
		}
	}
}
