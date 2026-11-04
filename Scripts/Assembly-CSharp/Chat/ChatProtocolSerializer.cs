using System;
using Chat.Protocol;
using Newtonsoft.Json;

namespace Chat
{
	public class ChatProtocolSerializer
	{
		public string BrowseRooms(int limit, int offset)
		{
			return JsonConvert.SerializeObject(new PayloadContainer<BrowseRoomsPayload>
			{
				type = "BROWSE_ROOMS",
				payload = new BrowseRoomsPayload
				{
					limit = limit,
					offset = offset
				}
			});
		}

		public string JoinRoom(Guid roomId, string password)
		{
			return JsonConvert.SerializeObject(new PayloadContainer<JoinRoomPayload>
			{
				type = "JOIN_ROOM",
				payload = new JoinRoomPayload
				{
					roomId = roomId,
					password = password
				}
			});
		}

		public string LeaveRoom(Guid roomId)
		{
			return JsonConvert.SerializeObject(new PayloadContainer<LeaveRoomPayload>
			{
				type = "LEAVE_ROOM",
				payload = new LeaveRoomPayload
				{
					roomId = roomId
				}
			});
		}

		public string GetAndJoinMpRoom(Guid roomId, int maxPlayersCount, string password)
		{
			return JsonConvert.SerializeObject(new PayloadContainer<GetAndJoinMpRoomPayload>
			{
				type = "GET_AND_JOIN_MP_ROOM",
				payload = new GetAndJoinMpRoomPayload
				{
					mpRoomId = roomId,
					maxPlayersCount = maxPlayersCount,
					password = password
				}
			});
		}

		public string SendMessage(TargetInfo target, Guid id, string message)
		{
			return JsonConvert.SerializeObject(new PayloadContainer<SendMessagePayload>
			{
				type = "MESSAGE",
				payload = new SendMessagePayload
				{
					id = id,
					message = message,
					target = target
				}
			});
		}

		public string Ping()
		{
			return "ping";
		}
	}
}
