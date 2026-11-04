using System;
using Binding.Components;

public struct HeaderActionData
{
	public string text;

	public Action action;

	public bool hasNotification;

	public HeaderActionBinding.ViewType viewType;
}
