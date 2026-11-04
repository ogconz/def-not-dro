using FMOD.Studio;
using GameInput;
using UnityEngine;

public class CarFlares : FMODBaseScript
{
	public GameObject flares;

	public string eventSound = "GameEvents/PoliceSiren";

	private bool m_flaresEnabled;

	private bool m_enabledSwitch;

	private FMODEventNode m_node;

	public void Start()
	{
		RaceCar componentInParent = GetComponentInParent<RaceCar>();
		if ((bool)componentInParent && componentInParent.isNetworkCar)
		{
			base.enabled = false;
		}
		CreateFlareNode();
	}

	private void CreateFlareNode()
	{
		if (!CreateNode(ref m_node, "event:/" + eventSound))
		{
			GameLog.Print(AlertLevel.Error, "Sound", "The sound block was not created for " + base.gameObject.name + ":" + eventSound);
		}
	}

	private void Update()
	{
		UpdateHandler(Time.deltaTime);
		if (flares.activeSelf && !m_node.IsPlaying())
		{
			m_node.Start();
			if (!m_node.IsPlaying())
			{
				m_node = null;
				CreateFlareNode();
			}
		}
		else if (!flares.activeSelf && m_node.IsPlaying())
		{
			m_node.Stop(STOP_MODE.ALLOWFADEOUT);
		}
	}

	public void FixedUpdate()
	{
		if (!InputManager.IsInputLocked() && InputManager.instance.IsActionPressed(ActionType.Flares) && !base.raceCar.isGhost && !base.raceCar.isNetworkCar)
		{
			m_enabledSwitch = !m_flaresEnabled;
			bool flag = base.raceCar.engineController.isIgnitionOn || States.mainCurrent is GarageGUIState;
			SetEnabled(m_enabledSwitch && flag);
		}
	}

	public void SetEnabled(bool val)
	{
		if (base.raceCar.isLocalPlayer && !m_enabledSwitch)
		{
			val = false;
		}
		if (m_flaresEnabled != val)
		{
			m_flaresEnabled = val;
			base.enabled = true;
			flares.SetActive(m_flaresEnabled);
		}
	}
}
