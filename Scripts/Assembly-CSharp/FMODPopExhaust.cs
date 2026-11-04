using UnityEngine;

public class FMODPopExhaust : FMODBaseScript
{
	private const string defaultPopEvent = "event:/GameEvents/Back-fire/Default";

	private const string defaultRevEvent = "event:/GameEvents/RevLimit/Default";

	private FMODEventNode m_popInstance;

	private FMODEventNode.Parameter m_powerParam;

	private FMODEventNode m_revInstance;

	protected override bool OnEnableHandler()
	{
		if (!base.raceCar)
		{
			return false;
		}
		RefreshSound();
		CreateCarNamedNode(ref m_revInstance, "event:/GameEvents/Cars/{0}/RevLimit", "event:/GameEvents/RevLimit/Default");
		return base.OnEnableHandler();
	}

	public override void RefreshSound()
	{
		int num;
		string sound;
		if (!base.raceCar.swap.isStockEngine)
		{
			num = ((!string.IsNullOrEmpty(base.raceCar.swap.meta.sound)) ? 1 : 0);
			if (num != 0)
			{
				sound = base.raceCar.swap.meta.sound;
				goto IL_005c;
			}
		}
		else
		{
			num = 0;
		}
		sound = base.raceCar.metaInfo.name;
		goto IL_005c;
		IL_005c:
		string metaName = sound;
		string eventRef = ((num != 0) ? "event:/GameEvents/Engines/{0}/{1}" : "event:/GameEvents/Cars/{0}/{1}");
		m_popInstance?.Release();
		if (CreateCustomCarNamedNode(ref m_popInstance, eventRef, metaName, "Back-fire", "event:/GameEvents/Back-fire/Default"))
		{
			m_powerParam = m_popInstance.RegisterParameter("Power");
		}
		base.RefreshSound();
	}

	public void Play()
	{
		m_popInstance.Start();
	}

	public void PlayRevLimit()
	{
		m_revInstance.Start();
	}

	protected override void UpdateSound(float dt)
	{
		base.UpdateSound(dt);
		float rpm = base.raceCar.carX.rpm;
		if (rpm > 2000f)
		{
			m_powerParam.SetValue(Mathf.Clamp01((rpm - 2000f) / 4000f));
		}
	}
}
