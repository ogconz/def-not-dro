using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace TriLib
{
	public static class AssimpInterop
	{
		private const string DllPath = "assimp";

		private const int MaxStringLength = 1024;

		private const int MaxInputStringLength = 2048;

		private static readonly bool Is32Bits = IntPtr.Size == 4;

		private static readonly int IntSize = (Is32Bits ? 4 : 8);

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiCreatePropertyStore")]
		private static extern IntPtr _aiCreatePropertyStore();

		public static IntPtr ai_CreatePropertyStore()
		{
			return _aiCreatePropertyStore();
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiReleasePropertyStore")]
		private static extern void _aiReleasePropertyStore(IntPtr ptrPropertyStore);

		public static void ai_CreateReleasePropertyStore(IntPtr ptrPropertyStore)
		{
			_aiReleasePropertyStore(ptrPropertyStore);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiSetImportPropertyInteger")]
		private static extern IntPtr _aiSetImportPropertyInteger(IntPtr ptrStore, IntPtr name, int value);

		public static IntPtr ai_SetImportPropertyInteger(IntPtr ptrStore, string name, int value)
		{
			GCHandle stringBuffer = GetStringBuffer(name);
			IntPtr result = _aiSetImportPropertyInteger(ptrStore, stringBuffer.AddrOfPinnedObject(), value);
			stringBuffer.Free();
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiSetImportPropertyFloat")]
		private static extern IntPtr _aiSetImportPropertyFloat(IntPtr ptrStore, IntPtr name, float value);

		public static IntPtr ai_SetImportPropertyFloat(IntPtr ptrStore, string name, float value)
		{
			GCHandle stringBuffer = GetStringBuffer(name);
			IntPtr result = _aiSetImportPropertyFloat(ptrStore, stringBuffer.AddrOfPinnedObject(), value);
			stringBuffer.Free();
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiSetImportPropertyString")]
		private static extern IntPtr _aiSetImportPropertyString(IntPtr ptrStore, IntPtr name, IntPtr ptrValue);

		public static IntPtr ai_SetImportPropertyString(IntPtr ptrStore, string name, string value)
		{
			GCHandle stringBuffer = GetStringBuffer(name);
			IntPtr assimpStringBuffer = GetAssimpStringBuffer(value);
			IntPtr result = _aiSetImportPropertyString(ptrStore, stringBuffer.AddrOfPinnedObject(), assimpStringBuffer);
			stringBuffer.Free();
			Marshal.FreeHGlobal(assimpStringBuffer);
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiSetImportPropertyMatrix")]
		private static extern IntPtr _aiSetImportPropertyMatrix(IntPtr ptrStore, IntPtr name, IntPtr ptrValue);

		public static IntPtr ai_SetImportPropertyMatrix(IntPtr ptrStore, string name, Vector3 translation, Vector3 rotation, Vector3 scale)
		{
			GCHandle stringBuffer = GetStringBuffer(name);
			GCHandle gCHandle = Matrix4x4ToAssimp(translation, rotation, scale);
			IntPtr result = _aiSetImportPropertyMatrix(ptrStore, stringBuffer.AddrOfPinnedObject(), gCHandle.AddrOfPinnedObject());
			stringBuffer.Free();
			gCHandle.Free();
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiImportFileFromMemory")]
		private static extern IntPtr _aiImportFileFromMemory(IntPtr ptrBuffer, uint uintLength, uint uintFlags, string strHint);

		public static IntPtr ai_ImportFileFromMemory(byte[] fileBytes, uint uintFlags, string strHint)
		{
			GCHandle gCHandle = LockGc(fileBytes);
			IntPtr result = _aiImportFileFromMemory(gCHandle.AddrOfPinnedObject(), (uint)fileBytes.Length, uintFlags, strHint);
			gCHandle.Free();
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiImportFileFromMemoryWithProperties")]
		private static extern IntPtr _aiImportFileFromMemoryWithProperties(IntPtr ptrBuffer, uint uintLength, uint uintFlags, string strHint, IntPtr ptrProps);

		public static IntPtr ai_ImportFileFromMemoryWithProperties(byte[] fileBytes, uint uintFlags, string strHint, IntPtr ptrProps)
		{
			GCHandle gCHandle = LockGc(fileBytes);
			IntPtr result = _aiImportFileFromMemoryWithProperties(gCHandle.AddrOfPinnedObject(), (uint)fileBytes.Length, uintFlags, strHint, ptrProps);
			gCHandle.Free();
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiImportFile")]
		private static extern IntPtr _aiImportFile(string filename, uint flags);

		public static IntPtr ai_ImportFile(string filename, uint flags)
		{
			return _aiImportFile(filename, flags);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiImportFileExWithProperties")]
		private static extern IntPtr _aiImportFileEx(string filename, uint flags, IntPtr ptrFS, IntPtr ptrProps);

		public static IntPtr ai_ImportFileEx(string filename, uint flags, IntPtr ptrFS, IntPtr ptrProp)
		{
			return _aiImportFileEx(filename, flags, ptrFS, ptrProp);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiReleaseImport")]
		private static extern void _aiReleaseImport(IntPtr scene);

		public static void ai_ReleaseImport(IntPtr scene)
		{
			_aiReleaseImport(scene);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiGetExtensionList")]
		private static extern void _aiGetExtensionList(IntPtr ptrExtensionList);

		public static void ai_GetExtensionList(out string strExtensionList)
		{
			byte[] byteArray;
			GCHandle newStringBuffer = GetNewStringBuffer(out byteArray);
			_aiGetExtensionList(newStringBuffer.AddrOfPinnedObject());
			newStringBuffer.Free();
			long num = (Is32Bits ? BitConverter.ToInt32(byteArray, 0) : BitConverter.ToInt64(byteArray, 0));
			strExtensionList = Encoding.UTF8.GetString(byteArray, IntSize, (int)num);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiGetErrorString")]
		private static extern IntPtr _aiGetErrorString();

		public static string ai_GetErrorString()
		{
			return Marshal.PtrToStringAnsi(_aiGetErrorString());
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiIsExtensionSupported")]
		private static extern bool _aiIsExtensionSupported(IntPtr strExtension);

		public static bool ai_IsExtensionSupported(string strExtension)
		{
			GCHandle stringBuffer = GetStringBuffer(strExtension);
			bool result = _aiIsExtensionSupported(stringBuffer.AddrOfPinnedObject());
			stringBuffer.Free();
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_HasMaterials")]
		private static extern bool _aiScene_HasMaterials(IntPtr ptrScene);

		public static bool aiScene_HasMaterials(IntPtr ptrScene)
		{
			return _aiScene_HasMaterials(ptrScene);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_GetNumMaterials")]
		private static extern uint _aiScene_GetNumMaterials(IntPtr ptrScene);

		public static uint aiScene_GetNumMaterials(IntPtr ptrScene)
		{
			return _aiScene_GetNumMaterials(ptrScene);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_GetNumMeshes")]
		private static extern uint _aiScene_GetNumMeshes(IntPtr ptrScene);

		public static uint aiScene_GetNumMeshes(IntPtr ptrScene)
		{
			return _aiScene_GetNumMeshes(ptrScene);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_GetNumAnimations")]
		private static extern uint _aiScene_GetNumAnimations(IntPtr ptrScene);

		public static uint aiScene_GetNumAnimations(IntPtr ptrScene)
		{
			return _aiScene_GetNumAnimations(ptrScene);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_GetNumCameras")]
		private static extern uint _aiScene_GetNumCameras(IntPtr ptrScene);

		public static uint aiScene_GetNumCameras(IntPtr ptrScene)
		{
			return _aiScene_GetNumCameras(ptrScene);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_GetNumLights")]
		private static extern uint _aiScene_GetNumLights(IntPtr ptrScene);

		public static uint aiScene_GetNumLights(IntPtr ptrScene)
		{
			return _aiScene_GetNumLights(ptrScene);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_HasMeshes")]
		private static extern bool _aiScene_HasMeshes(IntPtr ptrScene);

		public static bool aiScene_HasMeshes(IntPtr ptrScene)
		{
			return _aiScene_HasMeshes(ptrScene);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_HasAnimation")]
		private static extern bool _aiScene_HasAnimation(IntPtr ptrScene);

		public static bool aiScene_HasAnimation(IntPtr ptrScene)
		{
			return _aiScene_HasAnimation(ptrScene);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_HasCameras")]
		private static extern bool _aiScene_HasCameras(IntPtr ptrScene);

		public static bool aiScene_HasCameras(IntPtr ptrScene)
		{
			return _aiScene_HasCameras(ptrScene);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_HasLights")]
		private static extern bool _aiScene_HasLights(IntPtr ptrScene);

		public static bool aiScene_HasLights(IntPtr ptrScene)
		{
			return _aiScene_HasLights(ptrScene);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_GetRootNode")]
		private static extern IntPtr _aiScene_GetRootNode(IntPtr ptrScene);

		public static IntPtr aiScene_GetRootNode(IntPtr ptrScene)
		{
			return _aiScene_GetRootNode(ptrScene);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_GetMaterial")]
		private static extern IntPtr _aiScene_GetMaterial(IntPtr ptrScene, uint uintIndex);

		public static IntPtr aiScene_GetMaterial(IntPtr ptrScene, uint uintIndex)
		{
			return _aiScene_GetMaterial(ptrScene, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_GetMesh")]
		private static extern IntPtr _aiScene_GetMesh(IntPtr ptrScene, uint uintIndex);

		public static IntPtr aiScene_GetMesh(IntPtr ptrScene, uint uintIndex)
		{
			return _aiScene_GetMesh(ptrScene, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_GetAnimation")]
		private static extern IntPtr _aiScene_GetAnimation(IntPtr ptrScene, uint uintIndex);

		public static IntPtr aiScene_GetAnimation(IntPtr ptrScene, uint uintIndex)
		{
			return _aiScene_GetAnimation(ptrScene, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_GetCamera")]
		private static extern IntPtr _aiScene_GetCamera(IntPtr ptrScene, uint uintIndex);

		public static IntPtr aiScene_GetCamera(IntPtr ptrScene, uint uintIndex)
		{
			return _aiScene_GetCamera(ptrScene, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_GetLight")]
		private static extern IntPtr _aiScene_GetLight(IntPtr ptrScene, uint uintIndex);

		public static IntPtr aiScene_GetLight(IntPtr ptrScene, uint uintIndex)
		{
			return _aiScene_GetLight(ptrScene, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiNode_GetName")]
		private static extern IntPtr _aiNode_GetName(IntPtr ptrNode);

		public static string aiNode_GetName(IntPtr ptrNode)
		{
			return ReadStringFromPointer(_aiNode_GetName(ptrNode));
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiNode_GetNumChildren")]
		private static extern uint _aiNode_GetNumChildren(IntPtr ptrNode);

		public static uint aiNode_GetNumChildren(IntPtr ptrNode)
		{
			return _aiNode_GetNumChildren(ptrNode);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiNode_GetNumMeshes")]
		private static extern uint _aiNode_GetNumMeshes(IntPtr ptrNode);

		public static uint aiNode_GetNumMeshes(IntPtr ptrNode)
		{
			return _aiNode_GetNumMeshes(ptrNode);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiNode_GetChildren")]
		private static extern IntPtr _aiNode_GetChildren(IntPtr ptrNode, uint uintIndex);

		public static IntPtr aiNode_GetChildren(IntPtr ptrNode, uint uintIndex)
		{
			return _aiNode_GetChildren(ptrNode, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiNode_GetMeshIndex")]
		private static extern uint _aiNode_GetMeshIndex(IntPtr ptrNode, uint uintIndex);

		public static uint aiNode_GetMeshIndex(IntPtr ptrNode, uint uintIndex)
		{
			return _aiNode_GetMeshIndex(ptrNode, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiNode_GetParent")]
		private static extern IntPtr _aiNode_GetParent(IntPtr ptrNode);

		public static IntPtr aiNode_GetParent(IntPtr ptrNode)
		{
			return _aiNode_GetParent(ptrNode);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiNode_GetTransformation")]
		private static extern IntPtr _aiNode_GetTransformation(IntPtr ptrNode);

		public static Matrix4x4 aiNode_GetTransformation(IntPtr ptrNode)
		{
			return GetNewMatrix4x4(_aiNode_GetTransformation(ptrNode));
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_IsEmbeddedTextureCompressed")]
		private static extern bool _aiMaterial_IsEmbeddedTextureCompressed(IntPtr ptrScene, IntPtr ptrTexture);

		public static bool aiMaterial_IsEmbeddedTextureCompressed(IntPtr ptrScene, IntPtr ptrTexture)
		{
			return _aiMaterial_IsEmbeddedTextureCompressed(ptrScene, ptrTexture);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetEmbeddedTextureDataSize")]
		private static extern uint _aiMaterial_GetEmbeddedTextureDataSize(IntPtr ptrScene, IntPtr ptrTexture, bool boolCompressed);

		public static uint aiMaterial_GetEmbeddedTextureDataSize(IntPtr ptrScene, IntPtr ptrTexture, bool boolCompressed)
		{
			return _aiMaterial_GetEmbeddedTextureDataSize(ptrScene, ptrTexture, boolCompressed);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetEmbeddedTextureData")]
		private static extern void _aiMaterial_GetEmbeddedTextureData(IntPtr ptrScene, IntPtr ptrData, IntPtr ptrTexture, uint uintSize);

		public static byte[] aiMaterial_GetEmbeddedTextureData(IntPtr ptrScene, IntPtr ptrTexture, uint uintSize)
		{
			byte[] array = new byte[uintSize];
			GCHandle gCHandle = LockGc(array);
			_aiMaterial_GetEmbeddedTextureData(ptrScene, gCHandle.AddrOfPinnedObject(), ptrTexture, uintSize);
			gCHandle.Free();
			return array;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetEmbeddedTextureWidth")]
		private static extern int _aiMaterial_GetEmbeddedTextureWidth(IntPtr ptrTexture);

		public static int aiMaterial_GetEmbeddedTextureWidth(IntPtr ptrTexture)
		{
			return _aiMaterial_GetEmbeddedTextureWidth(ptrTexture);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetEmbeddedTextureHeight")]
		private static extern int _aiMaterial_GetEmbeddedTextureHeight(IntPtr ptrTexture);

		public static int aiMaterial_GetEmbeddedTextureHeight(IntPtr ptrTexture)
		{
			return _aiMaterial_GetEmbeddedTextureHeight(ptrTexture);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_GetEmbeddedTexture")]
		private static extern IntPtr _aiScene_GetEmbeddedTexture(IntPtr ptrScene, string strFilename);

		public static IntPtr aiScene_GetEmbeddedTexture(IntPtr ptrScene, string strFilename)
		{
			return _aiScene_GetEmbeddedTexture(ptrScene, strFilename);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetTextureCount")]
		private static extern uint _aiMaterial_GetTextureCount(IntPtr ptrMat, uint uintType);

		public static uint aiMaterial_GetTextureCount(IntPtr ptrMat, uint uintType)
		{
			return _aiMaterial_GetTextureCount(ptrMat, uintType);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_HasTextureDiffuse")]
		private static extern bool _aiMaterial_HasTextureDiffuse(IntPtr ptrMat, uint uintType);

		public static bool aiMaterial_HasTextureDiffuse(IntPtr ptrMat, uint uintType)
		{
			return _aiMaterial_HasTextureDiffuse(ptrMat, uintType);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetTextureDiffuse")]
		private static extern bool _aiMaterial_GetTextureDiffuse(IntPtr ptrMat, uint uintType, IntPtr strPath, IntPtr uintMapping, IntPtr uintUvIndex, IntPtr floatBlend, IntPtr uintOp, IntPtr uintMapMode);

		public static bool aiMaterial_GetTextureDiffuse(IntPtr ptrMat, uint uintType, out string strPath, out uint uintMapping, out uint uintUvIndex, out float floatBlend, out uint uintOp, out uint uintMapMode)
		{
			byte[] byteArray;
			GCHandle newStringBuffer = GetNewStringBuffer(out byteArray);
			uintMapping = 0u;
			GCHandle gCHandle = LockGc(uintMapping);
			uintUvIndex = 0u;
			GCHandle gCHandle2 = LockGc(uintUvIndex);
			floatBlend = 0f;
			GCHandle gCHandle3 = LockGc(floatBlend);
			uintOp = 0u;
			GCHandle gCHandle4 = LockGc(uintOp);
			uintMapMode = 0u;
			GCHandle gCHandle5 = LockGc(uintMapMode);
			bool result = _aiMaterial_GetTextureDiffuse(ptrMat, uintType, newStringBuffer.AddrOfPinnedObject(), gCHandle.AddrOfPinnedObject(), gCHandle2.AddrOfPinnedObject(), gCHandle3.AddrOfPinnedObject(), gCHandle4.AddrOfPinnedObject(), gCHandle5.AddrOfPinnedObject());
			strPath = ByteArrayToString(byteArray);
			newStringBuffer.Free();
			gCHandle.Free();
			gCHandle2.Free();
			gCHandle3.Free();
			gCHandle4.Free();
			gCHandle5.Free();
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetNumTextureDiffuse")]
		private static extern uint _aiMaterial_GetNumTextureDiffuse(IntPtr ptrMat);

		public static uint aiMaterial_GetNumTextureDiffuse(IntPtr ptrMat)
		{
			return _aiMaterial_GetNumTextureDiffuse(ptrMat);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_HasTextureEmissive")]
		private static extern bool _aiMaterial_HasTextureEmissive(IntPtr ptrMat, uint uintIndex);

		public static bool aiMaterial_HasTextureEmissive(IntPtr ptrMat, uint uintIndex)
		{
			return _aiMaterial_HasTextureEmissive(ptrMat, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetTextureEmissive")]
		private static extern bool _aiMaterial_GetTextureEmissive(IntPtr ptrMat, uint uintIndex, IntPtr strPath, IntPtr uintMapping, IntPtr uintUvIndex, IntPtr floatBlend, IntPtr uintOp, IntPtr uintMapMode);

		public static bool aiMaterial_GetTextureEmissive(IntPtr ptrMat, uint uintIndex, out string strPath, out uint uintMapping, out uint uintUvIndex, out float floatBlend, out uint uintOp, out uint uintMapMode)
		{
			byte[] byteArray;
			GCHandle newStringBuffer = GetNewStringBuffer(out byteArray);
			uintMapping = 0u;
			GCHandle gCHandle = LockGc(uintMapping);
			uintUvIndex = 0u;
			GCHandle gCHandle2 = LockGc(uintUvIndex);
			floatBlend = 0f;
			GCHandle gCHandle3 = LockGc(floatBlend);
			uintOp = 0u;
			GCHandle gCHandle4 = LockGc(uintOp);
			uintMapMode = 0u;
			GCHandle gCHandle5 = LockGc(uintMapMode);
			bool result = _aiMaterial_GetTextureEmissive(ptrMat, uintIndex, newStringBuffer.AddrOfPinnedObject(), gCHandle.AddrOfPinnedObject(), gCHandle2.AddrOfPinnedObject(), gCHandle3.AddrOfPinnedObject(), gCHandle4.AddrOfPinnedObject(), gCHandle5.AddrOfPinnedObject());
			strPath = ByteArrayToString(byteArray);
			newStringBuffer.Free();
			gCHandle.Free();
			gCHandle2.Free();
			gCHandle3.Free();
			gCHandle4.Free();
			gCHandle5.Free();
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetNumTextureEmissive")]
		private static extern uint _aiMaterial_GetNumTextureEmissive(IntPtr ptrMat);

		public static uint aiMaterial_GetNumTextureEmissive(IntPtr ptrMat)
		{
			return _aiMaterial_GetNumTextureEmissive(ptrMat);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_HasTextureSpecular")]
		private static extern bool _aiMaterial_HasTextureSpecular(IntPtr ptrMat, uint uintIndex);

		public static bool aiMaterial_HasTextureSpecular(IntPtr ptrMat, uint uintIndex)
		{
			return _aiMaterial_HasTextureSpecular(ptrMat, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetTextureSpecular")]
		private static extern bool _aiMaterial_GetTextureSpecular(IntPtr ptrMat, uint uintIndex, IntPtr strPath, IntPtr uintMapping, IntPtr uintUvIndex, IntPtr floatBlend, IntPtr uintOp, IntPtr uintMapMode);

		public static bool aiMaterial_GetTextureSpecular(IntPtr ptrMat, uint uintIndex, out string strPath, out uint uintMapping, out uint uintUvIndex, out float floatBlend, out uint uintOp, out uint uintMapMode)
		{
			byte[] byteArray;
			GCHandle newStringBuffer = GetNewStringBuffer(out byteArray);
			uintMapping = 0u;
			GCHandle gCHandle = LockGc(uintMapping);
			uintUvIndex = 0u;
			GCHandle gCHandle2 = LockGc(uintUvIndex);
			floatBlend = 0f;
			GCHandle gCHandle3 = LockGc(floatBlend);
			uintOp = 0u;
			GCHandle gCHandle4 = LockGc(uintOp);
			uintMapMode = 0u;
			GCHandle gCHandle5 = LockGc(uintMapMode);
			bool result = _aiMaterial_GetTextureSpecular(ptrMat, uintIndex, newStringBuffer.AddrOfPinnedObject(), gCHandle.AddrOfPinnedObject(), gCHandle2.AddrOfPinnedObject(), gCHandle3.AddrOfPinnedObject(), gCHandle4.AddrOfPinnedObject(), gCHandle5.AddrOfPinnedObject());
			strPath = ByteArrayToString(byteArray);
			newStringBuffer.Free();
			gCHandle.Free();
			gCHandle2.Free();
			gCHandle3.Free();
			gCHandle4.Free();
			gCHandle5.Free();
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetNumTextureSpecular")]
		private static extern uint _aiMaterial_GetNumTextureSpecular(IntPtr ptrMat);

		public static uint aiMaterial_GetNumTextureSpecular(IntPtr ptrMat)
		{
			return _aiMaterial_GetNumTextureSpecular(ptrMat);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_HasTextureNormals")]
		private static extern bool _aiMaterial_HasTextureNormals(IntPtr ptrMat, uint uintIndex);

		public static bool aiMaterial_HasTextureNormals(IntPtr ptrMat, uint uintIndex)
		{
			return _aiMaterial_HasTextureNormals(ptrMat, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetTextureNormals")]
		private static extern bool _aiMaterial_GetTextureNormals(IntPtr ptrMat, uint uintIndex, IntPtr strPath, IntPtr uintMapping, IntPtr uintUvIndex, IntPtr floatBlend, IntPtr uintOp, IntPtr uintMapMode);

		public static bool aiMaterial_GetTextureNormals(IntPtr ptrMat, uint uintIndex, out string strPath, out uint uintMapping, out uint uintUvIndex, out float floatBlend, out uint uintOp, out uint uintMapMode)
		{
			byte[] byteArray;
			GCHandle newStringBuffer = GetNewStringBuffer(out byteArray);
			uintMapping = 0u;
			GCHandle gCHandle = LockGc(uintMapping);
			uintUvIndex = 0u;
			GCHandle gCHandle2 = LockGc(uintUvIndex);
			floatBlend = 0f;
			GCHandle gCHandle3 = LockGc(floatBlend);
			uintOp = 0u;
			GCHandle gCHandle4 = LockGc(uintOp);
			uintMapMode = 0u;
			GCHandle gCHandle5 = LockGc(uintMapMode);
			bool result = _aiMaterial_GetTextureNormals(ptrMat, uintIndex, newStringBuffer.AddrOfPinnedObject(), gCHandle.AddrOfPinnedObject(), gCHandle2.AddrOfPinnedObject(), gCHandle3.AddrOfPinnedObject(), gCHandle4.AddrOfPinnedObject(), gCHandle5.AddrOfPinnedObject());
			strPath = ByteArrayToString(byteArray);
			newStringBuffer.Free();
			gCHandle.Free();
			gCHandle2.Free();
			gCHandle3.Free();
			gCHandle4.Free();
			gCHandle5.Free();
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetNumTextureNormals")]
		private static extern uint _aiMaterial_GetNumTextureNormals(IntPtr ptrMat);

		public static uint aiMaterial_GetNumTextureNormals(IntPtr ptrMat)
		{
			return _aiMaterial_GetNumTextureNormals(ptrMat);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_HasTextureHeight")]
		private static extern bool _aiMaterial_HasTextureHeight(IntPtr ptrMat, uint uintIndex);

		public static bool aiMaterial_HasTextureHeight(IntPtr ptrMat, uint uintIndex)
		{
			return _aiMaterial_HasTextureHeight(ptrMat, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetTextureHeight")]
		private static extern bool _aiMaterial_GetTextureHeight(IntPtr ptrMat, uint uintIndex, IntPtr strPath, IntPtr uintMapping, IntPtr uintUvIndex, IntPtr floatBlend, IntPtr uintOp, IntPtr uintMapMode);

		public static bool aiMaterial_GetTextureHeight(IntPtr ptrMat, uint uintIndex, out string strPath, out uint uintMapping, out uint uintUvIndex, out float floatBlend, out uint uintOp, out uint uintMapMode)
		{
			byte[] byteArray;
			GCHandle newStringBuffer = GetNewStringBuffer(out byteArray);
			uintMapping = 0u;
			GCHandle gCHandle = LockGc(uintMapping);
			uintUvIndex = 0u;
			GCHandle gCHandle2 = LockGc(uintUvIndex);
			floatBlend = 0f;
			GCHandle gCHandle3 = LockGc(floatBlend);
			uintOp = 0u;
			GCHandle gCHandle4 = LockGc(uintOp);
			uintMapMode = 0u;
			GCHandle gCHandle5 = LockGc(uintMapMode);
			bool result = _aiMaterial_GetTextureHeight(ptrMat, uintIndex, newStringBuffer.AddrOfPinnedObject(), gCHandle.AddrOfPinnedObject(), gCHandle2.AddrOfPinnedObject(), gCHandle3.AddrOfPinnedObject(), gCHandle4.AddrOfPinnedObject(), gCHandle5.AddrOfPinnedObject());
			strPath = ByteArrayToString(byteArray);
			newStringBuffer.Free();
			gCHandle.Free();
			gCHandle2.Free();
			gCHandle3.Free();
			gCHandle4.Free();
			gCHandle5.Free();
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetNumTextureHeight")]
		private static extern uint _aiMaterial_GetNumTextureHeight(IntPtr ptrMat);

		public static uint aiMaterial_GetNumTextureHeight(IntPtr ptrMat)
		{
			return _aiMaterial_GetNumTextureHeight(ptrMat);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_HasAmbient")]
		private static extern bool _aiMaterial_HasAmbient(IntPtr ptrMat);

		public static bool aiMaterial_HasAmbient(IntPtr ptrMat)
		{
			return _aiMaterial_HasAmbient(ptrMat);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetAmbient")]
		private static extern bool _aiMaterial_GetAmbient(IntPtr ptrMat, IntPtr colorOut);

		public static bool aiMaterial_GetAmbient(IntPtr ptrMat, out Color colorOut)
		{
			IntPtr intPtr = AllocHGlobal<Color>();
			bool result = _aiMaterial_GetAmbient(ptrMat, intPtr);
			colorOut = ReadStruct<Color>(intPtr);
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_HasDiffuse")]
		private static extern bool _aiMaterial_HasDiffuse(IntPtr ptrMat);

		public static bool aiMaterial_HasDiffuse(IntPtr ptrMat)
		{
			return _aiMaterial_HasDiffuse(ptrMat);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetDiffuse")]
		private static extern bool _aiMaterial_GetDiffuse(IntPtr ptrMat, IntPtr colorOut);

		public static bool aiMaterial_GetDiffuse(IntPtr ptrMat, out Color colorOut)
		{
			IntPtr intPtr = AllocHGlobal<Color>();
			bool result = _aiMaterial_GetDiffuse(ptrMat, intPtr);
			colorOut = ReadStruct<Color>(intPtr);
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_HasSpecular")]
		private static extern bool _aiMaterial_HasSpecular(IntPtr ptrMat);

		public static bool aiMaterial_HasSpecular(IntPtr ptrMat)
		{
			return _aiMaterial_HasSpecular(ptrMat);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetSpecular")]
		private static extern bool _aiMaterial_GetSpecular(IntPtr ptrMat, IntPtr colorOut);

		public static bool aiMaterial_GetSpecular(IntPtr ptrMat, out Color colorOut)
		{
			IntPtr intPtr = AllocHGlobal<Color>();
			bool result = _aiMaterial_GetSpecular(ptrMat, intPtr);
			colorOut = ReadStruct<Color>(intPtr);
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_HasEmissive")]
		private static extern bool _aiMaterial_HasEmissive(IntPtr ptrMat);

		public static bool aiMaterial_HasEmissive(IntPtr ptrMat)
		{
			return _aiMaterial_HasEmissive(ptrMat);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetEmissive")]
		private static extern bool _aiMaterial_GetEmissive(IntPtr ptrMat, IntPtr colorOut);

		public static bool aiMaterial_GetEmissive(IntPtr ptrMat, out Color colorOut)
		{
			IntPtr intPtr = AllocHGlobal<Color>();
			bool result = _aiMaterial_GetEmissive(ptrMat, intPtr);
			colorOut = ReadStruct<Color>(intPtr);
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_HasName")]
		private static extern bool _aiMaterial_HasName(IntPtr ptrMat);

		public static bool aiMaterial_HasName(IntPtr ptrMat)
		{
			return _aiMaterial_HasName(ptrMat);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetName")]
		private static extern bool _aiMaterial_GetName(IntPtr ptrMat, IntPtr strName);

		public static bool aiMaterial_GetName(IntPtr ptrMat, out string strName)
		{
			byte[] byteArray;
			GCHandle newStringBuffer = GetNewStringBuffer(out byteArray);
			bool result = _aiMaterial_GetName(ptrMat, newStringBuffer.AddrOfPinnedObject());
			strName = ByteArrayToString(byteArray);
			newStringBuffer.Free();
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_HasBumpScaling")]
		private static extern bool _aiMaterial_HasBumpScaling(IntPtr ptrMat);

		public static bool aiMaterial_HasBumpScaling(IntPtr ptrMat)
		{
			return _aiMaterial_HasBumpScaling(ptrMat);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetBumpScaling")]
		private static extern bool _aiMaterial_GetBumpScaling(IntPtr ptrMat, IntPtr floatOut);

		public static bool aiMaterial_GetBumpScaling(IntPtr ptrMat, out float floatOut)
		{
			floatOut = 1f;
			GCHandle gCHandle = LockGc(floatOut);
			bool result = _aiMaterial_GetBumpScaling(ptrMat, gCHandle.AddrOfPinnedObject());
			gCHandle.Free();
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_HasShininess")]
		private static extern bool _aiMaterial_HasShininess(IntPtr ptrMat);

		public static bool aiMaterial_HasShininess(IntPtr ptrMat)
		{
			return _aiMaterial_HasShininess(ptrMat);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetShininess")]
		private static extern bool _aiMaterial_GetShininess(IntPtr ptrMat, IntPtr floatOut);

		public static bool aiMaterial_GetShininess(IntPtr ptrMat, out float floatOut)
		{
			floatOut = 0f;
			GCHandle gCHandle = LockGc(floatOut);
			bool result = _aiMaterial_GetShininess(ptrMat, gCHandle.AddrOfPinnedObject());
			gCHandle.Free();
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_HasShininessStrength")]
		private static extern bool _aiMaterial_HasShininessStrength(IntPtr ptrMat);

		public static bool aiMaterial_HasShininessStrength(IntPtr ptrMat)
		{
			return _aiMaterial_HasShininessStrength(ptrMat);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetShininessStrength")]
		private static extern bool _aiMaterial_GetShininessStrength(IntPtr ptrMat, IntPtr floatOut);

		public static bool aiMaterial_GetShininessStrength(IntPtr ptrMat, out float floatOut)
		{
			floatOut = 0f;
			GCHandle gCHandle = LockGc(floatOut);
			bool result = _aiMaterial_GetShininessStrength(ptrMat, gCHandle.AddrOfPinnedObject());
			gCHandle.Free();
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_HasOpacity")]
		private static extern bool _aiMaterial_HasOpacity(IntPtr ptrMat);

		public static bool aiMaterial_HasOpacity(IntPtr ptrMat)
		{
			return _aiMaterial_HasOpacity(ptrMat);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMaterial_GetOpacity")]
		private static extern bool _aiMaterial_GetOpacity(IntPtr ptrMat, IntPtr floatOut);

		public static bool aiMaterial_GetOpacity(IntPtr ptrMat, out float floatOut)
		{
			floatOut = 1f;
			GCHandle gCHandle = LockGc(floatOut);
			bool result = _aiMaterial_GetOpacity(ptrMat, gCHandle.AddrOfPinnedObject());
			float[] array = new float[1];
			Marshal.Copy(gCHandle.AddrOfPinnedObject(), array, 0, 1);
			floatOut = array[0];
			gCHandle.Free();
			return result;
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_VertexCount")]
		private static extern uint _aiMesh_VertexCount(IntPtr ptrMesh);

		public static uint aiMesh_VertexCount(IntPtr ptrMesh)
		{
			return _aiMesh_VertexCount(ptrMesh);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_HasNormals")]
		private static extern bool _aiMesh_HasNormals(IntPtr ptrMesh);

		public static bool aiMesh_HasNormals(IntPtr ptrMesh)
		{
			return _aiMesh_HasNormals(ptrMesh);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_HasTangentsAndBitangents")]
		private static extern bool _aiMesh_HasTangentsAndBitangents(IntPtr ptrMesh);

		public static bool aiMesh_HasTangentsAndBitangents(IntPtr ptrMesh)
		{
			return _aiMesh_HasTangentsAndBitangents(ptrMesh);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_HasTextureCoords")]
		private static extern bool _aiMesh_HasTextureCoords(IntPtr ptrMesh, uint uintIndex);

		public static bool aiMesh_HasTextureCoords(IntPtr ptrMesh, uint uintIndex)
		{
			return _aiMesh_HasTextureCoords(ptrMesh, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_HasVertexColors")]
		private static extern bool _aiMesh_HasVertexColors(IntPtr ptrMesh, uint uintIndex);

		public static bool aiMesh_HasVertexColors(IntPtr ptrMesh, uint uintIndex)
		{
			return _aiMesh_HasVertexColors(ptrMesh, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_GetVertex")]
		private static extern IntPtr _aiMesh_GetVertex(IntPtr ptrMesh, uint uintIndex);

		public static Vector3 aiMesh_GetVertex(IntPtr ptrMesh, uint uintIndex)
		{
			return ReadStruct<Vector3>(_aiMesh_GetVertex(ptrMesh, uintIndex), dealloc: false);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_GetNormal")]
		private static extern IntPtr _aiMesh_GetNormal(IntPtr ptrMesh, uint uintIndex);

		public static Vector3 aiMesh_GetNormal(IntPtr ptrMesh, uint uintIndex)
		{
			return ReadStruct<Vector3>(_aiMesh_GetNormal(ptrMesh, uintIndex), dealloc: false);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_GetTangent")]
		private static extern IntPtr _aiMesh_GetTangent(IntPtr ptrMesh, uint uintIndex);

		public static Vector3 aiMesh_GetTangent(IntPtr ptrMesh, uint uintIndex)
		{
			return ReadStruct<Vector3>(_aiMesh_GetTangent(ptrMesh, uintIndex), dealloc: false);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_GetBitangent")]
		private static extern IntPtr _aiMesh_GetBitangent(IntPtr ptrMesh, uint uintIndex);

		public static Vector3 aiMesh_GetBitangent(IntPtr ptrMesh, uint uintIndex)
		{
			return ReadStruct<Vector3>(_aiMesh_GetBitangent(ptrMesh, uintIndex), dealloc: false);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_GetTextureCoord")]
		private static extern IntPtr _aiMesh_GetTextureCoord(IntPtr ptrMesh, uint uintChannel, uint uintIndex);

		public static Vector2 aiMesh_GetTextureCoord(IntPtr ptrMesh, uint uintChannel, uint uintIndex)
		{
			return ReadStruct<Vector2>(_aiMesh_GetTextureCoord(ptrMesh, uintChannel, uintIndex), dealloc: false);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_GetVertexColor")]
		private static extern IntPtr _aiMesh_GetVertexColor(IntPtr ptrMesh, uint uintChannel, uint uintIndex);

		public static Color aiMesh_GetVertexColor(IntPtr ptrMesh, uint uintChannel, uint uintIndex)
		{
			return ReadStruct<Color>(_aiMesh_GetVertexColor(ptrMesh, uintChannel, uintIndex), dealloc: false);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_GetMatrialIndex")]
		private static extern uint _aiMesh_GetMatrialIndex(IntPtr ptrMesh);

		public static uint aiMesh_GetMatrialIndex(IntPtr ptrMesh)
		{
			return _aiMesh_GetMatrialIndex(ptrMesh);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_GetName")]
		private static extern IntPtr _aiMesh_GetName(IntPtr ptrMesh);

		public static string aiMesh_GetName(IntPtr ptrMesh)
		{
			return ReadStringFromPointer(_aiMesh_GetName(ptrMesh));
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_HasFaces")]
		private static extern bool _aiMesh_HasFaces(IntPtr ptrMesh);

		public static bool aiMesh_HasFaces(IntPtr ptrMesh)
		{
			return _aiMesh_HasFaces(ptrMesh);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_GetNumFaces")]
		private static extern uint _aiMesh_GetNumFaces(IntPtr ptrMesh);

		public static uint aiMesh_GetNumFaces(IntPtr ptrMesh)
		{
			return _aiMesh_GetNumFaces(ptrMesh);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_GetFace")]
		private static extern IntPtr _aiMesh_GetFace(IntPtr ptrMesh, uint uintIndex);

		public static IntPtr aiMesh_GetFace(IntPtr ptrMesh, uint uintIndex)
		{
			return _aiMesh_GetFace(ptrMesh, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_HasBones")]
		private static extern bool _aiMesh_HasBones(IntPtr ptrMesh);

		public static bool aiMesh_HasBones(IntPtr ptrMesh)
		{
			return _aiMesh_HasBones(ptrMesh);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_GetNumBones")]
		private static extern uint _aiMesh_GetNumBones(IntPtr ptrMesh);

		public static uint aiMesh_GetNumBones(IntPtr ptrMesh)
		{
			return _aiMesh_GetNumBones(ptrMesh);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiMesh_GetBone")]
		private static extern IntPtr _aiMesh_GetBone(IntPtr ptrMesh, uint uintIndex);

		public static IntPtr aiMesh_GetBone(IntPtr ptrMesh, uint uintIndex)
		{
			return _aiMesh_GetBone(ptrMesh, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiFace_GetNumIndices")]
		private static extern uint _aiFace_GetNumIndices(IntPtr ptrFace);

		public static uint aiFace_GetNumIndices(IntPtr ptrFace)
		{
			return _aiFace_GetNumIndices(ptrFace);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiFace_GetIndex")]
		private static extern uint _aiFace_GetIndex(IntPtr ptrFace, uint uintIndex);

		public static uint aiFace_GetIndex(IntPtr ptrFace, uint uintIndex)
		{
			return _aiFace_GetIndex(ptrFace, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiBone_GetName")]
		private static extern IntPtr _aiBone_GetName(IntPtr ptrBone);

		public static string aiBone_GetName(IntPtr ptrBone)
		{
			return ReadStringFromPointer(_aiBone_GetName(ptrBone));
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiBone_GetNumWeights")]
		private static extern uint _aiBone_GetNumWeights(IntPtr ptrBone);

		public static uint aiBone_GetNumWeights(IntPtr ptrBone)
		{
			return _aiBone_GetNumWeights(ptrBone);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiBone_GetWeights")]
		private static extern IntPtr _aiBone_GetWeights(IntPtr ptrBone, uint uintIndex);

		public static IntPtr aiBone_GetWeights(IntPtr ptrBone, uint uintIndex)
		{
			return _aiBone_GetWeights(ptrBone, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiBone_GetOffsetMatrix")]
		private static extern IntPtr _aiBone_GetOffsetMatrix(IntPtr ptrBone);

		public static Matrix4x4 aiBone_GetOffsetMatrix(IntPtr ptrBone)
		{
			return GetNewMatrix4x4(_aiBone_GetOffsetMatrix(ptrBone));
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiVertexWeight_GetWeight")]
		private static extern float _aiVertexWeight_GetWeight(IntPtr ptrVweight);

		public static float aiVertexWeight_GetWeight(IntPtr ptrVweight)
		{
			return _aiVertexWeight_GetWeight(ptrVweight);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiVertexWeight_GetVertexId")]
		private static extern uint _aiVertexWeight_GetVertexId(IntPtr ptrVweight);

		public static uint aiVertexWeight_GetVertexId(IntPtr ptrVweight)
		{
			return _aiVertexWeight_GetVertexId(ptrVweight);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiAnimation_GetName")]
		private static extern IntPtr _aiAnimation_GetName(IntPtr ptrAnimation);

		public static string aiAnimation_GetName(IntPtr ptrAnimation)
		{
			return ReadStringFromPointer(_aiAnimation_GetName(ptrAnimation));
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiAnimation_GetDuraction")]
		private static extern float _aiAnimation_GetDuraction(IntPtr ptrAnimation);

		public static float aiAnimation_GetDuraction(IntPtr ptrAnimation)
		{
			return _aiAnimation_GetDuraction(ptrAnimation);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiAnimation_GetTicksPerSecond")]
		private static extern float _aiAnimation_GetTicksPerSecond(IntPtr ptrAnimation);

		public static float aiAnimation_GetTicksPerSecond(IntPtr ptrAnimation)
		{
			return _aiAnimation_GetTicksPerSecond(ptrAnimation);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiAnimation_GetNumChannels")]
		private static extern uint _aiAnimation_GetNumChannels(IntPtr ptrAnimation);

		public static uint aiAnimation_GetNumChannels(IntPtr ptrAnimation)
		{
			return _aiAnimation_GetNumChannels(ptrAnimation);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiAnimation_GetNumMorphChannels")]
		private static extern uint _aiAnimation_GetNumMorphChannels(IntPtr ptrAnimation);

		public static uint aiAnimation_GetNumMorphChannels(IntPtr ptrAnimation)
		{
			return _aiAnimation_GetNumMorphChannels(ptrAnimation);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiAnimation_GetNumMeshChannels")]
		private static extern uint _aiAnimation_GetNumMeshChannels(IntPtr ptrAnimation);

		public static uint aiAnimation_GetNumMeshChannels(IntPtr ptrAnimation)
		{
			return _aiAnimation_GetNumMeshChannels(ptrAnimation);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiAnimation_GetAnimationChannel")]
		private static extern IntPtr _aiAnimation_GetAnimationChannel(IntPtr ptrAnimation, uint uintIndex);

		public static IntPtr aiAnimation_GetAnimationChannel(IntPtr ptrAnimation, uint uintIndex)
		{
			return _aiAnimation_GetAnimationChannel(ptrAnimation, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiNodeAnim_GetNodeName")]
		private static extern IntPtr _aiNodeAnim_GetNodeName(IntPtr ptrNodeAnim);

		public static string aiNodeAnim_GetNodeName(IntPtr ptrNodeAnim)
		{
			return ReadStringFromPointer(_aiNodeAnim_GetNodeName(ptrNodeAnim));
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiNodeAnim_GetNumPositionKeys")]
		private static extern uint _aiNodeAnim_GetNumPositionKeys(IntPtr ptrNodeAnim);

		public static uint aiNodeAnim_GetNumPositionKeys(IntPtr ptrNodeAnim)
		{
			return _aiNodeAnim_GetNumPositionKeys(ptrNodeAnim);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiNodeAnim_GetNumRotationKeys")]
		private static extern uint _aiNodeAnim_GetNumRotationKeys(IntPtr ptrNodeAnim);

		public static uint aiNodeAnim_GetNumRotationKeys(IntPtr ptrNodeAnim)
		{
			return _aiNodeAnim_GetNumRotationKeys(ptrNodeAnim);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiNodeAnim_GetNumScalingKeys")]
		private static extern uint _aiNodeAnim_GetNumScalingKeys(IntPtr ptrNodeAnim);

		public static uint aiNodeAnim_GetNumScalingKeys(IntPtr ptrNodeAnim)
		{
			return _aiNodeAnim_GetNumScalingKeys(ptrNodeAnim);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiNodeAnim_GetPostState")]
		private static extern uint _aiNodeAnim_GetPostState(IntPtr ptrNodeAnim);

		public static uint aiNodeAnim_GetPostState(IntPtr ptrNodeAnim)
		{
			return _aiNodeAnim_GetPostState(ptrNodeAnim);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiNodeAnim_GetPreState")]
		private static extern uint _aiNodeAnim_GetPreState(IntPtr ptrNodeAnim);

		public static uint aiNodeAnim_GetPreState(IntPtr ptrNodeAnim)
		{
			return _aiNodeAnim_GetPreState(ptrNodeAnim);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiNodeAnim_GetPositionKey")]
		private static extern IntPtr _aiNodeAnim_GetPositionKey(IntPtr ptrNodeAnim, uint uintIndex);

		public static IntPtr aiNodeAnim_GetPositionKey(IntPtr ptrNodeAnim, uint uintIndex)
		{
			return _aiNodeAnim_GetPositionKey(ptrNodeAnim, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiNodeAnim_GetRotationKey")]
		private static extern IntPtr _aiNodeAnim_GetRotationKey(IntPtr ptrNodeAnim, uint uintIndex);

		public static IntPtr aiNodeAnim_GetRotationKey(IntPtr ptrNodeAnim, uint uintIndex)
		{
			return _aiNodeAnim_GetRotationKey(ptrNodeAnim, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiNodeAnim_GetScalingKey")]
		private static extern IntPtr _aiNodeAnim_GetScalingKey(IntPtr ptrNodeAnim, uint uintIndex);

		public static IntPtr aiNodeAnim_GetScalingKey(IntPtr ptrNodeAnim, uint uintIndex)
		{
			return _aiNodeAnim_GetScalingKey(ptrNodeAnim, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiVectorKey_GetTime")]
		private static extern float _aiVectorKey_GetTime(IntPtr ptrVectorKey);

		public static float aiVectorKey_GetTime(IntPtr ptrVectorKey)
		{
			return _aiVectorKey_GetTime(ptrVectorKey);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiVectorKey_GetValue")]
		private static extern IntPtr _aiVectorKey_GetValue(IntPtr ptrVectorKey);

		public static float[] aiVectorKey_GetValue(IntPtr ptrVectorKey)
		{
			return ReadFloatArray(_aiVectorKey_GetValue(ptrVectorKey), 3);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiQuatKey_GetTime")]
		private static extern float _aiQuatKey_GetTime(IntPtr ptrQuatKey);

		public static float aiQuatKey_GetTime(IntPtr ptrQuatKey)
		{
			return _aiQuatKey_GetTime(ptrQuatKey);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiQuatKey_GetValue")]
		private static extern IntPtr _aiQuatKey_GetValue(IntPtr ptrQuatKey);

		public static float[] aiQuatKey_GetValue(IntPtr ptrQuatKey)
		{
			return ReadFloatArray(_aiQuatKey_GetValue(ptrQuatKey), 4);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiCamera_GetAspect")]
		private static extern float _aiCamera_GetAspect(IntPtr ptrCamera);

		public static float aiCamera_GetAspect(IntPtr ptrCamera)
		{
			return _aiCamera_GetAspect(ptrCamera);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiCamera_GetClipPlaneFar")]
		private static extern float _aiCamera_GetClipPlaneFar(IntPtr ptrCamera);

		public static float aiCamera_GetClipPlaneFar(IntPtr ptrCamera)
		{
			return _aiCamera_GetClipPlaneFar(ptrCamera);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiCamera_GetClipPlaneNear")]
		private static extern float _aiCamera_GetClipPlaneNear(IntPtr ptrCamera);

		public static float aiCamera_GetClipPlaneNear(IntPtr ptrCamera)
		{
			return _aiCamera_GetClipPlaneNear(ptrCamera);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiCamera_GetHorizontalFOV")]
		private static extern float _aiCamera_GetHorizontalFOV(IntPtr ptrCamera);

		public static float aiCamera_GetHorizontalFOV(IntPtr ptrCamera)
		{
			return _aiCamera_GetHorizontalFOV(ptrCamera);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiCamera_GetLookAt")]
		private static extern IntPtr _aiCamera_GetLookAt(IntPtr ptrCamera);

		public static Vector3 aiCamera_GetLookAt(IntPtr ptrCamera)
		{
			return ReadStruct<Vector3>(_aiCamera_GetLookAt(ptrCamera), dealloc: false);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiCamera_GetName")]
		private static extern IntPtr _aiCamera_GetName(IntPtr ptrCamera);

		public static string aiCamera_GetName(IntPtr ptrCamera)
		{
			return ReadStringFromPointer(_aiCamera_GetName(ptrCamera));
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiCamera_GetPosition")]
		private static extern IntPtr _aiCamera_GetPosition(IntPtr ptrCamera);

		public static Vector3 aiCamera_GetPosition(IntPtr ptrCamera)
		{
			return ReadStruct<Vector3>(_aiCamera_GetPosition(ptrCamera), dealloc: false);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiCamera_GetUp")]
		private static extern IntPtr _aiCamera_GetUp(IntPtr ptrCamera);

		public static Vector3 aiCamera_GetUp(IntPtr ptrCamera)
		{
			return ReadStruct<Vector3>(_aiCamera_GetUp(ptrCamera), dealloc: false);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiLight_GetAngleInnerCone")]
		private static extern float _aiLight_GetAngleInnerCone(IntPtr ptrLight);

		public static float aiLight_GetAngleInnerCone(IntPtr ptrLight)
		{
			return _aiLight_GetAngleInnerCone(ptrLight);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiLight_GetAngleOuterCone")]
		private static extern float _aiLight_GetAngleOuterCone(IntPtr ptrLight);

		public static float aiLight_GetAngleOuterCone(IntPtr ptrLight)
		{
			return _aiLight_GetAngleOuterCone(ptrLight);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiLight_GetAttenuationConstant")]
		private static extern float _aiLight_GetAttenuationConstant(IntPtr ptrLight);

		public static float aiLight_GetAttenuationConstant(IntPtr ptrLight)
		{
			return _aiLight_GetAttenuationConstant(ptrLight);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiLight_GetAttenuationLinear")]
		private static extern float _aiLight_GetAttenuationLinear(IntPtr ptrLight);

		public static float aiLight_GetAttenuationLinear(IntPtr ptrLight)
		{
			return _aiLight_GetAttenuationLinear(ptrLight);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiLight_GetAttenuationQuadratic")]
		private static extern float _aiLight_GetAttenuationQuadratic(IntPtr ptrLight);

		public static float aiLight_GetAttenuationQuadratic(IntPtr ptrLight)
		{
			return _aiLight_GetAttenuationQuadratic(ptrLight);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiLight_GetColorAmbient")]
		private static extern IntPtr _aiLight_GetColorAmbient(IntPtr ptrLight);

		public static Color aiLight_GetColorAmbient(IntPtr ptrLight)
		{
			return ReadStruct<Color>(_aiLight_GetColorAmbient(ptrLight), dealloc: false);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiLight_GetColorDiffuse")]
		private static extern IntPtr _aiLight_GetColorDiffuse(IntPtr ptrLight);

		public static Color aiLight_GetColorDiffuse(IntPtr ptrLight)
		{
			return ReadStruct<Color>(_aiLight_GetColorDiffuse(ptrLight), dealloc: false);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiLight_GetColorSpecular")]
		private static extern IntPtr _aiLight_GetColorSpecular(IntPtr ptrLight);

		public static Color aiLight_GetColorSpecular(IntPtr ptrLight)
		{
			return ReadStruct<Color>(_aiLight_GetColorSpecular(ptrLight), dealloc: false);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiLight_GetDirection")]
		private static extern IntPtr _aiLight_GetDirection(IntPtr ptrLight);

		public static Vector3 aiLight_GetDirection(IntPtr ptrLight)
		{
			return ReadStruct<Vector3>(_aiLight_GetDirection(ptrLight), dealloc: false);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiLight_GetName")]
		private static extern IntPtr _aiLight_GetName(IntPtr ptrLight);

		public static string aiLight_GetName(IntPtr ptrLight)
		{
			return ReadStringFromPointer(_aiLight_GetName(ptrLight));
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_GetMetadataCount")]
		private static extern uint _aiScene_GetMetadataCount(IntPtr ptrScene);

		public static uint aiScene_GetMetadataCount(IntPtr ptrScene)
		{
			return _aiScene_GetMetadataCount(ptrScene);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_GetMetadataKey")]
		private static extern IntPtr _aiScene_GetMetadataKey(IntPtr ptrScene, uint uintIndex);

		public static string aiScene_GetMetadataKey(IntPtr ptrScene, uint uintIndex)
		{
			return ReadStringFromPointer(_aiScene_GetMetadataKey(ptrScene, uintIndex));
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_GetMetadataType")]
		private static extern int _aiScene_GetMetadataType(IntPtr ptrScene, uint uintIndex);

		public static AssimpMetadataType aiScene_GetMetadataType(IntPtr ptrScene, uint uintIndex)
		{
			return (AssimpMetadataType)_aiScene_GetMetadataType(ptrScene, uintIndex);
		}

		[DllImport("assimp", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "aiScene_GetMetadataValue")]
		private static extern IntPtr _aiScene_GetMetadataValue(IntPtr ptrScene, uint uintIndex);

		public static bool aiScene_GetMetadataBoolValue(IntPtr ptrScene, uint uintIndex)
		{
			return GetNewBool(_aiScene_GetMetadataValue(ptrScene, uintIndex));
		}

		public static int aiScene_GetMetadataInt32Value(IntPtr ptrScene, uint uintIndex)
		{
			return GetNewInt32(_aiScene_GetMetadataValue(ptrScene, uintIndex));
		}

		public static long aiScene_GetMetadataInt64Value(IntPtr ptrScene, uint uintIndex)
		{
			return GetNewInt64(_aiScene_GetMetadataValue(ptrScene, uintIndex));
		}

		public static float aiScene_GetMetadataFloatValue(IntPtr ptrScene, uint uintIndex)
		{
			return GetNewFloat(_aiScene_GetMetadataValue(ptrScene, uintIndex));
		}

		public static double aiScene_GetMetadataDoubleValue(IntPtr ptrScene, uint uintIndex)
		{
			return GetNewDouble(_aiScene_GetMetadataValue(ptrScene, uintIndex));
		}

		public static string aiScene_GetMetadataStringValue(IntPtr ptrScene, uint uintIndex)
		{
			return ReadStringFromPointer(_aiScene_GetMetadataValue(ptrScene, uintIndex));
		}

		public static Vector3 aiScene_GetMetadataVectorValue(IntPtr ptrScene, uint uintIndex)
		{
			return ReadStruct<Vector3>(_aiScene_GetMetadataValue(ptrScene, uintIndex), dealloc: false);
		}

		private static GCHandle LockGc(object value)
		{
			return GCHandle.Alloc(value, GCHandleType.Pinned);
		}

		private static IntPtr AllocHGlobal<T>()
		{
			return Marshal.AllocHGlobal(Marshal.SizeOf<T>());
		}

		private static T ReadStruct<T>(IntPtr pointer, bool dealloc = true)
		{
			T result = Marshal.PtrToStructure<T>(pointer);
			if (dealloc)
			{
				Marshal.FreeHGlobal(pointer);
			}
			return result;
		}

		private static byte[] StringToByteArray(string str, int length)
		{
			return Encoding.ASCII.GetBytes(str.PadRight(length, '\0'));
		}

		private static string ByteArrayToString(byte[] value)
		{
			int num = Array.IndexOf(value, (byte)0, 0);
			if (num < 0)
			{
				num = value.Length;
			}
			return Encoding.ASCII.GetString(value, 0, num);
		}

		private static GCHandle GetStringBuffer(string value)
		{
			return LockGc(StringToByteArray(value, 1024));
		}

		private static IntPtr GetAssimpStringBuffer(string value)
		{
			int num = (Is32Bits ? 4 : 8);
			IntPtr intPtr = Marshal.AllocHGlobal(num + value.Length);
			if (Is32Bits)
			{
				Marshal.WriteInt32(intPtr, value.Length);
			}
			Marshal.WriteInt64(intPtr, value.Length);
			Marshal.Copy(Encoding.ASCII.GetBytes(value), 0, new IntPtr(Is32Bits ? intPtr.ToInt32() : (intPtr.ToInt64() + num)), value.Length);
			return intPtr;
		}

		private static GCHandle GetNewStringBuffer(out byte[] byteArray)
		{
			byteArray = new byte[2048];
			return LockGc(byteArray);
		}

		private static string ReadStringFromPointer(IntPtr pointer)
		{
			return Marshal.PtrToStringAnsi(pointer);
		}

		private static bool GetNewBool(IntPtr pointer)
		{
			return Marshal.ReadByte(pointer) == 1;
		}

		private static int GetNewInt32(IntPtr pointer)
		{
			return Marshal.ReadInt32(pointer);
		}

		private static long GetNewInt64(IntPtr pointer)
		{
			return Marshal.ReadInt64(pointer);
		}

		private static float GetNewFloat(IntPtr pointer)
		{
			float[] array = new float[1];
			Marshal.Copy(pointer, array, 0, 1);
			return array[0];
		}

		private static double GetNewDouble(IntPtr pointer)
		{
			double[] array = new double[1];
			Marshal.Copy(pointer, array, 0, 1);
			return array[0];
		}

		private static Matrix4x4 GetNewMatrix4x4(IntPtr pointer)
		{
			Matrix4x4 result = default(Matrix4x4);
			float[] array = new float[16];
			Marshal.Copy(pointer, array, 0, 16);
			result[0] = array[0];
			result[4] = array[1];
			result[8] = array[2];
			result[12] = array[3];
			result[1] = array[4];
			result[5] = array[5];
			result[9] = array[6];
			result[13] = array[7];
			result[2] = array[8];
			result[6] = array[9];
			result[10] = array[10];
			result[14] = array[11];
			result[3] = array[12];
			result[7] = array[13];
			result[11] = array[14];
			result[15] = array[15];
			return result;
		}

		private static GCHandle Matrix4x4ToAssimp(Vector3 translation, Vector3 rotation, Vector3 scale)
		{
			Matrix4x4 matrix4x = Matrix4x4.TRS(translation, Quaternion.Euler(rotation), scale);
			float[] array = new float[16];
			array[0] = matrix4x[0];
			array[4] = matrix4x[1];
			array[8] = matrix4x[2];
			array[12] = matrix4x[3];
			array[1] = matrix4x[4];
			array[5] = matrix4x[5];
			array[9] = matrix4x[6];
			array[13] = matrix4x[7];
			array[2] = matrix4x[8];
			array[6] = matrix4x[9];
			array[10] = matrix4x[10];
			array[14] = matrix4x[11];
			array[3] = matrix4x[12];
			array[7] = matrix4x[13];
			array[11] = matrix4x[14];
			array[15] = matrix4x[15];
			return LockGc(array);
		}

		private static float[] ReadFloatArray(IntPtr pointer, int size)
		{
			float[] array = new float[size];
			Marshal.Copy(pointer, array, 0, size);
			return array;
		}
	}
}
