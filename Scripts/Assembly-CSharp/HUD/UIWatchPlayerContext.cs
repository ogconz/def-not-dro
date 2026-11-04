using System;
using System.Collections.Generic;
using System.Text;
using Binding;
using Binding.Base;
using CarX;
using GameInput;
using SyncMultiplayer;
using UnityEngine;

namespace HUD
{
	[GameHotkeyBlocker(true)]
	public class UIWatchPlayerContext : BaseBindingBehaviourTarget
	{
		private string m_massLocalizedString;

		private string m_horsePowerLocalizedString;

		private StringProperty m_title = new StringProperty();

		private StringProperty m_wheelParm = new StringProperty();

		private RaceCar m_target;

		private BaseCar.Desc m_targetCarDesc;

		private Car m_carXCar;

		private RaceStatsPlayersContainer m_raceStatsPlayers;

		private StringBuilder stringBuffer = new StringBuilder();

		private List<ActionHintData> m_actionHintsData = new List<ActionHintData>();

		private string m_cooldownRemainingText;

		private int m_prevCooldownValue;

		public readonly StringProperty massData = new StringProperty();

		public readonly StringProperty horsePowerData = new StringProperty();

		public readonly StringProperty tyresData = new StringProperty();

		public readonly IntProperty playerInputType = new IntProperty();

		private static NetworkPlayer m_networkPlayer;

		private NetGameCarSwapSystem m_networkCarSwapSystem;

		private NetGameDynoSystem m_netGameDynoSystem;

		public static NetworkPlayer networkPlayer => m_networkPlayer;

		public bool isChampionshipMode => GameConsole.Bool["ChampionshipMode"];

		protected override void OnActivate()
		{
			base.OnActivate();
			LocalizeTelemetricKeys();
			PlayerCarControl instance = PlayerCarControl.instance;
			CameraSwitch instance2 = CameraSwitch.instance;
			instance2.onSwitchCar += OnSwitchCarHandler;
			if ((bool)instance)
			{
				instance.SetBlockControl(value: true);
				instance2.InformChangeCameraView(instance.car.carX, outside: true, frontCam: false);
			}
			CarXDboxManager.instance?.Stop();
			SubscribeOnCarSwapChanges();
			SubscribeOnDynoChanges();
		}

		protected override void OnDeactivate()
		{
			m_title.value = string.Empty;
			base.owner.Hide<UIRaceHintsContext>();
			PlayerCarControl instance = PlayerCarControl.instance;
			CameraSwitch instance2 = CameraSwitch.instance;
			instance2.onSwitchCar -= OnSwitchCarHandler;
			if ((bool)m_target)
			{
				m_target.SetVisibleUIName(m_target.prefs.globals.showNickNames);
				instance2.InformChangeCameraView(m_target.carX, outside: true, frontCam: false);
			}
			ActivateLastTargetHead();
			if ((bool)instance)
			{
				instance.SetBlockControl(value: false);
				instance2.SetCar(instance.car, resetInterpolation: true);
				UIInputOverlayContext.instance.SetCar(instance.car);
			}
			base.OnDeactivate();
			CarXDboxManager.instance?.Start();
			UnsubscribeFromCarSwapChanges();
			UnsubscribeFromDynoChanges();
			m_networkPlayer = null;
			m_carXCar = null;
			m_targetCarDesc = null;
			m_target = null;
		}

		private void LocalizeTelemetricKeys()
		{
			m_massLocalizedString = LocalizationKeys.garage_setup_kg.Localized();
			m_horsePowerLocalizedString = LocalizationKeys.garage_setup_hp.Localized();
		}

		private void SubscribeOnCarSwapChanges()
		{
			if (m_networkCarSwapSystem == null)
			{
				m_networkCarSwapSystem = NetworkController.InstanceGame.systems.Get<NetGameCarSwapSystem>();
			}
			NetGameCarSwapSystem networkCarSwapSystem = m_networkCarSwapSystem;
			networkCarSwapSystem.onCarSlotChanged = (Action<NetworkPlayer>)Delegate.Combine(networkCarSwapSystem.onCarSlotChanged, new Action<NetworkPlayer>(OnCarSlotChangedHandler));
		}

		private void UnsubscribeFromCarSwapChanges()
		{
			if (m_networkCarSwapSystem != null)
			{
				NetGameCarSwapSystem networkCarSwapSystem = m_networkCarSwapSystem;
				networkCarSwapSystem.onCarSlotChanged = (Action<NetworkPlayer>)Delegate.Remove(networkCarSwapSystem.onCarSlotChanged, new Action<NetworkPlayer>(OnCarSlotChangedHandler));
			}
		}

