using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace TriLib
{
	public class AssetDownloader : MonoBehaviour
	{
		public string AssetURI;

		public string AssetExtension;

		public GameObject WrapperGameObject;

		public bool ShowProgress;

		public bool Async;

		private UnityWebRequest _unityWebRequest;

		private GUIStyle _centeredStyle;

		private string _error;

		public bool HasStarted => _unityWebRequest != null;

		public bool IsDone
		{
			get
			{
				if (_unityWebRequest != null)
				{
					return _unityWebRequest.isDone;
				}
				return false;
			}
		}

		public string Error
		{
			get
			{
				if (HasStarted)
				{
					return _unityWebRequest.error ?? _error;
				}
				return _error;
			}
		}

		public float Progress
		{
			get
			{
				if (!HasStarted)
				{
					return 0f;
				}
				return _unityWebRequest.downloadProgress;
			}
		}

		protected void Start()
		{
			if (!string.IsNullOrEmpty(AssetURI) && !string.IsNullOrEmpty(AssetExtension))
			{
				DownloadAsset(AssetURI, AssetExtension, null, null, null, WrapperGameObject);
			}
		}

		protected void OnGUI()
		{
			if (ShowProgress && HasStarted && !IsDone)
			{
				if (_centeredStyle == null)
				{
					_centeredStyle = GUI.skin.GetStyle("Label");
					_centeredStyle.alignment = TextAnchor.UpperCenter;
				}
				GUI.Label(new Rect((float)Screen.width / 2f - 100f, (float)Screen.height / 2f - 25f, 200f, 50f), $"Downloaded {Progress:P2}", _centeredStyle);
			}
		}

		public bool DownloadAsset(string assetURI, string assetExtension, ObjectLoadedHandle onAssetLoaded = null, TexturePreLoadHandle onTexturePreLoad = null, AssetLoaderOptions options = null, GameObject wrapperGameObject = null)
		{
			if (HasStarted && !IsDone)
			{
				return false;
			}
			AssetURI = assetURI;
			AssetExtension = assetExtension;
			WrapperGameObject = wrapperGameObject;
			StartCoroutine(DoDownloadAsset(assetURI, assetExtension, onAssetLoaded, options, wrapperGameObject));
			return true;
		}

		private IEnumerator DoDownloadAsset(string assetUri, string assetExtension, ObjectLoadedHandle onAssetLoaded, AssetLoaderOptions options = null, GameObject wrapperGameObject = null)
		{
			_unityWebRequest = UnityWebRequest.Get(assetUri);
			yield return _unityWebRequest.SendWebRequest();
			if (string.IsNullOrEmpty(_unityWebRequest.error))
			{
				byte[] data = _unityWebRequest.downloadHandler.data;
				try
				{
					if (Async)
					{
						using AssetLoaderAsync assetLoaderAsync = new AssetLoaderAsync();
						assetLoaderAsync.LoadFromMemory(data, assetExtension, options, wrapperGameObject, onAssetLoaded);
					}
					else
					{
						using AssetLoader assetLoader = new AssetLoader();
						assetLoader.OnObjectLoaded += onAssetLoaded;
						assetLoader.LoadFromMemoryWithTextures(data, assetExtension, options, wrapperGameObject);
					}
				}
				catch (Exception ex)
				{
					_error = ex.ToString();
				}
			}
			_unityWebRequest.Dispose();
			_unityWebRequest = null;
		}
	}
}
