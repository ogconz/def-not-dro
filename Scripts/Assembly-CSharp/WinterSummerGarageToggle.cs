using System;
using ArrayExtension;
using Custom.SdrConfigs;
using UnityEngine;

public class WinterSummerGarageToggle : MonoBehaviour
{
	[SerializeField]
	private GameObject[] m_summerAssets;

	[SerializeField]
	private GameObject[] m_winterAssets;

	private void OnEnable()
	{
		if (ViewConfigManager.isReceived && ViewConfigManager.isInited)
		{
			ApplyAsset(ViewConfigManager.sdrRestViewConfig.viewWinterGarageEnable);
		}
		else
		{
			ApplyAsset(IsWinterTime(DateTime.UtcNow));
		}
	}

	private void ApplyAsset(bool isWinter)
	{
		m_summerAssets?.ForEach(delegate(GameObject go)
		{
			go.SetActive(!isWinter);
		});
		m_winterAssets?.ForEach(delegate(GameObject go)
		{
			go.SetActive(isWinter);
		});
	}

	private bool IsWinterTime(DateTime dateTime)
	{
		int month = dateTime.Month;
		if (month < 12)
		{
			return month <= 1;
		}
		return true;
	}
}
