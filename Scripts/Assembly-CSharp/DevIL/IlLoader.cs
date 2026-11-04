using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DevIL
{
	public static class IlLoader
	{
		public static bool LoadTexture2DFromByteArray(byte[] bytes, int length, out Texture2D texture2D)
		{
			texture2D = null;
			bool result = false;
			try
			{
				IlInterop.ilInit();
				IlInterop.ilEnable(1536);
				IlInterop.ilOriginFunc(1537);
				int Image = IlInterop.ilGenImage();
				IlInterop.ilBindImage(Image);
				if (IlInterop.ilLoadL(0, bytes, length))
				{
					IlInterop.ilConvertImage(6408, 5121);
					int num = IlInterop.ilGetInteger(3556);
					int num2 = IlInterop.ilGetInteger(3557);
					int num3 = num * num2 * 4;
					IntPtr source = IlInterop.ilGetData();
					byte[] array = new byte[num3];
					Marshal.Copy(source, array, 0, num3);
					texture2D = new Texture2D(num, num2, TextureFormat.RGBA32, mipChain: false);
					texture2D.LoadRawTextureData(array);
					texture2D.Apply();
					result = true;
				}
				IlInterop.ilDeleteImages(1, ref Image);
			}
			catch
			{
			}
			return result;
		}
	}
}
