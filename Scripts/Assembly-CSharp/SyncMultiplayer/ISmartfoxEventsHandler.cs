using System.Collections.Generic;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;

namespace SyncMultiplayer
{
	public interface ISmartfoxEventsHandler
	{
		void OnInternalError(string message);

		void OnStateChanged(ClientState previousState, ClientState newState);

		void OnLoginError(short code, string message);

		void OnSocketError(short code, string message);

		void OnJoinedLobby(SmartfoxClient client);

		void OnRoomAdded(Room room);

		void OnRoomRemoved(Room room);

		void OnRoomJoined(Room room);

		void OnRoomVariablesUpdate(Room room, HashSet<string> changedProps);

		void OnRoomJoinError(short code, string message);

		void OnRoomCreateError(short code, string message);

		void OnUserVariablesUpdate(SFSUser sender, HashSet<string> changedProps);

		void OnPlayerJoinedRoom(Sfs2X.Entities.User user);

		void OnPlayerLeftRoom(Sfs2X.Entities.User user);

		void OnRoomMessage(SFSUser sender, ISFSObject data);

		void OnServerCommand(string cmd, ISFSObject parameters);
	}
}
