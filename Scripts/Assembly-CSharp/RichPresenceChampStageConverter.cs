using System.Collections.Generic;
using Championship;

public class RichPresenceChampStageConverter : IRichPresenceConverter<ChampStageType>, IRichPresenceConverter
{
	private Dictionary<ChampStageType, string> m_champStageKvp = new Dictionary<ChampStageType, string>
	{
		{
			ChampStageType.REGISTRATION,
			"Training"
		},
		{
			ChampStageType.QUALIFICATION,
			"Qualification"
		},
		{
			ChampStageType.TOP32,
			"TOP32"
		},
		{
			ChampStageType.TOP16,
			"TOP16"
		},
		{
			ChampStageType.TOP8,
			"TOP8"
		},
		{
			ChampStageType.TOP4,
			"TOP4"
		},
		{
			ChampStageType.FINAL,
			"final"
		},
		{
			ChampStageType.LOSER,
			"third"
		}
	};

	public bool TryConvertFrom(ChampStageType stageType, out string result)
	{
		return m_champStageKvp.TryGetValue(stageType, out result);
	}
}
