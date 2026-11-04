using System;
using GameOverlay;
using SimpleJSON;

namespace PrefsModels
{
	public class CustomProfile
	{
		[JsonIgnore]
		public const int MAX_SLOT_COUNT = 16;

		[JsonSerialize]
		public string id;

		[JsonSerialize]
		public int carId;

		[JsonSerialize]
		public int engineId;

		[JsonSerialize]
		public string name;

		[JsonSerialize]
		public int mask;

		[JsonSerialize]
		public ProfilesInfo.Profile profile;

		[JsonSerialize]
		public string code;

		[JsonSerialize]
		public bool isOfficial;

		public CustomProfile()
		{
		}

		public CustomProfile(string name, int carId, int engineId, ProfilesInfo.Profile profile, string setupId = "")
		{
			this.name = name;
			this.carId = carId;
			this.engineId = engineId;
			this.profile = profile;
			id = (string.IsNullOrEmpty(setupId) ? Guid.NewGuid().ToString() : setupId);
			mask = (int)Overlay.GetPlatform();
		}

		public void SetServerData(string code, bool isOfficial)
		{
			this.code = code;
			this.isOfficial = isOfficial;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is CustomProfile customProfile))
			{
				return false;
			}
			return id == customProfile.id;
		}

		public override int GetHashCode()
		{
			return id.GetHashCode();
		}
	}
}
