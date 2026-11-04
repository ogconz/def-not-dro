using System;
using System.Collections;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using CarProfile;
using DB;
using DB.Meta;
using DG.Tweening;
using DI;
using LocalCaching.Manager;
using LocalCaching.Model;
using UnityEngine;
using UnityEngine.UI;

public class CarSpecsTableContext : BaseBindingBehaviourTarget
{
	public enum ProfileName
	{
		Stock = 0,
		Turbo = 1,
		Racing = 2,
		Drift = 3,
		Ultimate = 4,
		UltimatePro = 5
	}

	private class SetupsRange
	{
		public float minTopSpeed;

		public float maxTopSpeed;

		public float minPower;

		public float maxPower;

		public float minTorque;

		public float maxTorque;

		public float minAccel;

		public float maxAccel;

		public float minWeight;

		public float maxWeight;

		public void SetDefaults()
		{
			minTopSpeed = float.MaxValue;
			minPower = float.MaxValue;
			minTorque = float.MaxValue;
			minAccel = float.MaxValue;
			minWeight = float.MaxValue;
			maxTopSpeed = 0f;
			maxPower = 0f;
			maxTorque = 0f;
			maxAccel = 0f;
			maxWeight = 0f;
		}
	}

	private class Params
	{
		public float speed;

		public float power;

		public float torque;

		public float accel;

		public float weight;
	}

	private const float MinRangeValue = 0.3f;

	private const float MaxRangeValue = 1f;

	private const string NEW_VALUE_FORMAT = "<color=#9CC959>{0}</color>";

	public Sprite[] classGradients;

	public Color[] classColors;

	[SerializeField]
	private Slider topSpeedSlider;

	[SerializeField]
	private Slider accelerationSlider;

	[SerializeField]
	private Slider powerSlider;

	[SerializeField]
	private Slider torqueSlider;

	[SerializeField]
	private Slider weightSlider;

	[Dependency]
	private GamePrefs m_prefs;

	[Dependency]
	private BaseModel m_baseModel;

	[Dependency]
	private PlayerCarFactory m_carFactory;

	public readonly StringProperty carTitle = new StringProperty();

	public readonly StringProperty carClass = new StringProperty();

	public readonly SpriteProperty classSpriteGradient = new SpriteProperty();

	public readonly StringProperty carProfile = new StringProperty();

	public readonly StringProperty transmissionIcon = new StringProperty();

	public readonly StringProperty cardlcName = new StringProperty();

	public readonly StringProperty topSpeed = new StringProperty();

	public readonly StringProperty accelTo = new StringProperty();

	public readonly StringProperty maxPower = new StringProperty();

	public readonly StringProperty maxTorque = new StringProperty();

	public readonly StringProperty powerDensity = new StringProperty();

	public readonly StringProperty weight = new StringProperty();

	public readonly StringProperty setupIcon = new StringProperty();

	public readonly BoolProperty swapInstalled = new BoolProperty(false);

	private readonly SpriteProperty m_swapIcon = new SpriteProperty();

	private SetupsRange m_setupsRange = new SetupsRange();

	private Params m_oldParams;

	private Params m_params;

	private SwapsAsset m_swapsAsset;

	private string m_initTopSpeed;

	private string m_initAccelTo;

	private string m_initMaxPower;

	private string m_initMaxPowerRPM;

	private string m_initMaxTorque;

	private string m_initMaxTorqueRPM;

	private string m_initWeight;

	private string m_initTransmission;

	private string m_initPowerDensity;

	private int m_prevProfileId;

	private int m_prevCarId;

	private float m_prevDiam;

