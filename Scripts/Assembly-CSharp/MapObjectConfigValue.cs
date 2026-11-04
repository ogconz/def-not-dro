using System;
using UnityEngine;

[Serializable]
public struct MapObjectConfigValue
{
	public string targetBundleMapName;

	public string mapName;

	[TextArea]
	public string mapDescription;

	public Texture2D icon;

	public Texture2D largeIcon;

	public Texture2D miniMapIcon;

	[TextArea]
	public string[] standalone;
}
