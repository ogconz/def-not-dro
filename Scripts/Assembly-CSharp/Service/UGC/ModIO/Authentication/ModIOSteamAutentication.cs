using Cysharp.Threading.Tasks;
using ModIO;
using ModIO.Util;
using Steamworks;

namespace Service.UGC.ModIO.Authentication
{
	public class ModIOSteamAutentication : ModIOSSOAutentication
	{
		protected override async UniTask<bool> AuthenticateWithPlatformTokenAsync()
		{
			m_authResult = await ModIOUnityAsync.AuthenticateUserViaSteam(m_platformAuthToken, null, m_termsOfUse.hash);
			return m_authResult.Value.Succeeded();
		}

		protected override async UniTask<string> GetPlatformAuthTokenAsync()
		{
			byte[] array = await SteamUser.RequestEncryptedAppTicketAsync();
			if (array == null || array.Length == 0)
			{
				return string.Empty;
			}
			return global::ModIO.Util.Utility.EncodeEncryptedSteamAppTicket(array, (uint)array.Length);
		}
	}
}
