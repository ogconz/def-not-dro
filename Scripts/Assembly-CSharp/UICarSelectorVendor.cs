using Binding;
using Binding.Base;
using UnityEngine;

public class UICarSelectorVendor : BaseBindingBehaviourTarget
{
	private readonly BoolProperty m_selected = new BoolProperty();

	private readonly SpriteProperty m_sprite = new SpriteProperty();

	public RectTransform rectTransform;

	private UICarSelectorContext m_context;

	private CarVendor m_meta;

	public bool isActiveGo => base.gameObject.activeSelf;

	public bool isSelected => m_selected.value;

	public int vendorId => m_meta?.id ?? (-1);

	public void Initialize(UICarSelectorContext ctx, CarVendor meta)
	{
		m_context = ctx;
		m_meta = meta;
		m_sprite.value = meta.sprite;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_context = null;
		m_meta = null;
		m_sprite.value = null;
	}

	public void SetSelected(bool selected)
	{
		m_selected.value = selected;
	}

	public void OnClick()
	{
		SoundController.Play2DUISound(SoundSample.DefaultBtn);
		m_context.SelectItemInCurrentCategory(vendorId, forceScrollTo: true);
	}

	public void OnSelect()
	{
		m_context.SelectItemInCurrentCategory(vendorId);
		m_context.SubmitVendor(vendorId);
	}
}
