using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using DB;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using SyncMultiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace HUD
{
	[RaycastBlocker(true)]
	public class UIVoteContext : BaseBindingBehaviourTarget
	{
		private class VoteListItem : BaseListElementData
		{
			private StringProperty m_name = new StringProperty();

			private BoolProperty m_isAdvancedItem = new BoolProperty(false);

			public object advancedTag;

			public string target;

			public string additionalTarget;

			public object id { get; }

			public VoteType voteType { get; }

			public VoteReason voteReason { get; }

			public bool isAdvancedItem
			{
				get
				{
					return m_isAdvancedItem.value;
				}
				set
				{
					m_isAdvancedItem.value = value;
				}
			}

			public VoteListItem(string text, string target, VoteType voteType, VoteReason voteReason, object id, bool advancedItem = false, string additionalTarget = "")
			{
				this.id = id;
				this.voteType = voteType;
				this.voteReason = voteReason;
				m_name.value = text;
				this.target = target;
				this.additionalTarget = additionalTarget;
				m_isAdvancedItem.value = advancedItem;
			}
		}

		private enum Mode
		{
			Category = 0,
			List = 1,
			Vote = 2,
			KickBanReason = 3
		}

		private const int MIN_PLAYERS_VOTE_COUNT = 2;

		public static Action<bool> voteContextActivatedAction;

		private readonly BoolProperty m_nightVoteVisibility = new BoolProperty();

		private readonly BoolProperty m_lightVoteVisibility = new BoolProperty();

		private const string NIGHT_POSTFIX = "_night";

		private BoolProperty m_needToBuyNightMode = new BoolProperty(false);

		private ListProperty m_list = new ListProperty();

		private IntProperty m_mode = new IntProperty();

		private VoteType? m_currentVoteType;

		private VoteReason m_currentVoteReason = VoteReason.None;

		private bool m_changedNavOptionsSet;

		private bool m_forceResetSelection;

		private Dictionary<Mode, int> m_modeSelectionDict = new Dictionary<Mode, int>();

		[Dependency]
		private BaseModel m_model;

		private NetGameServerVoteSystem m_votesController;

		[SerializeField]
		private GridLayoutGroup listLayoutGroup;

		private int m_rows = 5;

		private int m_id;

		private FloatProperty m_voteTime = new FloatProperty();

		private FloatProperty m_voteTimeFill = new FloatProperty();

		private StringProperty m_voteText = new StringProperty();

		private StringProperty m_voteMapName = new StringProperty();

		private StringProperty m_additionalName = new StringProperty();

		private Mode voteMode
		{
			get
			{
				return (Mode)m_mode.value;
			}
			set
			{
				m_changedNavOptionsSet = m_mode.value != (int)value && base.navElements != null && base.navElements.Count > 0;
				if (m_changedNavOptionsSet)
				{
					int num = base.navElements.FindIndex((ElementNavigate el) => el.selected);
					if (num == -1)
					{
						num = 0;
					}
					m_modeSelectionDict[(Mode)m_mode.value] = num;
				}
				m_mode.value = (int)value;
				if (m_changedNavOptionsSet)
				{
					List<ElementNavigate> list = base.navElements.FindAll((ElementNavigate el) => el.gameObject.activeSelf);
					if (list != null && list.Count > 0)
					{
						if (m_modeSelectionDict.TryGetValue(value, out var value2))
						{
							base.selectedItem = list[(!m_forceResetSelection) ? value2 : 0].gameObject;
						}
						else
						{
							base.selectedItem = list[0].gameObject;
						}
					}
				}
				ShowHints();
			}
		}

		private void Awake()
		{
			m_list.OnClick += OnClickItem;
			if (listLayoutGroup.constraint == GridLayoutGroup.Constraint.FixedRowCount)
			{
				m_rows = listLayoutGroup.constraintCount;
			}
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			if (m_votesController == null)
			{
				m_votesController = NetworkController.InstanceGame.systems.Get<NetGameServerVoteSystem>();
			}
			if (m_votesController.currentVote != null)
			{
				voteMode = Mode.Vote;
				m_voteMapName.value = string.Empty;
				m_additionalName.value = string.Empty;
			}
			else
			{
				voteMode = Mode.Category;
			}
			m_list.Clear();
			m_needToBuyNightMode.value = !GameManager.NightModeInstalled;
			if (base.navElements != null && base.navElements.Count > 0)
			{
				base.selectedItem = base.navElements[0].gameObject;
			}
			m_votesController.onVoteFinished += Hide;
			m_votesController.onVoteStart += OnVoteStart;
			voteContextActivatedAction.SafeCall(param: true);
			XRModeManager.TryApplyCockpitCamera();
			GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
			DB.Meta.Track byId = m_model.GetById<DB.Meta.Track>(gamePrefs.globals.lastTrackId);
			m_nightVoteVisibility.value = byId != null && !byId.external;
			m_lightVoteVisibility.value = m_nightVoteVisibility.value;
		}

		protected override void OnDeactivate()
		{
			voteContextActivatedAction.SafeCall(param: false);
			m_modeSelectionDict.Clear();
			m_votesController.onVoteFinished -= Hide;
			m_votesController.onVoteStart -= OnVoteStart;
			base.owner.Hide<UIRaceHintsContext>();
			m_list.Clear();
			base.OnDeactivate();
		}

		protected override bool OnEscape()
		{
			return true;
		}

		private void OnVoteStart(VoteType voteType, string user)
		{
			if (voteMode == Mode.Category || voteMode == Mode.List)
			{
				voteMode = Mode.Vote;
				m_voteMapName.value = string.Empty;
				m_additionalName.value = string.Empty;
			}
		}

		private void ShowHints()
		{
			List<ActionHintData> list = new List<ActionHintData>();
			list.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = NavCancelBtnB
			});
			if (voteMode == Mode.Vote)
			{
				if (!(m_votesController?.currentVote is IServerVotePlayer serverVotePlayer) || !(serverVotePlayer.targetId == NetworkController.InstanceGame.LocalPlayer.PlayerId))
				{
					list.Add(new ActionHintData
					{
						hint = LocalizationKeys.Yes.Localized(),
						type = ActionType.NavSelectBtnA,
						action = NavSelectBtnA
					});
					list.Add(new ActionHintData
					{
						hint = LocalizationKeys.No.Localized(),
						type = ActionType.NavAdditional0BtnX,
						action = NavAdditional0BtnX
					});
				}
			}
			else
			{
				list.Add(new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_select.Localized(),
					type = ActionType.NavSelectBtnA,
					action = NavSelectBtnA
				});
			}
			UIRaceHintsContext.ShowHints(base.owner, list);
		}

		public void ShowVoteReasonList(int voteType)
		{
			m_forceResetSelection = true;
			voteMode = Mode.KickBanReason;
			m_forceResetSelection = false;
			m_currentVoteType = (VoteType)voteType;
		}

		public void FillPlayersList(int voteReasonType)
		{
			int num = 0;
			foreach (NetworkPlayer player in NetworkController.InstanceGame.Players)
			{
				if (player != null && !(player.userCar == null) && !player.isNetworkCarLoading)
				{
					num++;
				}
			}
			if (num < 2)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.vote_few_players.Localized(), 5f);
				return;
			}
			m_currentVoteReason = (VoteReason)voteReasonType;
			if (m_currentVoteType.HasValue)
			{
				FillPlayersList(m_currentVoteType.Value);
			}
		}

		private void FillPlayersList(VoteType voteType)
		{
			if (voteType == VoteType.Map || voteType == VoteType.Night)
			{
				return;
			}
			m_list.Clear();
			foreach (NetworkPlayer player in NetworkController.InstanceGame.Players)
			{
				if (!player.IsLocal && !(player.userCar == null) && !player.isNetworkCarLoading)
				{
					m_list.Add(new VoteListItem(player.FilteredNickNameWithPlatformIcon, player.OriginaNickName, voteType, m_currentVoteReason, player.PlayerId));
				}
			}
			m_list.Selection(0, selected: true);
			voteMode = Mode.List;
		}

		public void FillMapList()
		{
			m_list.Clear();
			List<DB.Meta.Track> list = m_model.QueryAll<DB.Meta.Track>();
			list.Sort(DB.Meta.Track.Sort);
			GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
			foreach (DB.Meta.Track item in list)
			{
				if (item.enabled && !item.hidden && !item.external && gamePrefs.globals.lastTrackId != item.id && !(item.configuration.ToLower() != "default"))
				{
					m_list.Add(new VoteListItem(LocalizationService.Localize(item.name), item.name, VoteType.Map, VoteReason.None, item.id)
					{
						isAdvancedItem = (item.dlcCode != DLCCode.Unknown && !Overlay.instance.IsDLCInstalled(item.dlcCode)),
						advancedTag = item.dlcCode
					});
				}
			}
			m_list.Selection(0, selected: true);
			voteMode = Mode.List;
			m_currentVoteType = VoteType.Map;
		}

		public void SelectNightVote()
		{
			if (!GameManager.NightModeInstalled)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.no_dlc_night_mode.Localized());
				return;
			}
			voteMode = Mode.List;
			m_currentVoteType = VoteType.Night;
			FillLightPresetList(string.Empty, VoteType.Night);
		}

		public void SelectLightPreset()
		{
			m_currentVoteType = VoteType.Light;
			voteMode = Mode.List;
			FillLightPresetList();
		}

		private void FillLightPresetList(string sceneName = "", VoteType voteType = VoteType.Light)
		{
			m_list.Clear();
			TrackLightVariantsBase instance = ScriptableSingleton<TrackLightVariantsBase>.instance;
			GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
			string target = string.Empty;
			if (string.IsNullOrEmpty(sceneName))
			{
				sceneName = gamePrefs.globals.sceneName;
			}
			else
			{
				target = sceneName;
				string text = (BaseRaceState.NightModeActive ? "_night" : string.Empty);
				sceneName = ScriptableSingleton<TrackLightVariantsBase>.instance.GetLightSceneNameOfScene(sceneName) + text;
			}
			if (voteType == VoteType.Night)
			{
				if (BaseRaceState.NightModeActive)
				{
					if (sceneName.Contains("_night"))
					{
						sceneName = sceneName.Substring(0, sceneName.Length - "_night".Length);
					}
				}
				else
				{
					sceneName += "_night";
				}
			}
			string[] allLightVariantForScene = instance.GetAllLightVariantForScene(sceneName);
			string[] array = allLightVariantForScene;
			foreach (string text2 in array)
			{
				if (text2 == gamePrefs.globals.lightPresetName && sceneName == gamePrefs.globals.sceneName)
				{
					if (allLightVariantForScene.Length <= 1)
					{
						GUICommonGodVoice.ShowText(LocalizationKeys.cant_vote_light.Localized());
						NavCancelBtnB();
						return;
					}
					continue;
				}
				string text3 = (instance.IsDefaultPresetForLightScene(sceneName, text2) ? LocalizationKeys.default_light_preset.Localized() : LocalizationService.Localize(instance.GetVisibleNameForLightPreset(text2)));
				switch (voteType)
				{
				case VoteType.Map:
					AddVoteItem(text3, target, voteType, VoteReason.None, -1, advancedItem: false, text2);
					break;
				case VoteType.Light:
					AddVoteItem(text3, text2, voteType, VoteReason.None, 0);
					break;
				case VoteType.Night:
				{
					int num = ((!BaseRaceState.NightModeActive) ? 1 : 0);
					string target2 = (BaseRaceState.NightModeActive ? "Day" : "Night");
					AddVoteItem(text3, target2, voteType, VoteReason.None, num, advancedItem: false, text2);
					break;
				}
				}
			}
			m_list.Selection(0, selected: true);
		}

		private void AddVoteItem(string text, string target, VoteType voteType, VoteReason voteReason, object id, bool advancedItem = false, string additionalTarget = "")
		{
			m_list.Add(new VoteListItem(text, target, voteType, voteReason, id, advancedItem, additionalTarget));
		}

		private void NavSelectBtnA()
		{
			if (!NetworkController.IsVoteStartSystemAllowed())
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.vote_only_admin.Localized());
				return;
			}
			switch (voteMode)
			{
			case Mode.List:
			{
				if (!(m_list.GetSelected() is VoteListItem voteListItem))
				{
					break;
				}
				if (voteListItem.voteType == VoteType.Map && voteListItem.isAdvancedItem)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.vote_need_buy_dlc_track.LocalizedFormat(Overlay.instance.GetDLCShortName((DLCCode)voteListItem.advancedTag).Replace("DLC ", "")));
					break;
				}
				if (!int.TryParse(voteListItem.id.ToString(), out var result))
				{
					result = 0;
				}
				if (voteListItem.voteType == VoteType.Map && result > 0)
				{
					m_id = result;
					FillLightPresetList(voteListItem.target, VoteType.Map);
					break;
				}
				Dictionary<string, string> dictionary = new Dictionary<string, string>
				{
					["id"] = voteListItem.id.ToString(),
					["name"] = voteListItem.target,
					["additional"] = voteListItem.additionalTarget
				};
				if (result < 0)
				{
					dictionary["id"] = m_id.ToString();
					dictionary["externalId"] = "0";
					dictionary["ugcServiceId"] = "0";
				}
				m_votesController.StartVote(voteListItem.voteType, voteListItem.voteReason, dictionary);
				m_voteMapName.value = string.Empty;
				m_additionalName.value = string.Empty;
				voteMode = Mode.Vote;
				break;
			}
			case Mode.Vote:
				if (m_votesController.currentVote != null)
				{
					m_votesController.SetVote(value: true);
				}
				break;
			case Mode.Category:
				SubmitCurrentSelected();
				break;
			case Mode.KickBanReason:
				FillPlayersList(base.navElements.FindIndex((ElementNavigate el) => el.selected));
				break;
			}
		}

		private void NavAdditional0BtnX()
		{
			if (m_votesController.currentVote != null)
			{
				m_votesController.SetVote(value: false);
			}
		}

		private void Update()
		{
			if (m_votesController.currentVote != null && voteMode != Mode.Vote)
			{
				voteMode = Mode.Vote;
			}
			else if (voteMode == Mode.Vote && m_votesController.currentVote == null)
			{
				voteMode = Mode.Category;
			}
			if (voteMode == Mode.Vote)
			{
				UpdateVoteInfo();
			}
		}

		public void UpdateVoteInfo()
		{
			IServerVote currentVote = m_votesController.currentVote;
			string reasonVote = m_votesController.reasonVote;
			if (currentVote == null)
			{
				return;
			}
			m_votesController.GetVoteStats(out var pos, out var neg, out var _, out var self);
			string text = ChatTools.WrapColor(pos.ToString(), (self.HasValue && self.Value) ? Color.green : Color.white);
			string text2 = ChatTools.WrapColor(neg.ToString(), (self.HasValue && !self.Value) ? Color.green : Color.white);
			m_voteText.value = currentVote.GetTitle() + GetReasonVoteLocalized(reasonVote) + " (" + text + " : " + text2 + ")";
			m_voteTime.value = Mathf.Max(0, (int)(m_votesController.voteStarted + 60f - Time.unscaledTime));
			m_voteTimeFill.value = m_voteTime.value / 60f;
			if (string.IsNullOrEmpty(m_voteMapName.value))
			{
				string target = currentVote.GetTarget();
				string additionalTarget = currentVote.GetAdditionalTarget();
				if (currentVote.voteType != VoteType.Map && currentVote.voteType != VoteType.Night && currentVote.voteType != VoteType.Light)
				{
					NetworkPlayer networkPlayer = NetworkController.InstanceGame.FindPlayerByUID(target);
					target = ((networkPlayer != null) ? networkPlayer.FilteredNickNameWithPlatformIcon : string.Empty);
				}
				else
				{
					target = LocalizationService.Localize(target);
				}
				if (!string.IsNullOrEmpty(additionalTarget))
				{
					m_additionalName.value = LocalizationService.Localize(additionalTarget);
				}
				m_voteMapName.value = target;
			}
		}

		private string GetReasonVoteLocalized(string reasonVote)
		{
			LocalizationKeys.Key key = null;
			if (Enum.TryParse<VoteReason>(reasonVote, ignoreCase: true, out var result))
			{
				switch (result)
				{
				case VoteReason.Cheating:
					key = LocalizationKeys.using_cheats_reason;
					break;
				case VoteReason.Griefing:
					key = LocalizationKeys.griefing_reason;
					break;
				case VoteReason.Livery:
					key = LocalizationKeys.bad_vinyls_reason;
					break;
				}
			}
			if (key != null)
			{
				return ": " + key.Localized();
			}
			return string.Empty;
		}

		private void NavCancelBtnB()
		{
			switch (voteMode)
			{
			case Mode.Category:
			case Mode.Vote:
				Hide();
				break;
			case Mode.List:
			{
				m_currentVoteReason = VoteReason.None;
				bool flag = m_currentVoteType.HasValue && (m_currentVoteType == VoteType.Ban || m_currentVoteType == VoteType.Kick);
				voteMode = (flag ? Mode.KickBanReason : Mode.Category);
				break;
			}
			case Mode.KickBanReason:
				voteMode = Mode.Category;
				break;
			}
		}

		public override bool IsAllowNavigation()
		{
			return true;
		}

		public override void SelectionMove(NavigateDirection direction)
		{
			if (voteMode == Mode.List)
			{
				switch (direction)
				{
				case NavigateDirection.Left:
					NavLeft();
					break;
				case NavigateDirection.Right:
					NavRight();
					break;
				case NavigateDirection.Up:
					NavUp();
					break;
				case NavigateDirection.Down:
					NavDown();
					break;
				}
			}
			else
			{
				base.SelectionMove(direction);
			}
		}

		private void NavLeft()
		{
			m_list.Selection(m_list.GetSelectedIndex() - m_rows, selected: true);
		}

		private void NavRight()
		{
			m_list.Selection(m_list.GetSelectedIndex() + m_rows, selected: true);
		}

		private void NavUp()
		{
			m_list.Selection(m_list.GetSelectedIndex() - 1, selected: true);
		}

		private void NavDown()
		{
			m_list.Selection(m_list.GetSelectedIndex() + 1, selected: true);
		}

		private void OnClickItem(BaseListElementData obj)
		{
			m_list.Selection(obj, selected: true);
			NavSelectBtnA();
		}
	}
}
