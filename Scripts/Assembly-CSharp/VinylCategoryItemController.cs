using Binding;
using GameOverlay;
using UnityEngine;

public class VinylCategoryItemController : BaseItemController<VinylCategoryItem>
{
	[SerializeField]
	private RectTransform m_dlcLabel;

	private SpriteProperty m_icon = new SpriteProperty();

	private StringProperty m_headerText = new StringProperty();

	private StringProperty m_dlcName = new StringProperty();

	private BoolProperty m_isLockedDlc = new BoolProperty();

	public override void SetItem(VinylCategoryItem newItem)
	{
		base.SetItem(newItem);
		if (newItem == null)
		{
			ClearProperties();
			return;
		}
		newItem.LoadTexture(SetupIcon);
		UpdateData();
		void SetupIcon(Sprite icon)
		{
			if (newItem == base.item)
			{
				m_icon.value = icon;
			}
		}
	}

	public void UpdateData()
	{
		DLCCode dlcCode = base.item.dlcCode;
		if (dlcCode != DLCCode.Unknown)
		{
			IGameOverlay instance = Overlay.instance;
			m_isLockedDlc.value = !instance.IsDLCInstalled(dlcCode);
			m_dlcName.value = instance.GetDLCName(dlcCode);
		}
		else
		{
			m_isLockedDlc.value = false;
		}
		m_isLockedDlc.ForceValueChanged();
		m_headerText.value = (base.item.showVinylsCount ? base.item.GetVinylsElements().Count.ToString() : string.Empty);
	}

	private void ClearProperties()
	{
		m_icon.value = null;
		m_headerText.value = string.Empty;
		m_dlcName.value = string.Empty;
		m_isLockedDlc.value = false;
	}

	private void Update()
	{
		if (m_isLockedDlc.value && base.item != null && !(m_dlcLabel == null))
		{
			Vector2 a = new Vector2(104.5f, -10.7f);
			Vector2 b = new Vector2(-299.6f, -10.7f);
			base.item.currentLoopTime += Time.deltaTime;
			if (base.item.currentLoopTime > 11.9f)
			{
				base.item.currentLoopTime = 0f;
			}
			m_dlcLabel.anchoredPosition = Vector2.Lerp(a, b, base.item.currentLoopTime / 11.9f);
		}
	}
}
