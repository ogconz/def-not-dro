using UnityEngine;

public static class GameVersion
{
	private static int m_version;

	private static string m_currNetworkVersion;

	public static int version
	{
		get
		{
			if (m_version == -1)
			{
				m_version = ParseVersion(versionAsString);
			}
			return m_version;
		}
	}

	public static string versionAsString => Application.version;

	public static string networkVersion => m_currNetworkVersion;

	public static int ParseVersion(string version)
	{
		string text = version.Replace(".", string.Empty);
		int result = 0;
		if (!int.TryParse(text, out result))
		{
			result = 0;
			Debug.Log("Cannot init version from: " + text);
		}
		return result;
	}

	static GameVersion()
	{
		m_version = -1;
		m_currNetworkVersion = "2.28.2";
	}
}
