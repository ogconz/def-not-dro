using System;
using System.Collections;
using System.Collections.Generic;
using CarProfile;
using CarX;
using DB;
using DB.Meta;
using DI;
using LocalCaching.Model;
using PrefsModels;
using UnityEngine;

namespace LocalCaching.Manager
{
	public static class CarProfileCacheManager
	{
		public const float TARGET_ACCEL_KMH = 100f;

		public const float KMH_COEF = 3.6f;

		public const float TARGET_ACCEL_MPH = 60f;

		public const float MPH_COEF = 2.2369363f;

		private static Dictionary<int, CarProfileCache> m_carProfileCache = new Dictionary<int, CarProfileCache>();

		private static bool m_batchMode = false;

		private static RaceCar m_car = null;

		private static CarVisualManager m_carVisualManager = null;

		private static GamePrefs s_gamePrefs;

		private static BaseModel s_baseModel;

		private static PlayerCarFactory s_carFactory;

		private static List<CarProfileProxy> m_profileProxyList = new List<CarProfileProxy>();

		private const string KEY_CAR_ID = "carId";

		public static void ClearCarProfile(int id)
		{
			m_carProfileCache.Remove(id);
		}

		public static CarProfileCache GetCarProfileCache(PlayerCarFactory carFactory, PlayerCarProfile profile, RaceCar raceCar = null, bool allowApplyProfile = true)
		{
			if (!m_carProfileCache.TryGetValue(profile.id, out var value))
			{
				value = new CarProfileCache(profile.id);
				FillItemFromCar(ref value, profile.carId, carFactory, raceCar, allowApplyProfile);
				m_carProfileCache[value.id] = value;
			}
			return value;
		}

		public static List<CarProfileProxy> GetProfileList(BaseModel db, PlayerCarFactory carFactory, int carId)
		{
			m_profileProxyList.Clear();
			if (db != null)
			{
				List<PlayerCarProfile> list = db.QueryRecordsByColumnValue("carId", carId, (PlayerCarProfile x) => x.carId == carId);
				m_batchMode = true;
				try
				{
					foreach (PlayerCarProfile item in list)
					{
						CarProfileCache carProfileCache = GetCarProfileCache(carFactory, item);
						m_profileProxyList.Add(new CarProfileProxy(item, carProfileCache));
					}
				}
				finally
				{
					m_batchMode = false;
					if (m_car != null)
					{
						UnityEngine.Object.Destroy(m_car.gameObject);
						m_car = null;
					}
				}
			}
			return m_profileProxyList;
		}

		private static void FillItemFromCar(ref CarProfileCache profileItem, int carId, PlayerCarFactory carFactory, RaceCar raceCar = null, bool forceApplyProfile = true)
		{
			int num = -1;
			if (States.current is BaseGameState baseGameState)
			{
				GarageCar3dViewContext garageCar3dViewContext = baseGameState.contextOwner.Get<GarageCar3dViewContext>();
				if ((bool)garageCar3dViewContext)
				{
					if (raceCar == null)
					{
						raceCar = garageCar3dViewContext.GetRaceCar();
					}
					if (raceCar != null)
					{
						num = raceCar.profileMetaInfo.id;
					}
				}
			}
			if (raceCar == null)
			{
				if (m_car == null)
				{
					m_car = carFactory.CreatePreviewProfiledRaceCar(carId, profileItem.id);
					if (m_car == null)
					{
						GameLog.Print(AlertLevel.Error, "System", "CarProfileCacheManager> failed to create preview!");
						return;
					}
				}
				raceCar = m_car;
			}
			if (m_carVisualManager == null)
			{
				m_carVisualManager = DependencyInjector.Resolve<CarVisualManager>();
			}
			if (forceApplyProfile && raceCar != null)
			{
				if (s_baseModel == null)
				{
					s_baseModel = DependencyInjector.Resolve<BaseModel>();
				}
				s_gamePrefs = ((s_gamePrefs != null) ? s_gamePrefs : DependencyInjector.Resolve<GamePrefs>());
				CustomProfile installedCustomProfile = s_gamePrefs.carSettings.GetInstalledCustomProfile(raceCar.carId);
				ProfilesInfo.Profile profileInfo = s_gamePrefs.carSettings.GetProfileInfo(profileItem.id, raceCar.carId, installedCustomProfile?.id);
				IEnumerator enumerator = TimeController.RunCoroutine(raceCar.swap.Initialize(raceCar, s_gamePrefs, s_baseModel));
				while (enumerator.MoveNext())
				{
				}
				enumerator = TimeController.RunCoroutine(raceCar.swap.Refresh(raceCar.swap.engineId));
				while (enumerator.MoveNext())
				{
				}
				PlayerCarProfileMetaExtensions.ValidateEngineSwapProfileControllers(profileInfo.id);
				raceCar.ApplyAdvancedProfile(s_baseModel, profileInfo, s_gamePrefs.carSettings, reset: false, skipSwapControllers: true);
			}
			Recalculate(ref profileItem, raceCar.carX, CarProfileProxy.InternationalUnits.Both);
			if (num != -1 && forceApplyProfile)
			{
				m_carVisualManager?.ApplyProfile(raceCar, num, null);
			}
			if (!m_batchMode && (bool)m_car)
			{
				UnityEngine.Object.Destroy(m_car.gameObject);
				m_car = null;
			}
		}

