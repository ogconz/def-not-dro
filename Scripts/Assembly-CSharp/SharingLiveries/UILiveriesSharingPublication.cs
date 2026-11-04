using Binding;
using Binding.Base;
using GameOverlay;

namespace SharingLiveries
{
	public class UILiveriesSharingPublication : BaseBindingBehaviourTarget
	{
		private StringProperty m_setupName = new StringProperty();

		private StringProperty m_publicationCode = new StringProperty();

		private StringProperty m_creatorNickname = new StringProperty();

		private BoolProperty m_showPlatformIcon = new BoolProperty();

		private BoolProperty m_myCustom = new BoolProperty();

		private IntProperty m_platform = new IntProperty();

		private StringProperty m_uploadDate = new StringProperty();

		private StringProperty m_downloadCount = new StringProperty();

		private StringProperty m_layersCount = new StringProperty();

		private FloatProperty m_rating = new FloatProperty();

		public void SetData(LiverySharingListElement item, bool isFilterCrossplatformEnabled)
		{
			if (item == null)
			{
				return;
			}
			m_setupName.value = item.elementName;
			m_publicationCode.value = item.code.RepeatSpacePaste(" ", 3);
			PlayerId playerId = PlayerId.Parse(item.publisherPlayerId);
			string publisherNickname = item.publisherNickname;
			UserPlatform.Id platform = playerId.platform;
			if (!string.IsNullOrEmpty(publisherNickname))
			{
				Overlay.filterTextManager.GetFilteredCachedText(TextFilterType.Nickname, platform, publisherNickname, delegate(string filteredNickname)
				{
					m_creatorNickname.value = filteredNickname;
				});
			}
			m_showPlatformIcon.value = isFilterCrossplatformEnabled && UserPlatform.IsNeedShowLocalPlatformIcon(platform);
			m_myCustom.value = playerId.Equals(Overlay.instance.player.id);
			platform = ((platform == UserPlatform.Id.MyGames || platform == UserPlatform.Id.EpicGames) ? UserPlatform.Id.Steam : platform);
			m_platform.value = (int)platform;
			m_uploadDate.value = item.createDate.ToLocalTime().ToString();
			m_downloadCount.value = item.downloadCount.ToString();
			m_layersCount.value = item.preset?.layers.Count.ToString();
			m_rating.value = item.rating;
		}

		public void Reset()
		{
			ResetStringProperties(new StringProperty[6] { m_publicationCode, m_setupName, m_creatorNickname, m_uploadDate, m_downloadCount, m_layersCount });
			m_showPlatformIcon.value = false;
			m_myCustom.value = false;
			m_rating.value = 0f;
			m_platform.value = -1;
			static void ResetStringProperties(StringProperty[] stringProperties)
			{
				for (int i = 0; i < stringProperties.Length; i++)
				{
					stringProperties[i].value = string.Empty;
				}
			}
		}
	}
}
