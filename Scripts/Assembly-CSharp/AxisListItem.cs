using Rewired;

public class AxisListItem : BaseListItem
{
	private const float RECT_WIDTH = 840f;

	private const float RECT_HEIGHT = 52f;

	private UIInputBindsSettingsContext m_context;

	public string number;

	public string title;

	public int currentAxis;

	public float currentNormalizedAxis;

	public Controller device;

	public Controller.Axis axis;

	public AxisListItem(UIInputBindsSettingsContext context, string number, string title, Controller.Axis axis, Controller device)
	{
		m_context = context;
		this.axis = axis;
		this.device = device;
		this.number = number;
		this.title = title;
		width = 840f;
		height = 52f;
	}

	public void SelectAxis(bool isDoubleClick)
	{
		if (m_context != null)
		{
			m_context.OnSelectAxisItem(this, useHotkey: false, isDoubleClick);
		}
	}
}
