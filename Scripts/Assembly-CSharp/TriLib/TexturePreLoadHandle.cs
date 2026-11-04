using System;
using UnityEngine;

namespace TriLib
{
	public delegate void TexturePreLoadHandle(IntPtr scene, string path, string name, Material material, string propertyName, ref bool checkAlphaChannel, TextureWrapMode textureWrapMode = TextureWrapMode.Repeat, string basePath = null, TextureLoadHandle onTextureLoaded = null, TextureCompression textureCompression = TextureCompression.None, bool isNormalMap = false);
}
