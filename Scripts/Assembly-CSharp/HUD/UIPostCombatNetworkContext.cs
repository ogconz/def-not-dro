using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Binding;
using Binding.Base;
using DB;
using DI;
using GameInput;
using GameOverlay;
using Quests;
using SyncMultiplayer;
using UnityEngine;

namespace HUD
{
	[GameHotkeyBlocker(true)]
	public class UIPostCombatNetworkContext : BaseBindingBehaviourTarget, IPostCombatNetworkContext
	{
		public class PlayerInRoomResultItem : BaseListElementData
		{
			private StringProperty m_name = new StringProperty();

			private IntProperty m_number = new IntProperty();

			private BoolProperty m_self = new BoolProperty();

			private StringProperty m_result = new StringProperty();

			private BoolProperty m_disqualified = new BoolProperty();

			private BoolProperty m_showPlatformIcon = new BoolProperty();

			private IntProperty m_platform = new IntProperty();

			private SpriteProperty m_avatar = new SpriteProperty();

			private BoolProperty m_winer = new BoolProperty();

			private PlayerId m_id;

			public PlayerInRoomResultItem(int number, PlayerId playerId, string nickName, string result, bool isLocal, bool disqualified)
			{
				m_number.value = number;
				m_winer.value = !disqualified && number == 1;
				if (m_id != playerId)
				{
					m_id = playerId;
					m_name.value = nickName;
				}
				m_result.value = result;
				m_self.value = isLocal;
				m_disqualified.value = disqualified;
				SetSprite(m_avatar, GameManager.instance.defaultAvatar);
				Overlay.instance.RequestUserAvatarAndName(playerId, OnLoadedAvatar);
			}

			private void OnLoadedAvatar(PlayerId id, string name, Texture2D texture)
			{
				SetSprite(m_avatar, texture);
			}

			private void SetSprite(SpriteProperty property, Texture2D texture)
			{
				Texture2D texture2D = texture ?? GameManager.instance.defaultAvatar;
				property.value = Sprite.Create(texture, new Rect(0f, 0f, texture2D.width, texture2D.height), Vector2.one * 0.5f);
			}
		}

		private ListProperty m_playersList = new ListProperty();

		private StringProperty m_subroomMode = new StringProperty();

		private StringProperty m_config = new StringProperty();

		private BoolProperty m_showRaceHints = new BoolProperty(startValue: true);

		[SerializeField]
		private UIRaceHintsContext m_hints;

		[SerializeField]
		private UIPostCombatRewardContext m_rewards;

		[Dependency]
		private GamePrefs m_prefs;

		[Dependency]
		private BaseModel m_model;

		private void Awake()
		{
			if (m_hints == null)
			{
				m_hints = GetComponentInChildren<UIRaceHintsContext>();
			}
		}

		private void Start()
		{
			if ((bool)m_hints)
			{
				m_hints.Clear();
				m_hints.AddElement(LocalizationKeys.nav_hint_exit.Localized(), ActionType.NavCancelBtnB, Close);
			}
		}

		protected override void OnActivate()
		{
			if (base.owner.IsActive<UIBaseDialogContext>())
			{
				base.owner.Hide<UIBaseDialogContext>();
			}
			if (KeepAliveManager.isUIHidden)
			{
				Hide();
				return;
			}
			base.OnActivate();
			BringForwardByDepth();
			if ((bool)PlayerCarControl.instance)
			{
				PlayerCarControl.instance.SetBlockControl(value: true);
			}
			XRModeManager.TryApplyCockpitCamera();
		}

		protected override void OnDeactivate()
		{
			if ((bool)PlayerCarControl.instance)
			{
				PlayerCarControl.instance.SetBlockControl(value: false);
			}
			base.OnDeactivate();
		}

		protected override bool OnEscape()
		{
			return true;
		}

		public void Close()
		{
			Hide();
		}

