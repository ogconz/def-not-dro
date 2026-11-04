using System.IO;
using CarProfile;
using DB.Meta;
using DI;
using Db.Meta.CarProfile;
using SimpleJSON;
using SyncMultiplayer;
using UnityEngine;

public class SetupSharingManager : BaseSharingConsole
{
	private class ProfileData
	{
		[JsonSerialize]
		public int version;

		[JsonSerialize]
		public int carId;

		[JsonSerialize]
		public ProfilesInfo.Profile profileInfoPreview;

		[JsonSerialize]
		public PlayerCarProfile playerCarProfile;
	}

	private static readonly string SETUPS_DIR = Application.persistentDataPath + "/Setups";

	private static readonly string FILE_EXT = ".cxsetup";

	private const int VERSION = 0;

	private const string LOAD_FAILURE_MSG = "Unable to load setup";

	private const string SAVE_FAILURE_MSG = "Unable to save setup";

	private const string LOAD_CMD = "loadSetup";

	private const string SAVE_CMD = "saveSetup";

	[Dependency]
	private GamePrefs m_prefs;

	protected override string loadFailureMessage => "Unable to load setup";

	protected override string saveFailureMessage => "Unable to save setup";

	protected override string GetDirectory()
	{
		return SETUPS_DIR;
	}

	protected override string GetFileExtension()
	{
		return FILE_EXT;
	}

	public SetupSharingManager()
	{
		commandsDict["loadSetup"] = LoadSetup;
		commandsDict["saveSetup"] = SaveSetup;
	}

	private void LoadSetup()
	{
		string fileNameForCmd = GetFileNameForCmd("loadSetup");
		if (string.IsNullOrEmpty(fileNameForCmd))
		{
			return;
		}
		UIDynostandContext uIDynostandContext = ObtainContext<UIDynostandContext>();
		if (uIDynostandContext == null || !uIDynostandContext.isActive)
		{
			GameConsole.Print("Unable to load setup");
			return;
		}
		ProfileData profileData = LoadFile<ProfileData>(fileNameForCmd);
		if (profileData == null)
		{
			return;
		}
		if (profileData.version != 0)
		{
			GameConsole.Print(string.Format("{0}, you try to load file with version '{1}', need '{2}' version", "Unable to load setup", profileData.version, 0));
			return;
		}
		if (profileData.playerCarProfile == null || profileData.profileInfoPreview == null || profileData.profileInfoPreview.controllers == null)
		{
			GameConsole.Print("Unable to load setup, invalid setup data");
			return;
		}
		PlayerCarProfile currentPlayerCarProfile = GetCurrentPlayerCarProfile(uIDynostandContext);
		if (currentPlayerCarProfile == null || uIDynostandContext.userCar == null)
		{
			GameConsole.Print("Unable to load setup. invalid playercar data");
			return;
		}
		if (uIDynostandContext.carId != profileData.carId)
		{
			GameConsole.Print("Unable to load setup. You are loading setup from another car");
			return;
		}
		string name = profileData.playerCarProfile.name;
		if (!name.ToLower().Equals(currentPlayerCarProfile.name.ToLower()))
		{
			GameConsole.Print("Unable to load setup. You try to load setup of type '" + name + "' to the '" + currentPlayerCarProfile.name + "' setup type");
			return;
		}
		PlayerCarProfile.DynoInfo dynoInfo = profileData.playerCarProfile.dynoInfo;
		if (dynoInfo == null || dynoInfo.controllers == null)
		{
			GameConsole.Print("Unable to load setup. Invalid loaded data");
			return;
		}
		foreach (BaseDynoParamsItemViewModel item in uIDynostandContext.items)
		{
			if (item == null || item.controller == null)
			{
				continue;
			}
			foreach (CarProfileController controller in dynoInfo.controllers)
			{
				if (controller != null && item.controller.name.Equals(controller.name))
				{
					if (uIDynostandContext.model.GetById<CarProfileParameterMeta>(item.controller.changers[0].parameterId).isBool && profileData.profileInfoPreview.controllers.TryGetValue(controller.name, out var value))
					{
						item.value = value;
					}
					else
					{
						item.value = GetControllerValue(profileData.profileInfoPreview, controller);
					}
				}
			}
		}
		uIDynostandContext.userCar.ApplyAdvancedProfile(uIDynostandContext.model, profileData.profileInfoPreview, m_prefs.carSettings);
		GameConsole.Print("Setup successfully loaded and applied from file '" + SETUPS_DIR + "/" + fileNameForCmd + FILE_EXT + "'");
	}

