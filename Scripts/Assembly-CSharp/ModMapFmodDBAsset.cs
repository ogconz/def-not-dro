using System;
using System.Collections.Generic;
using FMODUnity;

public class ModMapFmodDBAsset : SingletonScriptableObject<ModMapFmodDBAsset>
{
	[Serializable]
	public class FmodData
	{
		public string path;

		public EventReference eventRef;
	}

	public List<FmodData> fmodDatas = new List<FmodData>();

	public FmodData Get(string path)
	{
		return fmodDatas.Find((FmodData data) => data.path == path);
	}
}