		public void Fill(SubroomInfo info, bool showRaceHints = true, bool top32 = false)
		{
			List<ValidatedRoomResult> allPlayersValidatedResult = info.GetAllPlayersValidatedResult();
			m_subroomMode.value = LocalizationKeys.network_room_mode_.AddLocalized(info.subroomMode.ToString().ToLower());
			m_config.value = info.pointName;
			m_playersList.Clear();
			for (int i = 0; i < allPlayersValidatedResult.Count; i++)
			{
				ValidatedRoomResult validatedRoomResult = allPlayersValidatedResult[i];
				string filteredNickName = info.GetFilteredNickName(validatedRoomResult.playerId);
				bool isLocal = validatedRoomResult.playerId.Equals(Overlay.playerId);
				PlayerInRoomResultItem data = new PlayerInRoomResultItem(i + 1, validatedRoomResult.playerId, filteredNickName, validatedRoomResult.result.asString, isLocal, !validatedRoomResult.result.isFinished);
				m_playersList.Add(data);
			}
			m_rewards.Init();
			int num = allPlayersValidatedResult.FindIndex((ValidatedRoomResult r) => r.playerId.Equals(Overlay.playerId));
			if (num != -1)
			{
				ValidatedRoomResult validatedRoomResult2 = allPlayersValidatedResult[num];
				ReadOnlyCollection<int> mp_challengeList = m_prefs.gameSettings.expSettings.mp_challengeList;
				int num2 = 0;
				switch (info.subroomMode)
				{
				case SubroomMode.Challenge:
				case SubroomMode.TimeAttack:
				case SubroomMode.XDS:
					if (num + 1 < mp_challengeList.Count)
					{
						num2 = mp_challengeList[num];
					}
					break;
				case SubroomMode.ClippingDuel:
				case SubroomMode.Tandem:
				case SubroomMode.TandemXDS:
					if (num == 0)
					{
						num2 = mp_challengeList[0];
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
				case SubroomMode.ShowTrack:
					break;
				}
				NetworkController.InstanceGame.stats.xp += num2;
				m_prefs.playerProfile.exp += num2;
				GameLog.Print(AlertLevel.Notify, "PlayerProfile", $"(exp) net race finished, exp for position ({num + 1}) is {num2}");
				if (validatedRoomResult2 != null && validatedRoomResult2.result.isFinished && !top32)
				{
					bool singlePlayer = allPlayersValidatedResult.Count == 1;
					List<(QuestsAsset.QuestMeta, List<QuestsAsset.QuestReward>)> list = CompleteQuests(singlePlayer, num + 1, validatedRoomResult2, info.subroomMode);
					if (list != null)
					{
						foreach (var item in list)
						{
							foreach (QuestsAsset.QuestReward item2 in item.Item2)
							{
								m_rewards.AddReward(item2.icon, item2.id, item2.count);
							}
						}
					}
				}
			}
			m_showRaceHints.value = showRaceHints;
		}

		private List<(QuestsAsset.QuestMeta, List<QuestsAsset.QuestReward>)> CompleteQuests(bool singlePlayer, int finishPosition, ValidatedRoomResult result, SubroomMode mode)
		{
			if (mode == SubroomMode.XDS)
			{
				return QuestController.EventCompleted("mp_xds_points_single_run", (int)result.result.points);
			}
			if (singlePlayer)
			{
				return null;
			}
			QuestController.EventCompleted("mp_any_race_finish", result.result.isFinished ? 1 : 0, null, getRewards: false);
			return mode switch
			{
				SubroomMode.Challenge => QuestController.EventCompleted("mp_drift_win", finishPosition), 
				SubroomMode.ClippingDuel => QuestController.EventCompleted("mp_duel_win", finishPosition), 
				SubroomMode.Tandem => QuestController.EventCompleted("mp_tandem_dp_win", finishPosition), 
				SubroomMode.TimeAttack => QuestController.EventCompleted("mp_time_attack_win", finishPosition), 
				_ => null, 
			};
		}
	}
}
