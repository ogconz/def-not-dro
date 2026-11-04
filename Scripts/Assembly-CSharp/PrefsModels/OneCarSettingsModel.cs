using System;
using System.Text;
using SimpleJSON;
using SyncMultiplayer;

namespace PrefsModels
{
	public class OneCarSettingsModel : BasePrefsModel
	{
		[JsonSerialize]
		public int carId;

		[JsonSerialize]
		public CarVisualManager.VisualSettings carVisualSettingsData;

		[JsonSerialize]
		public CarVilylsInfo carVinylPreset;

		[JsonIgnore]
		public bool hasChanged;

		[JsonIgnore]
		public bool isLoaded;

		private StringBuilder m_sbModelName;

		private StringBuilder m_sbRemoteName;

		[JsonIgnore]
		public bool needWriteToRemote;

		private const char UNDERSCORE = '_';

		private readonly string REMOTE_MODEL_NAME = "customization";

		private readonly string REMOTE_MODEL_EXT = ".dat";

		private string m_remoteName;

		public string remoteName => m_remoteName ?? (m_remoteName = GetRemoteName());

		protected override string GetModelName()
		{
			if (m_sbModelName == null)
			{
				m_sbModelName = new StringBuilder();
			}
			m_sbModelName.Clear();
			m_sbModelName.Append(BasePrefsModel.storageSystem.EncodeName(GetType().Name));
			m_sbModelName.Append('_');
			m_sbModelName.Append(carId);
			return m_sbModelName.ToString();
		}

		private string GetRemoteName()
		{
			if (m_sbRemoteName == null)
			{
				m_sbRemoteName = new StringBuilder();
			}
			m_sbRemoteName.Clear();
			m_sbRemoteName.Append(REMOTE_MODEL_NAME);
			m_sbRemoteName.Append(carId);
			m_sbRemoteName.Append(REMOTE_MODEL_EXT);
			return m_sbRemoteName.ToString();
		}

		public void WriteToRemote(string json)
		{
			if (BasePrefsModel.m_storageSystem.IsRemoteSupported())
			{
				byte[] array = null;
				try
				{
					array = CustomTypes.Zip(json);
				}
				catch (Exception ex)
				{
					GameLog.Print(AlertLevel.Error, "Save", ex.ToString());
					array = null;
				}
				if (array != null)
				{
					BasePrefsModel.m_storageSystem.WriteToRemoteStorage(remoteName, array);
				}
			}
		}
	}
}
