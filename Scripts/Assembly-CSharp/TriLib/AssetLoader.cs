using UnityEngine;

namespace TriLib
{
	public class AssetLoader : AssetLoaderBase
	{
		public GameObject LoadFromFile(string filename, AssetLoaderOptions options = null, GameObject wrapperGameObject = null)
		{
			InternalLoadFromFile(filename, options, wrapperGameObject != null);
			string fileDirectory = FileUtils.GetFileDirectory(filename);
			return BuildGameObject(options, fileDirectory, wrapperGameObject);
		}

		public GameObject LoadFromMemory(byte[] fileBytes, string filename, AssetLoaderOptions options = null, GameObject wrapperGameObject = null)
		{
			InternalLoadFromMemory(fileBytes, filename, options, wrapperGameObject != null);
			string fileDirectory = FileUtils.GetFileDirectory(filename);
			return BuildGameObject(options, fileDirectory, wrapperGameObject);
		}

		public GameObject LoadFromMemoryWithTextures(byte[] fileData, string assetExtension, AssetLoaderOptions options, GameObject wrapperGameObject)
		{
			InternalLoadFromMemoryAndZip(fileData, assetExtension, options, wrapperGameObject != null);
			return BuildGameObject(options, assetExtension, wrapperGameObject);
		}
	}
}