	private void Awake()
	{
		m_swapsAsset = SwapsAsset.LoadAndInitDB();
		if (!m_prefs)
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
			m_baseModel = DependencyInjector.Resolve<BaseModel>();
			m_carFactory = DependencyInjector.Resolve<PlayerCarFactory>();
		}
	}

	public void InitCarInfo(PlayerCar carInfo)
	{
		carTitle.value = carInfo.name;
		carClass.value = FormatClassCar(carInfo);
		if (carInfo.coinsFactor <= (float)classGradients.Length)
		{
			classSpriteGradient.value = GetClassGradient(carInfo);
		}
	}

	public IEnumerator InitProfileInfo(GarageCar3dViewContext car3dView, UISetupsContext.Mode? mode, CarProfileProxy profileProxy, BaseModel model, RaceCar car, string customSetupId, bool skipSwap = false, ProfilesInfo.Profile info = null, int? engineId = null, bool updateFromCarSpecsTable = true)
	{
		if (profileProxy == null)
		{
			yield break;
		}
		carProfile.value = profileProxy.name;
		setupIcon.value = profileProxy.GetProfile().GetProfileIconName();
		bool changedProfile = false;
		if (car != null)
		{
			int profileId = profileProxy.id;
			PlayerCarProfileMetaExtensions.FixProfileIdIfNeed(car.carId, ref profileId);
			changedProfile = m_prevProfileId != profileId;
			int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(car.carId);
			m_prefs.carSettings.SetProfilePerCar(car.carId, profileId);
			if (changedProfile)
			{
				PlayerCarProfile playerCarProfile = (car.profileMetaInfo = model.GetById<PlayerCarProfile>(profileId));
				car.InitDesc(playerCarProfile.asset, skipWheelBase: true);
				m_prevProfileId = profileId;
			}
			ProfilesInfo.Profile profile = info ?? m_prefs.carSettings.GetProfileInfo(profileId, car.carId, customSetupId, mode == UISetupsContext.Mode.Setups).Copy();
			EngineInfo engineInfo = m_swapsAsset.Find(engineId ?? car.swap.engineId);
			bool skipSwapControllers = !string.IsNullOrEmpty(customSetupId) || engineInfo.id == 0;
			car.ApplyAdvancedProfile(model, profile, m_prefs.carSettings, reset: false, skipSwapControllers, null, customSetupId, updateFromCarSpecsTable);
			m_prefs.carSettings.SetProfilePerCar(car.carId, profileIdForCar);
			if (car.TryGetDiscDiamCoeff(out var discDiam, checkHeight: false) || !mode.HasValue)
			{
				float num = Math.Abs(m_prevDiam - discDiam);
				if (m_prevCarId != car.carId)
				{
					num = 0f;
				}
				if (num > 5f || !mode.HasValue)
				{
					car.AlignCarPositionAtSpawn(adjustRotation: false);
				}
				m_prevDiam = discDiam;
			}
			bool flag = !car.swap.isStockEngine && profileProxy.GetProfile().CanSwap();
			swapInstalled.value = flag && engineInfo.id != 0;
			m_swapIcon.value = (flag ? engineInfo.logo : null);
			m_prevCarId = car.carId;
		}
		yield return null;
		if (changedProfile && mode != UISetupsContext.Mode.Swaps)
		{
			car3dView.ApplyDefaultCustomization();
		}
		string text;
		string text2;
		float num2;
		float num3;
		if (m_prefs.globals.speedKmh)
		{
			profileProxy.RecalculateAll(car.carX, CarProfileProxy.InternationalUnits.KmH);
			text = LocalizationKeys.settings_game_speed_unit_kmh.Localized().ToLower();
			text2 = $"(0-{100f} {text})";
			num2 = profileProxy.topSpeedKmH;
			num3 = profileProxy.accelKmH;
		}
		else
		{
			profileProxy.RecalculateAll(car.carX, CarProfileProxy.InternationalUnits.MpH);
			text = LocalizationKeys.settings_game_speed_unit_mph.Localized().ToLower();
			num2 = profileProxy.topSpeedMpH;
			num3 = profileProxy.accelMpH;
			text2 = $"(0-{60f} {text})";
		}
		bool flag2 = info == null && !mode.HasValue;
		string text3 = UIUtils.DigitsWithSeparate((int)num2);
		topSpeed.value = GetDiffValue(!flag2, m_initTopSpeed, text3) + " " + text;
		TrySnapshotInitParam(flag2, ref m_initTopSpeed, text3);
		string text4 = $"{num3:F2}";
		accelTo.value = GetDiffValue(!flag2, m_initAccelTo, text4) + " " + LocalizationKeys.garage_setup_second.Localized() + " " + text2;
		TrySnapshotInitParam(flag2, ref m_initAccelTo, text4);
		string text5 = (mode.HasValue ? string.Empty : Environment.NewLine);
		string text6 = UIUtils.DigitsWithSeparate(profileProxy.maxPower);
		string text7 = UIUtils.DigitsWithSeparate(profileProxy.maxPowerRPM);
		maxPower.value = GetDiffValue(!flag2, m_initMaxPower, text6) + " " + LocalizationKeys.garage_setup_hp.Localized() + " " + text5 + "(" + GetDiffValue(!flag2, m_initMaxPowerRPM, text7) + " " + LocalizationKeys.garage_setup_rpm.Localized() + ")";
		TrySnapshotInitParam(flag2, ref m_initMaxPower, text6);
		TrySnapshotInitParam(flag2, ref m_initMaxPowerRPM, text7);
		string text8 = UIUtils.DigitsWithSeparate(profileProxy.maxTorque);
		string text9 = UIUtils.DigitsWithSeparate(profileProxy.maxTorqueRPM);
		maxTorque.value = GetDiffValue(!flag2, m_initMaxTorque, text8) + " " + LocalizationKeys.garage_setup_nm.Localized() + " " + text5 + "(" + GetDiffValue(!flag2, m_initMaxTorqueRPM, text9) + " " + LocalizationKeys.garage_setup_rpm.Localized() + ")";
		string text10 = UIUtils.DigitsWithSeparate(profileProxy.powerDensity);
		powerDensity.value = GetDiffValue(!flag2, m_initPowerDensity, text10) + " " + LocalizationKeys.garage_setup_hp.Localized() + "/" + LocalizationKeys.garage_setup_kg.Localized();
		TrySnapshotInitParam(flag2, ref m_initPowerDensity, text10);
		TrySnapshotInitParam(flag2, ref m_initMaxTorque, text8);
		TrySnapshotInitParam(flag2, ref m_initMaxTorqueRPM, text9);
		string text11 = UIUtils.DigitsWithSeparate(profileProxy.weight);
		weight.value = GetDiffValue(!flag2, m_initWeight, text11) + " " + LocalizationKeys.garage_setup_kg.Localized();
		TrySnapshotInitParam(flag2, ref m_initWeight, text11);
		string transmission = profileProxy.transmission;
		transmissionIcon.value = GetDiffValue(!flag2, m_initTransmission, transmission) ?? "";
		TrySnapshotInitParam(flag2, ref m_initTransmission, transmission);
		InitSetupsRanges(profileProxy.carId, customSetupId);
		UpdateProfilePreviewParams(profileProxy);
	}

	private string GetDiffValue(bool skip, string initParam, string value)
	{
		if (string.IsNullOrEmpty(initParam) || !skip)
		{
			return value;
		}
		return initParam + "/" + $"<color=#9CC959>{value}</color>";
	}

	private void TrySnapshotInitParam(bool needSnap, ref string param, string val)
	{
		if (needSnap)
		{
			param = val;
		}
	}

	public void ClearCachedInitParams()
	{
		m_initTopSpeed = (m_initAccelTo = (m_initAccelTo = (m_initMaxPower = (m_initMaxTorque = (m_initWeight = (m_initTransmission = null))))));
	}

	private void InitSetupsRanges(int carId, string customSetupId)
	{
		m_setupsRange.SetDefaults();
		foreach (CarProfileProxy profile in CarProfileCacheManager.GetProfileList(m_baseModel, m_carFactory, carId))
		{
			float val = profile.topSpeedKmH;
			float val2 = profile.accelKmH;
			if (!m_prefs.globals.speedKmh)
			{
				val = profile.topSpeedMpH;
				val2 = profile.accelMpH;
			}
			m_setupsRange.minTopSpeed = Math.Min(m_setupsRange.minTopSpeed, val);
			m_setupsRange.maxTopSpeed = Math.Max(m_setupsRange.maxTopSpeed, val);
			m_setupsRange.minAccel = Math.Min(m_setupsRange.minAccel, val2);
			m_setupsRange.maxAccel = Math.Max(m_setupsRange.maxAccel, val2);
			m_setupsRange.minPower = Math.Min(m_setupsRange.minPower, profile.maxPower);
			m_setupsRange.maxPower = Math.Max(m_setupsRange.maxPower, profile.maxPower);
			m_setupsRange.minTorque = Math.Min(m_setupsRange.minTorque, profile.maxTorque);
			m_setupsRange.maxTorque = Math.Max(m_setupsRange.maxTorque, profile.maxTorque);
			m_setupsRange.minWeight = Math.Min(m_setupsRange.minWeight, profile.weight);
			m_setupsRange.maxWeight = Math.Max(m_setupsRange.maxWeight, profile.weight);
		}
	}

	private Params MakeParams(CarProfileProxy info)
	{
		Params obj = new Params();
		float value = CalcParam(m_setupsRange.minTopSpeed, m_setupsRange.maxTopSpeed, info.topSpeedKmH);
		float value2 = 1f - CalcParam(m_setupsRange.minAccel, m_setupsRange.maxAccel, info.accelKmH);
		if (!m_prefs.globals.speedKmh)
		{
			value2 = 1f - CalcParam(m_setupsRange.minAccel, m_setupsRange.maxAccel, info.accelMpH);
			value = CalcParam(m_setupsRange.minTopSpeed, m_setupsRange.maxTopSpeed, info.topSpeedMpH);
		}
		obj.accel = ShiftMinRange(value2);
		obj.speed = ShiftMinRange(value);
		float value3 = CalcParam(m_setupsRange.minPower, m_setupsRange.maxPower, info.maxPower);
		obj.power = ShiftMinRange(value3);
		float value4 = CalcParam(m_setupsRange.minTorque, m_setupsRange.maxTorque, info.maxTorque);
		obj.torque = ShiftMinRange(value4);
		float value5 = 1f - CalcParam(m_setupsRange.minWeight, m_setupsRange.maxWeight, info.weight);
		obj.weight = ShiftMinRange(value5);
		return obj;
	}

	private void UpdateProfilePreviewParams(CarProfileProxy info)
	{
		m_params = MakeParams(info);
		if (m_oldParams == null)
		{
			m_oldParams = m_params;
		}
		if ((bool)topSpeedSlider)
		{
			topSpeedSlider.DOValue(m_params.speed, 1f);
		}
		if ((bool)accelerationSlider)
		{
			accelerationSlider.DOValue(m_params.accel, 1f);
		}
		if ((bool)powerSlider)
		{
			powerSlider.DOValue(m_params.power, 1f);
		}
		if ((bool)torqueSlider)
		{
			torqueSlider.DOValue(m_params.torque, 1f);
		}
		if ((bool)weightSlider)
		{
			weightSlider.DOValue(m_params.weight, 1f);
		}
		m_oldParams = m_params;
	}

	private string GetProfileName(int id)
	{
		int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(id);
		PlayerCarProfile byId = m_baseModel.GetById<PlayerCarProfile>(profileIdForCar);
		if (byId != null)
		{
			return byId.name;
		}
		return "";
	}

	public string FormatClassCar(PlayerCar car)
	{
		return FormatClassCar(car.coinsFactor);
	}

	public string FormatClassCar(float classCar)
	{
		return UIGarageContext.FormatClassCar(classCar);
	}

	public Sprite GetClassGradient(float classCar)
	{
		return classGradients[(int)classCar - 1];
	}

	public Sprite GetClassGradient(PlayerCar car)
	{
		return GetClassGradient(car.coinsFactor);
	}

	public Color GetClassColor(float classCar)
	{
		return classColors[(int)classCar - 1];
	}

	private CarProfileProxy GetProfileCar(int carId, byte profileNumber, string customSetupId)
	{
		int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(carId);
		if (m_baseModel.GetById<PlayerCarProfile>(profileIdForCar) != null)
		{
			List<CarProfileProxy> profileList = CarProfileCacheManager.GetProfileList(m_baseModel, m_carFactory, carId);
			if (profileList.Count > 0 && profileNumber < profileList.Count)
			{
				return profileList[profileNumber];
			}
		}
		return null;
	}

	private CarProfileProxy GetProfileCar(int carId, string customSetupId)
	{
		int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(carId);
		PlayerCarProfile byId = m_baseModel.GetById<PlayerCarProfile>(profileIdForCar);
		if (byId != null)
		{
			foreach (CarProfileProxy profile in CarProfileCacheManager.GetProfileList(m_baseModel, m_carFactory, carId))
			{
				if (profile.id == byId.id)
				{
					return profile;
				}
			}
		}
		return null;
	}

	public static float CalcParam(float minValue, float maxValue, float value)
	{
		float num = maxValue - minValue;
		if (num > 0f)
		{
			return (value - minValue) / num;
		}
		return 0f;
	}

	public static float ShiftMinRange(float value)
	{
		return 0.3f + value * 100f * 0.0069999998f;
	}
}
