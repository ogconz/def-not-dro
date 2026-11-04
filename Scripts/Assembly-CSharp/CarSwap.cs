using System.Collections;
using CarProfile;
using CarX;
using DB;
using DB.Meta;
using DI;
using Db.Meta.CarProfile;
using PrefsModels;
using UnityEngine;

public class CarSwap
{
	private EngineInfo m_engineInfo;

	private SwapsAsset m_swapsAsset;

	private GamePrefs m_prefs;

	private BaseModel m_model;

	private RaceCar m_car;

	private EngineRootComponent m_engineRoot;

	private Transform m_engineTransform;

	private WaitForEndOfFrame m_waitEndFrame = new WaitForEndOfFrame();

	private bool m_stockPrefabLoading;

	private bool m_swapInstalled;

	public bool isStockEngine => engineId <= 0;

	public int engineId => m_engineInfo?.id ?? (-1);

	public string engineName => m_engineInfo?.name;

	public EngineInfo meta => m_engineInfo;

	public Vector3 engineRootLocalPosition
	{
		get
		{
			if (!(m_engineTransform == null))
			{
				return m_engineTransform.transform.localPosition;
			}
			return Vector3.zero;
		}
	}

	public bool carAttached => m_car != null;

	public IEnumerator Initialize(RaceCar car, GamePrefs prefs, BaseModel model, int overrideEngineId = -1, bool force = false, PlayerCarProfile carProfile = null)
	{
		m_swapsAsset = SwapsAsset.LoadAndInitDB();
		m_prefs = prefs;
		m_model = model;
		m_car = car;
		m_engineTransform = car.GetComponentInChildren<FMODCarEngine>().transform;
		ResetEngineIfNeed(ref overrideEngineId);
		yield return Refresh(overrideEngineId, force);
	}

	public IEnumerator Refresh(int eid, bool force = false)
	{
		if (engineId != eid || force || engineId == -1)
		{
			if (eid < 0)
			{
				eid = m_prefs.carSettings.GetEngineIdForCar(m_car.carId);
			}
			m_engineInfo = m_swapsAsset.Find(eid);
			yield return RefreshPrefab();
			RefreshSound();
		}
	}

	private void ResetEngineIfNeed(ref int engineId)
	{
		if (!m_car.isNetworkCar)
		{
			int id = m_prefs.carSettings.GetInstalledCustomProfile(m_car.carId)?.profile.id ?? m_prefs.carSettings.GetProfileIdForCar(m_car.carId);
			PlayerCarProfile byId = m_model.GetById<PlayerCarProfile>(id);
			if (engineId == -1 && !byId.CanSwap())
			{
				engineId = 0;
			}
		}
	}

	private void RefreshSound()
	{
		if (!isStockEngine && string.IsNullOrWhiteSpace(meta.sound))
		{
			GameLog.Print(AlertLevel.Error, "Swaps", "[Swap]: Engine sound is not set for '" + meta.name + "'");
			meta.sound = m_car.metaInfo.name;
		}
		if (!(m_car == null))
		{
			FMODCarEngine componentInChildren = m_car.GetComponentInChildren<FMODCarEngine>();
			if (componentInChildren != null)
			{
				componentInChildren.RefreshSound();
			}
			FMODCarTurbo componentInChildren2 = m_car.GetComponentInChildren<FMODCarTurbo>();
			if (componentInChildren2 != null)
			{
				componentInChildren2.RefreshSound();
			}
			FMODPopExhaust componentInChildren3 = m_car.GetComponentInChildren<FMODPopExhaust>();
			if (componentInChildren3 != null)
			{
				componentInChildren3.RefreshSound();
			}
		}
	}

