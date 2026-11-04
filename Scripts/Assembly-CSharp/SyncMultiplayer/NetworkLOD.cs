using System.Collections.Generic;
using UnityEngine;

namespace SyncMultiplayer
{
	public class NetworkLOD
	{
		private NetworkGame m_host;

		private int m_current;

		private Transform m_cameraTransform;

		public NetworkLOD(NetworkGame host)
		{
			m_host = host;
		}

		public void Update()
		{
			if (m_host == null || m_host.Players == null)
			{
				return;
			}
			List<NetworkPlayer> players = m_host.Players;
			if (players != null && players.Count != 0)
			{
				if (m_current >= players.Count)
				{
					m_current = 0;
				}
				UpdatePlayer(players[m_current++]);
			}
		}

		private void UpdatePlayer(NetworkPlayer player)
		{
			if (player == null || player.IsLocal)
			{
				return;
			}
			RaceCar userCar = player.userCar;
			if (userCar == null || userCar.gameObject == null || userCar.getRigidbody == null || !userCar.gameObject.activeSelf)
			{
				return;
			}
			if (m_cameraTransform == null)
			{
				Camera main = Camera.main;
				if (main == null)
				{
					return;
				}
				m_cameraTransform = main.transform;
				if (m_cameraTransform == null)
				{
					return;
				}
			}
			float sqrMagnitude = (userCar.getRigidbody.position - m_cameraTransform.position).sqrMagnitude;
			bool flag = sqrMagnitude > 40000f;
			if (sqrMagnitude > 90000f)
			{
				userCar.networkQuality = 0f;
			}
			else if (flag)
			{
				userCar.networkQuality = 0.5f;
			}
			else
			{
				userCar.networkQuality = 1f;
			}
		}
	}
}
