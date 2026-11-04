using Chat.Features;
using Cysharp.Threading.Tasks;

namespace Chat.MessagePipelineStages
{
	public class UserInfoRegistryStage : IMessagePipelineStage
	{
		private UserInfoProvider m_userInfoProvider;

		public UserInfoRegistryStage(UserInfoProvider userInfoProvider)
		{
			m_userInfoProvider = userInfoProvider;
		}

		public UniTask ProcessAsync(Message message)
		{
			if (!m_userInfoProvider.Exist(message.userId))
			{
				UserInfo user = new UserInfo(message.userId, message.username);
				m_userInfoProvider.Add(message.userId, user);
			}
			return UniTask.CompletedTask;
		}
	}
}
