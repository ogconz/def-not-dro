using System.Linq;
using CarProfile;
using DB.Meta;
using DI;
using PrefsModels;

public class CustomSetupSaveHelper
{
	private RaceCar m_userCar;

	private GamePrefs m_prefs;

	private PlayerCarProfile m_playerCarProfile;

	private PlayerCarProfile m_defaultCarProfile;

	private ProfilesInfo.Profile m_profileInfoPreview;

	private int m_profileId;

	private int m_carId;

	private string m_newSetupName;

	private SwapsAsset m_swapsAsset;

	private bool m_swapApplied;

	public void InitHelper(RaceCar car, PlayerCarProfile playerCarProfile, ProfilesInfo.Profile profileInfoPreview, int profileId, int carId, bool swapApplied)
	{
		m_userCar = car;
		if ((object)m_prefs == null)
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
		}
		m_playerCarProfile = playerCarProfile;
		m_profileInfoPreview = profileInfoPreview;
		m_profileId = profileId;
		m_carId = carId;
		m_swapsAsset = SwapsAsset.LoadAndInitDB();
		m_swapApplied = swapApplied;
		m_defaultCarProfile = m_playerCarProfile.Copy();
	}

	public void SetNewSetupName(string text, out bool isNameValid)
	{
		string text2 = UIUtils.FilterInvalidTags(text);
		isNameValid = IsInputValid(text2);
		if (!isNameValid)
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.custom_setup_fail_save.Localized(), 3f);
			return;
		}
		text2 = text2.Trim('\n', '\r');
		text2 = text2.Trim();
		if (text2.Length > 16)
		{
			text2 = text2.Substring(0, 16);
		}
		m_newSetupName = text2;
		static bool IsInputValid(string text3)
		{
			if (string.IsNullOrEmpty(text3) || string.IsNullOrWhiteSpace(text3))
			{
				return false;
			}
			return UIUtils.IsMatchRegularExpression(text3, "^[\\p{L}0-9_\\s!?;:№\u02dc~@#$%^ˆ&`*{}\\[\\]()=+\"',/|.-]*$");
		}
	}

	public void SaveSetup(out CustomSetupsSaveResult finalSaveResult)
	{
		int engineId = m_userCar.swap.engineId;
		m_prefs.carSettings.SetCustomProfileInfo(out var reason, out var customProfile, m_newSetupName, m_profileId, m_carId, engineId, m_profileInfoPreview.Copy());
		string swapKey = $"{m_carId.ToString()}_{customProfile?.id}";
		SaveSwapIfNeeded(swapKey);
		GUICommonGodVoice.ShowText(GetSaveCustomMsg(m_newSetupName, reason), 3f);
		finalSaveResult = reason;
		static string GetSaveCustomMsg(string setupName, CustomSetupsSaveResult result)
		{
			return result switch
			{
				CustomSetupsSaveResult.Success => LocalizationKeys.custom_setup_success_save.LocalizedFormat(setupName), 
				CustomSetupsSaveResult.Rewrite => LocalizationKeys.custom_setup_rewrite_save.LocalizedFormat(setupName), 
				CustomSetupsSaveResult.SlotExceed => LocalizationKeys.custom_setup_slots_exceed.Localized(), 
				_ => LocalizationKeys.custom_setup_fail_save.Localized(), 
			};
		}
	}

	public void SaveSwapIfNeeded(string swapKey = null)
	{
		if (m_playerCarProfile.CanSwap())
		{
			EngineSwapData swap = ConstructEngineSwapData();
			m_prefs.carSettings.SetEngineSwapDataForCar(m_carId, swap, swapKey);
		}
	}

	public EngineSwapData ConstructEngineSwapData()
	{
		EngineSwapData engineSwapData = new EngineSwapData(m_prefs.carSettings.GetEngineIdForCar(m_carId));
		foreach (CarProfileController controller in m_playerCarProfile.dynoInfo.controllers)
		{
			if (controller == null || controller.changers.Count < 1 || !m_profileInfoPreview.controllers.TryGetValue(controller.name, out var value))
			{
				continue;
			}
			string name = controller.name;
			if (!name.StartsWith("gearbox"))
			{
				switch (name)
				{
				case "turbo_charged":
				case "turbo_pressure":
				case "rev_limiter":
				case "max_torque":
				case "max_torque_rpm":
				case "final_drive":
					break;
				default:
					continue;
				}
			}
			engineSwapData.AddController(name, value);
		}
		return engineSwapData;
	}

	public void ApplySavedSwap(ProfilesInfo.Profile profile, bool haveInstalledCustomProfile, bool openedFromCreation)
	{
		bool flag = (openedFromCreation ? haveInstalledCustomProfile : HaveInstalledCustomProfile());
		if (m_swapApplied || !m_playerCarProfile.CanSwap() || flag)
		{
			return;
		}
		m_swapApplied = true;
		if (!m_prefs.carSettings.SwapAvailableForCar(m_carId))
		{
			return;
		}
		EngineSwapData engineSwapData = m_prefs.carSettings.GetEngineSwapDataForCar(m_carId);
		if (engineSwapData.id == 0 && engineSwapData.controllers.Count == 0)
		{
			int engineIdForCar = m_prefs.carSettings.GetEngineIdForCar(m_carId);
			EngineInfo engineInfo = m_swapsAsset.Find(engineIdForCar);
			if (engineInfo.id == 0)
			{
				EngineInfo engineInfo2 = m_swapsAsset.swaps.First();
				if (engineInfo2 == null)
				{
					return;
				}
				FillDefaultEngineParams(engineInfo, engineInfo2);
			}
			engineSwapData = new EngineSwapData(engineIdForCar);
			FillSwapControllers(engineInfo, engineSwapData);
		}
		if (engineSwapData.controllers == null)
		{
			return;
		}
		foreach (EngineSwapData.SwapController controller in engineSwapData.controllers)
		{
			profile.controllers[controller.key] = controller.value;
		}
	}

	private void FillDefaultEngineParams(EngineInfo info, EngineInfo referenceInfo)
	{
		info.turboCharged = CreateStockParam(referenceInfo.turboCharged);
		info.turboPressure = CreateStockParam(referenceInfo.turboPressure);
		info.revLimiter = CreateStockParam(referenceInfo.revLimiter);
		info.maxTorque = (EngineParamShiftInfo)CreateStockParam(referenceInfo.maxTorque);
		info.maxTorqueRpm = CreateStockParam(referenceInfo.maxTorqueRpm);
		EngineParamInfo CreateStockParam(EngineParamInfo reference)
		{
			EngineParamInfo engineParamInfo = null;
			if (m_defaultCarProfile == null || m_defaultCarProfile.dynoInfo == null)
			{
				return null;
			}
			foreach (CarProfileController controller in m_defaultCarProfile.dynoInfo.controllers)
			{
				if (!(controller.name != reference.name))
				{
					CarProfileChanger carProfileChanger = controller.changers[0];
					if (carProfileChanger == null || carProfileChanger.values.Count < 2)
					{
						engineParamInfo = reference.Copy();
					}
					else
					{
						engineParamInfo = ((reference is EngineParamShiftInfo) ? new EngineParamShiftInfo() : new EngineParamInfo());
						engineParamInfo.name = controller.name;
						engineParamInfo.value = carProfileChanger.defaultValue;
						engineParamInfo.min = carProfileChanger.values[0];
						engineParamInfo.max = carProfileChanger.values[1];
						if (engineParamInfo is EngineParamShiftInfo engineParamShiftInfo)
						{
							engineParamShiftInfo.multiplier = carProfileChanger.multiplier;
							engineParamShiftInfo.shift = carProfileChanger.shift;
						}
					}
					break;
				}
			}
			return engineParamInfo;
		}
	}

	private void FillSwapControllers(EngineInfo info, EngineSwapData swap)
	{
		AddControllerIfNeeded(info.turboCharged);
		AddControllerIfNeeded(info.turboPressure);
		AddControllerIfNeeded(info.revLimiter);
		AddControllerIfNeeded(info.maxTorque);
		AddControllerIfNeeded(info.maxTorqueRpm);
		void AddControllerIfNeeded(EngineParamInfo param)
		{
			if (param != null && swap.controllers.FindIndex((EngineSwapData.SwapController sc) => sc.key == param.name) == -1)
			{
				swap.AddController(CarSwapData.GetController(param));
			}
		}
	}

	public bool HaveInstalledCustomProfile()
	{
		return m_prefs.carSettings.GetHaveCustomSetupsPerCar(m_carId).haveInstalledCustom;
	}
}
