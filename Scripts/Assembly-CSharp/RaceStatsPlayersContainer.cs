using System.Collections.Generic;
using GameOverlay;
using HUD;
using SyncMultiplayer;
using UnityEngine;

public class RaceStatsPlayersContainer
{
	public List<UIRaceStatsContext.PlayerStatsItem> playersList;

	private Dictionary<PlayerId, Texture> m_playersAvatarDictionary;

	private int m_selectedIndex;

	public int selectedIndex => m_selectedIndex;

	public void Update()
	{
		if (playersList.Count != NetworkController.InstanceGame.Players.Count)
		{
			FillPlayersList();
		}
	}

	public RaceStatsPlayersContainer()
	{
		playersList = new List<UIRaceStatsContext.PlayerStatsItem>();
		m_playersAvatarDictionary = new Dictionary<PlayerId, Texture>();
	}

	public void ClearData()
	{
		playersList?.ForEach(delegate(UIRaceStatsContext.PlayerStatsItem item)
		{
			item.Dispose();
		});
		playersList?.Clear();
		m_playersAvatarDictionary?.Clear();
	}

	public void CacheAvatar(PlayerId uid, Texture avatar)
	{
		if (!m_playersAvatarDictionary.ContainsKey(uid))
		{
			m_playersAvatarDictionary.Add(uid, avatar);
		}
	}

	public bool ContainsAvatar(PlayerId uid)
	{
		return m_playersAvatarDictionary.ContainsKey(uid);
	}

	public Texture GetCachedAvatar(PlayerId key)
	{
		return m_playersAvatarDictionary.GetValueOrDefault(key);
	}

	public bool UpdateListIfNeed()
	{
		List<NetworkPlayer> players = NetworkController.InstanceGame.Players;
		if (players.Count == playersList.Count)
		{
			for (int i = 0; i < players.Count; i++)
			{
				UIRaceStatsContext.PlayerStatsItem playerStatsItem = playersList[i];
				if (players[i] != playerStatsItem.player)
				{
					return true;
				}
			}
			return false;
		}
		return true;
	}

	public void FillPlayersList()
	{
		List<NetworkPlayer> players = NetworkController.InstanceGame.Players;
		for (int i = 0; i < playersList.Count; i++)
		{
			UIRaceStatsContext.PlayerStatsItem playerStatsItem = playersList[i];
			if (players.IndexOf(playerStatsItem.player) == -1 || players[i] != playerStatsItem.player)
			{
				playersList.RemoveAt(i--);
			}
		}
		for (int j = 0; j < players.Count; j++)
		{
			NetworkPlayer np = players[j];
			if (playersList.FindIndex((UIRaceStatsContext.PlayerStatsItem p) => p.player == np) == -1)
			{
				playersList.Add(new UIRaceStatsContext.PlayerStatsItem(this, np, j + 1));
			}
		}
		for (int num = 0; num < playersList.Count; num++)
		{
			playersList[num].number.value = num + 1;
		}
	}

	public UIRaceStatsContext.PlayerStatsItem GetItemWithSelectedCar()
	{
		RaceCar car = CameraSwitch.instance.GetCar();
		for (int i = 0; i < playersList.Count; i++)
		{
			UIRaceStatsContext.PlayerStatsItem playerStatsItem = playersList[i];
			if (playerStatsItem.player.userCar == car)
			{
				return playerStatsItem;
			}
		}
		return null;
	}

	public void SetSelectedIndex(UIRaceStatsContext.PlayerStatsItem playerStatsItem)
	{
		m_selectedIndex = playersList.IndexOf(playerStatsItem);
	}

	public void RefreshListViewData()
	{
		playersList.ForEach(delegate(UIRaceStatsContext.PlayerStatsItem item)
		{
			item.Refresh();
		});
	}

	public UIRaceStatsContext.PlayerStatsItem TryGetSelectedItem()
	{
		if (playersList.Count < m_selectedIndex || playersList.Count == 0)
		{
			m_selectedIndex = 0;
			return null;
		}
		m_selectedIndex = Mathf.Clamp(m_selectedIndex, 0, playersList.Count);
		return playersList[m_selectedIndex];
	}

	public UIRaceStatsContext.PlayerStatsItem GetNextTarget(bool nextTarget)
	{
		FindCarToWatch(ref m_selectedIndex, m_selectedIndex, nextTarget);
		return playersList[m_selectedIndex];
	}

	private void FindCarToWatch(ref int idx, int stopIdx, bool next)
	{
		idx = (next ? (idx + 1) : (idx - 1));
		if (idx >= playersList.Count)
		{
			idx = 0;
		}
		if (idx < 0)
		{
			idx = playersList.Count - 1;
		}
		UIRaceStatsContext.PlayerStatsItem playerStatsItem = playersList[idx];
		RaceCar userCar = playerStatsItem.player.userCar;
		bool flag = userCar != null && userCar.gameObject.activeSelf;
		if (((playerStatsItem != null && playerStatsItem.player.IsLocal) || !flag) && stopIdx != idx)
		{
			FindCarToWatch(ref idx, stopIdx, next);
		}
	}
}
