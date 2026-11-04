using CarProfile;
using SharingSetups;

public abstract class BaseDynoParamsItemViewModel : BaseListItem
{
	protected UIDynostandContext m_dynoContext;

	protected UISetupsSharingContext m_setupsSharingContext;

	public string title { get; protected set; }

	public string titleDescription { get; protected set; }

	public string description { get; protected set; }

	public bool isShortItem { get; protected set; }

	public CarProfileController controller { get; protected set; }

	public virtual float value { get; set; }

	public virtual string outputValue { get; protected set; }

	public virtual void OnUpMouse()
	{
	}

	public virtual int CompareTo(BaseDynoParamsItemViewModel other)
	{
		return 999;
	}

	public abstract void RevertParameter();

	public abstract void RevertToDefault();
}
