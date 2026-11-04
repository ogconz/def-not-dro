using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using CarProfile;
using DB;
using DB.Meta;
using DB.XML;
using Db.Meta.CarProfile;
using GameOverlay;
using SimpleXML;
using UnityEngine;

[Serializable]
public class StaticDictionariesHolder : BaseXMLDictionaryHolder
{
	public static readonly bool AssetOnlyMode = true;

	public GlobalGameSettings GlobalGameSettings = new GlobalGameSettings();

	public FeatuersLocksInfo FeatuersLocksInfo = new FeatuersLocksInfo();

	public List<PlayerCar> playerCars = new List<PlayerCar>();

	public List<PlayerCarProfile> profiles = new List<PlayerCarProfile>();

	[XmlIgnore]
	public List<ShopInfo> shopInfo = new List<ShopInfo>();

	public List<DB.Meta.Track> tracks = new List<DB.Meta.Track>();

	public List<CarColorMeta> carColors = new List<CarColorMeta>();

	public List<VinylColorMeta> vinylColors = new List<VinylColorMeta>();

	public List<WheelSpokeColorMeta> wheelSpokeColors = new List<WheelSpokeColorMeta>();

	public List<WheelRimColorMeta> wheelRimColors = new List<WheelRimColorMeta>();

	public List<GlassColorMeta> glassColors = new List<GlassColorMeta>();

	public List<SmokeColorMeta> smokeColors = new List<SmokeColorMeta>();

	public List<CarProfileParameterMeta> carProfileParameters = new List<CarProfileParameterMeta>();

	public List<TrackDriftRaceNormMeta> driftRaceNorms = new List<TrackDriftRaceNormMeta>();

	public List<TrackTimeAttackRaceNormMeta> timeAttackRaceNorms = new List<TrackTimeAttackRaceNormMeta>();

	public List<TrackDriftRaceRewardMeta> driftRaceRewards = new List<TrackDriftRaceRewardMeta>();

	public List<TrackTimeAttackRaceRewardMeta> timeAttackRaceRewards = new List<TrackTimeAttackRaceRewardMeta>();

	public StaticDictionariesHolder()
	{
	}

	public StaticDictionariesHolder(BaseModel baseModel)
	{
		GlobalGameSettings = baseModel.GetSingle<GlobalGameSettings>();
		FeatuersLocksInfo = baseModel.GetSingle<FeatuersLocksInfo>();
		playerCars = baseModel.QueryAll<PlayerCar>();
		shopInfo = baseModel.QueryAll<ShopInfo>();
		profiles = baseModel.QueryAll<PlayerCarProfile>();
		tracks = baseModel.QueryAll<DB.Meta.Track>();
		carColors = baseModel.QueryAll<CarColorMeta>();
		vinylColors = baseModel.QueryAll<VinylColorMeta>();
		wheelSpokeColors = baseModel.QueryAll<WheelSpokeColorMeta>();
		wheelRimColors = baseModel.QueryAll<WheelRimColorMeta>();
		glassColors = baseModel.QueryAll<GlassColorMeta>();
		smokeColors = baseModel.QueryAll<SmokeColorMeta>();
		carProfileParameters = baseModel.QueryAll<CarProfileParameterMeta>();
		driftRaceNorms = baseModel.QueryAll<TrackDriftRaceNormMeta>();
		timeAttackRaceNorms = baseModel.QueryAll<TrackTimeAttackRaceNormMeta>();
		driftRaceRewards = baseModel.QueryAll<TrackDriftRaceRewardMeta>();
		timeAttackRaceRewards = baseModel.QueryAll<TrackTimeAttackRaceRewardMeta>();
		Initialize();
	}

	protected override void OnInitialize()
	{
		if (!AssetOnlyMode)
		{
			LoadShopInfo();
		}
		AddSingle(GlobalGameSettings);
		AddSingle(FeatuersLocksInfo);
		allLists.Clear();
		allLists.Add(new PlayerCarStore(playerCars));
		allLists.Add(new ListToDictionary<ShopInfo>(shopInfo));
		allLists.Add(new ListToDictionary<PlayerCarProfile>(profiles));
		allLists.Add(new ListToDictionary<DB.Meta.Track>(tracks));
		allLists.Add(new ListToDictionary<CarColorMeta>(carColors));
		allLists.Add(new ListToDictionary<VinylColorMeta>(vinylColors));
		allLists.Add(new ListToDictionary<WheelSpokeColorMeta>(wheelSpokeColors));
		allLists.Add(new ListToDictionary<WheelRimColorMeta>(wheelRimColors));
		allLists.Add(new ListToDictionary<GlassColorMeta>(glassColors));
		allLists.Add(new ListToDictionary<SmokeColorMeta>(smokeColors));
		allLists.Add(new ListToDictionary<CarProfileParameterMeta>(carProfileParameters));
		allLists.Add(new ListToDictionary<TrackDriftRaceNormMeta>(driftRaceNorms));
		allLists.Add(new ListToDictionary<TrackTimeAttackRaceNormMeta>(timeAttackRaceNorms));
		allLists.Add(new ListToDictionary<TrackDriftRaceRewardMeta>(driftRaceRewards));
		allLists.Add(new ListToDictionary<TrackTimeAttackRaceRewardMeta>(timeAttackRaceRewards));
		foreach (PlayerCarProfile profile in profiles)
		{
			foreach (CarProfileController controller in profile.dynoInfo.controllers)
			{
				foreach (CarProfileChanger changer in controller.changers)
				{
					changer.parameter = GetById<CarProfileParameterMeta>(changer.parameterId);
				}
			}
			foreach (PlayerCar playerCar in playerCars)
			{
				DLCCode dlcCode = playerCar.dlcCode;
				if (dlcCode != DLCCode.Unknown && dlcCode != DLCCode.RuntimeDLC && string.IsNullOrEmpty(Overlay.instance.GetDLCName(playerCar.dlcCode)))
				{
					playerCar.forcedDisable = true;
				}
			}
		}
	}