		public static void RecalculateAll(RaceCar raceCar, bool allowApplyProfileByLazy)
		{
			s_gamePrefs = ((s_gamePrefs != null) ? s_gamePrefs : DependencyInjector.Resolve<GamePrefs>());
			if (s_carFactory == null)
			{
				s_carFactory = DependencyInjector.Resolve<PlayerCarFactory>();
			}
			PlayerCarProfile profileById = PlayerCarProfileMetaExtensions.GetProfileById(s_gamePrefs.carSettings.GetProfileIdForCar(raceCar.carId));
			CarProfileCache profileItem = GetCarProfileCache(s_carFactory, profileById, raceCar, allowApplyProfileByLazy);
			Recalculate(ref profileItem, raceCar.carX, CarProfileProxy.InternationalUnits.Both);
		}

		public static void Recalculate(ref CarProfileCache profileItem, Car car, CarProfileProxy.InternationalUnits units)
		{
			CarDesc desc = null;
			car.GetCarDesc(ref desc);
			car.CalcMaxPowerMaxTorque(out var max_hp, out var max_hp_rpm, out var max_tq, out var max_tq_rpm);
			switch (units)
			{
			case CarProfileProxy.InternationalUnits.KmH:
				profileItem.topSpeedKmH = Mathf.RoundToInt(car.CalcTopSpeed() * 3.6f);
				profileItem.accelKmH = car.CalcAccelTime(0f, 27.777779f);
				break;
			case CarProfileProxy.InternationalUnits.MpH:
				profileItem.topSpeedMpH = Mathf.RoundToInt(car.CalcTopSpeed() * 2.2369363f);
				profileItem.accelMpH = car.CalcAccelTime(0f, 26.8224f);
				break;
			case CarProfileProxy.InternationalUnits.Both:
				profileItem.topSpeedKmH = Mathf.RoundToInt(car.CalcTopSpeed() * 3.6f);
				profileItem.topSpeedMpH = Mathf.RoundToInt(car.CalcTopSpeed() * 2.2369363f);
				profileItem.accelKmH = car.CalcAccelTime(0f, 27.777779f);
				profileItem.accelMpH = car.CalcAccelTime(0f, 26.8224f);
				break;
			}
			profileItem.maxPower = Mathf.RoundToInt(max_hp);
			profileItem.maxPowerRPM = Mathf.RoundToInt(max_hp_rpm);
			profileItem.maxTorque = Mathf.RoundToInt(max_tq);
			profileItem.maxTorqueRPM = Mathf.RoundToInt(max_tq_rpm);
			profileItem.weight = Mathf.RoundToInt(desc.weight.mass);
			profileItem.transmission = GetTransmissionName(desc.transmission.gearType);
			profileItem.traction_control = desc.engine.useTC;
			profileItem.tyres = GetTyresName(desc.rearTyre.tyreType);
			profileItem.powerDensity = MathF.Round(max_hp / desc.weight.mass, 2);
		}

		public static string GetTransmissionName(GearType gearType)
		{
			return gearType switch
			{
				GearType.GEAR_RWD => "RWD", 
				GearType.GEAR_FWD => "FWD", 
				GearType.GEAR_4WD => "4WD", 
				GearType.GEAR_AWD => "AWD", 
				_ => "RWD", 
			};
		}

		private static string GetTyresName(TyreType tyreType)
		{
			return tyreType switch
			{
				TyreType.Racing => "Racing", 
				TyreType.Sport => "Sport", 
				TyreType.Drag => "Drag", 
				TyreType.AllSeason => "AllSeason", 
				_ => "Sport", 
			};
		}
	}
}
