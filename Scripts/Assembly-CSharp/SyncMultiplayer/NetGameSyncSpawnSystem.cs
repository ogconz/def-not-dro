using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SyncMultiplayer
{
	public class NetGameSyncSpawnSystem : BaseNetGameSystem
	{
		private const int BATCH_LOAD_LIMIT = 3;

		private const float LOAD_VISUAL_TIMEOUT = 20f;

		private int m_spawnCounter;

		private int m_visualLoadCounter;

		private Queue<NetworkPlayer> m_networkPlayersLoadQueue = new Queue<NetworkPlayer>();

		public override void OnReconnect()
		{
			base.OnReconnect();
			Reset();
		}

		public override void DoPlayerLeave(NetworkPlayer player)
		{
			HandleEarlyPlayerLeave(player);
		}

		public override void Update()
		{
			TrySpawnQueuePlayers();
		}

		public void TryEnqueuePlayer(NetworkPlayer player, Action<NetworkPlayer> onCarAttachedCallback)
		{
			if (!m_networkPlayersLoadQueue.Contains(player))
			{
				player.OnCarAttached = (Action<NetworkPlayer>)Delegate.Combine(player.OnCarAttached, onCarAttachedCallback);
				m_networkPlayersLoadQueue.Enqueue(player);
			}
		}

		private void TrySpawnQueuePlayers()
		{
			if (m_visualLoadCounter != 0 && m_visualLoadCounter >= m_spawnCounter)
			{
				ResetCounters();
			}
			if (m_spawnCounter < 3 && m_networkPlayersLoadQueue.Count != 0)
			{
				m_spawnCounter++;
				NetworkPlayer networkPlayer = m_networkPlayersLoadQueue.Dequeue();
				TimeController.StartNow(TrySubscribeVisualLoad(networkPlayer));
				networkPlayer.NetworkSpawnAsync(respawn: true).Forget();
			}
		}

		private IEnumerator TrySubscribeVisualLoad(NetworkPlayer networkPlayer)
		{
			float time = Time.unscaledTime;
			while (true)
			{
				if (networkPlayer == null || Time.unscaledTime - time > 20f)
				{
					ResetCounters();
					yield break;
				}
				if (!(networkPlayer.userCar == null))
				{
					break;
				}
				yield return null;
			}
			RaceCar userCar = networkPlayer.userCar;
			if (!(userCar == null))
			{
				while (userCar.IsLoadingVisualInProgress)
				{
					yield return null;
				}
				m_visualLoadCounter++;
			}
		}

		private void ResetCounters()
		{
			m_visualLoadCounter = (m_spawnCounter = 0);
		}

		private void HandleEarlyPlayerLeave(NetworkPlayer networkPlayer)
		{
			if (!m_networkPlayersLoadQueue.Contains(networkPlayer) && m_spawnCounter > m_visualLoadCounter)
			{
				m_visualLoadCounter++;
			}
		}

		private void Reset()
		{
			m_networkPlayersLoadQueue.Clear();
			ResetCounters();
		}
	}
}
