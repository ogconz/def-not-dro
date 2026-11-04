using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[DefaultExecutionOrder(-16001)]
public class TimeController : MonoBehaviour
{
	public static Action onGameEngineStop;

	private static readonly List<ActionHolder> m_callbacks = new List<ActionHolder>();

	private static readonly List<ActionHolder> m_syncCallbacks = new List<ActionHolder>();

	private static readonly List<IEnumerator> m_coroutinesToStart = new List<IEnumerator>();

	private static readonly List<IEnumerator> m_coroutinesToStop = new List<IEnumerator>();

	private List<ActionHolder> m_callbacksTemp = new List<ActionHolder>();

	private WaitForFixedUpdate m_waitForFixedUpdate = new WaitForFixedUpdate();

	public static Action onUpdate;

	public static Action onFixedUpdate;

	public static Action onAfterFixedUpdate;

	public static Action onLateUpdate;

	public static TimeController instance { get; private set; }

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		StartCoroutine(WaitForFixedUpdateLoop());
	}

	public static ActionHolder AddCallback(float delay, Action callback)
	{
		if (callback == null)
		{
			return null;
		}
		ActionHolder actionHolder = new ActionHolder(Time.realtimeSinceStartup + delay, callback);
		m_callbacks.Add(actionHolder);
		return actionHolder;
	}

	public static void RemoveCallback(ActionHolder callback)
	{
		m_callbacks.Remove(callback);
	}

	public static void CallFromMainThread(Action action)
	{
		ActionHolder item = new ActionHolder(0f, action);
		lock (m_syncCallbacks)
		{
			m_syncCallbacks.Add(item);
		}
	}

	public static void CallFromMainThread<T>(Action<T> action, T param)
	{
		ActionHolderWParam<T> item = new ActionHolderWParam<T>(0f, action, param);
		lock (m_syncCallbacks)
		{
			m_syncCallbacks.Add(item);
		}
	}

	public static void CallWithTimeout(Action<Action> actionWithCallback, float timeout, Action onDoneCallback, Action timeoutCallback)
	{
		bool fulfilled = false;
		bool timeouted = false;
		AddCallback(timeout, delegate
		{
			timeouted = true;
			if (!fulfilled && timeoutCallback != null)
			{
				timeoutCallback();
			}
		});
		Action obj = delegate
		{
			fulfilled = true;
			if (!timeouted && onDoneCallback != null)
			{
				onDoneCallback();
			}
		};
		actionWithCallback(obj);
	}

	public static Coroutine StartNow(IEnumerator coroutine)
	{
		return instance.StartCoroutine(coroutine);
	}

	public static void StopNow(IEnumerator coroutine)
	{
		if (instance != null)
		{
			instance.StopCoroutine(coroutine);
		}
	}

	public static void StopNow(Coroutine coroutine)
	{
		if (instance != null)
		{
			instance.StopCoroutine(coroutine);
		}
	}

	public static IEnumerator RunCoroutine(IEnumerator coroutine)
	{
		if (coroutine == null)
		{
			return null;
		}
		m_coroutinesToStart.Add(coroutine);
		return coroutine;
	}

	public static void RemoveCoroutine(IEnumerator coroutine)
	{
		if (coroutine != null)
		{
			m_coroutinesToStop.Add(coroutine);
		}
	}

	public static void RunInThreadPool<T>(Func<T> job, Action<T> callback)
	{
		if (job != null && !ThreadPool.QueueUserWorkItem(delegate
		{
			T param = job();
			CallFromMainThread(callback, param);
		}))
		{
			GameLog.Print(AlertLevel.Error, "TimeController", "Could not queue thread pool job");
		}
	}

	private void OnEnable()
	{
		GameEvents.OnGameEngineStop += new GameEventHandler<GameEvents.EventInfo>(OnGameEngineStop);
	}

	private void OnDisable()
	{
		GameEvents.OnGameEngineStop -= new GameEventHandler<GameEvents.EventInfo>(OnGameEngineStop);
	}

	private void OnGameEngineStop(GameEvents.EventInfo e)
	{
		onGameEngineStop.SafeCall();
	}

	private void Update()
	{
		if (m_syncCallbacks.Count > 0)
		{
			lock (m_syncCallbacks)
			{
				m_callbacks.AddRange(m_syncCallbacks);
				m_syncCallbacks.Clear();
			}
		}
		if (m_callbacks.Count > 0)
		{
			m_callbacksTemp.Clear();
			m_callbacksTemp.AddRange(m_callbacks);
			for (int i = 0; i < m_callbacksTemp.Count; i++)
			{
				ActionHolder actionHolder = m_callbacksTemp[i];
				if (actionHolder.IsReady())
				{
					m_callbacks.Remove(actionHolder);
					actionHolder.Call();
				}
			}
		}
		if (m_coroutinesToStart.Count > 0)
		{
			for (int j = 0; j < m_coroutinesToStart.Count; j++)
			{
				StartCoroutine(m_coroutinesToStart[j]);
			}
			m_coroutinesToStart.Clear();
		}
		if (m_coroutinesToStop.Count > 0)
		{
			for (int k = 0; k < m_coroutinesToStop.Count; k++)
			{
				StopCoroutine(m_coroutinesToStop[k]);
			}
			m_coroutinesToStop.Clear();
		}
		onUpdate.SafeCall();
	}

	private void LateUpdate()
	{
		onLateUpdate.SafeCall();
	}

	private void FixedUpdate()
	{
		onFixedUpdate.SafeCall();
	}

	private IEnumerator WaitForFixedUpdateLoop()
	{
		while (true)
		{
			yield return m_waitForFixedUpdate;
			onAfterFixedUpdate.SafeCall();
		}
	}
}
