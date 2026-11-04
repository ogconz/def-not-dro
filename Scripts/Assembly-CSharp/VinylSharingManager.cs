using System.IO;
using SyncMultiplayer;
using UnityEngine;

public class VinylSharingManager : BaseSharingConsole
{
	public static readonly string VINYLS_DIR = Application.persistentDataPath + "/Vinyls";

	private static readonly string FILE_EXT = ".cxvnl";

	private static readonly string BACKUP_FORMAT = "{0} - #{1}";

	private const int VERSION = 0;

	private const string LOAD_FAILURE_MSG = "Unable to load vinyls";

	private const string SAVE_FAILURE_MSG = "Unable to save vinyls";

	private const string LOAD_CMD = "loadVinyl";

	private const string SAVE_CMD = "saveVinyl";

	private const string BACKUP_CMD = "backupVinyls";

	protected override string loadFailureMessage => "Unable to load vinyls";

	protected override string saveFailureMessage => "Unable to save vinyls";

	protected override string GetDirectory()
	{
		return VINYLS_DIR;
	}

	protected override string GetFileExtension()
	{
		return FILE_EXT;
	}

	public VinylSharingManager()
	{
		commandsDict["loadVinyl"] = LoadVinyl;
		commandsDict["saveVinyl"] = SaveVinyl;
		commandsDict["backupVinyls"] = BackupVinyls;
	}

	private void LoadVinyl()
	{
	}

	public bool TryLoadLocalVinyl(out CarVilylsInfo.VinylPreset loadedPreset, string fileName)
	{
		loadedPreset = null;
		if (string.IsNullOrEmpty(fileName))
		{
			return false;
		}
		loadedPreset = LoadFile<CarVilylsInfo.VinylPreset>(fileName);
		if (loadedPreset == null)
		{
			return false;
		}
		GameConsole.Print("Successfully loaded vinyls from file '" + VINYLS_DIR + "/" + fileName + FILE_EXT + "'");
		return true;
	}

	private void SaveVinyl()
	{
	}

	private void BackupVinyls()
	{
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
