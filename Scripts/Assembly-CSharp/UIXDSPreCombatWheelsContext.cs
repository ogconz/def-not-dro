using System;
using System.Collections;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using CarProfile;
using CarX;
using DB;
using DB.Meta;
using DI;
using Db.Meta.CarProfile;
using GameInput;
using PrefsModels;
using UnityEngine;

[AudioFXBlocker(false)]
public class UIXDSPreCombatWheelsContext : BaseBindingBehaviourTarget
{
	public UIColorChangerContext frontWheelsSlider;

	public UIColorChangerContext rearWheelsSlider;

	public StringProperty raceType = new StringProperty();

	public StringProperty playerRole = new StringProperty();

	public StringProperty frontPressure = new StringProperty();

	public StringProperty rearPressure = new StringProperty();

	public System.Action onComplete;

	private GamePrefs m_prefs;

	private CarDesc m_desc;

	private RaceCar m_car;

	private CameraSwitch m_cameraSwitch;

	private List<ActionHintData> m_actionsHints = new List<ActionHintData>();

	private float m_defaultFrontPressure;

	private float m_defaultRearPressure;

	private int m_currentCarId;

	private float m_pressureFrontMin = 50000f;

	private float m_pressureFrontMax = 400000f;

	private float m_pressureFrontScaleInv = 0.001f;

	private float m_pressureRearMin = 50000f;

	private float m_pressureRearMax = 400000f;

	private float m_pressureRearScaleInv = 0.001f;

