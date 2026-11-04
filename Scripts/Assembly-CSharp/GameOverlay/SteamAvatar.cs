using System;
using System.Collections;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameOverlay
{
	public class SteamAvatar
	{
		private static byte[] m_defaultAvatarRAW;

		private static byte[] m_defaultAvatarRAW_normal;

		private static byte[] m_defaultAvatarRAW_small;

		private readonly SteamId m_steamId;

		private readonly AvatarSize m_avatarSize;

		public SteamAvatar(SteamId steamId, AvatarSize avatarSize)
		{
			m_steamId = steamId;
			m_avatarSize = avatarSize;
			LoadDefaultAvatar();
		}

		public void Load(Action<Texture2D> onDone)
		{
			KeepAliveManager.instance.StartCoroutine(LoadCoroutine(onDone));
		}

		private void LoadDefaultAvatar()
		{
			if (m_defaultAvatarRAW == null)
			{
				TextAsset textAsset = Resources.Load("avatarDefaultRAW") as TextAsset;
				m_defaultAvatarRAW = ((textAsset != null) ? textAsset.bytes : new byte[0]);
				textAsset = Resources.Load("avatarDefaultRAW_normal") as TextAsset;
				m_defaultAvatarRAW_normal = ((textAsset != null) ? textAsset.bytes : new byte[0]);
				textAsset = Resources.Load("avatarDefaultRAW_small") as TextAsset;
				m_defaultAvatarRAW_small = ((textAsset != null) ? textAsset.bytes : new byte[0]);
			}
		}

		private static bool IsDefaultRaw(byte[] source)
		{
			byte[] array = null;
			if (source.Length == m_defaultAvatarRAW.Length)
			{
				array = m_defaultAvatarRAW;
			}
			if (source.Length == m_defaultAvatarRAW_normal.Length)
			{
				array = m_defaultAvatarRAW_normal;
			}
			if (source.Length == m_defaultAvatarRAW_small.Length)
			{
				array = m_defaultAvatarRAW_small;
			}
			if (array == null)
			{
				return false;
			}
			float num = 0f;
			for (int i = 0; i < source.Length / 2; i++)
			{
				num += (float)Mathf.Abs(source[i] - array[i]) / 255f;
			}
			num = 2f * num / (float)source.Length;
			return num < 1E-05f;
		}

		private static void GetDefault(out Texture2D tex)
		{
			AsyncOperationHandle<Texture2D> handle = Addressables.LoadAssetAsync<Texture2D>("avatarDefault");
			tex = handle.WaitForCompletion();
			Addressables.Release(handle);
		}

		private static async Task<Image?> GetAvatar(SteamId steamId, AvatarSize avatarSize)
		{
			return avatarSize switch
			{
				AvatarSize.Large => await SteamFriends.GetLargeAvatarAsync(steamId), 
				AvatarSize.Small => await SteamFriends.GetSmallAvatarAsync(steamId), 
				_ => await SteamFriends.GetMediumAvatarAsync(steamId), 
			};
		}

		private static Texture2D LoadAvatar(Image image)
		{
			if (IsDefaultRaw(image.Data))
			{
				GetDefault(out var tex);
				if ((bool)tex)
				{
					return tex;
				}
			}
			Texture2D texture2D = new Texture2D((int)image.Width, (int)image.Height, TextureFormat.RGBA32, mipChain: false);
			texture2D.LoadRawTextureData(image.Data);
			Texture2D texture2D2 = SteamOverlay.FlipTexture(texture2D);
			texture2D2.Apply(updateMipmaps: true);
			return texture2D2;
		}

		private IEnumerator LoadCoroutine(Action<Texture2D> onDone)
		{
			Task<Image?> avatarTask = GetAvatar(m_steamId, m_avatarSize);
			yield return avatarTask.AsIEnumerator();
			Image? result = avatarTask.Result;
			if (!result.HasValue || result.Value.Data == null)
			{
				GetDefault(out var tex);
				onDone.Call(tex);
			}
			else
			{
				onDone.Call(LoadAvatar(result.Value));
			}
		}
	}
}
