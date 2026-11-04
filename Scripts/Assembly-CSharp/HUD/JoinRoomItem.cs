using Binding;
using SyncMultiplayer;
using UnityEngine;
using UnityEngine.U2D;

namespace HUD
{
	public class JoinRoomItem : BaseListElementData
	{
		private ColorProperty m_color = new ColorProperty(Color.red);

		private SpriteProperty m_icon = new SpriteProperty();

		private BoolProperty m_safeMode = new BoolProperty();

		private StringProperty m_name = new StringProperty();

		private StringProperty m_mode = new StringProperty();

		private StringProperty m_track = new StringProperty();

		private StringProperty m_count = new StringProperty();

		private IntProperty m_time = new IntProperty();

		private StringProperty m_status = new StringProperty();

		private FloatProperty m_timeFill = new FloatProperty();

		public SubroomInfo roomInfo { get; private set; }

		public JoinRoomItem()
		{
		}

		public JoinRoomItem(SubroomInfo roomInfo, SpriteAtlas atlas)
		{
			Init(roomInfo, atlas);
		}

		public void Init(SubroomInfo roomInfo, SpriteAtlas atlas)
		{
			m_color.value = UIRaceNetworkModeContext.subroomColor[roomInfo.subroomMode];
			m_icon.value = atlas.GetSprite(roomInfo.subroomMode.ToString().ToLower());
			m_time.OnValueChanged += OnTimerTick;
			m_safeMode.value = roomInfo.subroomMode == SubroomMode.TandemXDS && !roomInfo.settings.xdsFlags.HasFlag(SubroomFlags.XDSCollide);
			this.roomInfo = roomInfo;
			m_mode.value = LocalizationKeys.network_room_mode_.AddLocalized(roomInfo.subroomMode.ToString().ToLower());
			m_track.value = roomInfo.pointName;
			NetworkPlayer networkPlayer = ((roomInfo != null) ? NetworkController.InstanceGame.FindPlayer(roomInfo.creatorId) : null);
			if (networkPlayer != null)
			{
				m_name.value = LocalizationKeys.network_room_creator.LocalizedFormat(networkPlayer.FilteredNickNameWithPlatformIcon);
			}
			else
			{
				m_name.value = "Player was not found";
			}
		}

		public void Refresh()
		{
			m_count.value = roomInfo.playersCount + "/" + roomInfo.settingsPlayersCount;
			float liveTime = roomInfo.GetLiveTime();
			m_timeFill.value = liveTime / roomInfo.GetLiveTimeBound();
			m_time.value = Mathf.RoundToInt(liveTime);
			m_status.value = LocalizationKeys.network_room_state_waitingplayers.Localized();
		}

		private void OnTimerTick()
		{
			int value = m_time.value;
			if (value <= 5 || (value <= 30 && value % 10 == 0))
			{
				SoundController.Play2DUISound(SoundSample.MPTimerTick, 0f);
			}
		}
	}
}
