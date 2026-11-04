using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TriLib
{
	public class AssetLoaderZip : AssetLoaderBase
	{
		public GameObject LoadFromFile(string filename, AssetLoaderOptions options = null, GameObject wrapperGameObject = null)
		{
			return LoadFileInternal(filename, options, wrapperGameObject) as GameObject;
		}

		public Task LoadFromFileAsync(string filename, AssetLoaderOptions options, GameObject wrapperGameObject, Action<GameObject> onAssetLoaded)
		{
			return (Task)LoadFileInternal(filename, options, wrapperGameObject, onAssetLoaded);
		}

		private static string GetReadableAssetPath(string path)
		{
			string supportedFileExtensions = AssetLoaderBase.GetSupportedFileExtensions();
			string[] files = Directory.GetFiles(path);
			foreach (string text in files)
			{
				string fileExtension = FileUtils.GetFileExtension(text);
				if (supportedFileExtensions.Contains(fileExtension))
				{
					return text;
				}
			}
			files = Directory.GetDirectories(path);
			for (int i = 0; i < files.Length; i++)
			{
				string readableAssetPath = GetReadableAssetPath(files[i]);
				if (readableAssetPath != null)
				{
					return readableAssetPath;
				}
			}
			return null;
		}

		private object LoadFileInternal(string filename, AssetLoaderOptions assetLoaderOptions = null, GameObject wrapperGameObject = null, Action<GameObject> onAssetLoaded = null, bool async = false)
		{
			if (FileUtils.GetFileExtension(filename) == ".zip")
			{
				throw new Exception("Please enable TriLib ZIP loading");
			}
			if (async)
			{
				return AsyncLoadFileInternal(filename, assetLoaderOptions, wrapperGameObject, onAssetLoaded);
			}
			return SyncLoadFileInternal(filename, assetLoaderOptions, wrapperGameObject);
		}

		private object SyncLoadFileInternal(string filename, AssetLoaderOptions options, GameObject wrapperGameObject)
		{
			InternalLoadFromFile(filename, options, wrapperGameObject != null);
			string fileDirectory = FileUtils.GetFileDirectory(filename);
			return BuildGameObject(options, fileDirectory, wrapperGameObject);
		}

		private object AsyncLoadFileInternal(string filename, AssetLoaderOptions options, GameObject wrapperGameObject, Action<GameObject> onAssetLoaded)
		{
			bool usesWrapperGameObject = wrapperGameObject != null;
			return ThreadUtils.RunThread(delegate
			{
				InternalLoadFromFile(filename, options, usesWrapperGameObject);
			}, delegate
			{
				string fileDirectory = FileUtils.GetFileDirectory(filename);
				GameObject obj = BuildGameObject(options, fileDirectory, wrapperGameObject);
				if (onAssetLoaded != null)
				{
					onAssetLoaded(obj);
				}
			});
		}

		private static string GetSha256(string localFilename)
		{
			byte[] array = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(localFilename));
			StringBuilder stringBuilder = new StringBuilder();
			byte[] array2 = array;
			foreach (byte b in array2)
			{
				stringBuilder.Append(b.ToString("x2"));
			}
			return stringBuilder.ToString();
		}
	}
}
