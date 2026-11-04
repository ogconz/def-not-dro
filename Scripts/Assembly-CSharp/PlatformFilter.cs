using System.Collections.Generic;
using UnityEngine;

public class PlatformFilter : MonoBehaviour
{
	public enum PlatformGroup
	{
		None = 0,
		IOS = 1,
		Android = 2,
		Standalone = 3,
		PS4 = 4
	}

	[Tooltip("Leave empty to disable this filter")]
	public List<PlatformGroup> enabledOnPlatform;

	public List<PlatformGroup> disabledOnPlatform;

	private static readonly Dictionary<PlatformGroup, List<RuntimePlatform>> PlatformGroups = new Dictionary<PlatformGroup, List<RuntimePlatform>>
	{
		{
			PlatformGroup.IOS,
			new List<RuntimePlatform> { RuntimePlatform.IPhonePlayer }
		},
		{
			PlatformGroup.PS4,
			new List<RuntimePlatform> { RuntimePlatform.PS4 }
		},
		{
			PlatformGroup.Android,
			new List<RuntimePlatform> { RuntimePlatform.Android }
		},
		{
			PlatformGroup.Standalone,
			new List<RuntimePlatform>
			{
				RuntimePlatform.WindowsEditor,
				RuntimePlatform.WindowsPlayer,
				RuntimePlatform.OSXEditor,
				RuntimePlatform.OSXPlayer,
				RuntimePlatform.LinuxPlayer
			}
		}
	};

	private void OnEnable()
	{
		if (enabledOnPlatform.Count > 0 && !enabledOnPlatform.Exists((PlatformGroup x) => PlatformGroups[x].Contains(Application.platform)))
		{
			base.gameObject.SetActive(value: false);
		}
		else if (disabledOnPlatform.Exists((PlatformGroup x) => PlatformGroups[x].Contains(Application.platform)))
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
