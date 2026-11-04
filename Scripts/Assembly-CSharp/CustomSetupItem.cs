using LocalCaching.Model;
using PrefsModels;

public class CustomSetupItem : BaseListItem, ISetupsItem
{
	public enum CustomSetupType
	{
		Racing = 0,
		Drift = 1,
		Ultimate = 2,
		UltimatePro = 3
	}

	private UISetupsContext m_context;

	public string setupId;

	public string setupName;

	public int engineId;

	public bool isPlusElement;

	private CustomProfile m_customProfile;

	public CarProfileProxy profileProxy { get; private set; }

	public bool current { get; set; }

	public string customSetupId => m_customProfile?.id;

	public CustomSetupItem(UISetupsContext context, CustomProfile customProfile, CarProfileProxy profileProxy, bool isPlusElem)
	{
		m_context = context;
		m_customProfile = customProfile;
		setupId = customProfile.id;
		setupName = customProfile.name;
		engineId = customProfile.engineId;
		this.profileProxy = profileProxy;
		isPlusElement = isPlusElem;
	}

	public CustomSetupItem(UISetupsContext context, int swapEngineId, CarProfileProxy proxy)
	{
		m_context = context;
		engineId = swapEngineId;
		isPlusElement = true;
		profileProxy = proxy;
	}

	public CustomSetupItem(UISetupsContext context, CarProfileProxy proxy)
	{
		m_context = context;
		isPlusElement = true;
		profileProxy = proxy;
	}

	public override void OnClick()
	{
		m_context.OnClickCustomUIItem(setupId, isPlusElement);
	}

	public void PlayHover()
	{
	}
}
