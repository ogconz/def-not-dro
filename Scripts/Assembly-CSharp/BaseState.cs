using UnityEngine;

public class BaseState : MonoBehaviour
{
	[SerializeField]
	private string m_stateScene;

	[SerializeField]
	private string[] m_additiveScenes;

	private bool m_isPopupState;

	public string stateScene => m_stateScene;

	public string[] additiveScenes => m_additiveScenes;

	public bool isPopupState
	{
		get
		{
			return m_isPopupState;
		}
		set
		{
			m_isPopupState = value;
		}
	}

	public BaseState parent
	{
		get
		{
			if (!base.transform.parent)
			{
				return null;
			}
			return base.transform.parent.GetComponent<BaseState>();
		}
	}

	public bool isCurrent => States.current == this;

	private static void LogState(BaseState state, string message)
	{
		if (States.isLogEnabled())
		{
			GameLog.Print(AlertLevel.Notify, "States", $"[States] \t{message}: {state}");
		}
	}

	public BaseState FindParentStateInStack(BaseState state)
	{
		if (IsConnectedState(state))
		{
			return this;
		}
		BaseState baseState = parent;
		if (baseState == state)
		{
			return baseState;
		}
		if (!(baseState != null))
		{
			return null;
		}
		return baseState.FindParentStateInStack(state);
	}

	public bool IsConnectedState(BaseState state)
	{
		foreach (Transform item in base.transform)
		{
			if (item.GetComponent<BaseState>() == state)
			{
				return true;
			}
		}
		return false;
	}

	public static BaseState FindChildState(Transform root, string name)
	{
		Transform transform = root.Find(name);
		if (!transform)
		{
			return null;
		}
		return transform.GetComponent<BaseState>();
	}

	public virtual void OnEnter()
	{
		LogState(this, "ENTER");
	}

	public virtual void OnExit()
	{
		LogState(this, "EXIT");
	}

	public virtual void OnPaused()
	{
		LogState(this, "PAUSED");
	}

	public virtual void OnResume()
	{
		LogState(this, "RESUME");
	}

	public virtual void OnInit()
	{
	}

	public virtual void OnUpdate()
	{
	}

	public virtual void OnFixedUpdate()
	{
	}

	public virtual void OnLateUpdate()
	{
	}

	public override string ToString()
	{
		int num = base.name.IndexOf(" <Current>");
		string arg = ((num != -1) ? base.name.Substring(0, num) : base.name);
		return $"{arg} ({GetType()})";
	}
}