	protected override void OnSave()
	{
		SaveToSplittedFiles(Path.GetDirectoryName(fileName));
	}

	public void SaveToFile(string filePath, bool saveShopInfo = true)
	{
		GameLog.Print(AlertLevel.Notify, "Debug", "Saving static dictionary to the " + filePath);
		SaveXMLData(this, filePath);
		string directoryName = Path.GetDirectoryName(filePath);
		if (saveShopInfo)
		{
			filePath = directoryName + "/shopInfo.xml";
			GameLog.Print(AlertLevel.Notify, "Debug", "Saving shopInfo to the " + filePath);
			SaveXMLData(shopInfo, filePath);
		}
	}

	public void SaveToSplittedFiles(string folderPath)
	{
		GameLog.Print(AlertLevel.Notify, "Debug", "Saving splitted static dictionary to the " + folderPath);
		List<StaticDictionariesHolderExtension.CarDataContainer> list = this.ExtractCarsData();
		DirectoryInfo directoryInfo = Directory.CreateDirectory(folderPath + "/Cars");
		for (int i = 0; i < list.Count; i++)
		{
			SaveXMLData(list[i], $"{directoryInfo.FullName}/CarsTier{i + 1}.xml");
		}
		SaveXMLData(this.ExtractTrackData(), folderPath + "/Tracks.xml");
		SaveXMLData(this.ExtractCarColorData(), folderPath + "/CarColors.xml");
		SaveXMLData(this.ExtractGlobalSettingsData(), folderPath + "/GlobalGameSettings.xml");
		SaveXMLData(this.ExtractCarProfileParamsData(), folderPath + "/CarProfileParameters.xml");
		SaveXMLData(this.ExtractTrackNorms(), folderPath + "/TrackNorms.xml");
		SaveXMLData(this.ExtractTrackRewards(), folderPath + "/TrackRewards.xml");
		SaveXMLData(shopInfo, folderPath + "/shopInfo.xml");
	}

	private void LoadShopInfo()
	{
		shopInfo = LoadShopInfoFromXML();
	}

	public static List<ShopInfo> LoadShopInfoFromXML()
	{
		List<ShopInfo> list = new List<ShopInfo>();
		TextAsset textAsset = Resources.Load<TextAsset>("DB/shopInfo");
		if (textAsset != null)
		{
			foreach (XMLDoc child in new XMLDoc(textAsset.text, buildKeyValues: false).childs)
			{
				ShopInfo shopInfo = new ShopInfo();
				int result = 0;
				int.TryParse(child.FindChild("id").nodeValue, out result);
				shopInfo.id = result;
				shopInfo.rimColorPrice = child.FindChild("rimColorPrice").nodeValue;
				shopInfo.spokeColorPrice = child.FindChild("spokeColorPrice").nodeValue;
				shopInfo.wheelPrice = child.FindChild("wheelPrice").nodeValue;
				list.Add(shopInfo);
			}
		}
		return list;
	}

	public int LoadEmptyBonnetIfFromXML(int carId, string identifier)
	{
		string text = string.Empty;
		string[] files = Directory.GetFiles("Assets/Editor/Resources/DB/Parts");
		foreach (string path in files)
		{
			string text2 = $"{carId}_";
			if (carId < 10)
			{
				text2 = text2.Insert(0, "0");
			}
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
			if (fileNameWithoutExtension.StartsWith(text2))
			{
				text = fileNameWithoutExtension;
				break;
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			Debug.LogError($"Unable to find car parts asset for {identifier} ({carId})");
			return -1;
		}
		int result = -1;
		TextAsset textAsset = Resources.Load("DB/Parts/" + text) as TextAsset;
		try
		{
			if (textAsset != null)
			{
				XMLDoc xMLDoc = new XMLDoc(textAsset.text, buildKeyValues: false);
				if (xMLDoc.HasAttribute("emptyBonnetId"))
				{
					int.TryParse(xMLDoc.GetAttribute("emptyBonnetId"), out result);
				}
			}
		}
		catch (Exception arg)
		{
			Debug.LogError($"Unable to parse empty bonnet id for {identifier} ({carId}), {arg}");
		}
		return result;
	}
}
