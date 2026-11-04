using Binding;
using UnityEngine;

public class UISetupsCustomItem : BaseItemController<CustomSetupItem>
{
	private IntProperty m_setupType = new IntProperty();

	private IntProperty m_engineType = new IntProperty();

	private StringProperty m_customName = new StringProperty();

	private BoolProperty m_isCurrentSetup = new BoolProperty();

	private BoolProperty m_isPlusElement = new BoolProperty();

	[SerializeField]
	private SoundSample m_hoverClip = SoundSample.HoverClip;

	public int engineId => m_engineType.value;

	public override void SetItem(CustomSetupItem newItem)
	{
		if (newItem != null)
		{
			if (!newItem.isPlusElement)
			{
				m_setupType.value = GetInfo(newItem);
				m_customName.value = newItem.setupName;
				m_isCurrentSetup.value = newItem.current;
				m_engineType.value = newItem.engineId;
			}
			else
			{
				m_setupType.value = -1;
				m_customName.value = string.Empty;
				m_isCurrentSetup.value = false;
				m_engineType.value = -1;
			}
			m_isPlusElement.value = newItem.isPlusElement;
		}
		base.SetItem(newItem);
	}

	private int GetInfo(CustomSetupItem newItem)
	{
		return newItem.profileProxy.name switch
		{
			"Racing" => 0, 
			"Drift" => 1, 
			"Ultimate" => 2, 
			"UltimatePro" => 3, 
			_ => -1, 
		};
	}

	public void PlayHover()
	{
		SoundController.Play2DSound(m_hoverClip);
	}
}
