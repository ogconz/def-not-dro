using Binding;
using Binding.Base;
using UnityEngine;
using UnityEngine.UI;

public class SettingsDeviceItemView : BaseItemController<SettingsDeviceItemViewModel>
{
	[SerializeField]
	private Button m_FFBSwitcherButton;

	public Property<string> deviceName = new StringProperty();

	public Property<string> deviceType = new StringProperty();

	public Property<bool> deviceActived = new BoolProperty();

	public Property<bool> fxActive = new BoolProperty();

	public Property<bool> fxDisabled = new BoolProperty();

	public Property<bool> showDeviceType = new BoolProperty();

	private Property<bool> m_availableNext = new BoolProperty(false);

	private Property<bool> m_availablePrev = new BoolProperty(false);

	protected override void Awake()
	{
		base.Awake();
		m_FFBSwitcherButton.onClick.AddListener(OnFFBSwitch);
	}

	public override void SetItem(SettingsDeviceItemViewModel newItem)
	{
		base.SetItem(newItem);
		if (newItem != null)
		{
			deviceName.value = newItem.deviceName;
			deviceType.value = newItem.deviceType;
			deviceActived.value = newItem.deviceActived;
			fxActive.value = newItem.fxActive;
			fxDisabled.value = newItem.fxDisabled;
			m_availableNext.value = newItem.availableNextType;
			m_availablePrev.value = newItem.availablePrevType;
			showDeviceType.value = true;
		}
	}

	public void OnMissClick()
	{
		if (selected.value)
		{
			m_item.OnMissClick();
		}
	}

	public void OnClickNext()
	{
		m_item?.NextDeviceTypeValue();
	}

	public void OnClickPrev()
	{
		m_item?.PrevDeviceTypeValue();
	}

	private void OnFFBSwitch()
	{
		m_item?.OnFFBSwitch();
	}

	protected override void OnDestroy()
	{
		m_FFBSwitcherButton.onClick.RemoveListener(OnFFBSwitch);
		base.OnDestroy();
	}
}
