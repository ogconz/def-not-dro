using System;
using Binding;
using Binding.Base;
using UnityEngine;
using UnityEngine.UI;

public class BaseSettingsItemController : BaseItemController<BaseListSettingsItem>
{
	public Property<Sprite> icon = new SpriteProperty();

	public Property<int> mode = new IntProperty(0);

	public Property<string> title = new StringProperty();

	public Property<string> valueTitle = new StringProperty();

	public Property<bool> canNext = new BoolProperty(false);

	public Property<bool> canPrev = new BoolProperty(false);

	private BoolProperty m_settingsEnabled = new BoolProperty(startValue: true);

	private ContextOwner m_contextOwner;

	[SerializeField]
	private Slider m_slider;

	public Button prevBtn;

	public Button nextBtn;

	public new BaseListSettingsItem item
	{
		get
		{
			return m_item;
		}
		set
		{
			m_item = value;
			UpdateValue();
		}
	}

	protected override void Awake()
	{
		BaseGameState baseGameState = States.current as BaseGameState;
		if (baseGameState != null)
		{
			m_contextOwner = baseGameState.contextOwner;
		}
		base.Awake();
	}

	protected override void OnEnable()
	{
		if (m_contextOwner != null)
		{
			ContextOwner contextOwner = m_contextOwner;
			contextOwner.onContextActiveChanged = (Action<BaseContext>)Delegate.Combine(contextOwner.onContextActiveChanged, new Action<BaseContext>(HandleSlidersInteractable));
		}
		if (m_slider != null)
		{
			m_slider.interactable = true;
		}
	}

	protected override void Start()
	{
		base.Start();
		if (prevBtn != null)
		{
			prevBtn.onClick.AddListener(OnClickPrev);
		}
		if (nextBtn != null)
		{
			nextBtn.onClick.AddListener(OnClickNext);
		}
	}

	protected override void OnDisable()
	{
		if (m_contextOwner != null)
		{
			ContextOwner contextOwner = m_contextOwner;
			contextOwner.onContextActiveChanged = (Action<BaseContext>)Delegate.Remove(contextOwner.onContextActiveChanged, new Action<BaseContext>(HandleSlidersInteractable));
		}
	}

	private void HandleSlidersInteractable(BaseContext ctx)
	{
		if (!(m_slider == null) && ctx is UIBaseDialogContext uIBaseDialogContext)
		{
			m_slider.interactable = !uIBaseDialogContext.isActive;
		}
	}

	public override void SetItem(BaseListSettingsItem item)
	{
		base.SetItem(item);
		if (!(item is HeaderListSettingsItem))
		{
			if (!(item is SwitchListSettingsItem))
			{
				if (!(item is TrackListSettingsItem))
				{
					if (!(item is ButtonListSettingsItem))
					{
						if (item == null)
						{
							mode.value = -1;
							m_settingsEnabled.value = false;
							return;
						}
						mode.value = 0;
						m_settingsEnabled.value = true;
					}
					else
					{
						mode.value = -1;
						m_settingsEnabled.value = true;
					}
				}
				else
				{
					mode.value = 1;
					m_settingsEnabled.value = true;
				}
			}
			else
			{
				mode.value = 0;
				m_settingsEnabled.value = true;
			}
		}
		else
		{
			mode.value = 2;
			m_settingsEnabled.value = false;
		}
		UpdateValue();
	}

	public void UpdateValue()
	{
		if (m_item != null)
		{
			valueTitle.value = m_item.GetCurrentValueTitle();
			title.value = m_item.title;
			icon.value = m_item.sprite;
			canNext.value = m_item.canNext;
			canPrev.value = m_item.canPrev;
			if (m_slider != null)
			{
				m_slider.value = m_item.GetTrackValue();
			}
		}
	}

	public void OnClickNext()
	{
		m_item?.NextValue();
		UpdateValue();
		OnClick();
	}

	public void OnClickPrev()
	{
		m_item?.PrevValue();
		UpdateValue();
		OnClick();
	}

	public void OnChangedValue()
	{
		if (m_item != null && m_slider != null)
		{
			m_item.SetTrackValue(m_slider.value);
		}
		UpdateValue();
	}
}
