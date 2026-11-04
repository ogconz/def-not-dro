using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace LoadingArt
{
	public class LoadingArtAssetReference
	{
		private readonly AssetReferenceSprite m_assetRef;

		private AsyncOperationHandle<Sprite> handle;

		private Sprite m_sprite;

		public bool loaded { get; private set; }

		public LoadingArtAssetReference(AssetReferenceSprite assetReferenceSprite)
		{
			m_assetRef = assetReferenceSprite;
		}

		public Sprite Load()
		{
			if (m_sprite != null)
			{
				return m_sprite;
			}
			m_sprite = m_assetRef.LoadAssetAsync<Sprite>().WaitForCompletion();
			loaded = true;
			return m_sprite;
		}

		public async UniTask<Sprite> LoadAsync()
		{
			if (m_sprite != null)
			{
				return await UniTask.FromResult(m_sprite);
			}
			handle = m_assetRef.LoadAssetAsync<Sprite>();
			handle.Completed += OnCompleted;
			return await handle;
		}

		public void Unload()
		{
			m_assetRef.ReleaseAsset();
			m_sprite = null;
			loaded = false;
		}

		private void OnCompleted(AsyncOperationHandle<Sprite> handle)
		{
			if (handle.Status == AsyncOperationStatus.Succeeded)
			{
				m_sprite = handle.GetAwaiter().GetResult();
				loaded = true;
			}
			handle.Completed -= OnCompleted;
		}
	}
}
