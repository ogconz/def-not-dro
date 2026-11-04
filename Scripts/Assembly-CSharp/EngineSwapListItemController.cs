using Binding;
using UnityEngine;

public class EngineSwapListItemController : BaseItemController<EngineSwapListItem>
{
	private readonly BoolProperty m_installed = new BoolProperty(false);

	private readonly BoolProperty m_locked = new BoolProperty(false);

	private readonly BoolProperty m_noParts = new BoolProperty(false);

	private readonly BoolProperty m_hasItem = new BoolProperty(false);

	private readonly StringProperty m_partsText = new StringProperty();

	private readonly SpriteProperty m_logoSprite = new SpriteProperty();

	private readonly SpriteProperty m_engineSprite = new SpriteProperty();

	private readonly StringProperty m_stockSetupSprite = new StringProperty();

	[SerializeField]
	private SoundSample m_hoverClip = SoundSample.HoverClip;

	public string requiredPartCountText { get; private set; }

	public EngineInfo engineInfo { get; private set; }

	public override void SetItem(EngineSwapListItem newItem)
	{
		base.SetItem(newItem);
		if (newItem != null)
		{
			UpdateItem();
		}
	}

	private void UpdateItem()
	{
		engineInfo = m_item.info;
		m_installed.value = m_item.IsInstalled();
		if (m_item.isStock)
		{
			m_engineSprite.value = null;
			m_noParts.value = false;
			m_locked.value = false;
			m_hasItem.value = true;
			m_logoSprite.value = null;
			m_stockSetupSprite.value = PlayerCarProfileMetaExtensions.GetProfileIconNameByCar(m_item.carId);
		}
		else
		{
			m_stockSetupSprite.value = null;
			m_engineSprite.value = engineInfo.sprite;
			m_logoSprite.value = engineInfo.logo;
			RefreshPartsCount();
		}
	}

	public void RefreshPartsCount()
	{
		if (engineInfo.id > 0)
		{
			if (GameManager.SwapsDLCInstalled)
			{
				m_noParts.value = false;
				m_locked.value = false;
				m_hasItem.value = true;
			}
			else
			{
				UpdateTextLabelRequiredParts();
				m_hasItem.value = m_item.HasInInventory();
				m_noParts.value = !m_item.IsCollectedPartsCountEnough();
				m_locked.value = !m_hasItem.value || (!m_hasItem.value && m_noParts.value);
			}
		}
	}

	private void UpdateTextLabelRequiredParts()
	{
		requiredPartCountText = (GameManager.SwapsDLCInstalled ? string.Empty : $"{m_item.GetCollectedPartsCount()}/{engineInfo.partsToInstall}");
		m_partsText.value = requiredPartCountText;
	}

	public void PlayHover()
	{
		SoundController.Play2DSound(m_hoverClip);
	}
}
