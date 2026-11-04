using System;
using System.Collections.Generic;
using System.Linq;
using GameOverlay;
using Google.Protobuf;
using UnityEngine;

namespace chats
{
	public class Client
	{
		private static List<ChsOutcomingMessageTypes> TYPES_WITH_CONTENT = new List<ChsOutcomingMessageTypes>
		{
			ChsOutcomingMessageTypes.UserEntersRoom,
			ChsOutcomingMessageTypes.UserLeavesRoom,
			ChsOutcomingMessageTypes.UserMessage
		};

		public Action<ChsContentItemDTO, string> onMessageReceived;

		public Dictionary<string, RequestFinishedDelegate> handlers = new Dictionary<string, RequestFinishedDelegate>();

		private ChsAuthorizedTokenDTO token;

		private float m_endChatBanTime;

		public ChatClientEndpoint endpoint { get; private set; }

		public string roomName { get; private set; }

		public string userId { get; private set; }

		public string userName { get; private set; }

		public bool isInRoom { get; private set; }

		public bool isBanned => Time.realtimeSinceStartup < m_endChatBanTime;

		public float banTime => m_endChatBanTime - Time.realtimeSinceStartup;

		public Client(string roomName, string userId, string userName, ChatClientEndpoint endpoint)
		{
			isInRoom = false;
			this.roomName = roomName;
			this.userId = userId;
			this.userName = userName;
			this.endpoint = endpoint;
			endpoint.MessageHandler = (Action<byte[]>)Delegate.Combine(endpoint.MessageHandler, new Action<byte[]>(MessageHandler));
			endpoint.OnLostConnection = (Action)Delegate.Combine(endpoint.OnLostConnection, (Action)delegate
			{
				isInRoom = false;
			});
		}

		public void DoLostConnection()
		{
			isInRoom = false;
		}

		public void EnterRoom(string roomId, RequestFinishedDelegate handler = null)
		{
			RtuAuthenticationCredentialsDTO rtuAuthenticationCredentialsDTO = new RtuAuthenticationCredentialsDTO();
			rtuAuthenticationCredentialsDTO.Name = userName;
			rtuAuthenticationCredentialsDTO.RoomId = roomId;
			rtuAuthenticationCredentialsDTO.GlobalToken = new ChsGlobalTokenDTO
			{
				Authority = "STEAM",
				Token = GameSync.token
			};
			ChsIncomingMessageDTO chsIncomingMessageDTO = NewMessage(ChsIncomingMessageTypes.Enter);
			chsIncomingMessageDTO.Payload = Serialize(rtuAuthenticationCredentialsDTO);
			SendMessage(chsIncomingMessageDTO, delegate(ChsOutcomingMessageDTO e)
			{
				RECV_EnterRoomResult(e);
				handler(e);
			});
		}

		public void SayPhrase(string phrase, PlayerId target, RequestFinishedDelegate callback)
		{
			ChsIncomingMessageDTO chsIncomingMessageDTO = null;
			if (target.IsInvalid)
			{
				chsIncomingMessageDTO = NewMessage(ChsIncomingMessageTypes.Message);
				chsIncomingMessageDTO.Payload = SerializeString(phrase);
			}
			else
			{
				chsIncomingMessageDTO = NewMessage(ChsIncomingMessageTypes.PrivateMessage);
				ChsIncomingPrivateMessageDTO chsIncomingPrivateMessageDTO = new ChsIncomingPrivateMessageDTO();
				chsIncomingPrivateMessageDTO.Content = SerializeString(phrase);
				chsIncomingPrivateMessageDTO.Receivers.Add(target.ToString());
				chsIncomingMessageDTO.Payload = Serialize(chsIncomingPrivateMessageDTO);
			}
			SendMessage(chsIncomingMessageDTO, callback);
		}

		public List<string> GetUserData(string key)
		{
			foreach (ChsUserDataDTO additionalDataCollection in token.AdditionalDataCollections)
			{
				if (additionalDataCollection.Key == key)
				{
					return new List<string>(additionalDataCollection.Values);
				}
			}
			return new List<string>();
		}

