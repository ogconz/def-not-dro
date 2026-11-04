using System.Collections.Generic;
using SyncMultiplayer;

public class RichPresenceSubroomModeConverter : IRichPresenceConverter<SubroomMode>, IRichPresenceConverter
{
	private Dictionary<SubroomMode, string> m_subroomKvp = new Dictionary<SubroomMode, string>
	{
		{
			SubroomMode.Challenge,
			"Drift"
		},
		{
			SubroomMode.TimeAttack,
			"TimeAttack"
		},
		{
			SubroomMode.ClippingDuel,
			"ClippingDuel"
		},
		{
			SubroomMode.Tandem,
			"XDS"
		},
		{
			SubroomMode.TandemXDS,
			"XDS_tandem"
		},
		{
			SubroomMode.XDS,
			"XDS_competition"
		}
	};

	public bool TryConvertFrom(SubroomMode subroomMode, out string result)
	{
		return m_subroomKvp.TryGetValue(subroomMode, out result);
	}
}
