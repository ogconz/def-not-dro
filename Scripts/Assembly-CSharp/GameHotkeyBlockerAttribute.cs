using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class GameHotkeyBlockerAttribute : Attribute
{
	public readonly bool isBlocker;

	public GameHotkeyBlockerAttribute(bool isBlocker)
	{
		this.isBlocker = isBlocker;
	}
}
