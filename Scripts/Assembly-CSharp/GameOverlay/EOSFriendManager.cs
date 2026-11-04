using System.Collections.Generic;
using System.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.UserInfo;
using EpicUnity;

namespace GameOverlay
{
	public class EOSFriendManager
	{
		private const float FetchFriendsTimeInSec = 60f;

		private List<EOSFriend> m_friends = new List<EOSFriend>();

		public async Task<List<EOSFriend>> FetchFriends()
		{
			m_friends.Clear();
			EpicAccountId[] array = await EOSManager.instance.FetchFriends(60f);
			foreach (EpicAccountId player in array)
			{
				UserInfoData? userInfoData = await EOSManager.instance.GetPlayerInfo(player);
				if (userInfoData.HasValue)
				{
					UserInfoData value = userInfoData.Value;
					EOSFriend item = new EOSFriend(value.UserId)
					{
						nickName = value.DisplayName,
						epicId = value.UserId,
						country = value.Country,
						id = new PlayerId(UserPlatform.Id.EpicGames, value.UserId.ToString())
					};
					m_friends.Add(item);
				}
			}
			return m_friends;
		}
	}
}
