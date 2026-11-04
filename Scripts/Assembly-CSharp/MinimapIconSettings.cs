using System;
using UnityEngine;

[Serializable]
public class MinimapIconSettings : ScriptableSingleton<MinimapIconSettings>
{
	public Color[] networkColors;

	public Minimap.IconType[] iconTypes;
}