		private void SubscribeOnDynoChanges()
		{
			if (m_netGameDynoSystem == null)
			{
				m_netGameDynoSystem = NetworkController.InstanceGame.systems.Get<NetGameDynoSystem>();
			}
			NetGameDynoSystem netGameDynoSystem = m_netGameDynoSystem;
			netGameDynoSystem.onNetworkPlayerProfileApplied = (Action<NetworkPlayer>)Delegate.Combine(netGameDynoSystem.onNetworkPlayerProfileApplied, new Action<NetworkPlayer>(OnNetworkPlayerProfileAppliedHandler));
		}

		private void UnsubscribeFromDynoChanges()
		{
			if (m_netGameDynoSystem != null)
			{
				NetGameDynoSystem netGameDynoSystem = m_netGameDynoSystem;
				netGameDynoSystem.onNetworkPlayerProfileApplied = (Action<NetworkPlayer>)Delegate.Remove(netGameDynoSystem.onNetworkPlayerProfileApplied, new Action<NetworkPlayer>(OnNetworkPlayerProfileAppliedHandler));
			}
		}

		private void OnCarSlotChangedHandler(NetworkPlayer networkPlayer)
		{
			WatchPlayer(networkPlayer);
		}

		public void SetPlayersListData(RaceStatsPlayersContainer raceStatsPlayers)
		{
			m_raceStatsPlayers = raceStatsPlayers;
		}

		private void ActivateLastTargetHead(RaceCar newTarget = null)
		{
			if ((!(newTarget != null) || !(m_target == newTarget)) && m_target != null && m_target.driverCamSwitchHandler != null)
			{
				m_target.driverCamSwitchHandler.ForcedChangeDriverHead(active: false);
			}
		}

		private void SetInputDeviceType(int deviceType)
		{
			playerInputType.value = deviceType;
		}

		public void WatchPlayer(NetworkPlayer player)
		{
			ActivateLastTargetHead(player.userCar);
			m_target = player.userCar;
			string filteredNickName = player.FilteredNickName;
			if ((bool)m_target)
			{
				m_title.value = UIUtils.ConvertNGUIColorToRich(filteredNickName) + " | " + m_target.metaInfo.name;
				m_wheelParm.value = string.Empty;
				m_networkPlayer = player;
				m_targetCarDesc = m_target.GetDesc();
				m_carXCar = m_target.GetComponent<Car>();
				if (isChampionshipMode && m_carXCar != null)
				{
					if (stringBuffer == null)
					{
						stringBuffer = new StringBuilder();
					}
					else
					{
						stringBuffer.Clear();
					}
					stringBuffer.Append($"Front / Rear rim diameter | {Mathf.Round(m_carXCar.frontTyreDiscDiam)}in / {Mathf.Round(m_carXCar.rearTyreDiscDiam)}in");
					stringBuffer.Append($"\nFront / Rear tyre width | {Mathf.Round(m_carXCar.frontTyreWidth)}mm / {Mathf.Round(m_carXCar.rearTyreWidth)}mm");
					stringBuffer.Append($"\nFront / Rear wheel adhesion | {m_carXCar.frontTyreFrictionMultiplier * 100f}% / {m_carXCar.rearTyreFrictionMultiplier * 100f}%");
					stringBuffer.Append($"\nFront / Rear tyre pressure | {Mathf.Round(m_carXCar.GetWheel(WheelIndex.FrontLeft).tyrePressure) / 1000f}kPa / {Mathf.Round(m_carXCar.GetWheel(WheelIndex.RearLeft).tyrePressure) / 1000f}kPa");
					stringBuffer.Append($"\nFront / Rear tyre profile | {Mathf.Round(m_carXCar.frontTyreProfile)}% / {Mathf.Round(m_carXCar.rearTyreProfile)}%");
					m_wheelParm.value = stringBuffer.ToString();
				}
				CameraSwitch.instance.SetCar(m_target, resetInterpolation: true);
				UIInputOverlayContext.instance.SetCar(m_target);
			}
			if (PlayerCarControl.instance.car != m_target)
			{
				CarCameraSwitchHandler driverCamSwitchHandler = PlayerCarControl.instance.car.driverCamSwitchHandler;
				if (driverCamSwitchHandler != null)
				{
					driverCamSwitchHandler.ForcedChangeDriverHead(active: false);
				}
			}
			UpdateTelemetricValues();
			UpdateHints();
		}

		private void UpdateTelemetricValues()
		{
			UpdateRearTyresData();
			UpdateWeightData();
			UpdateHorsePowerData();
		}

