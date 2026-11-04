using System.Collections.Generic;
using MessagePack;

public class ProfilesInfo
{
	[MessagePackObject(false)]
	public class Profile
	{
		[Key(0)]
		public int id { get; private set; }

		[Key(1)]
		public bool forceDefault { get; set; }

		[Key(2)]
		public Dictionary<string, float> controllers { get; set; }

		[Key(3)]
		public Dictionary<string, float> values { get; set; }

		public Profile(int profileId)
		{
			id = profileId;
			forceDefault = false;
			controllers = new Dictionary<string, float>();
			values = new Dictionary<string, float>();
		}

		public Profile()
		{
			id = -1;
			controllers = new Dictionary<string, float>();
			values = new Dictionary<string, float>();
		}

		public Profile Copy()
		{
			Profile obj = MemberwiseClone() as Profile;
			obj.controllers = new Dictionary<string, float>(controllers);
			obj.values = new Dictionary<string, float>(values);
			return obj;
		}

		internal void FixId(int id)
		{
			this.id = id;
		}
	}

	public const int InvalidProfileId = 0;

	public Dictionary<int, Profile> profiles = new Dictionary<int, Profile>();
}
