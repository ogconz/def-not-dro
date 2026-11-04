using Binding;
using Customization;
using DB;
using DB.Meta;
using DI;
using GameOverlay;

public class SlotListElementData : BaseListElementData
{
	private TextureProperty m_preview = new TextureProperty();

	private StringProperty m_setupIcon = new StringProperty();

	private StringProperty m_setupCaption = new StringProperty();

	private StringProperty m_engineModelText = new StringProperty();

	private StringProperty m_chassisModelText = new StringProperty();

	private StringProperty m_questText = new StringProperty();

	private BoolProperty m_isSlotUnlockedAndFree = new BoolProperty();

	private BoolProperty m_isSlotBlocked = new BoolProperty();

	private BoolProperty m_isSlotUsed = new BoolProperty();

	private GamePrefs m_prefs;

	private BaseModel m_baseModel;

	public int slotId { get; private set; }

	public bool isUsed => m_isSlotUsed.value;

	public bool isBlocked => m_isSlotBlocked.value;

	public SlotListElementData(int carId, int slotId)
	{
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
		m_baseModel = DependencyInjector.Resolve<BaseModel>();
		Init(carId, slotId);
	}

	public void Init(int carId, int slotId)
	{
		this.slotId = slotId;
		m_questText.value = LocalizationKeys.car_slot_locked.Localized();
		m_isSlotUsed.value = m_prefs.slotLock.IsSlotUsed(carId, slotId);
		m_isSlotBlocked.value = !m_prefs.slotLock.IsSlotUnlocked(carId, slotId);
		m_isSlotUnlockedAndFree.value = !m_isSlotBlocked.value && !m_isSlotUsed.value;
		if (m_isSlotUsed.value)
		{
			PrefsCarSlotDataModel slot = m_prefs.carSettings.GetSlotById(carId, slotId);
			m_setupCaption.value = slot.caption;
			m_preview.value = IconStorageSystem.ReadLiveryPreview(carId, slotId);
			PlayerCarProfile byId = m_baseModel.GetById<PlayerCarProfile>(slot.profileId);
			m_setupIcon.value = ((byId != null) ? ("ico_" + byId.name.ToLower()) : "ico_stock");
			m_engineModelText.value = ((slot.swapData.currentId > 0) ? SwapsAsset.LoadAndInitDB().Find(slot.swapData.currentId).name : "STOCK");
			CarPart carPart = CarPartDatabase.GetParts(new CarPartDatabase.SlotScheme(carId, slot.visual.parts), "chassis", string.Empty, forceAdd: true).Find((CarPart part) => slot.visual.parts.Contains(part.id));
			DefineDlcChassis(carPart);
			m_chassisModelText.value = (string.IsNullOrEmpty(carPart?.vendor) ? "STOCK" : carPart?.vendor.ToUpper());
		}
	}

	private void DefineDlcChassis(CarPart part)
	{
		if (part != null && part.DLCCode != DLCCode.Unknown && part.DLCCode != DLCCode.RuntimeDLC && string.IsNullOrEmpty(part.vendor))
		{
			part.vendor = "dlc";
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		m_preview.value = null;
	}
}
