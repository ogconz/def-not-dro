using System;
using System.Threading.Tasks;
using UnityEngine;

namespace TriLib
{
	public class AssetLoaderAsync : AssetLoaderBase
	{
		public Task LoadFromFile(string filename, AssetLoaderOptions options, GameObject wrapperGameObject, Action<GameObject> onAssetLoaded)
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

		public Task LoadFromMemory(byte[] fileBytes, string filename, AssetLoaderOptions options, GameObject wrapperGameObject, ObjectLoadedHandle onAssetLoaded)
		{
			bool usesWrapperGameObject = wrapperGameObject != null;
			return ThreadUtils.RunThread(delegate
			{
				InternalLoadFromMemory(fileBytes, filename, options, usesWrapperGameObject);
			}, delegate
			{
				GameObject loadedGameObject = BuildGameObject(options, filename, wrapperGameObject);
				if (onAssetLoaded != null)
				{
					onAssetLoaded(loadedGameObject);
				}
			});
		}

		public Task LoadFromMemoryWithTextures(byte[] fileData, string assetExtension, AssetLoaderOptions options, GameObject wrapperGameObject, Action<GameObject> onAssetLoaded)
		{
			bool usesWrapperGameObject = wrapperGameObject != null;
			return ThreadUtils.RunThread(delegate
			{
				InternalLoadFromMemoryAndZip(fileData, assetExtension, options, usesWrapperGameObject);
			}, delegate
			{
				GameObject obj = BuildGameObject(options, assetExtension, wrapperGameObject);
				if (onAssetLoaded != null)
				{
					onAssetLoaded(obj);
				}
			});
		}
	}
}
