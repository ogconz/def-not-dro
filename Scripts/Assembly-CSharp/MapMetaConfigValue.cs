using System;
using UnityEngine;

[Serializable]
public struct MapMetaConfigValue
{
	public string mapName;

	public string mapDescription;

	public Texture2D icon;

	public Texture2D largeIcon;

	public PlatformBuild platform;

	public CompressBuild compress;
}
