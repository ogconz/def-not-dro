using Binding;
using Binding.Base;
using LocalCaching.Model;
using UnityEngine;

public class UISetupsItem : BaseBindingBehaviourTarget, ISetupsItem
{
	public readonly IntProperty requiredLevel = new IntProperty(-1);

	public readonly BoolProperty lockedByLevel = new BoolProperty(false);

	public readonly BoolProperty isCurrent = new BoolProperty(false);

	public readonly BoolProperty locked = new BoolProperty(false);

	public readonly StringProperty text = new StringProperty();

	private GamePrefs m_prefs;

	[SerializeField]
	private SoundSample m_hoverClip = SoundSample.HoverClip;

	public bool current => isCurrent.value;

	public int priceValue => profileProxy.cost;

	public bool canBeUnlocked
	{
		get
		{
			if (locked.value)
			{
				return !lockedByLevel.value;
			}
			return false;
		}
	}

	public CarProfileProxy profileProxy { get; private set; }

	public string customSetupId => string.Empty;

	public void Init(GamePrefs prefs, CarProfileProxy profile)
	{
		profileProxy = profile;
		m_prefs = prefs;
		requiredLevel.value = m_prefs.globals.GetLockLevelForFeature(profile.AsGameFeature());
		lockedByLevel.value = m_prefs.playerProfile.level < requiredLevel.value;
	}

	public void Refresh(int? currentPofileId)
	{
		locked.value = lockedByLevel.value;
		locked.value |= profileProxy != null && m_prefs.locks.GetProfileLockState(profileProxy.id);
		if (locked.value)
		{
			isCurrent.value = false;
		}
		else
		{
			int lastCarId = m_prefs.globals.lastCarId;
			bool item = m_prefs.carSettings.GetHaveCustomSetupsPerCar(lastCarId).haveInstalledCustom;
			isCurrent.value = profileProxy != null && currentPofileId == profileProxy.id && !item;
		}
		if (lockedByLevel.value)
		{
			text.value = LocalizationKeys.locked_track_info.LocalizedFormat(requiredLevel.value.ToString());
		}
		else if (locked.value)
		{
			text.value = UIUtils.AddWhiteDollarSignAtStart(UIUtils.DigitsWithSeparate(priceValue));
		}
		else
		{
			text.value = string.Empty;
		}
	}

	public void PlayHover()
	{
		SoundController.Play2DSound(m_hoverClip);
	}
}
