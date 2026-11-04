using System;
using Binding;
using Binding.Base;
using Customization;
using DB.Meta;
using DI;
using GameOverlay;
using PrefsModels;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Utils.ColorExtensions;

public class UICarSelectorCar : BaseBindingBehaviourTarget
{
	private const int CHAR_LIMIT = 20;

	private readonly BoolProperty m_selected = new BoolProperty();

	private readonly BoolProperty m_owned = new BoolProperty();

	private readonly BoolProperty m_favorite = new BoolProperty();

	private readonly BoolProperty m_locked = new BoolProperty();

	private readonly BoolProperty m_dlcLocked = new BoolProperty();

	private readonly BoolProperty m_eventLocked = new BoolProperty();

	private readonly BoolProperty m_questLocked = new BoolProperty();

	private readonly BoolProperty m_moveTextNeeded = new BoolProperty();

	private readonly BoolProperty m_loadIcon = new BoolProperty();

	private readonly StringProperty m_dlcName = new StringProperty();

	private readonly StringProperty m_eventName = new StringProperty();

	private readonly StringProperty m_activeSlot = new StringProperty();

	private readonly IntProperty m_slotsCount = new IntProperty();

	private readonly IntProperty m_requiredLevel = new IntProperty();

	private readonly IntProperty m_showCarUpdateIcon = new IntProperty();

	private readonly StringProperty m_carName = new StringProperty();

	private readonly StringProperty m_cost = new StringProperty();

	private readonly RenderTextureProperty m_sprite = new RenderTextureProperty();

	private readonly MaterialProperty m_spriteMaterial = new MaterialProperty();

	public RectTransform rectTransform;

	[SerializeField]
	private float m_textMoveSpeed;

	[SerializeField]
	private RectTransform[] m_movingText;

	[SerializeField]
	private CatalogCarMarkers m_carsMarkers;

	private PlayerCar m_meta;

	private Vector3 m_bottomLeftCorner;

	private UICarSelectorContext m_context;

	private AsyncOperationHandle<Texture2D> m_handle;

	private GamePrefs m_prefs;

	private RenderTexture m_defaultSprite;

	private IconCarData m_carData;

	private bool m_lastLoad;

	private bool m_select;

	private bool m_isQuestRewardCar;

	public int carId => m_meta?.id ?? (-1);

	public PlayerCar meta => m_meta;

	public bool isActiveGo => base.gameObject.activeSelf;

	public bool isSelected => m_selected.value;

	public bool isFavorite => m_favorite.value;

	public bool isOwned => m_owned.value;

	public string costStr => m_cost.value;

	public void SetMeta(PlayerCar playerCar)
	{
		m_meta = playerCar;
		m_handle = ResourcesWrapper.LoadAsync("ui/catalog/" + meta.icon, delegate(Texture2D sprite)
		{
			if (sprite != null)
			{
				if (m_sprite.value == null)
				{
					m_sprite.value = new RenderTexture(sprite.width, sprite.height, 0, RenderTextureFormat.ARGB32);
				}
				Graphics.Blit(sprite, m_sprite.value);
				m_defaultSprite = m_sprite.value;
			}
		});
	}

