using UnityEngine;

public class FMODGearShifting : FMODBaseScript
{
	private const string defaultManualGearEvent = "event:/GameEvents/Gearbox/Lever/Default";

	private const string defaultAutoGearEvent = "event:/GameEvents/Gearbox/Sequential/Default";

	private const string specialSeqGearEvent = "event:/GameEvents/Gearbox/Sequential/1R";

	private int m_gearPrev;

	private FMODEventNode m_gearManualNode;

	private FMODEventNode m_gearManualNodePad;

	private FMODEventNode m_gearAutoNode;

	private FMODEventNode m_gearAutoNodePad;

	private FMODEventNode m_gearSequentialSpecialNode;

	private FMODEventNode m_gearSequentialSpecialNodePad;

	private bool m_isSeqTransmissionAvailable;

	protected override bool OnEnableHandler()
	{
		m_gearPrev = base.raceCar.carX.gear;
		CreateNode(ref m_gearManualNode, "event:/GameEvents/Gearbox/Lever/Default");
		CreateNode(ref m_gearAutoNode, "event:/GameEvents/Gearbox/Sequential/Default");
		CreateNode(ref m_gearSequentialSpecialNode, "event:/GameEvents/Gearbox/Sequential/1R");
		UpdateSeqTransmissionAvailabilityHandler(base.raceCar);
		base.raceCar.OnSetDescEvent += UpdateSeqTransmissionAvailabilityHandler;
		return base.OnEnableHandler();
	}

	protected override void OnDisableHandler()
	{
		base.raceCar.OnSetDescEvent -= UpdateSeqTransmissionAvailabilityHandler;
		base.OnDisableHandler();
	}

	private void Start()
	{
		if (base.raceCar.isLocalPlayer && SoundController.isPadSoundSupported)
		{
			m_gearManualNodePad = CreateNodeIfExist("event:/GameEvents/Gearbox/Lever/DefaultPad");
			m_gearAutoNodePad = CreateNodeIfExist("event:/GameEvents/Gearbox/Sequential/DefaultPad");
			m_gearSequentialSpecialNodePad = CreateNodeIfExist("event:/GameEvents/Gearbox/Sequential/1RPad");
		}
	}

	private void UpdateSeqTransmissionAvailabilityHandler(BaseCar baseCar)
	{
		RaceCar raceCar = baseCar as RaceCar;
		m_isSeqTransmissionAvailable = raceCar != null && raceCar.isSequentialTransmissionAvailable;
	}

	protected override void UpdateSound(float dt)
	{
		int gear = base.raceCar.carX.gear;
		if (gear != m_gearPrev)
		{
			BaseState mainCurrent = States.mainCurrent;
			if (mainCurrent != null && !(mainCurrent is DynoMenuState) && !(mainCurrent is GarageGUIState))
			{
				bool flag = m_prefs != null && m_prefs.globals.padSound;
				if (m_isSeqTransmissionAvailable)
				{
					if (Mathf.Abs(gear) == 1 && m_gearPrev != 2)
					{
						m_gearSequentialSpecialNode.Start();
						if (flag && m_gearSequentialSpecialNodePad != null)
						{
							m_gearSequentialSpecialNodePad.Start();
						}
					}
					else if (gear != 0)
					{
						m_gearAutoNode.Start();
						if (flag && m_gearAutoNodePad != null)
						{
							m_gearAutoNodePad.Start();
						}
					}
				}
				else
				{
					m_gearManualNode.Start();
					if (flag && m_gearManualNodePad != null)
					{
						m_gearManualNodePad.Start();
					}
				}
			}
			m_gearPrev = gear;
		}
		base.UpdateSound(dt);
	}
}
