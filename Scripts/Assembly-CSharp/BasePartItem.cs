using Customization;
using UnityEngine;

public class BasePartItem : BaseListItem
{
	protected UIBodyCustomizationContext m_context;

	public string icon;

	public Sprite asset;

	public string title;

	private bool m_locked;

	public bool isDirty = true;

	public bool isModPart;

	public bool isFavourite;

	public bool selected;

	public bool equipped;

	private int m_price;

	public bool locked
	{
		get
		{
			return m_locked;
		}
		set
		{
			m_locked = value;
			isDirty = true;
		}
	}

	public CarPart part { get; protected set; }

	public int price
	{
		get
		{
			return m_price;
		}
		set
		{
			m_price = value;
			formattedPrice = ((value == 0) ? string.Empty : UIUtils.DigitsWithSeparate(value));
		}
	}

	public string formattedPrice { get; private set; }

	protected BasePartItem(UIBodyCustomizationContext context)
	{
		m_context = context;
	}

	public override void OnClick()
	{
		m_context.OnSelectBodyPart(this);
	}
}
