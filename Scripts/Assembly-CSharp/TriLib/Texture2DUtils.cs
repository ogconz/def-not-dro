using System;
using System.IO;
using System.Text;
using DevIL;
using JetBrains.Annotations;
using UnityEngine;

namespace TriLib
{
	public static class Texture2DUtils
	{
		public static Texture2D LoadTextureFromFile(string path, int width, int height, string name, ref bool checkAlphaChannel, [CanBeNull] byte[] data, bool isRawData = false, bool isNormalMap = false, string basePath = null, TextureWrapMode textureWrapMode = TextureWrapMode.Repeat, TextureCompression textureCompression = TextureCompression.None)
		{
			string filename = path;
			if (data == null)
			{
				string text = null;
				data = FileUtils.LoadFileData(filename);
				if (data.Length == 0 && basePath != null)
				{
					filename = Path.Combine(basePath, path);
					data = FileUtils.LoadFileData(filename);
				}
				if (data.Length == 0)
				{
					text = FileUtils.GetFilename(path);
					filename = text;
					data = FileUtils.LoadFileData(filename);
				}
				if (data.Length == 0 && basePath != null && text != null)
				{
					filename = Path.Combine(basePath, text);
					data = FileUtils.LoadFileData(filename);
				}
				if (data.Length == 0)
				{
					return null;
				}
			}
			return LoadTextureFromMemory(data, path, ref checkAlphaChannel, textureWrapMode, textureCompression, isNormalMap, isRawData, width, height);
		}

		public static Texture2D LoadTextureFromMemory(byte[] data, string path, ref bool checkAlphaChannel, TextureWrapMode textureWrapMode = TextureWrapMode.Repeat, TextureCompression textureCompression = TextureCompression.None, bool isNormalMap = false, bool isRawData = false, int width = 0, int height = 0)
		{
			if (data.Length == 0 || string.IsNullOrEmpty(path))
			{
				return null;
			}
			checkAlphaChannel = path.StartsWith("a_");
			string extension = Path.GetExtension(path);
			if (extension == ".dds" && LoadTextureDXT(data, out var tempTexture2D) && (bool)tempTexture2D)
			{
				tempTexture2D.name = "@" + StringUtils.GenerateUniqueName(path.GetHashCode());
				return tempTexture2D;
			}
			if (ApplyTextureData(data, isRawData, out tempTexture2D, width, height, extension != ".png"))
			{
				return ProccessTextureData(tempTexture2D, StringUtils.GenerateUniqueName(path.GetHashCode()), textureWrapMode, null, textureCompression, isNormalMap);
			}
			return null;
		}

		public static bool LoadTextureDXT(byte[] ddsBytes, out Texture2D tempTexture2D)
		{
			tempTexture2D = null;
			if (Encoding.ASCII.GetString(ddsBytes, 0, 4) != "DDS ")
			{
				return false;
			}
			string value = Encoding.ASCII.GetString(ddsBytes, 84, 4);
			TextureFormat textureFormat = (TextureFormat)Enum.Parse(typeof(TextureFormat), value);
			if (textureFormat != TextureFormat.DXT1 && textureFormat != TextureFormat.DXT5)
			{
				return false;
			}
			int num = BitConverter.ToInt32(ddsBytes, 4);
			int height = BitConverter.ToInt32(ddsBytes, 12);
			int width = BitConverter.ToInt32(ddsBytes, 16);
			int num2 = num + 4;
			byte[] array = new byte[ddsBytes.Length - num2];
			Buffer.BlockCopy(ddsBytes, num2, array, 0, ddsBytes.Length - num2);
			tempTexture2D = new Texture2D(width, height, textureFormat, mipChain: false);
			tempTexture2D.LoadRawTextureData(array);
			tempTexture2D.Apply();
			return true;
		}

