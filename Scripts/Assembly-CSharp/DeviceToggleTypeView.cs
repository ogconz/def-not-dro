using System;
using UnityEngine;
using UnityEngine.UI;

public class DeviceToggleTypeView : MonoBehaviour
{
	[SerializeField]
	private Button m_nextDeviceTypeButton;

	[SerializeField]
	private Button m_prevDeviceTypeButton;

	public Action OnClickButtonNext;

	public Action OnClickButtonPrev;

	private void Awake()
	{
		m_nextDeviceTypeButton.onClick.AddListener(delegate
		{
			OnClickButtonNext.SafeCall();
		});
		m_prevDeviceTypeButton.onClick.AddListener(delegate
		{
			OnClickButtonPrev.SafeCall();
		});
	}

	protected void OnDestroy()
	{
		m_nextDeviceTypeButton.onClick.RemoveAllListeners();
		m_prevDeviceTypeButton.onClick.RemoveAllListeners();
	}
}
