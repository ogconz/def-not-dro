using System;
using UnityEngine;

[AddComponentMenu("Scripts/UI/ReplayRewindButton")]
public class UIReplayRewindButton : MonoBehaviour
{
	public Action<bool> onPressEvent;

	private void OnPress(bool isPressed)
	{
		onPressEvent.Call(isPressed);
	}
}
