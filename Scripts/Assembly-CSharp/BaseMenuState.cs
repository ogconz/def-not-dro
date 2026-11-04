using GameInput;
using UnityEngine;

[AddComponentMenu("")]
public abstract class BaseMenuState : BaseGameState
{
	[SerializeField]
	private bool m_disableMainCamera;

	private bool m_storedMainCameraEnabled;

	private Camera m_storedMainCamera;

	protected static bool m_pauseOnShow = true;

	protected static bool m_pauseSfxOnShow = true;

	protected static bool m_resumeSfxOnExit = true;

	private bool m_playerCarControlEnabled;

	public override void OnEnter()
	{
		base.OnEnter();
		OpenMenu();
	}

	public override void OnExit()
	{
		base.OnExit();
		CloseMenu();
	}

	public override void OnPaused()
	{
		base.OnPaused();
		CloseMenu(m_resumeSfxOnExit);
	}

	public override void OnResume()
	{
		base.OnResume();
		OpenMenu();
	}

	private void OpenMenu()
	{
		base.gameManager.SetGamePause(m_pauseOnShow);
		SoundController.instance.fxAudioPause = m_pauseSfxOnShow;
		m_playerCarControlEnabled = false;
		if ((bool)PlayerCarControl.instance && PlayerCarControl.instance.enabled)
		{
			m_playerCarControlEnabled = true;
			PlayerCarControl.instance.SetBlockControl(value: true);
		}
		else
		{
			InputManager.instance.StopAllInputFX();
		}
		if ((bool)Camera.main && m_disableMainCamera)
		{
			m_storedMainCamera = Camera.main;
			m_storedMainCameraEnabled = m_storedMainCamera.enabled;
			m_storedMainCamera.enabled = false;
		}
	}

	private void CloseMenu(bool resumeSFX = true)
	{
		base.gameManager.SetGamePause(pause: false);
		SoundController.instance.fxAudioPause = !resumeSFX;
		if ((bool)PlayerCarControl.instance && m_playerCarControlEnabled)
		{
			PlayerCarControl.instance.SetBlockControl(value: false);
		}
		else
		{
			InputManager.instance.EnableAllInputFX();
		}
		if ((bool)m_storedMainCamera && m_disableMainCamera)
		{
			m_storedMainCamera.enabled = m_storedMainCameraEnabled;
			m_storedMainCamera = null;
		}
	}
}
