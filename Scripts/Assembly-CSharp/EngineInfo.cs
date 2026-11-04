using System;
using DB.Meta;
using UnityEngine;

[Serializable]
public class EngineInfo
{
	[Flags]
	public enum Type
	{
		I4 = 1,
		I6 = 2,
		V6 = 4,
		V8 = 8,
		V10 = 0x10,
		V12 = 0x20,
		Rotary = 0x40,
		Opposite = 0x80,
		Other = 0x100
	}

	public int id;

	public string itemId;

	public string name;

	public Type type;

	public Sprite sprite;

	public Sprite logo;

	public string partItem;

	public int partsToInstall;

	public string prefabPath;

	public string descriptionLocalizationKey;

	public string sound;

	public GameObject prefab;

	public float weight;

	public float inertiaRatio;

	public float brakeTorqueRatio;

	public float revLimiterStep;

	public float cutRpm;

	public float idleRpm;

	public EngineParamInfo turboCharged;

	public EngineParamInfo turboPressure;

	public EngineParamInfo revLimiter;

	public EngineParamShiftInfo maxTorque;

	public EngineParamInfo maxTorqueRpm;

	public int poolInstantiateCount;

	public string GetEngineIdFromCar(int carId)
	{
		PlayerCarProfile profileByCar = PlayerCarProfileMetaExtensions.GetProfileByCar(carId);
		return GetEngineIdFromProfile(profileByCar);
	}

	public string GetEngineIdFromProfile(PlayerCarProfile profileMeta)
	{
		if (profileMeta.IsUltimate())
		{
			return $"{profileMeta.carId}_{itemId.ToLower()}";
		}
		return $"{profileMeta.carId}_{profileMeta.id}_{itemId.ToLower()}";
	}
}
