using System.Collections.Generic;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;

namespace SyncMultiplayer
{
	public class SmartfoxEventsNOOPHandler : ISmartfoxEventsHandler
	{
		public void OnInternalError(string message)
		{
		}

		public void OnStateChanged(ClientState previousState, ClientState newState)
		{
		}

		public void OnLoginError(short code, string message)
		{
		}

		public void OnSocketError(short code, string message)
		{
		}

		public void OnJoinedLobby(SmartfoxClient client)
		{
		}

		public void OnRoomAdded(Room room)
		{
		}

		public void OnRoomRemoved(Room room)
		{
		}

		public void OnRoomJoined(Room room)
		{
		}

		public void OnRoomVariablesUpdate(Room room, HashSet<string> changedProps)
		{
		}

		public void OnRoomJoinError(short code, string message)
		{
		}

		public void OnRoomCreateError(short code, string message)
		{
		}

		public void OnUserVariablesUpdate(SFSUser sender, HashSet<string> changedProps)
		{
		}

		public void OnPlayerJoinedRoom(Sfs2X.Entities.User user)
		{
		}

		public void OnPlayerLeftRoom(Sfs2X.Entities.User user)
		{
		}

		public void OnRoomMessage(SFSUser sender, ISFSObject data)
		{
		}

		public void OnServerCommand(string cmd, ISFSObject parameters)
		{
		}
	}
}