		private static bool ApplyTextureData(byte[] data, bool isRawData, out Texture2D tempTexture2D, int width, int height, bool useDevIL)
		{
			if (data.Length == 0)
			{
				tempTexture2D = null;
				return false;
			}
			if (isRawData)
			{
				try
				{
					tempTexture2D = new Texture2D(width, height, TextureFormat.ARGB32, mipChain: true);
					tempTexture2D.LoadRawTextureData(data);
					tempTexture2D.Apply();
					return true;
				}
				catch
				{
				}
			}
			if (useDevIL)
			{
				return IlLoader.LoadTexture2DFromByteArray(data, data.Length, out tempTexture2D);
			}
			tempTexture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true);
			return tempTexture2D.LoadImage(data);
		}

		private static Texture2D ProccessTextureData(Texture2D tempTexture2D, string name, TextureWrapMode textureWrapMode, string finalPath, TextureCompression textureCompression, bool isNormalMap)
		{
			if (tempTexture2D == null)
			{
				return null;
			}
			tempTexture2D.name = name;
			tempTexture2D.wrapMode = textureWrapMode;
			Texture2D texture2D = tempTexture2D;
			if (isNormalMap)
			{
				Color32[] pixels = tempTexture2D.GetPixels32();
				texture2D = UnityEngine.Object.Instantiate(AssetLoaderBase.NormalBaseTexture);
				texture2D.filterMode = tempTexture2D.filterMode;
				texture2D.wrapMode = tempTexture2D.wrapMode;
				texture2D.Reinitialize(tempTexture2D.width, tempTexture2D.height);
				texture2D.SetPixels32(pixels);
				texture2D.Apply();
			}
			else if (tempTexture2D.format != TextureFormat.DXT5 && tempTexture2D.format != TextureFormat.DXT1)
			{
				if (tempTexture2D.format != TextureFormat.ARGB32)
				{
					Color32[] pixels2 = tempTexture2D.GetPixels32();
					texture2D = new Texture2D(tempTexture2D.width, tempTexture2D.height, TextureFormat.ARGB32, mipChain: true);
					texture2D.SetPixels32(pixels2);
					texture2D.Apply();
				}
				if (textureCompression != TextureCompression.None && IsDXTValidToCompress(texture2D.width, texture2D.height))
				{
					texture2D.Compress(textureCompression == TextureCompression.HighQuality);
				}
			}
			return texture2D;
		}

		private static bool IsDXTValidToCompress(int width, int height)
		{
			if (width % 4 == 0)
			{
				return height % 4 == 0;
			}
			return false;
		}

		public static Texture2D DeCompress(this Texture2D source)
		{
			RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.sRGB);
			Graphics.Blit(source, temporary);
			RenderTexture active = RenderTexture.active;
			RenderTexture.active = temporary;
			Texture2D texture2D = new Texture2D(source.width, source.height, TextureFormat.RGBA32, mipChain: false, linear: false);
			texture2D.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
			texture2D.Apply();
			RenderTexture.active = active;
			RenderTexture.ReleaseTemporary(temporary);
			return texture2D;
		}

		public static RenderTexture FilteredDownscale(RenderTexture source, int width, int height)
		{
			RenderTexture active = RenderTexture.active;
			int num = source.width / 2;
			int num2 = source.height / 2;
			if (num < width || num2 < height)
			{
				num = width;
				num2 = height;
			}
			RenderTexture renderTexture = RenderTexture.GetTemporary(num, num2, 0, RenderTextureFormat.ARGB32);
			Graphics.Blit(source, renderTexture);
			while (num > width && num2 > height)
			{
				num /= 2;
				num2 /= 2;
				if (num < width || num2 < height)
				{
					num = width;
					num2 = height;
				}
				RenderTexture temporary = RenderTexture.GetTemporary(num, num2, 0, RenderTextureFormat.ARGB32);
				Graphics.Blit(renderTexture, temporary);
				RenderTexture renderTexture2 = temporary;
				RenderTexture renderTexture3 = renderTexture;
				renderTexture = renderTexture2;
				temporary = renderTexture3;
				RenderTexture.ReleaseTemporary(temporary);
			}
			RenderTexture.active = active;
			return renderTexture;
		}
	}
}
