namespace Chat.Protocol
{
	public class PayloadType
	{
		public const string PlayerInfo = "PLAYER_INFO";

		public const string BrowseRooms = "BROWSE_ROOMS";

		public const string RoomsInfo = "ROOMS_INFO";

		public const string JoinRoom = "JOIN_ROOM";

		public const string LeaveRoom = "LEAVE_ROOM";

		public const string GetAndJoinMpRoom = "GET_AND_JOIN_MP_ROOM";

		public const string Message = "MESSAGE";

		public const string RoomRemoved = "ROOM_REMOVED";

		public const string Ban = "BAN";

		public const string Ping = "ping";

		public const string Pong = "pong";
	}
}
