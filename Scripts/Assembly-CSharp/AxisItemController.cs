using Binding;
using Binding.Base;
using DI;
using PrefsModels;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

public class AxisItemController : BaseItemController<AxisListItem>
{
	private const string MOUSE_HORIZONTAL_AXIS_TITLE = "mouse horizontal";

	private const string MOUSE_VERTICAL_AXIS_TITLE = "mouse vertical";

	private const float DOUBLE_CLICK_TIME_DETECT = 0.25f;

	public Property<string> number = new StringProperty();

	public Property<string> title = new StringProperty();

	public Property<int> currentAxis = new IntProperty();

	public Property<float> currentNormalizedAxis = new FloatProperty();

	public Slider slider;

	private float m_lastTimeButtonPressed;

	private AxisListItem m_prevItem;

	private GamePrefs m_gamePrefs;

	protected void InitSlider()
	{
		if (m_gamePrefs == null)
		{
			m_gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		}
		if (!(slider != null) || base.item == null)
		{
			return;
		}
		if (base.item.device.type == ControllerType.Mouse)
		{
			if (m_gamePrefs != null && m_gamePrefs.globals.moveMouseAxisType == GlobalModel.MouseMoveType.Coord)
			{
				if (base.item.title.ToLower().Equals("mouse horizontal"))
				{
					slider.minValue = (float)(-Screen.width) / 2f;
					slider.maxValue = (float)Screen.width / 2f;
				}
				else if (base.item.title.ToLower().Equals("mouse vertical"))
				{
					slider.minValue = (float)(-Screen.height) / 2f;
					slider.maxValue = (float)Screen.height / 2f;
				}
			}
			else
			{
				slider.minValue = -10f;
				slider.maxValue = 10f;
			}
		}
		else
		{
			slider.minValue = -1f;
			slider.maxValue = 1f;
		}
	}

	public override void OnClick()
	{
		bool isDoubleClick = Time.fixedUnscaledTime - m_lastTimeButtonPressed < 0.25f;
		m_lastTimeButtonPressed = Time.fixedUnscaledTime;
		base.OnClick();
		if (m_item != null)
		{
			m_item.SelectAxis(isDoubleClick);
		}
	}

	public override void SetItem(AxisListItem listItem)
	{
		base.SetItem(listItem);
		if (m_prevItem != base.item)
		{
			m_prevItem = base.item;
			InitSlider();
		}
		if (listItem == null)
		{
			return;
		}
		number.value = listItem.number;
		title.value = listItem.title;
		if (!(m_gamePrefs == null))
		{
			if (base.item.device.type != ControllerType.Mouse || m_gamePrefs.globals.moveMouseAxisType == GlobalModel.MouseMoveType.Coord)
			{
				currentAxis.value = listItem.currentAxis;
				currentNormalizedAxis.value = listItem.currentNormalizedAxis;
				slider.value = listItem.currentNormalizedAxis;
			}
			else if (m_gamePrefs.globals.moveMouseAxisType == GlobalModel.MouseMoveType.Delta)
			{
				currentAxis.value = Mathf.CeilToInt(listItem.axis.value);
				currentNormalizedAxis.value = listItem.axis.value;
				slider.value = listItem.axis.value;
			}
		}
	}
}
