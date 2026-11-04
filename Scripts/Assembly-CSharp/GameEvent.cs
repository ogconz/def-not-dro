using System.Collections.Generic;

public class GameEvent<T> : IGameEvent where T : GameEvents.EventInfo
{
	private uint SUBSCRIBER_ID_COUNTER = 1u;

	private List<GameEventSubscriber<T>> m_subscribers = new List<GameEventSubscriber<T>>();

	private List<T> m_autoCallArgs;

	public uint eventID { get; private set; }

	public GameEvent(bool eventRegistration = false)
	{
		if (eventRegistration)
		{
			GameEvents.EVENT_ID_COUNTER++;
			eventID = GameEvents.EVENT_ID_COUNTER;
			GameEvents.RegisterEvent(this);
		}
	}

	public GameEvent(uint customID)
	{
		eventID = customID;
	}

	public uint GetID()
	{
		return eventID;
	}

	public void ActivateInvokeOnSubscribeMode(T args)
	{
		if (m_autoCallArgs == null)
		{
			m_autoCallArgs = new List<T>();
		}
		m_autoCallArgs.Add(args);
	}

	public bool IsActiveInvokeOnSubsribeMode()
	{
		return m_autoCallArgs != null;
	}

	public void DeactivateInvokeOnSubscribeMode()
	{
		m_autoCallArgs = null;
	}

	public void Invoke(T args, uint subscriberID = 0u)
	{
		args.eventID = eventID;
		if (m_subscribers.Count <= 0)
		{
			return;
		}
		for (int num = m_subscribers.Count - 1; num >= 0; num--)
		{
			if (subscriberID == 0 || subscriberID == m_subscribers[num].ID)
			{
				args.subscriberID = m_subscribers[num].ID;
				if ((m_subscribers[num].flags & GameEventFlag.OneCall) != GameEventFlag.Global)
				{
					m_subscribers[num].callback(args);
					m_subscribers.RemoveAt(num);
				}
				else
				{
					m_subscribers[num].callback(args);
				}
			}
		}
	}

	public uint Subscribe(GameEventHandler<GameEvents.EventInfo> callback)
	{
		return Subscribe(GameEventFlag.Global, callback);
	}

	uint IGameEvent.Subscribe(GameEventFlag flags, GameEventHandler<GameEvents.EventInfo> callback)
	{
		return Subscribe(flags, callback);
	}

	public uint Subscribe(GameEventFlag flags, GameEventHandler<T> callback)
	{
		uint sUBSCRIBER_ID_COUNTER = SUBSCRIBER_ID_COUNTER;
		SUBSCRIBER_ID_COUNTER++;
		if (SUBSCRIBER_ID_COUNTER == uint.MaxValue)
		{
			SUBSCRIBER_ID_COUNTER = 1u;
		}
		if (m_autoCallArgs != null)
		{
			if ((flags & GameEventFlag.OneCall) != GameEventFlag.Global)
			{
				foreach (T autoCallArg in m_autoCallArgs)
				{
					autoCallArg.eventID = eventID;
					autoCallArg.subscriberID = 0u;
					callback(autoCallArg);
				}
				return 0u;
			}
			GameEventSubscriber<T> gameEventSubscriber = new GameEventSubscriber<T>();
			gameEventSubscriber.ID = sUBSCRIBER_ID_COUNTER;
			gameEventSubscriber.flags = flags;
			gameEventSubscriber.callback = callback;
			m_subscribers.Add(gameEventSubscriber);
			foreach (T autoCallArg2 in m_autoCallArgs)
			{
				Invoke(autoCallArg2, gameEventSubscriber.ID);
			}
			return gameEventSubscriber.ID;
		}
		GameEventSubscriber<T> gameEventSubscriber2 = new GameEventSubscriber<T>();
		gameEventSubscriber2.ID = sUBSCRIBER_ID_COUNTER;
		gameEventSubscriber2.flags = flags;
		gameEventSubscriber2.callback = callback;
		m_subscribers.Add(gameEventSubscriber2);
		return gameEventSubscriber2.ID;
	}

	public void Unsubscribe(GameEventHandler<T> callback)
	{
		m_subscribers.RemoveAll((GameEventSubscriber<T> c) => c.callback == callback);
	}

	public void Unsubscribe(uint ID)
	{
		m_subscribers.RemoveAll((GameEventSubscriber<T> c) => c.ID == ID);
	}

	public void CleanUp(GameEventFlag flags)
	{
		m_subscribers.RemoveAll((GameEventSubscriber<T> c) => (c.flags & flags) != 0);
	}

	public void Clear()
	{
		m_subscribers.Clear();
	}

	public static GameEvent<T> operator +(GameEvent<T> lhs, GameEventHandler<T> rhs)
	{
		lhs.Subscribe(GameEventFlag.Global, rhs);
		return lhs;
	}

	public static GameEvent<T> operator -(GameEvent<T> lhs, GameEventHandler<T> rhs)
	{
		lhs.Unsubscribe(rhs);
		return lhs;
	}

	public static GameEvent<T> operator -(GameEvent<T> lhs, uint rhs)
	{
		lhs.Unsubscribe(rhs);
		return lhs;
	}
}
