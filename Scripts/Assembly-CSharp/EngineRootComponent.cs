using System.Collections;
using System.Collections.Generic;
using DB.Meta;
using DI;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.ResourceManagement.AsyncOperations;

public class EngineRootComponent : MonoBehaviour
{
	[SerializeField]
	private GameObject objToHide;

	private const string ENGINE_PREFAB_TEMPLATE = "prefabs/carsexteriorparts/engines/{0}";

	private PlayerCarFactory.EnginesPoolManager m_enginesPool;

	private int m_currentEngineId = int.MinValue;

	private int m_prevEngineId;

	private RaceCar m_userCar;

	private PlayerCarFactory m_carFactory;

	private Coroutine m_initEngineCoroutine;

	private IEnumerator m_loadEngineCoroutine;

	private AsyncOperationHandle<GameObject> m_aohResource;

	private void Start()
	{
		RaceCar componentInParent = GetComponentInParent<RaceCar>();
		if (componentInParent != null)
		{
			m_initEngineCoroutine = TimeController.StartNow(LoadEnginePrefab(componentInParent, componentInParent.swap.meta));
		}
	}

	public IEnumerator LoadEnginePrefab(RaceCar car, EngineInfo engineInfo)
	{
		if (m_currentEngineId != engineInfo.id)
		{
			m_userCar = car;
			if (m_carFactory == null)
			{
				m_carFactory = DependencyInjector.Resolve<PlayerCarFactory>();
			}
			m_enginesPool = m_carFactory.enginesPool;
			m_currentEngineId = engineInfo.id;
			HideExtraObjects(m_currentEngineId == 0);
			m_loadEngineCoroutine = TryLoadEnginePrefabAsync();
			yield return m_loadEngineCoroutine;
		}
	}

	private void HideExtraObjects(bool needHide)
	{
		if (objToHide != null)
		{
			objToHide.SetActive(needHide);
		}
	}

	private IEnumerator TryLoadEnginePrefabAsync()
	{
		ReturnEnginesToPoolIfNeed();
		if (m_currentEngineId > 0 && !m_userCar.metaInfo.isMidEngine)
		{
			GetFromPool(m_currentEngineId);
		}
		else
		{
			PlayerCar metaInfo = m_userCar.metaInfo;
			string text = ((metaInfo.isMidEngine && m_currentEngineId != 0) ? metaInfo.midEngine : metaInfo.stockEnginePrefab);
			bool flag = false;
			int engineId = 0;
			foreach (EngineInfo swap in SwapsAsset.LoadAndInitDB().swaps)
			{
				if (swap.prefab.name == text)
				{
					flag = true;
					engineId = swap.id;
					break;
				}
			}
			if (flag)
			{
				GetFromPool(engineId);
			}
			else
			{
				m_aohResource = ResourcesWrapper.LoadAsync<GameObject>($"prefabs/carsexteriorparts/engines/{text.ToLower()}");
				yield return m_aohResource;
				if (m_aohResource.IsValid() && (bool)base.transform)
				{
					m_enginesPool.poolApplication.GetGameObjectFromPool(m_aohResource.Result, base.transform.position, base.transform);
				}
			}
		}
		m_prevEngineId = m_currentEngineId;
		void GetFromPool(int swapId)
		{
			GameObject obj = m_enginesPool.GivePrefabPool(swapId);
			obj.transform.SetParent(base.transform);
			obj.ResetPRS();
		}
	}

	private void OnDestroy()
	{
		if (m_initEngineCoroutine != null)
		{
			TimeController.StopNow(m_initEngineCoroutine);
			m_initEngineCoroutine = null;
		}
		if (m_loadEngineCoroutine != null)
		{
			TimeController.StopNow(m_loadEngineCoroutine);
			m_loadEngineCoroutine = null;
		}
		ReturnEnginesToPoolIfNeed();
		ResourcesWrapper.UnloadAsset(m_aohResource);
	}

	private void ReturnEnginesToPoolIfNeed()
	{
		int childCount = base.transform.childCount;
		if (childCount == 0)
		{
			return;
		}
		List<Transform> list = new List<Transform>(childCount);
		for (int i = 0; i < childCount; i++)
		{
			list.Add(base.transform.GetChild(i));
		}
		foreach (Transform item in list)
		{
			item.SetLightLayer(RenderingLayerMask.RenderingLayer1);
			m_enginesPool?.ReturnToPool(item.gameObject);
		}
	}
}