	private void SaveSetup()
	{
		string fileNameForCmd = GetFileNameForCmd("saveSetup");
		if (string.IsNullOrEmpty(fileNameForCmd))
		{
			return;
		}
		UIDynostandContext uIDynostandContext = ObtainContext<UIDynostandContext>();
		if (uIDynostandContext == null)
		{
			GameConsole.Print("Unable to save setup");
			return;
		}
		PlayerCarProfile currentPlayerCarProfile = GetCurrentPlayerCarProfile(uIDynostandContext);
		if (currentPlayerCarProfile == null || uIDynostandContext.userCar == null)
		{
			GameConsole.Print("Unable to save setup. invalid playercar data");
			return;
		}
		if (uIDynostandContext.profileInfoPreview.controllers == null)
		{
			Debug.LogError("Unable to save setup, invalid car data");
			return;
		}
		if (uIDynostandContext.profileInfoPreview.controllers.Count == 0)
		{
			Debug.LogError("Unable to save setup, no changes to save");
			return;
		}
		ProfileData profileData = new ProfileData
		{
			version = 0,
			carId = uIDynostandContext.carId,
			profileInfoPreview = uIDynostandContext.profileInfoPreview.Copy(),
			playerCarProfile = currentPlayerCarProfile
		};
		if (profileData.profileInfoPreview == null)
		{
			Debug.LogError("Unable to save setup, invalid car data");
			return;
		}
		PlayerCarProfile.DynoInfo dynoInfo = profileData.playerCarProfile.dynoInfo;
		if (dynoInfo == null || dynoInfo.controllers == null)
		{
			Debug.LogError("Unable to save setup, invalid car data");
			return;
		}
		profileData.version = 0;
		if (SaveFile(out var saveActionStr, fileNameForCmd, profileData))
		{
			GameConsole.Print("Setup '" + profileData.playerCarProfile.name + "' successfully " + saveActionStr + " at " + SETUPS_DIR + "/" + fileNameForCmd + FILE_EXT);
		}
	}

	private static PlayerCarProfile GetCurrentPlayerCarProfile(UIDynostandContext dynoContext)
	{
		if (dynoContext == null)
		{
			return null;
		}
		int carId = dynoContext.carId;
		int profileId = dynoContext.profileId;
		return dynoContext.model.QueryRecordsByColumnValue("carId", carId, (PlayerCarProfile x) => x.carId == carId).Find((PlayerCarProfile x) => x.id == profileId);
	}

	private float GetControllerDefValue(CarProfileController profileController)
	{
		return profileController.changers[0].EvaluatePhase(profileController.changers[0].defaultValue);
	}

	private float GetControllerValue(ProfilesInfo.Profile profileInfoPreview, CarProfileController profileController)
	{
		if (profileInfoPreview.controllers.TryGetValue(profileController.name, out var value))
		{
			return value;
		}
		return GetControllerDefValue(profileController);
	}

	protected override byte[] Serialize<T>(Stream stream, T saveData)
	{
		return CustomTypes.SerializeByJSON<T>(saveData);
	}

	protected override T Deserialize<T>(Stream stream, byte[] rawData)
	{
		using BinaryReader binaryReader = new BinaryReader(stream);
		int num = binaryReader.ReadInt32();
		if (rawData.Length != num + 4)
		{
			GameConsole.Print(loadFailureMessage + " at path ");
			return null;
		}
		return CustomTypes.DeserializeByJSON<T>(binaryReader.ReadBytes(num));
	}
}