	public void SetupEngineParams(BaseCar.Desc desc, PlayerCarProfile carProfile, string customSetupId, bool isNetworkCar, bool skipControllers = false, EngineSwapData swapOverride = null)
	{
		if (m_prefs == null)
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
		}
		if (m_car == null || (!isNetworkCar && !m_prefs.carSettings.SwapAvailableForCar(m_car.carId)))
		{
			return;
		}
		bool flag = !IsProfileHaveMassChanger(carProfile);
		float carProfileMass = m_car.GetCarProfileMass(carProfile.asset);
		if (meta == null || isStockEngine)
		{
			if (flag)
			{
				desc.carXDesc.weight.mass = carProfileMass;
			}
			desc.carXDesc.engine.revLimiterStep = m_car.GetRevLimiterStep(carProfile.asset);
			return;
		}
		EngineSwapData engineSwapData = ((swapOverride != null) ? swapOverride : ((!isNetworkCar) ? m_prefs.carSettings.GetEngineSwapDataForCar(m_car.carId, engineId, customSetupId) : CarSwapData.GetDefaultMeta(engineId)));
		int id = engineSwapData?.id ?? meta.id;
		EngineInfo engineInfo = m_swapsAsset.Find(id);
		CarDesc.Engine engine = desc.carXDesc.engine;
		if (!Mathf.Approximately(meta.turboCharged.value, 1f))
		{
			engine.turboCharged = false;
		}
		else if (!skipControllers)
		{
			engine.turboCharged = GetControllerValue(engineInfo, engineSwapData, "turbo_charged") > 0.5f;
		}
		if (!skipControllers)
		{
			engine.turboPressure = GetControllerValue(engineInfo, engineSwapData, "turbo_pressure");
			engine.revLimiter = GetControllerValue(engineInfo, engineSwapData, "rev_limiter");
			engine.maxTorque = GetControllerValue(engineInfo, engineSwapData, "max_torque");
			engine.maxTorqueRPM = GetControllerValue(engineInfo, engineSwapData, "max_torque_rpm");
		}
		engine.inertiaRatio = engineInfo.inertiaRatio;
		engine.brakeTorqueRatio = engineInfo.brakeTorqueRatio;
		engine.revLimiterStep = engineInfo.revLimiterStep;
		engine.cutRPM = engineInfo.cutRpm;
		engine.idleRPM = engineInfo.idleRpm;
		if (flag)
		{
			float mass = carProfileMass - m_car.metaInfo.stockEngineWeight + meta.weight;
			desc.carXDesc.weight.mass = mass;
		}
	}

	private bool IsProfileHaveMassChanger(PlayerCarProfile profile)
	{
		if (profile == null)
		{
			return false;
		}
		foreach (CarProfileController controller in profile.dynoInfo.controllers)
		{
			foreach (CarProfileChanger changer in controller.changers)
			{
				if (changer.parameter.type == CarProfileParameterType.Mass)
				{
					return true;
				}
			}
		}
		return false;
	}

	private float GetControllerValue(EngineInfo swapEngineInfo, EngineSwapData savedSwapData, string controllerId)
	{
		EngineParamInfo engineParamInfo = controllerId switch
		{
			"turbo_charged" => swapEngineInfo.turboCharged, 
			"turbo_pressure" => swapEngineInfo.turboPressure, 
			"rev_limiter" => swapEngineInfo.revLimiter, 
			"max_torque" => swapEngineInfo.maxTorque, 
			"max_torque_rpm" => swapEngineInfo.maxTorqueRpm, 
			_ => null, 
		};
		if (engineParamInfo == null)
		{
			return 1f;
		}
		float mult = 1f;
		float shift = 0f;
		EngineParamShiftInfo engineParamShiftInfo = engineParamInfo as EngineParamShiftInfo;
		if (engineParamShiftInfo != null)
		{
			mult = engineParamShiftInfo.multiplier;
			shift = 0f - engineParamShiftInfo.shift;
		}
		CarProfileChanger carProfileChanger = new CarProfileChanger(engineParamInfo.min, engineParamInfo.max, engineParamInfo.value, 0, mult, "", "", shift);
		if (savedSwapData == null || savedSwapData.controllers == null || savedSwapData.controllers.Count == 0)
		{
			return engineParamShiftInfo?.shift ?? engineParamInfo.value;
		}
		EngineSwapData.SwapController swapController = null;
		int num = savedSwapData.controllers.FindIndex((EngineSwapData.SwapController c) => c.key == controllerId);
		if (num != -1)
		{
			swapController = savedSwapData.controllers[num];
		}
		float num2 = engineParamInfo.value;
		if (swapController != null)
		{
			num2 = carProfileChanger.EvaluateValue(swapController.value);
		}
		else if (engineParamShiftInfo != null)
		{
			num2 = engineParamShiftInfo.shift;
		}
		if (engineParamShiftInfo == null)
		{
			return num2;
		}
		return Mathf.Abs(num2);
	}

	private IEnumerator RefreshPrefab()
	{
		if (m_engineRoot == null)
		{
			yield return InitEngineRoot();
		}
		yield return m_waitEndFrame;
		if (!(m_engineRoot == null))
		{
			yield return m_engineRoot.LoadEnginePrefab(m_car, m_engineInfo);
		}
	}

	private IEnumerator InitEngineRoot()
	{
		while (!m_car || m_car.carModel.IsCarPartsLoading)
		{
			yield return null;
		}
		m_engineRoot = m_car.GetComponentInChildren<EngineRootComponent>(includeInactive: true);
	}

	public static void SaveStockEngineIfNeeded(int carId, PrefsCarSlotsModel carSettings, ProfilesInfo.Profile info = null)
	{
		int profileId = carSettings.GetProfileIdForCar(carId);
		ProfilesInfo.Profile profileInfo = info ?? carSettings.GetProfileInfo(profileId, carId).Copy();
		if (profileInfo == null)
		{
			return;
		}
		EngineSwapData savedSwap = carSettings.GetEngineSwapDataForCar(carId, 0);
		if (savedSwap.controllers.Count > 0)
		{
			return;
		}
		PlayerCarProfile profileMeta = DependencyInjector.Resolve<BaseModel>()?.QueryRecordsByColumnValue("carId", carId, (PlayerCarProfile x) => x.carId == carId).Find((PlayerCarProfile x) => x.id == profileId).Copy();
		if (profileMeta.CanSwap())
		{
			SaveEngineParam("turbo_charged");
			SaveEngineParam("turbo_pressure");
			SaveEngineParam("rev_limiter");
			SaveEngineParam("max_torque");
			SaveEngineParam("max_torque_rpm");
			SaveEngineParam("final_drive");
			for (int num = 0; num < 7; num++)
			{
				SaveEngineParam("gearbox" + num);
			}
			if (savedSwap.controllers.Count > 0)
			{
				carSettings.SaveEngineSwapDataForCar(carId, savedSwap);
			}
		}
		void SaveEngineParam(string controllerName)
		{
			if (savedSwap.controllers.FindIndex((EngineSwapData.SwapController c) => c.key == controllerName) == -1)
			{
				if (profileInfo.controllers.TryGetValue(controllerName, out var value))
				{
					savedSwap.AddController(new EngineSwapData.SwapController(controllerName, value));
				}
				else
				{
					int num2 = profileMeta.dynoInfo.controllers.FindIndex((CarProfileController c) => c.name == controllerName);
					if (num2 != -1)
					{
						CarProfileController carProfileController = profileMeta.dynoInfo.controllers[num2];
						if (carProfileController.changers != null && carProfileController.changers.Count != 0)
						{
							CarProfileChanger carProfileChanger = carProfileController.changers[0];
							float value2 = carProfileChanger.EvaluatePhase(carProfileChanger.defaultValue);
							savedSwap.AddController(new EngineSwapData.SwapController(controllerName, value2));
						}
					}
				}
			}
		}
	}

	public bool IsEngineModelLoaded()
	{
		if (m_swapInstalled)
		{
			return true;
		}
		return !m_stockPrefabLoading;
	}
}
