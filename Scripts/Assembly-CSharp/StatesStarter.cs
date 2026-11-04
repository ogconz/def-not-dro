using System;
using System.Collections;
using CarModelSystem.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VinylSystem;

public class StatesStarter : BaseLoggingMonobeh
{
	public enum EStartStateType
	{
		StartState = 0
	}

	[SerializeField]
	private EStartStateType m_startStateType;

	[SerializeField]
	private AssetReferenceGameObject m_keepAlivePrefab;

	public Type startStateType
	{
		get
		{
			if (m_startStateType == EStartStateType.StartState)
			{
				return typeof(StartState);
			}
			return null;
		}
	}

	protected override void AwakeHandler()
	{
		StartCoroutine(Init());
	}

	private IEnumerator Init()
	{
		CarModelConfig.InitConfig(Resources.Load<CarModelConfig>("CarModelConfig"));
		VinylSystemConfig.InitConfig(ResourcesWrapper.Load<VinylSystemConfig>("VinylSystemConfig"));
		CARXSurfaceManager.InitTemplates(ResourcesWrapper.LoadAll<CARXSurfaceTemplate>("SurfaceTemplate"));
		States.PrepareAutoStartState(startStateType);
		yield return m_keepAlivePrefab.InstantiateAsync();
		UnityEngine.Object.Destroy(base.gameObject);
		FocusState.ForceActive();
	}
}