		public void UserDataOperation(UserDataOperation operation, string key, string value)
		{
			ChsIncomingMessageDTO chsIncomingMessageDTO = null;
			switch (operation)
			{
			default:
				return;
			case chats.UserDataOperation.Add:
			{
				chsIncomingMessageDTO = NewMessage(ChsIncomingMessageTypes.AddData);
				bool flag = false;
				foreach (ChsUserDataDTO additionalDataCollection in token.AdditionalDataCollections)
				{
					if (additionalDataCollection.Key == key)
					{
						additionalDataCollection.Values.Remove(value);
						additionalDataCollection.Values.Add(value);
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					ChsUserDataDTO chsUserDataDTO2 = new ChsUserDataDTO();
					chsUserDataDTO2.Key = key;
					chsUserDataDTO2.Values.Add(value);
					token.AdditionalDataCollections.Add(chsUserDataDTO2);
				}
				break;
			}
			case chats.UserDataOperation.Remove:
				chsIncomingMessageDTO = NewMessage(ChsIncomingMessageTypes.RemoveData);
				foreach (ChsUserDataDTO additionalDataCollection2 in token.AdditionalDataCollections)
				{
					if (additionalDataCollection2.Key == key)
					{
						additionalDataCollection2.Values.Remove(value);
					}
				}
				break;
			case chats.UserDataOperation.Replace:
			{
				chsIncomingMessageDTO = NewMessage(ChsIncomingMessageTypes.AddData);
				foreach (ChsUserDataDTO additionalDataCollection3 in token.AdditionalDataCollections)
				{
					if (additionalDataCollection3.Key == key)
					{
						token.AdditionalDataCollections.Remove(additionalDataCollection3);
						break;
					}
				}
				ChsUserDataDTO chsUserDataDTO = new ChsUserDataDTO();
				chsUserDataDTO.Key = key;
				chsUserDataDTO.Values.Add(value);
				token.AdditionalDataCollections.Add(chsUserDataDTO);
				break;
			}
			}
			ChsUserDataDTO chsUserDataDTO3 = new ChsUserDataDTO();
			chsUserDataDTO3.Key = key;
			chsUserDataDTO3.Values.Add(value);
			chsIncomingMessageDTO.Payload = Serialize(chsUserDataDTO3);
			SendMessage(chsIncomingMessageDTO, delegate
			{
			});
		}

		public void LeaveRoom(RequestFinishedDelegate completedHandler)
		{
			if (!isInRoom)
			{
				completedHandler(null);
				return;
			}
			SendMessage(NewMessage(ChsIncomingMessageTypes.Leave), delegate(ChsOutcomingMessageDTO e)
			{
				isInRoom = false;
				completedHandler(e);
			});
		}

		private void MessageHandler(byte[] data)
		{
			try
			{
				ChsOutcomingMessageDTO chsOutcomingMessageDTO = ChsOutcomingMessageDTO.Parser.ParseFrom(data);
				if (!IsMessageForThisClient(chsOutcomingMessageDTO))
				{
					return;
				}
				if (chsOutcomingMessageDTO.Type == ChsOutcomingMessageTypes.Error)
				{
					ChsErrorInfoDTO chsErrorInfoDTO = ChsErrorInfoDTO.Parser.ParseFrom(chsOutcomingMessageDTO.Content);
					switch (chsErrorInfoDTO.ErrorType)
					{
					case ChsErrorTypes.FilterRestriction:
					{
						ChsFilterAlertDTO error = ChsFilterAlertDTO.Parser.ParseFrom(chsErrorInfoDTO.Payload);
						ReceiveFilterErrorMessage(error);
						break;
					}
					case ChsErrorTypes.NoPermission:
					{
						ChsNoPermissionErrorDTO error2 = ChsNoPermissionErrorDTO.Parser.ParseFrom(chsErrorInfoDTO.Payload);
						ReceivePermissionErrorMessage(error2);
						break;
					}
					default:
					{
						string text = "";
						try
						{
							text = BytesToString(chsErrorInfoDTO.Payload);
						}
						catch
						{
							text = "";
						}
						GameConsole.Print(string.IsNullOrEmpty(text) ? $"{LocalizationKeys.god_voice_chat_error.Localized()} {chsErrorInfoDTO.ErrorType}" : $"{LocalizationKeys.god_voice_chat_error.Localized()} {chsErrorInfoDTO.ErrorType}\r\n{text}");
						break;
					}
					case ChsErrorTypes.NoMessageReceivers:
						break;
					}
				}
				else if (TYPES_WITH_CONTENT.Contains(chsOutcomingMessageDTO.Type))
				{
					ChsContentItemDTO chsContentItemDTO = ChsContentItemDTO.Parser.ParseFrom(chsOutcomingMessageDTO.Content);
					switch (chsOutcomingMessageDTO.Type)
					{
					case ChsOutcomingMessageTypes.UserMessage:
						if (onMessageReceived != null)
						{
							onMessageReceived(chsContentItemDTO, BytesToString(chsContentItemDTO.RawContent));
						}
						break;
					case ChsOutcomingMessageTypes.UserEntersRoom:
						Log(chsContentItemDTO.AuthorName + " enters room");
						break;
					case ChsOutcomingMessageTypes.UserLeavesRoom:
						Log(chsContentItemDTO.AuthorName + " leaves room");
						break;
					}
				}
				if (chsOutcomingMessageDTO.Type != ChsOutcomingMessageTypes.Ack && chsOutcomingMessageDTO.Type != ChsOutcomingMessageTypes.Error)
				{
					Ack(chsOutcomingMessageDTO.MessageToken);
				}
				FireRequestFinished(chsOutcomingMessageDTO);
			}
			catch (Exception ex)
			{
				Log(ex.ToString());
			}
		}

		private void ReceiveFilterErrorMessage(ChsFilterAlertDTO error)
		{
			m_endChatBanTime = Time.realtimeSinceStartup + (float)error.PenaltyDurationMilliseconds / 1000f;
			InformUserAboutChatBan(justTaked: true);
		}

		private void ReceivePermissionErrorMessage(ChsNoPermissionErrorDTO error)
		{
			m_endChatBanTime = Time.realtimeSinceStartup + (float)error.DeniedUntil / 1000f;
			InformUserAboutChatBan();
		}

		public void InformUserAboutChatBan(bool justTaked = false)
		{
			if (onMessageReceived != null)
			{
				float f = m_endChatBanTime - Time.realtimeSinceStartup;
				ChsContentItemDTO arg = new ChsContentItemDTO();
				string arg2 = LocalizationKeys.chat_warning_muted.LocalizedFormat(Mathf.RoundToInt(f).ToString());
				onMessageReceived(arg, arg2);
			}
		}

		public bool IsMessageForThisClient(ChsOutcomingMessageDTO message)
		{
			if (!handlers.ContainsKey(BytesToString(message.MessageToken)) && (token == null || message.Token == null || !token.Token.SequenceEqual(message.Token)))
			{
				return false;
			}
			return true;
		}

		private void RECV_EnterRoomResult(ChsOutcomingMessageDTO message)
		{
			if (message.Type == ChsOutcomingMessageTypes.Error)
			{
				if (ChsErrorInfoDTO.Parser.ParseFrom(message.Content).Code == 401)
				{
					GameSync.AuthorizeReset();
				}
				return;
			}
			ChsEnterRoomInfoDTO chsEnterRoomInfoDTO = ChsEnterRoomInfoDTO.Parser.ParseFrom(message.Content);
			token = chsEnterRoomInfoDTO.Token;
			isInRoom = true;
			_ = chsEnterRoomInfoDTO.Content;
			if (chsEnterRoomInfoDTO.UsersInRoom != null)
			{
				string text = "";
				foreach (ChsUserInfoDTO item in chsEnterRoomInfoDTO.UsersInRoom)
				{
					text = text + item.Name + ";";
				}
				Log("Users now in room: " + text);
			}
			if (chsEnterRoomInfoDTO.TemporallyDeniedPermissions == null)
			{
				return;
			}
			foreach (ChsKeyValueDTO temporallyDeniedPermission in chsEnterRoomInfoDTO.TemporallyDeniedPermissions)
			{
				if (temporallyDeniedPermission.Key == "SEND_MESSAGE")
				{
					ulong result = 0uL;
					ulong.TryParse(temporallyDeniedPermission.Value, out result);
					if (result != 0)
					{
						m_endChatBanTime = Time.realtimeSinceStartup + (float)result / 1000f;
					}
				}
			}
		}

		private void Log(string message)
		{
		}

		private void Ack(ByteString messageToken)
		{
			ChsIncomingMessageDTO chsIncomingMessageDTO = NewMessage(ChsIncomingMessageTypes.Ack);
			chsIncomingMessageDTO.MessageToken = messageToken;
			SendMessage(chsIncomingMessageDTO, null);
		}

		private ChsIncomingMessageDTO NewMessage(ChsIncomingMessageTypes type)
		{
			ChsIncomingMessageDTO chsIncomingMessageDTO = new ChsIncomingMessageDTO();
			chsIncomingMessageDTO.Type = type;
			chsIncomingMessageDTO.MessageToken = ByteString.Unsafe.FromBytes(Guid.NewGuid().ToByteArray());
			if (token != null)
			{
				chsIncomingMessageDTO.Token = token.Token;
			}
			return chsIncomingMessageDTO;
		}

		private void SendMessage(ChsIncomingMessageDTO message, RequestFinishedDelegate handler)
		{
			if (handler != null)
			{
				lock (handlers)
				{
					handlers.Add(BytesToString(message.MessageToken), handler);
				}
			}
			endpoint.Send(ByteString.Unsafe.GetBuffer(MessageToBytes(message)));
		}

		private void FireRequestFinished(ChsOutcomingMessageDTO message)
		{
			RequestFinishedDelegate value = null;
			lock (handlers)
			{
				string key = BytesToString(message.MessageToken);
				if (handlers.TryGetValue(key, out value))
				{
					handlers.Remove(key);
				}
			}
			value?.Invoke(message);
		}

		private ByteString MessageToBytes(ChsIncomingMessageDTO dto)
		{
			return Serialize(dto);
		}

		private ByteString SerializeString(string str)
		{
			return ByteString.CopyFromUtf8(str);
		}

		private ByteString Serialize(IMessage item)
		{
			return item.ToByteString();
		}

		public string BytesToString(ByteString bytes)
		{
			return bytes.ToStringUtf8();
		}
	}
}
