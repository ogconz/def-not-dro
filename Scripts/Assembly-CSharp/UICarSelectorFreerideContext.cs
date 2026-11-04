using System;
using Customization;
using Cysharp.Threading.Tasks;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using SyncMultiplayer;
using UnityEngine;

public class UICarSelectorFreerideContext : UICarSelectorContext
{
	private const float SWAP_CAR_COOLDOWN = 10f;

	private const float SWAP_CAR_COOLDOWN_PASS_TRIGGER = 0.3f;

	private const float TEXT_UPDATE_COOLDOWN = 0.5f;

	private static float m_timeOfLastSwapCar;

	private static float m_timeOfLastTextUpdate;

	public System.Action OnSelectCar;

	[Dependency]
	private GamePrefs m_prefs;

	private BaseRaceModeState m_race;

	protected override void OnActivate()
	{
		m_race = States.mainCurrent as BaseRaceModeState;
		SetupAvailableCarsFilter();
		goBackAction = Hide;
		if (m_race is SyncNetFreerideRaceModeState)
		{
			onCarSelected = SelectItemWithRoomValidate;
		}
		else
		{
			onCarSelected = DefaultSelectItem;
		}
		base.OnActivate();
	}

	private void SetupAvailableCarsFilter()
	{
		SetupFilter(delegate(PlayerCar car)
		{
			bool flag;
			if (car.dlcCode != DLCCode.Unknown)
			{
				flag = Overlay.instance.IsDLCInstalled(car.dlcCode);
			}
			else
			{
				int num = ((!car.questReward) ? m_prefs.globals.GetLockLevelForFeature(car.AsGameFeature(), car.id) : 0);
				flag = CarPartDatabase.IsCarUnlockedByDLC(car.id) && m_prefs.playerProfile.level >= num;
			}
			flag = flag || m_prefs.questsInventory.rewardAvailableCarItems.Contains(car.id);
			int num2 = (m_prefs.locks.GetCarLockState(car.id) ? car.cost : 0);
			return flag || (car.dlcCode == DLCCode.Unknown && num2 == 0);
		});
	}

	private async void DefaultSelectItem(UICarSelectorCar carItem)
	{
		int id = carItem.meta.id;
		if (CooldownTimePassed() && CarLoaded())
		{
			if (m_prefs.globals.lastCarId != id)
			{
				await PressSwapCarAsync(id, null);
			}
			OnSelectCar?.SafeCall();
		}
	}

	private void SelectItemWithRoomValidate(UICarSelectorCar carItem)
	{
		int carId = carItem.meta.id;
		if (m_prefs.globals.lastCarId == carId)
		{
			OnSelectCar?.SafeCall();
		}
		else
		{
			if (!CarLoaded() || !CooldownTimePassed())
			{
				return;
			}
			NetworkGame game = NetworkController.InstanceGame;
			int adhLimit = game.lastRoomOptions.RoomAdhLimit;
			int tcMode = game.lastRoomOptions.RoomTractionControlMode;
			int wdLimit = game.lastRoomOptions.RoomWheelDriveType;
			int roomPowerDensityLimit = game.lastRoomOptions.RoomPowerDensityLimit;
			NetworkController.CheckForCriticalParamsBySlotId(adhLimit, roomPowerDensityLimit, wdLimit, tcMode, carId, m_prefs.carSettings.GetActiveSlotId(carId), delegate(bool criticalChangeConfirmed)
			{
				if (criticalChangeConfirmed)
				{
					NetworkController.CheckWheelDriveBySlotId(wdLimit, carId, m_prefs.carSettings.GetActiveSlotId(carId), delegate(bool wdChangeConfirmed)
					{
						if (wdChangeConfirmed)
						{
							NetworkController.CheckTractionControlBySlotId(tcMode, carId, m_prefs.carSettings.GetActiveSlotId(carId), delegate(bool tcChangeConfirmed)
							{
								if (tcChangeConfirmed)
								{
									NetworkController.CheckAdhBySlotId(adhLimit, carId, m_prefs.carSettings.GetActiveSlotId(carId), async delegate(bool adhChangeConfirmed)
									{
										if (adhChangeConfirmed)
										{
											RecordLastSwapTime();
											m_prefs.globals.lastCarId = carId;
											game.FillLocalPlayerData();
											await PressSwapCarAsync(carId, delegate
											{
												game.SetTireProtector(game.lastRoomOptions.TrackID, adhLimit);
											});
											OnSelectCar?.SafeCall();
										}
									});
								}
							});
						}
					});
				}
			});
		}
	}

	private async UniTask PressSwapCarAsync(int carId, System.Action callback)
	{
		UILockInputContext.SetLock(base.owner);
		UILockInputContext.SetStatusText(LocalizationKeys.loading_car.Localized());
		await m_race.SwapCar(carId);
		callback.SafeCall();
		BaseState mainCurrent = States.mainCurrent;
		if (mainCurrent is BaseRaceState state)
		{
			while (state.carSwapRunning)
			{
				await UniTask.Yield();
			}
		}
		GC.Collect();
		GC.WaitForPendingFinalizers();
		UILockInputContext.DisableLock();
	}

	public void Update()
	{
		m_timeOfLastTextUpdate += Time.unscaledDeltaTime;
		if (m_timeOfLastTextUpdate > 0.5f)
		{
			m_timeOfLastTextUpdate = 0f;
			UpdateBottomPanel();
		}
	}

	public void RecordLastSwapTime()
	{
		m_timeOfLastSwapCar = Time.unscaledTime;
	}

	private bool CarLoaded()
	{
		if (m_race.userCar != null)
		{
			return m_race.userCar.IsVisualLoaded;
		}
		return false;
	}

	public bool CooldownTimePassed()
	{
		return GetSwapInterval() < 0.3f;
	}

	public string ConstructWaitText()
	{
		return $"{LocalizationKeys.please_wait.Localized()} ({Mathf.CeilToInt(GetSwapInterval())})";
	}

	public override void UpdateBottomPanel()
	{
		if (CarLoaded() && (CooldownTimePassed() || EqualSelectedCarId(m_race.userCar.carId)))
		{
			base.UpdateBottomPanel();
			return;
		}
		ClearBottomPanel();
		AddGoBackAction();
		if (m_race is SyncNetFreerideRaceModeState)
		{
			AddActionInBottomPanel(ConstructWaitText(), ActionType.NavSelectBtnA);
		}
		AddSlotChangeAction();
		AddFollowCarAction();
		RefreshBottomPanel();
	}

	private float GetSwapInterval()
	{
		int num = 0;
		return m_timeOfLastSwapCar + (10f + (float)num) - Time.unscaledTime;
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	protected override void NavigationUp()
	{
		base.NavigationUp();
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	protected override void NavigationDown()
	{
		base.NavigationDown();
	}

	[GameHotkey(ActionType.NavRight, false, true)]
	protected override void NavigationRight()
	{
		base.NavigationRight();
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	protected override void NavigationLeft()
	{
		base.NavigationLeft();
	}
}
