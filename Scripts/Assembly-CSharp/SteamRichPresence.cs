using System;
using System.Collections.Generic;
using Championship;
using Steamworks;
using SyncMultiplayer;
using UnityEngine;

public class SteamRichPresence : IRichPresence
{
	private const string Display = "steam_display";

	private Dictionary<RichPresenceStatus, string> m_statusKvp = new Dictionary<RichPresenceStatus, string>
	{
		{
			RichPresenceStatus.MainMenu,
			"#Status_AtMainMenu"
		},
		{
			RichPresenceStatus.CarTuning,
			"#Status_CarTuning"
		},
		{
			RichPresenceStatus.CarDesigning,
			"#Status_CarDesigning"
		},
		{
			RichPresenceStatus.SinglePlayer,
			"#Status_SinglePlayer"
		},
		{
			RichPresenceStatus.MultiplayerFreeride,
			"#Status_MultiPlayer"
		},
		{
			RichPresenceStatus.MultiplayerSubroom,
			"#Status_MultiPlayer_Subroom"
		},
		{
			RichPresenceStatus.Championship,
			"#Status_Championship"
		}
	};

	private Dictionary<RichPresenceVariables, string> m_variablesKvp = new Dictionary<RichPresenceVariables, string>
	{
		{
			RichPresenceVariables.TrackName,
			"track_name"
		},
		{
			RichPresenceVariables.SinglePlayerMode,
			"singleplayer_mode"
		},
		{
			RichPresenceVariables.PlayersCountInRoom,
			"players_count_room"
		},
		{
			RichPresenceVariables.MaxPlayersCountInRoom,
			"max_players_count_room"
		},
		{
			RichPresenceVariables.ChampionshipStage,
			"championship_stage"
		},
		{
			RichPresenceVariables.SubroomMode,
			"subroom_mode"
		}
	};

	private Dictionary<Type, IRichPresenceConverter> m_typeConverters = new Dictionary<Type, IRichPresenceConverter>
	{
		{
			typeof(SubroomMode),
			new RichPresenceSubroomModeConverter()
		},
		{
			typeof(ChampStageType),
			new RichPresenceChampStageConverter()
		},
		{
			typeof(RaceMode),
			new RichPresenceRaceModeConverter()
		},
		{
			typeof(string),
			new RichPresenceStringConverter()
		}
	};

	private bool m_presenceSetted;

	public void Show(RichPresenceStatus presenceStatus)
	{
		SteamFriends.SetRichPresence("steam_display", m_statusKvp[presenceStatus]);
		m_presenceSetted = true;
	}

	public void SetVariable<T>(RichPresenceVariables presenceVar, T variable)
	{
		if (!m_typeConverters.TryGetValue(typeof(T), out var value))
		{
			Debug.LogError($"Rich presence convertation error with {typeof(T)}");
			return;
		}
		try
		{
			if ((value as IRichPresenceConverter<T>).TryConvertFrom(variable, out var result))
			{
				SteamFriends.SetRichPresence(m_variablesKvp[presenceVar], result);
				m_presenceSetted = true;
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Error while set rich presence variable -> " + ex.Message);
		}
	}

	public void ClearAll()
	{
		if (m_presenceSetted)
		{
			try
			{
				SteamFriends.ClearRichPresence();
			}
			catch (Exception)
			{
			}
			m_presenceSetted = false;
		}
	}
}
