using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FavoriteCarsModelDefaultConfig", menuName = "Other/FavoriteCarsModelDefaultConfig")]
public class FavoriteCarsModelDefaultConfig : ScriptableObject
{
	private const string DBPath = "DB/FavoriteCarsModelDefaultConfig";

	private static FavoriteCarsModelDefaultConfig s_dB;

	[SerializeField]
	private List<int> m_favoriteCarsIds = new List<int>();

	public IReadOnlyCollection<int> favoriteCarsIds => m_favoriteCarsIds;

	public static FavoriteCarsModelDefaultConfig LoadAndInitDB()
	{
		if (s_dB == null)
		{
			s_dB = Resources.Load<FavoriteCarsModelDefaultConfig>("DB/FavoriteCarsModelDefaultConfig");
		}
		return s_dB;
	}
}
