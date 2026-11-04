using System;
using System.Collections.Generic;
using System.Reflection;
using GameInput;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class GameHotkeyAttribute : Attribute
{
	public readonly ActionType actionType;

	public readonly bool skipInputFX;

	public readonly bool repeat;

	private static Dictionary<MethodInfo, GameHotkeyAttribute[]> m_cacheMethodAttributes = new Dictionary<MethodInfo, GameHotkeyAttribute[]>();

	public GameHotkeyAttribute(ActionType actionType, bool skipInputFX = false, bool repeat = false)
	{
		this.actionType = actionType;
		this.skipInputFX = skipInputFX;
		this.repeat = repeat;
	}

	public static GameHotkeyAttribute[] GetAttributes(MethodInfo m)
	{
		if (!m_cacheMethodAttributes.TryGetValue(m, out var value))
		{
			value = Attribute.GetCustomAttributes(m, typeof(GameHotkeyAttribute)) as GameHotkeyAttribute[];
			m_cacheMethodAttributes[m] = value;
		}
		return value;
	}
}
