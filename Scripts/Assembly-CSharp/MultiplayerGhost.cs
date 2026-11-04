using System;
using SimpleJSON;

public class MultiplayerGhost
{
	public class ClientData
	{
		public int carId;

		public int profileId;

		public string profileData;

		public string visualSettings;
	}

	public string ghostUrl;

	public int driftPoints;

	public ClientData settings;

	public string nickName;

	[JsonIgnore]
	public DateTime? outdateTime;

	[JsonIgnore]
	public byte[] rawData;

	[JsonIgnore]
	private bool m_prepared;

	[JsonIgnore]
	public int outdatesIn
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			outdateTime = GeneralExtensions.MakeTime(value);
		}
	}

	public float? secondsTillOutdate
	{
		get
		{
			if (!outdateTime.HasValue)
			{
				return null;
			}
			return (float)(outdateTime.Value - DateTime.Now).TotalSeconds;
		}
	}

	public MultiplayerGhost()
	{
	}

	public MultiplayerGhost(byte[] data, int driftPoints, ClientData clientData)
	{
		rawData = data;
		this.driftPoints = driftPoints;
		settings = clientData;
	}

	public void PrepareToUse(Protocol protocol, Action<MultiplayerGhost> onDone)
	{
		if (m_prepared)
		{
			onDone.SafeCall(this);
			return;
		}
		m_prepared = true;
		protocol.DownloadUserGhost(ghostUrl, delegate(byte[] ghostData)
		{
			rawData = ghostData;
			onDone.SafeCall(this);
		});
	}
}
