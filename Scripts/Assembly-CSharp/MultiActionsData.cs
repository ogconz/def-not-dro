using System;
using GameInput;

public struct MultiActionsData
{
	public string hint;

	public ActionType[] type;

	public System.Action[] action;

	public MultiActionsData(string hint, params (ActionType type, System.Action action)[] actions)
	{
		this.hint = hint;
		type = new ActionType[actions.Length];
		action = new System.Action[actions.Length];
		for (int i = 0; i < actions.Length; i++)
		{
			type[i] = actions[i].type;
			action[i] = actions[i].action;
		}
	}

	public MultiActionsData(string hint, params ActionType[] actions)
	{
		this.hint = hint;
		type = new ActionType[actions.Length];
		action = new System.Action[actions.Length];
		for (int i = 0; i < actions.Length; i++)
		{
			type[i] = actions[i];
			action[i] = null;
		}
	}

	public MultiActionsData(string hint, ActionType type, System.Action action)
		: this(hint, (type, action))
	{
	}
}
