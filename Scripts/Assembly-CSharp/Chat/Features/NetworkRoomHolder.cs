using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Chat.Protocol;
using SyncMultiplayer;

namespace Chat.Features
{
	public class NetworkRoomHolder
	{
		private ChatClient m_client;

		private ChannelProvider m_channelProvider;

		public NetworkRoomHolder(ChatClient client, ChannelProvider channelProvider)
		{
			m_client = client;
			m_channelProvider = channelProvider;
			NetworkGame.onEnterRoomEvent += OnEnterRoomHandler;
			NetworkGame.onLeaveRoomEvent += OnLeaveRoomHandler;
			NetworkController.OnDisconnectDetected += OnLeaveRoomHandler;
			NetworkController.onAutoJoinRoomError += OnLeaveRoomHandler;
		}

		public void Release()
		{
			NetworkGame.onEnterRoomEvent -= OnEnterRoomHandler;
			NetworkGame.onLeaveRoomEvent -= OnLeaveRoomHandler;
			NetworkController.OnDisconnectDetected -= OnLeaveRoomHandler;
			NetworkController.onAutoJoinRoomError -= OnLeaveRoomHandler;
			m_channelProvider = null;
			m_client = null;
		}

		private void OnEnterRoomHandler()
		{
			TimeController.AddCallback(1f, ConnectToRoomChannel);
		}

		private void ConnectToRoomChannel()
		{
			SmartfoxRoomOptions smartfoxRoomOptions = NetworkController.InstanceGame?.lastRoomOptions;
			if (smartfoxRoomOptions != null)
			{
				Guid guidFromString = GetGuidFromString(smartfoxRoomOptions.Room.Id.ToString());
				Guid result;
				string password = (Guid.TryParse(smartfoxRoomOptions.Name.ToString(), out result) ? null : GetGuidFromString(smartfoxRoomOptions.Name).ToString());
				m_client.GetAndJoinMpRoom(guidFromString, password);
			}
		}

		private void OnLeaveRoomHandler()
		{
			foreach (MessageChannel item in new List<MessageChannel>(m_channelProvider.GetAllByType(TargetType.Mp_Room)))
			{
				Guid id = item.GetTargetInfo().id;
				m_client.LeaveRoom(id);
				m_channelProvider.Remove(id);
			}
		}

		private Guid GetGuidFromString(string source)
		{
			using MD5 mD = MD5.Create();
			return new Guid(mD.ComputeHash(Encoding.UTF8.GetBytes(source)));
		}
	}
}
