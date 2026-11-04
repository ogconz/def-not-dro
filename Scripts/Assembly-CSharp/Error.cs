using SimpleJSON;

public class Error
{
	public enum EType
	{
		Unknown = 0,
		None = 1,
		NoResponse = 2,
		RequestFailed = 3,
		EmptyResponse = 4,
		CorruptedResponse = 5,
		ServerOverloaded = 6,
		LegacyError = 7,
		LOW_LEVEL_ERRORS_DELIMITER = 8,
		NativeAuthFailed = 9,
		WrongClientVersion = 10,
		DebugFailure = 11,
		not_authorized = 12,
		invalid_ticket = 13,
		no_connection_to_validation_server = 14,
		hash_changed = 15,
		old_token = 16,
		unknown_user = 17,
		steam_id_is_compromised = 18,
		no_possible_nodes = 19,
		no_possible_create = 20,
		could_not_create_room = 21
	}

	[JsonIgnore]
	public EType type = EType.None;

	[JsonIgnore]
	private string m_codeValue;

	public string code
	{
		get
		{
			return m_codeValue;
		}
		set
		{
			m_codeValue = value;
			type = GeneralExtensions.TryParseEnum(value, EType.Unknown);
		}
	}

	public string debugMessage { get; set; }

	public string guiMessage { get; set; }

	public bool IsConnectionError()
	{
		if (type != EType.None)
		{
			return type < EType.LOW_LEVEL_ERRORS_DELIMITER;
		}
		return false;
	}

	public override string ToString()
	{
		return $"[{type}] {debugMessage} ({guiMessage})]";
	}
}
