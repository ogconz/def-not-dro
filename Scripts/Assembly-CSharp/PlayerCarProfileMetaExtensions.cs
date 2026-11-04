using System.Collections.Generic;
using DB;
using DB.Meta;
using DI;
using PrefsModels;

public static class PlayerCarProfileMetaExtensions
{
	private static GamePrefs s_gamePrefs;

	private static BaseModel s_baseModel;

	private static GamePrefs GetGamePrefs()
	{
		if (!(s_gamePrefs != null))
		{
			return s_gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		}
		return s_gamePrefs;
	}

	private static BaseModel GetBaseModel()
	{
		if (s_baseModel == null)
		{
			return s_baseModel = DependencyInjector.Resolve<BaseModel>();
		}
		return s_baseModel;
	}

	public static PlayerCarProfile GetProfileById(int profileId)
	{
		return GetBaseModel().GetById<PlayerCarProfile>(profileId);
	}

	public static PlayerCarProfile GetProfileByCar(int carId)
	{
		return GetProfileById(GetGamePrefs().carSettings.GetInstalledCustomProfile(carId)?.profile.id ?? GetGamePrefs().carSettings.GetProfileIdForCar(carId));
	}

	public static bool IsProfileExist(int profileId)
	{
		return GetProfileById(profileId) != null;
	}

	public static void FixProfileIdIfNeed(int carId, ref int profileId)
	{
		PlayerCarProfile profileById = GetProfileById(profileId);
		if (profileById != null)
		{
			return;
		}
		List<PlayerCarProfile> list = GetBaseModel().QueryRecordsByColumnValue(null, null, (PlayerCarProfile x) => x.carId == carId);
		if (list != null && list.Count != 0)
		{
			profileById = list.Find((PlayerCarProfile x) => x.IsStock());
			if (profileById == null)
			{
				profileById = list[0];
			}
			if (profileById != null)
			{
				profileId = profileById.id;
			}
		}
	}

	public static void ValidateEngineSwapProfileControllers(int profileId)
	{
		PrefsCarSlotsModel carSettings = GetGamePrefs().carSettings;
		PlayerCarProfile profileById = GetProfileById(profileId);
		ProfilesInfo.Profile profile = carSettings.GetProfileInfo(customSetupId: carSettings.GetInstalledCustomProfile(profileById.carId)?.id, profileId: profileId, carId: profileById.carId);
		CustomSetupSaveHelper customSetupSaveHelper = new CustomSetupSaveHelper();
		customSetupSaveHelper.InitHelper(null, profileById, profile, profileId, profileById.carId, swapApplied: false);
		customSetupSaveHelper.ApplySavedSwap(profile, haveInstalledCustomProfile: false, openedFromCreation: false);
	}

	public static string GetProfileNameByCar(int carId)
	{
		return GetProfileByCar(carId).GetProfileName();
	}

	public static string GetProfileIconNameByCar(int carId)
	{
		return GetProfileByCar(carId).GetProfileIconName();
	}

	public static bool EqualProfileName(this PlayerCarProfile profile, string profileName)
	{
		if (profile == null || profileName == null)
		{
			return false;
		}
		return string.Equals(profile.name, profileName);
	}

	public static string GetProfileName(this PlayerCarProfile profile)
	{
		if (profile == null)
		{
			return string.Empty;
		}
		return profile.name;
	}

	public static string GetProfileIconName(this PlayerCarProfile profile)
	{
		return "ico_" + profile.GetProfileName().ToLower();
	}

	public static bool IsStock(this PlayerCarProfile profile)
	{
		return profile.EqualProfileName("Stock");
	}

	public static bool IsTurbo(this PlayerCarProfile profile)
	{
		return profile.EqualProfileName("Turbo");
	}

	public static bool IsRacing(this PlayerCarProfile profile)
	{
		return profile.EqualProfileName("Racing");
	}

	public static bool IsDrift(this PlayerCarProfile profile)
	{
		return profile.EqualProfileName("Drift");
	}

	public static bool IsUltimate(this PlayerCarProfile profile)
	{
		return profile.EqualProfileName("Ultimate");
	}

	public static bool IsUltimatePro(this PlayerCarProfile profile)
	{
		return profile.EqualProfileName("UltimatePro");
	}

	public static bool CanSwap(this PlayerCarProfile profile)
	{
		if (!profile.IsUltimate() && !profile.IsRacing())
		{
			return profile.IsUltimatePro();
		}
		return true;
	}

	public static bool CanSequentialTransmission(this PlayerCarProfile profile)
	{
		return profile.CanSwap();
	}

	public static bool CanWastegate(this PlayerCarProfile profile)
	{
		return profile.CanSwap();
	}
}
