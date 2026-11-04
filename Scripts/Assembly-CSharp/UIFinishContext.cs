using System;
using Binding;
using Binding.Base;
using FMODUnity;
using UnityEngine;

public class UIFinishContext : BaseBindingBehaviourTarget
{
	public const float SHOW_TIME = 5f;

	private BoolProperty m_finish = new BoolProperty();

	private BoolProperty m_timeIsOut = new BoolProperty();

	private BoolProperty m_custom = new BoolProperty();

	private BoolProperty m_customFail = new BoolProperty();

	private StringProperty m_customText = new StringProperty();

	[SerializeField]
	private GameObject m_fmodTimeOverEventObject;

	[SerializeField]
	private GameObject m_fmodFinishEventObject;

	[SerializeField]
	private GameObject m_fmodCustomEventObject;

	public event Action onFinishedEvent;

	protected override void OnActivate()
	{
		base.OnActivate();
		m_fmodTimeOverEventObject.SetActive(value: false);
		m_fmodFinishEventObject.SetActive(value: false);
		m_fmodCustomEventObject.SetActive(value: false);
		if (XRModeManager.IsXRActive())
		{
			XRModeManager.TryApplyCockpitCamera();
			Cursor.visible = false;
		}
	}

	protected override void OnDeactivate()
	{
		CancelInvoke();
		Cursor.visible = true;
		this.onFinishedEvent = null;
		base.OnDeactivate();
	}

	public void StopAllEvents()
	{
		StopEvent(m_fmodTimeOverEventObject);
		StopEvent(m_fmodFinishEventObject);
		StopEvent(m_fmodCustomEventObject);
	}

	public void PlayCustom(string text, bool fail = true)
	{
		m_finish.value = false;
		m_timeIsOut.value = false;
		m_customFail.value = fail;
		m_custom.value = true;
		m_customText.value = text;
		m_fmodCustomEventObject.SetActive(value: true);
		Invoke("OnFinished", 5f);
	}

	public void Play(bool timeIsOut)
	{
		m_finish.value = false;
		m_timeIsOut.value = false;
		m_custom.value = false;
		m_customText.value = "";
		if (timeIsOut)
		{
			Invoke("PlayTimeIsOut", 1f);
			return;
		}
		m_finish.value = true;
		m_fmodFinishEventObject.SetActive(value: true);
		Invoke("OnFinished", 5f);
	}

	private void PlayTimeIsOut()
	{
		m_timeIsOut.value = true;
		m_fmodTimeOverEventObject.SetActive(value: true);
		Invoke("OnFinished", 5f);
	}

	public void Stop()
	{
		this.onFinishedEvent = null;
		Hide();
	}

	public void OnFinished()
	{
		this.onFinishedEvent.Call();
		Hide();
	}

	protected override bool OnEscape()
	{
		OnFinished();
		return true;
	}

	private static void StopEvent(GameObject obj)
	{
		StudioEventEmitter component = obj.GetComponent<StudioEventEmitter>();
		if (!(component == null))
		{
			component.Stop();
		}
	}
}