		private void UpdateWeightData()
		{
			massData.value = m_targetCarDesc.carXDesc.weight.mass.ToString("0") + m_massLocalizedString;
		}

		private void UpdateHorsePowerData()
		{
			m_target.carX.CalcMaxPowerMaxTorque(out var max_hp, out var _, out var _, out var _);
			horsePowerData.value = MathF.Round(max_hp, 0) + m_horsePowerLocalizedString;
		}

		private void UpdateRearTyresData()
		{
			tyresData.value = CreateWheelDataString((int)m_carXCar.rearTyreDiscDiam, (int)m_carXCar.rearTyreWidth, (int)m_carXCar.rearTyreProfile);
		}

		private string CreateWheelDataString(int tyreDiam, int tyreWeight, int tyreProfile)
		{
			return $"{tyreWeight}/{tyreProfile}/R{tyreDiam}";
		}

		private void UpdateHints()
		{
			m_actionHintsData.Clear();
			m_actionHintsData.AddRange(new ActionHintData[2]
			{
				new ActionHintData
				{
					hint = LocalizationKeys.teleport_to_player.Localized() + " " + m_cooldownRemainingText,
					type = ActionType.NavSelectBtnA,
					action = TeleportToPlayer
				},
				new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_exit_watch_mode.Localized(),
					type = ActionType.NavCancelBtnB,
					action = NavCancelBtnB
				}
			});
			UIRaceHintsContext.ShowHints(base.owner, m_actionHintsData);
		}

		private void SetAndWatchTarget(UIRaceStatsContext.PlayerStatsItem item)
		{
			RaceCar userCar = item.player.userCar;
			if (userCar != null && userCar.gameObject.activeSelf && !item.player.IsLocal)
			{
				WatchPlayer(item.player);
			}
		}

		private void OnSwitchCarHandler(RaceCar target)
		{
			if (m_target != target)
			{
				Hide();
			}
		}

		private void OnNetworkPlayerProfileAppliedHandler(NetworkPlayer networkPlayer)
		{
			if (networkPlayer.PlayerId == m_networkPlayer.PlayerId)
			{
				UpdateTelemetricValues();
			}
		}

		private void NavCancelBtnB()
		{
			Hide();
		}

		private void TeleportToPlayer()
		{
			m_raceStatsPlayers.SetSelectedIndex(m_raceStatsPlayers.GetItemWithSelectedCar());
			UIRaceStatsContext.PlayerStatsItem playerStatsItem = m_raceStatsPlayers.TryGetSelectedItem();
			if (playerStatsItem != null)
			{
				NetworkPlayer player = playerStatsItem.player;
				if (PlayerCarControl.instance.car.TryResetToPlayer(player.userCar))
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.you_are_teleported.Localized(), 3f);
					Hide();
				}
				else
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.teleport_is_not_possible.Localized(), 3f);
				}
			}
		}

		[GameHotkey(ActionType.RaceStats, false, false)]
		public void ShowStatsPanel()
		{
			base.owner.Push<UIRaceStatsContext>();
		}

		[GameHotkey(ActionType.NavRight, false, false)]
		public void ShowNextCar()
		{
			m_raceStatsPlayers.SetSelectedIndex(m_raceStatsPlayers.GetItemWithSelectedCar());
			SetAndWatchTarget(m_raceStatsPlayers?.GetNextTarget(nextTarget: true));
		}

		[GameHotkey(ActionType.NavLeft, false, false)]
		public void ShowPrevCar()
		{
			m_raceStatsPlayers.SetSelectedIndex(m_raceStatsPlayers.GetItemWithSelectedCar());
			SetAndWatchTarget(m_raceStatsPlayers?.GetNextTarget(nextTarget: false));
		}

		private void Update()
		{
			UIRaceStatsContext uIRaceStatsContext = base.owner.Get<UIRaceStatsContext>();
			if (!(uIRaceStatsContext != null) || !uIRaceStatsContext.isActiveAndEnabled)
			{
				m_raceStatsPlayers?.Update();
				if (m_networkPlayer != null)
				{
					SetInputDeviceType((int)m_networkPlayer.PlayerProperties.InputDeviceType);
				}
				int remainingCooldownTime = PlayerCarControl.instance.car.carPlacementModel.remainingCooldownTime;
				m_cooldownRemainingText = ((remainingCooldownTime > 0) ? $" ({remainingCooldownTime})" : string.Empty);
				if (remainingCooldownTime != m_prevCooldownValue)
				{
					UpdateHints();
					m_prevCooldownValue = remainingCooldownTime;
				}
			}
		}
	}
}
