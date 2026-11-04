using Rewired;

public class ButtonListItem : BaseListItem
{
	private const float RECT_WIDTH = 280f;

	private const float RECT_HEIGHT = 52f;

	public string title;

	public bool isPressed;

	public bool needHide;

	public Controller device;

	public Controller.Button button;

	private UIInputBindsSettingsContext m_context;

	public ButtonListItem(UIInputBindsSettingsContext context, string number, string title, Controller.Button button, Controller device, bool needHide = false)
	{
		m_context = context;
		this.button = button;
		this.device = device;
		this.title = title;
		this.needHide = needHide;
		width = 280f;
		height = 52f;
	}

	public void OnSelectButton()
	{
		if (m_context != null)
		{
			m_context.OnSelectButtonItem(this, useHotkey: false);
		}
	}
}