	protected override void OnActivate()
	{
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
		base.OnActivate();
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.xds_ui_select_pressure.Localized());
		RefreshBottomPanel();
		m_topPanelContext.BringForwardByDepth();
		m_bottomPanelContext.BringForwardByDepth();
		m_cameraSwitch = UnityEngine.Object.FindObjectOfType<CameraSwitch>();
		if ((bool)m_cameraSwitch)
		{
			m_cameraSwitch.rotateMode = false;
		}
		CaptureCar();
		XDSRaceModeState xDSRaceModeState = States.mainCurrent as XDSRaceModeState;
		if ((bool)xDSRaceModeState)
		{
			TimeController.StartNow(RefreshBottomContinueCoroutine());
			playerRole.value = (xDSRaceModeState.firstRace ? LocalizationKeys.xds_ui_leader.Localized() : LocalizationKeys.xds_ui_chaser.Localized());
			raceType.value = (xDSRaceModeState.firstRace ? LocalizationKeys.xds_ui_race1.Localized() : LocalizationKeys.xds_ui_race2.Localized());
			frontWheelsSlider.StartListenKeys(ActionType.NavLeft, ActionType.NavRight, multiplyInputs: true);
			rearWheelsSlider.StartListenKeys(ActionType.NavAddLeft, ActionType.NavAddRight, multiplyInputs: true);
		}
	}

	protected override void OnDeactivate()
	{
		if ((bool)m_cameraSwitch)
		{
			m_cameraSwitch.rotateMode = true;
		}
		base.OnDeactivate();
	}

	private void RefreshBottomPanel()
	{
		m_actionsHints.Clear();
		m_actionsHints.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_exit.Localized(),
			type = ActionType.NavCancelBtnB,
			action = GotoGarage
		});
		m_actionsHints.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_reset.Localized(),
			type = ActionType.NavAdditional0BtnX,
			action = ResetToDefaultTyrePressure
		});
		ShowBottomPanel(m_actionsHints);
	}

	private IEnumerator RefreshBottomContinueCoroutine()
	{
		if (!(m_car == null))
		{
			while (m_car.IsLoadingVisualInProgress)
			{
				yield return null;
			}
			if (!(m_car == null) && !base.owner.IsActive<UIPlayerProfileContext>())
			{
				m_actionsHints.Add(new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_select.Localized(),
					type = ActionType.NavSelectBtnA,
					action = OnContinueClick
				});
				ShowBottomPanel(m_actionsHints);
			}
		}
	}

	private void CaptureCar()
	{
		BaseRaceState baseRaceState = States.mainCurrent as BaseRaceState;
		if ((bool)baseRaceState)
		{
			m_car = baseRaceState.userCar;
			Car carX = m_car.carX;
			InitiatePressureDefaults();
			ReadPressureInfo();
			frontWheelsSlider.slider.minValue = m_pressureFrontMin;
			frontWheelsSlider.slider.maxValue = m_pressureFrontMax;
			rearWheelsSlider.slider.minValue = m_pressureRearMin;
			rearWheelsSlider.slider.maxValue = m_pressureRearMax;
			carX.GetCarDesc(ref m_desc);
			frontWheelsSlider.slider.onValueChanged.RemoveListener(OnPressureChange);
			rearWheelsSlider.slider.onValueChanged.RemoveListener(OnPressureChange);
			m_defaultFrontPressure = m_desc.frontTyre.pressure;
			m_defaultRearPressure = m_desc.rearTyre.pressure;
			int lastCarId = m_prefs.globals.lastCarId;
			CarSettingsModel.TandemWheelPressure tandemWheelPressure = m_prefs.carSettings.GetTandemWheelPressure(lastCarId);
			frontWheelsSlider.slider.onValueChanged.AddListener(OnPressureChange);
			rearWheelsSlider.slider.onValueChanged.AddListener(OnPressureChange);
			if (tandemWheelPressure == null)
			{
				ResetToDefaultTyrePressure();
				m_prefs.carSettings.SetTandemPressure(lastCarId, frontWheelsSlider.slider.value, rearWheelsSlider.slider.value);
				tandemWheelPressure = m_prefs.carSettings.GetTandemWheelPressure(lastCarId);
			}
			frontWheelsSlider.slider.value = tandemWheelPressure.frontPressure;
			rearWheelsSlider.slider.value = tandemWheelPressure.rearPressure;
			OnPressureChange(0f);
			m_currentCarId = lastCarId;
			frontPressure.value = Mathf.RoundToInt(frontWheelsSlider.slider.value * m_pressureFrontScaleInv).ToString();
			rearPressure.value = Mathf.RoundToInt(rearWheelsSlider.slider.value * m_pressureRearScaleInv).ToString();
		}
	}

	public void OnPressureChange(float value)
	{
		if ((bool)(States.mainCurrent as BaseRaceState))
		{
			Car carX = m_car.carX;
			carX.GetCarDesc(ref m_desc);
			m_desc.frontTyre.pressure = frontWheelsSlider.slider.value;
			m_desc.rearTyre.pressure = rearWheelsSlider.slider.value;
			m_prefs.carSettings.SetTandemPressure(m_prefs.globals.lastCarId, frontWheelsSlider.slider.value, rearWheelsSlider.slider.value);
			carX.SetCarDesc(m_desc, apply: true);
			frontPressure.value = Mathf.RoundToInt(frontWheelsSlider.slider.value * m_pressureFrontScaleInv).ToString();
			rearPressure.value = Mathf.RoundToInt(rearWheelsSlider.slider.value * m_pressureRearScaleInv).ToString();
		}
	}

	public void GotoGarage()
	{
		States.Change<GarageGUIState>();
	}

	protected override bool OnEscape()
	{
		GotoGarage();
		return true;
	}

	public void OnContinueClick()
	{
		Hide();
		HideBottomPanel();
		HideTopPanel();
		onComplete.SafeCall();
	}

	private void ResetToDefaultTyrePressure()
	{
		frontWheelsSlider.slider.value = m_defaultFrontPressure;
		rearWheelsSlider.slider.value = m_defaultRearPressure;
	}

	private void ReadPressureInfo()
	{
		int carId = m_car.metaInfo.id;
		int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(carId);
		CustomProfile installedCustomProfile = m_prefs.carSettings.GetInstalledCustomProfile(carId);
		ProfilesInfo.Profile profile = m_prefs.carSettings.GetProfileInfo(profileIdForCar, carId, installedCustomProfile?.id);
		if (profile == null)
		{
			return;
		}
		List<PlayerCarProfile> list = DependencyInjector.Resolve<BaseModel>().QueryRecordsByColumnValue("carId", carId, (PlayerCarProfile x) => x != null && x.carId == carId);
		if (list == null || list.Count == 0)
		{
			return;
		}
		PlayerCarProfile playerCarProfile = list.Find((PlayerCarProfile x) => x != null && x.id == profile.id);
		if (playerCarProfile != null)
		{
			CarProfileChanger changer = playerCarProfile.dynoInfo.GetChanger(CarProfileControllerType.Wheels, CarProfileParameterType.FrontTyrePressure);
			CarProfileChanger changer2 = playerCarProfile.dynoInfo.GetChanger(CarProfileControllerType.Wheels, CarProfileParameterType.RearTyrePressure);
			if (changer != null)
			{
				m_pressureFrontMin = changer.EvaluateValue(0f);
				m_pressureFrontMax = changer.EvaluateValue(1f);
				m_pressureFrontScaleInv = changer.multiplier;
			}
			if (changer2 != null)
			{
				m_pressureRearMin = changer2.EvaluateValue(0f);
				m_pressureRearMax = changer2.EvaluateValue(1f);
				m_pressureRearScaleInv = changer2.multiplier;
			}
			if (Mathf.Abs(m_pressureFrontMin - m_pressureFrontMax) < 1f || Mathf.Abs(m_pressureRearMin - m_pressureRearMax) < 1f)
			{
				InitiatePressureDefaults();
			}
		}
	}

	private void InitiatePressureDefaults()
	{
		m_pressureFrontMin = 100000f;
		m_pressureFrontMax = 300000f;
		m_pressureFrontScaleInv = 0.001f;
		m_pressureRearMin = 100000f;
		m_pressureRearMax = 300000f;
		m_pressureRearScaleInv = 0.001f;
	}
}