	public void Initialize(UICarSelectorContext ctx)
	{
		m_context = ctx;
		if (m_prefs == null)
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
		}
		base.transform.localScale = Vector3.one;
		base.transform.SetLocalZ(0f);
		base.transform.localEulerAngles = Vector3.zero;
		m_showCarUpdateIcon.value = m_carsMarkers.GetUpdateCarType(carId);
		m_carName.value = meta.name;
		m_moveTextNeeded.value = meta.name.Length > 20;
		m_isQuestRewardCar = m_meta.questReward;
		m_requiredLevel.value = ((!m_isQuestRewardCar) ? m_context.prefs.globals.GetLockLevelForFeature(meta.AsGameFeature(), meta.id) : 0);
		m_slotsCount.value = m_prefs.carSettings.GetUsedSlots(m_meta.id).Count;
		string colored = StringColorFormatter.GetColored($"/{PrefsCarSlotsModel.GetMaxSlotCount()}", "B3B3B3");
		m_activeSlot.value = $"{m_slotsCount.value}{colored}";
		DLCCode dlcCode = meta.dlcCode;
		GameEventCode eventCode = meta.eventCode;
		if (eventCode != GameEventCode.Unknown)
		{
			m_locked.value = !Singleton<GameplayEvents>.instance.IsContentAvailable(eventCode);
			m_eventLocked.value = m_locked.value;
			m_dlcName.value = Singleton<GameplayEvents>.instance.GetEventName(eventCode);
		}
		else if (dlcCode != DLCCode.Unknown)
		{
			m_locked.value = !Overlay.instance.IsDLCInstalled(dlcCode);
			m_dlcLocked.value = m_locked.value;
			m_dlcName.value = Overlay.instance.GetDLCName(dlcCode);
		}
		else
		{
			m_dlcName.value = "";
			m_locked.value = !CarPartDatabase.IsCarUnlockedByDLC(meta.id) && m_context.prefs.playerProfile.level < m_requiredLevel.value;
		}
		m_carData = new IconCarData
		{
			carId = m_meta.id,
			iconName = m_meta.icon
		};
		IconGeneratorContext iconGeneratorContext = m_context.owner.Get<IconGeneratorContext>();
		if (iconGeneratorContext != null)
		{
			iconGeneratorContext.snapshotCallbackStart = (Action)Delegate.Combine(iconGeneratorContext.snapshotCallbackStart, new Action(UpdateIcon));
			iconGeneratorContext.snapshotCallback = (Action<RenderTexture>)Delegate.Combine(iconGeneratorContext.snapshotCallback, (Action<RenderTexture>)delegate
			{
				UpdateIcon();
			});
		}
		UpdateIcon();
		if (m_locked.value)
		{
			bool flag = HasQuestRewardCar(carId);
			if (m_isQuestRewardCar)
			{
				SetOwned(flag);
				if (flag)
				{
					SetFavorite(m_context.prefs.favoriteCars.IsFavorite(meta.id));
				}
				return;
			}
			SetOwned(flag);
			if (flag)
			{
				SetFavorite(m_context.prefs.favoriteCars.IsFavorite(meta.id));
			}
			m_cost.value = string.Empty;
		}
		else
		{
			int num = (m_context.prefs.locks.GetCarLockState(meta.id) ? meta.cost : 0);
			bool flag2 = num == 0;
			m_cost.value = (flag2 ? string.Empty : UIUtils.DigitsWithSeparate(num));
			SetOwned(flag2);
			SetFavorite(m_context.prefs.favoriteCars.IsFavorite(meta.id));
		}
	}

	private void OnEnable()
	{
		Vector3[] array = new Vector3[4];
		rectTransform.GetLocalCorners(array);
		m_bottomLeftCorner = array[0];
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		ResourcesWrapper.UnloadAsset(m_handle);
	}

	private void Update()
	{
		UpdateMovingText();
		m_loadIcon.value = IconGeneratorContext.IsContainQueue(m_carData) && States.mainCurrent is GarageGUIState;
		if (m_loadIcon.value != m_lastLoad)
		{
			UpdateIcon();
			m_lastLoad = m_loadIcon.value;
		}
		m_select = false;
	}

	private void UpdateIcon()
	{
		RenderTexture iconFromStorage = IconGeneratorContext.GetIconFromStorage(m_carData);
		m_sprite.value = ((iconFromStorage != null) ? iconFromStorage : m_defaultSprite);
	}

	private void UpdateMovingText()
	{
		if (!m_moveTextNeeded.value)
		{
			return;
		}
		for (int i = 0; i < m_movingText.Length; i++)
		{
			RectTransform rectTransform = m_movingText[i];
			rectTransform.anchoredPosition -= new Vector2(Time.unscaledDeltaTime * m_textMoveSpeed, 0f);
			if (rectTransform.anchoredPosition.x + rectTransform.rect.width < m_bottomLeftCorner.x)
			{
				int num = m_movingText.Length - 1;
				if (num == i)
				{
					num = 0;
				}
				RectTransform rectTransform2 = m_movingText[num];
				rectTransform.anchoredPosition = new Vector2(rectTransform2.anchoredPosition.x + rectTransform.rect.width, rectTransform.anchoredPosition.y);
			}
		}
	}

	public void SetOwned(bool owned)
	{
		m_owned.value = owned;
		if (m_isQuestRewardCar)
		{
			m_locked.value = !owned;
			m_questLocked.value = !owned;
			m_dlcName.value = (owned ? string.Empty : LocalizationKeys.available_only_in_quests.Localized());
		}
	}

	public void SetFavorite(bool favorite)
	{
		m_favorite.value = favorite;
	}

	public void SetSelected(bool selected)
	{
		m_selected.value = selected;
	}

	public void GotoCarSlotsContext()
	{
		if (m_context.CanBeInteractable())
		{
			m_context.GotoSlotsContext();
		}
	}

	public void OnClick()
	{
		if (m_context.CanBeInteractable())
		{
			SoundController.Play2DUISound(SoundSample.DefaultBtn);
			m_context.SelectItemInCurrentCategory(carId, forceScrollTo: true);
		}
	}

	public void OnSelect()
	{
		if (m_context.CanBeInteractable())
		{
			m_context.SelectItemInCurrentCategory(carId);
			m_context.SubmitCar(this);
			m_select = true;
		}
	}

	private bool HasQuestRewardCar(int id)
	{
		return m_prefs.questsInventory.rewardAvailableCarItems.Contains(id);
	}
}
