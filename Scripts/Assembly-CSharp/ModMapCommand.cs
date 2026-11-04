using System.Collections;
using FMODUnity;
using Unity.Mathematics;
using UnityEngine;

public class ModMapCommand : Validator.MethodStringInvoker<ModMapCommand>
{
	private GameMarkerData m_markerData;

	public override void InvokeCommand(GameObject data)
	{
		m_markerData = data.GetComponent<GameMarkerData>();
		if (!(m_markerData == null) && !(m_markerData.gameObject == null) && m_markerData.gameObject.activeSelf)
		{
			Invoke(m_markerData.markerData.head.ToLower());
		}
	}

	public void SpawnPoint()
	{
		TimeController.StartNow(SpawnCar(m_markerData.transform));
		static IEnumerator SpawnCar(Transform root)
		{
			yield return new WaitUntil(() => PlayerCarControl.instance != null && !BaseCar.IsNull(PlayerCarControl.instance.car));
			PlayerCarControl.instance.car.ugcMapSpawnPoint = root;
			PlayerCarControl.instance.car.ResetToSpawnPointOnUgcMap();
			GameLog.Print(AlertLevel.Notify, "ModMaps", $"spawn car at {root.position}");
		}
	}

	public void Road()
	{
		CARXSurface cARXSurface = m_markerData.gameObject.AddComponent<CARXSurface>();
		string[] array = m_markerData.markerData.param?.Split('/');
		if (cARXSurface == null)
		{
			return;
		}
		GameObject gameObject = cARXSurface.gameObject;
		if (array != null && array.Length > 1)
		{
			gameObject.layer = 11;
			cARXSurface.templateName = array[1];
			cARXSurface.UpdateTemplate();
			if (m_markerData.markerData.value is PhysicMaterialProperties physicMaterialProperties)
			{
				physicMaterialProperties.SetProperties(cARXSurface);
			}
			if (cARXSurface.template != null)
			{
				cARXSurface.template.allowMeshSmooth = false;
				gameObject.SetActive(value: false);
				gameObject.SetActive(value: true);
			}
		}
	}

	public void Ambient()
	{
		StudioEventEmitter studioEventEmitter = m_markerData.gameObject.AddComponent<StudioEventEmitter>();
		ModMapFmodDBAsset.FmodData fmodData = SingletonScriptableObject<ModMapFmodDBAsset>.instance.Get("event:/GameEvents/" + m_markerData.markerData.param);
		if (fmodData.eventRef.IsNull)
		{
			GameLog.Print(AlertLevel.Warning, "Fmod mod map", "Not existing sound on ModMapFmodDBAsset");
			return;
		}
		studioEventEmitter.EventReference = fmodData.eventRef;
		studioEventEmitter.PlayEvent = EmitterGameEvent.ObjectEnable;
		studioEventEmitter.StopEvent = EmitterGameEvent.ObjectDisable;
		studioEventEmitter.OverrideAttenuation = true;
		Vector3 lossyScale = m_markerData.transform.lossyScale;
		studioEventEmitter.OverrideMaxDistance = math.max(lossyScale.x, math.max(lossyScale.y, lossyScale.z)) / 2f;
		studioEventEmitter.Play();
	}

	public void NetworkObject()
	{
		m_markerData.gameObject.tag = "NetworkObject";
	}
}
