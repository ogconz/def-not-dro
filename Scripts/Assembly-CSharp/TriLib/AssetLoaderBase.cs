using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace TriLib
{
	public abstract class AssetLoaderBase : IDisposable
	{
		protected class __TextureInfo
		{
			public Texture2D tex2d;

			public bool alphaChecked;
		}

		protected const string AssimpFbxMagicString = "_$AssimpFbx$";

		protected NodeData RootNodeData;

		protected MaterialData[] MaterialData;

		protected MeshData[] MeshData;

		protected AnimationData[] AnimationData;

		protected CameraData[] CameraData;

		protected AssimpMetadata[] Metadata;

		protected Dictionary<string, string> NodesPath;

		protected Dictionary<string, Material> LoadedMaterials;

		protected Dictionary<string, __TextureInfo> LoadedTextures;

		protected Dictionary<SkinnedMeshRenderer, IList<string>> LoadedBoneNames;

		protected static Material StandardBaseMaterial;

		protected static Material StandardSpecularMaterial;

		protected static Material StandardBaseAlphaMaterial;

		protected static Material StandardSpecularAlphaMaterial;

		protected static Material StandardBaseCutoutMaterial;

		protected static Material StandardSpecularCutoutMaterial;

		protected static Texture2D NotFoundTexture;

		protected uint NodeId;

		private static Material m_carInteriorMaterial;

		private static Material m_carLiveryMaterial;

		private static Material m_carMiscMaterial;

		private static Material m_carPaintMaterial;

		private static Material m_carGlassMaterial;

		private static Material m_carBrakeMaterial;

		private static Dictionary<string, Material> m_materialMapper;

		protected bool HasBoneInfo;

		public static Texture2D NormalBaseTexture { get; private set; }

		protected bool HasOnMeshCreated => this.OnMeshCreated != null;

		protected bool HasOnMaterialCreated => this.OnMaterialCreated != null;

		protected bool HasOnTextureLoaded => this.OnTextureLoaded != null;

		[Obsolete("GetOnTextureLoaded is not used anymore. If you want to change loaded textures, override LoadTextureFromFile method.")]
		protected TextureLoadHandle GetOnTextureLoaded => this.OnTextureLoaded;

		[Obsolete("HasOnTexturePreLoad is not used anymore. If you want to change loaded textures, override LoadTextureFromFile method.")]
		protected bool HasOnTexturePreLoad => this.OnTexturePreLoad != null;

		protected bool HasOnAnimationClipCreated => this.OnAnimationClipCreated != null;

		protected bool HasOnObjectLoaded => this.OnObjectLoaded != null;

		protected bool HasOnMetadataProcessed => this.OnMetadataProcessed != null;

		public event EmbeddedTextureLoadCallback EmbeddedTextureLoad;

		public event MeshCreatedHandle OnMeshCreated;

		public event MaterialCreatedHandle OnMaterialCreated;

		public event TextureLoadHandle OnTextureLoaded;

		[Obsolete("OnTexturePreLoad is not used anymore. If you want to change loaded textures, override LoadTextureFromFile method.")]
		public event TexturePreLoadHandle OnTexturePreLoad;

		public event AnimationClipCreatedHandle OnAnimationClipCreated;

		public event ObjectLoadedHandle OnObjectLoaded;

		public event MetadataProcessedHandle OnMetadataProcessed;

		protected void TriggerOnMeshCreated(uint meshIndex, Mesh mesh)
		{
			if (this.OnMeshCreated != null)
			{
				this.OnMeshCreated(meshIndex, mesh);
			}
		}

		protected void TriggerOnMaterialCreated(uint materialIndex, bool isOverriden, Material material)
		{
			if (this.OnMaterialCreated != null)
			{
				this.OnMaterialCreated(materialIndex, isOverriden, material);
			}
		}

		protected void TriggerOnTextureLoaded(string sourcePath, Material material, string propertyName, Texture2D texture)
		{
			if (this.OnTextureLoaded != null)
			{
				this.OnTextureLoaded(sourcePath, material, propertyName, texture);
			}
		}

		[Obsolete("TriggerOnTexturePreLoad is not used anymore. If you want to change loaded textures, override LoadTextureFromFile method.")]
		protected void TriggerOnTexturePreLoad(IntPtr scene, string path, string name, Material material, string propertyName, ref bool checkAlphaChannel, TextureWrapMode textureWrapMode = TextureWrapMode.Repeat, string basePath = null, TextureLoadHandle onTextureLoaded = null, TextureCompression textureCompression = TextureCompression.None, bool isNormalMap = false)
		{
			if (this.OnTexturePreLoad != null)
			{
				this.OnTexturePreLoad(scene, path, name, material, propertyName, ref checkAlphaChannel, textureWrapMode, basePath, onTextureLoaded, textureCompression, isNormalMap);
			}
		}

		protected void TriggerOnAnimationClipCreated(uint animationClipIndex, AnimationClip animationClip)
		{
			if (this.OnAnimationClipCreated != null)
			{
				this.OnAnimationClipCreated(animationClipIndex, animationClip);
			}
		}

		protected void TriggerOnObjectLoaded(GameObject loadedGameObject)
		{
			if (this.OnObjectLoaded != null)
			{
				this.OnObjectLoaded(loadedGameObject);
			}
		}

		protected void TriggerOnMetadataProcessed(AssimpMetadataType metadataType, uint metadataIndex, string metadataKey, object metadataValue)
		{
			if (this.OnMetadataProcessed != null)
			{
				this.OnMetadataProcessed(metadataType, metadataIndex, metadataKey, metadataValue);
			}
		}

		public static bool IsExtensionSupported(string extension)
		{
			return AssimpInterop.ai_IsExtensionSupported(extension);
		}

		public static string GetSupportedFileExtensions()
		{
			AssimpInterop.ai_GetExtensionList(out var strExtensionList);
			return strExtensionList;
		}

		static AssetLoaderBase()
		{
			m_materialMapper = new Dictionary<string, Material>();
			LoadAllStandardMaterials();
		}

		private static void LoadAllStandardMaterials()
		{
			if (!LoadNotFoundTexture())
			{
				throw new Exception("Please import 'NotFound' asset from TriLib package 'TriLib\\Resources' to the project.");
			}
			if (!LoadNormalBaseTexture())
			{
				throw new Exception("Please import 'NormalBase.png' asset from TriLib package 'TriLib\\Resources' to the project.");
			}
			if (!LoadStandardMaterials())
			{
				throw new Exception("Please import all material assets from TriLib package 'TriLib\\Resources' to the project.");
			}
		}

		private static bool LoadStandardMaterials()
		{
			if (m_carPaintMaterial == null)
			{
				m_carPaintMaterial = ResourcesWrapper.LoadLegacy<Material>("ModsMaterials/CarPaint");
				m_materialMapper["Paint"] = m_carPaintMaterial;
				m_materialMapper["P"] = m_carPaintMaterial;
			}
			if (m_carMiscMaterial == null)
			{
				m_carMiscMaterial = ResourcesWrapper.LoadLegacy<Material>("ModsMaterials/CarMisc");
				m_materialMapper["Misc"] = m_carMiscMaterial;
			}
			if (m_carGlassMaterial == null)
			{
				m_carGlassMaterial = ResourcesWrapper.LoadLegacy<Material>("ModsMaterials/CarGlass");
				m_materialMapper["Glass"] = m_carGlassMaterial;
				m_materialMapper["G"] = m_carGlassMaterial;
			}
			if (m_carBrakeMaterial == null)
			{
				m_carBrakeMaterial = ResourcesWrapper.LoadLegacy<Material>("ModsMaterials/CarStoplight");
				m_materialMapper["tailbrake"] = m_carBrakeMaterial;
				m_materialMapper["Transparent"] = m_carBrakeMaterial;
			}
			if (m_carInteriorMaterial == null)
			{
				m_carInteriorMaterial = ResourcesWrapper.LoadLegacy<Material>("ModsMaterials/CarInterior");
				m_materialMapper["Interior"] = m_carInteriorMaterial;
			}
			if (m_carLiveryMaterial == null)
			{
				m_carLiveryMaterial = ResourcesWrapper.LoadLegacy<Material>("ModsMaterials/CarLivery");
				m_materialMapper["Livery"] = m_carLiveryMaterial;
			}
			if (StandardBaseMaterial == null)
			{
				StandardBaseMaterial = ResourcesWrapper.LoadLegacy<Material>("StandardMaterial");
			}
			if (StandardSpecularMaterial == null)
			{
				StandardSpecularMaterial = ResourcesWrapper.LoadLegacy<Material>("StandardSpecularMaterial");
			}
			if (StandardBaseAlphaMaterial == null)
			{
				StandardBaseAlphaMaterial = ResourcesWrapper.LoadLegacy<Material>("StandardBaseAlphaMaterial");
			}
			if (StandardSpecularAlphaMaterial == null)
			{
				StandardSpecularAlphaMaterial = ResourcesWrapper.LoadLegacy<Material>("StandardSpecularAlphaMaterial");
			}
			if (StandardBaseCutoutMaterial == null)
			{
				StandardBaseCutoutMaterial = ResourcesWrapper.LoadLegacy<Material>("StandardBaseCutoutMaterial");
			}
			if (StandardSpecularCutoutMaterial == null)
			{
				StandardSpecularCutoutMaterial = ResourcesWrapper.LoadLegacy<Material>("StandardSpecularCutoutMaterial");
			}
			if (StandardBaseMaterial != null && StandardSpecularMaterial != null && StandardBaseAlphaMaterial != null && StandardSpecularAlphaMaterial != null && StandardBaseCutoutMaterial != null)
			{
				return StandardSpecularCutoutMaterial != null;
			}
			return false;
		}

		private static bool LoadNotFoundTexture()
		{
			if (NotFoundTexture == null)
			{
				NotFoundTexture = ResourcesWrapper.LoadLegacy<Texture2D>("NotFound");
			}
			return NotFoundTexture != null;
		}

		private static bool LoadNormalBaseTexture()
		{
			if (NormalBaseTexture == null)
			{
				NormalBaseTexture = ResourcesWrapper.LoadLegacy<Texture2D>("NormalBase");
			}
			return NormalBaseTexture != null;
		}

		protected void InternalLoadFromMemory(byte[] fileBytes, string filename, AssetLoaderOptions options = null, bool usesWrapperGameObject = false)
		{
			IntPtr intPtr;
			try
			{
				string fileHint = (File.Exists(filename) ? Path.GetExtension(filename) : filename);
				intPtr = ImportFileFromMemory(fileBytes, fileHint, options);
			}
			catch (Exception innerException)
			{
				throw new Exception("Error parsing file.", innerException);
			}
			if (intPtr == IntPtr.Zero)
			{
				string arg = AssimpInterop.ai_GetErrorString();
				throw new Exception($"Error loading asset. Assimp returns: [{arg}]");
			}
			BuildMetadata(intPtr);
			LoadInternal(intPtr, options, usesWrapperGameObject);
			AssimpInterop.ai_ReleaseImport(intPtr);
		}

		protected void InternalLoadFromMemoryAndZip(byte[] data, string assetExtension, AssetLoaderOptions options = null, bool usesWrapperGameObject = false)
		{
			if (assetExtension.ToLowerInvariant() == ".zip")
			{
				throw new Exception("Please enable TRILIB_USE_ZIP to load zip files");
			}
			EmbeddedTextureLoad += (string path) => (EmbeddedTextureData)null;
			InternalLoadFromMemory(data, assetExtension, options, usesWrapperGameObject);
		}

		protected void InternalLoadFromFile(string filename, AssetLoaderOptions options = null, bool usesWrapperGameObject = false)
		{
			IntPtr intPtr;
			try
			{
				intPtr = ImportFile(filename, options);
			}
			catch (Exception innerException)
			{
				throw new Exception($"Error parsing file: {filename}", innerException);
			}
			if (intPtr == IntPtr.Zero)
			{
				string arg = AssimpInterop.ai_GetErrorString();
				throw new Exception($"Error loading asset. Assimp returns: [{arg}]");
			}
			LoadInternal(intPtr, options, usesWrapperGameObject);
			AssimpInterop.ai_ReleaseImport(intPtr);
		}

		protected GameObject BuildGameObject(AssetLoaderOptions options, string basePath = null, GameObject wrapperGameObject = null)
		{
			if (HasOnMetadataProcessed && Metadata != null && (options == null || !options.DontLoadMetadata))
			{
				AssimpMetadata[] metadata = Metadata;
				foreach (AssimpMetadata metadata2 in metadata)
				{
					ProcessMetadata(metadata2);
				}
			}
			if (MaterialData != null && (options == null || !options.DontLoadMaterials))
			{
				LoadAllStandardMaterials();
				LoadedMaterials = new Dictionary<string, Material>();
				LoadedTextures = new Dictionary<string, __TextureInfo>();
				MaterialData[] materialData = MaterialData;
				foreach (MaterialData materialData2 in materialData)
				{
					TransformMaterialData(materialData2, options, basePath);
				}
			}
			if (MeshData != null && (options == null || !options.DontLoadMeshes))
			{
				MeshData[] meshData = MeshData;
				foreach (MeshData meshData2 in meshData)
				{
					TransformMeshData(meshData2, options);
				}
			}
			GameObject gameObject = null;
			if (RootNodeData != null)
			{
				gameObject = TransformNodeData(RootNodeData, options, wrapperGameObject);
				if (gameObject != null)
				{
					if (LoadedBoneNames != null && LoadedBoneNames.Count > 0)
					{
						SetupSkinnedMeshRendererTransforms(gameObject);
					}
					if (options != null)
					{
						LoadContextOptions(gameObject, options);
					}
				}
			}
			if (AnimationData != null && (options == null || !options.DontLoadAnimations))
			{
				AnimationData[] animationData = AnimationData;
				foreach (AnimationData animationData2 in animationData)
				{
					TransformAnimationData(animationData2, options);
				}
			}
			if (gameObject != null)
			{
				if (AnimationData != null && (options == null || !options.DontApplyAnimations))
				{
					SetupAnimations(wrapperGameObject ?? gameObject, options);
				}
				if (CameraData != null && (options == null || !options.DontLoadCameras))
				{
					CameraData[] cameraData = CameraData;
					foreach (CameraData cameraData2 in cameraData)
					{
						TransformCameraData(gameObject, cameraData2, options);
					}
				}
				if (options != null && options.AddAssetUnloader)
				{
					gameObject.AddComponent<AssetUnloader>();
				}
				if (HasOnObjectLoaded)
				{
					TriggerOnObjectLoaded(gameObject);
				}
			}
			return gameObject;
		}

		protected virtual void SetupSkinnedMeshRendererTransforms(GameObject gameObject)
		{
			foreach (KeyValuePair<SkinnedMeshRenderer, IList<string>> loadedBoneName in LoadedBoneNames)
			{
				SkinnedMeshRenderer key = loadedBoneName.Key;
				IList<string> value = loadedBoneName.Value;
				int count = value.Count;
				List<Transform> list = new List<Transform>(count);
				for (int i = 0; i < count; i++)
				{
					Transform transform = gameObject.transform.FindDeepChild(value[i]);
					if (!(transform == null))
					{
						list.Add(transform);
					}
				}
				key.bones = list.ToArray();
			}
		}

		private static void LoadContextOptions(GameObject gameobject, AssetLoaderOptions options)
		{
			gameobject.transform.rotation = Quaternion.Euler(options.RotationAngles);
			gameobject.transform.localScale = Vector3.one * options.Scale;
		}

		protected virtual void ProcessMetadata(AssimpMetadata metadata)
		{
			TriggerOnMetadataProcessed(metadata.MetadataType, metadata.MetadataIndex, metadata.MetadataKey, metadata.MetadataValue);
		}

		protected virtual void SetupAnimations(GameObject gameObject, AssetLoaderOptions options)
		{
			if (options == null || options.UseLegacyAnimations)
			{
				Animation animation = gameObject.GetComponent<Animation>();
				if (animation == null)
				{
					animation = gameObject.AddComponent<Animation>();
				}
				AnimationClip clip = null;
				for (int i = 0; i < AnimationData.Length; i++)
				{
					AnimationClip animationClip = AnimationData[i].AnimationClip;
					if (!(animationClip == null))
					{
						animation.AddClip(animationClip, animationClip.name);
						if (i == 0)
						{
							clip = animationClip;
						}
					}
				}
				animation.clip = clip;
				if (options == null || options.AutoPlayAnimations)
				{
					animation.Play();
				}
			}
			else
			{
				Animator animator = gameObject.GetComponent<Animator>();
				if (animator == null)
				{
					animator = gameObject.AddComponent<Animator>();
				}
				if (options.AnimatorController != null)
				{
					animator.runtimeAnimatorController = options.AnimatorController;
				}
				if (!options.DontGenerateAvatar)
				{
					animator.avatar = ((options.Avatar != null) ? options.Avatar : AvatarBuilder.BuildGenericAvatar(gameObject, ""));
				}
			}
		}

		protected virtual GameObject TransformNodeData(NodeData nodeData, AssetLoaderOptions options, GameObject existingGameObject = null)
		{
			GameObject gameObject = new GameObject
			{
				name = nodeData.Name
			};
			GameObject gameObject2 = ((existingGameObject != null) ? existingGameObject : ((nodeData.Parent == null) ? null : nodeData.Parent.GameObject));
			if (gameObject2 != null)
			{
				gameObject.transform.SetParent(gameObject2.transform, worldPositionStays: false);
			}
			gameObject.transform.LoadMatrix(nodeData.Matrix);
			if (nodeData.Meshes != null && nodeData.Meshes.Length != 0 && MeshData != null && MeshData.Length != 0)
			{
				int num = 0;
				uint[] meshes = nodeData.Meshes;
				foreach (uint num2 in meshes)
				{
					MeshData meshData = MeshData[num2];
					num += meshData.Vertices.Length;
				}
				if (options == null || (options.Use32BitsIndexFormat && options.CombineMeshes) || (num < 65536 && options.CombineMeshes))
				{
					Material material = null;
					List<string> list = null;
					bool flag = true;
					CombineInstance[] array = new CombineInstance[nodeData.Meshes.Length];
					Material[] array2 = new Material[nodeData.Meshes.Length];
					for (int j = 0; j < nodeData.Meshes.Length; j++)
					{
						uint num3 = nodeData.Meshes[j];
						if (num3 >= MeshData.Length)
						{
							continue;
						}
						MeshData meshData2 = MeshData[num3];
						if (meshData2.HasBoneInfo && meshData2.BoneNames.Length != 0)
						{
							if (list == null)
							{
								list = new List<string>();
							}
							list.AddRange(meshData2.BoneNames);
						}
						CombineInstance combineInstance = new CombineInstance
						{
							mesh = meshData2.Mesh,
							transform = Matrix4x4.identity
						};
						array[j] = combineInstance;
						if (MaterialData != null && MaterialData.Length != 0 && meshData2.MaterialIndex < MaterialData.Length)
						{
							Material material2 = MaterialData[meshData2.MaterialIndex].Material;
							if (material != null && material2 != material)
							{
								flag = false;
							}
							array2[j] = material2;
							material = material2;
						}
					}
					Mesh mesh = new Mesh();
					if (options == null || options.Use32BitsIndexFormat)
					{
						mesh.indexFormat = IndexFormat.UInt32;
					}
					mesh.CombineMeshes(array, flag);
					mesh.name = FixName(nodeData.Name) + "_Mesh";
					CreateMeshComponents(gameObject, options, mesh, HasBoneInfo, array2, list, flag ? array2[0] : null);
				}
				else
				{
					for (int k = 0; k < nodeData.Meshes.Length; k++)
					{
						uint num4 = nodeData.Meshes[k];
						MeshData meshData3 = MeshData[num4];
						Material singleMaterial = ((MaterialData == null) ? null : MaterialData[meshData3.MaterialIndex].Material);
						GameObject gameObject3 = new GameObject
						{
							name = "SubMesh_" + k
						};
						gameObject3.transform.SetParent(gameObject.transform, worldPositionStays: false);
						CreateMeshComponents(gameObject3, options, meshData3.Mesh, HasBoneInfo, null, meshData3.BoneNames, singleMaterial);
					}
				}
			}
			nodeData.GameObject = gameObject;
			if (nodeData.Children != null)
			{
				NodeData[] children = nodeData.Children;
				foreach (NodeData nodeData2 in children)
				{
					TransformNodeData(nodeData2, options);
				}
			}
			return gameObject;
		}

		protected virtual void CreateMeshComponents(GameObject gameObject, AssetLoaderOptions options, Mesh mesh, bool hasBoneInfo, Material[] combinedMaterials, IList<string> boneNames = null, Material singleMaterial = null)
		{
			if (hasBoneInfo)
			{
				SkinnedMeshRenderer skinnedMeshRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
				skinnedMeshRenderer.sharedMesh = mesh;
				skinnedMeshRenderer.quality = SkinQuality.Bone4;
				if (boneNames != null)
				{
					if (LoadedBoneNames == null)
					{
						LoadedBoneNames = new Dictionary<SkinnedMeshRenderer, IList<string>>();
					}
					LoadedBoneNames.Add(skinnedMeshRenderer, boneNames);
				}
				if (singleMaterial != null)
				{
					skinnedMeshRenderer.sharedMaterial = singleMaterial;
				}
				else
				{
					skinnedMeshRenderer.sharedMaterials = combinedMaterials;
				}
			}
			else
			{
				gameObject.AddComponent<MeshFilter>().mesh = mesh;
				MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
				if (singleMaterial != null)
				{
					meshRenderer.sharedMaterial = singleMaterial;
				}
				else
				{
					meshRenderer.sharedMaterials = combinedMaterials;
				}
				if (options != null && options.GenerateMeshColliders)
				{
					MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
					meshCollider.sharedMesh = mesh;
					meshCollider.convex = options.ConvexMeshColliders;
				}
			}
			if (HasOnMeshCreated)
			{
				TriggerOnMeshCreated(0u, mesh);
			}
		}

		protected virtual void TransformCameraData(GameObject gameObject, CameraData cameraData, AssetLoaderOptions options)
		{
			Transform transform = gameObject.transform.FindDeepChild(cameraData.Name);
			if (!(transform == null))
			{
				Camera camera = transform.gameObject.AddComponent<Camera>();
				camera.aspect = cameraData.Aspect;
				camera.nearClipPlane = cameraData.NearClipPlane;
				camera.farClipPlane = cameraData.FarClipPlane;
				camera.fieldOfView = cameraData.FieldOfView;
				camera.transform.localPosition = cameraData.LocalPosition;
				camera.transform.LookAt(cameraData.Forward, cameraData.Up);
				cameraData.Camera = camera;
			}
		}

		private static AnimationCurve FixCurve(float animationLength, AnimationCurve curve)
		{
			if (Mathf.Approximately(animationLength, 0f))
			{
				animationLength = 1f;
			}
			if (curve.keys.Length == 1)
			{
				curve.AddKey(new Keyframe(animationLength, curve.keys[0].value));
			}
			return curve;
		}

		protected virtual void TransformAnimationData(AnimationData animationData, AssetLoaderOptions options)
		{
			AnimationClip animationClip = new AnimationClip
			{
				name = animationData.Name,
				frameRate = animationData.FrameRate,
				wrapMode = animationData.WrapMode,
				legacy = animationData.Legacy
			};
			AnimationChannelData[] channelData = animationData.ChannelData;
			foreach (AnimationChannelData animationChannelData in channelData)
			{
				if (!NodesPath.ContainsKey(animationChannelData.NodeName))
				{
					continue;
				}
				string relativePath = NodesPath[animationChannelData.NodeName];
				foreach (KeyValuePair<string, AnimationCurveData> curveDatum in animationChannelData.CurveData)
				{
					string key = curveDatum.Key;
					AnimationCurveData value = curveDatum.Value;
					animationClip.SetCurve(curve: value.AnimationCurve = FixCurve(animationData.Length, new AnimationCurve
					{
						keys = value.Keyframes
					}), relativePath: relativePath, type: typeof(Transform), propertyName: key);
				}
			}
			if (options != null && options.EnsureQuaternionContinuity)
			{
				animationClip.EnsureQuaternionContinuity();
			}
			if (HasOnAnimationClipCreated)
			{
				TriggerOnAnimationClipCreated(0u, animationClip);
			}
			animationData.AnimationClip = animationClip;
		}

		protected virtual void TransformMeshData(MeshData meshData, AssetLoaderOptions options)
		{
			Mesh mesh = new Mesh();
			if (options == null || options.Use32BitsIndexFormat)
			{
				mesh.indexFormat = IndexFormat.UInt32;
			}
			mesh.name = meshData.Name;
			mesh.vertices = meshData.Vertices;
			mesh.normals = meshData.Normals;
			mesh.uv4 = meshData.Uv3;
			mesh.uv3 = meshData.Uv2;
			mesh.uv2 = meshData.Uv1;
			mesh.uv = meshData.Uv;
			mesh.tangents = meshData.Tangents;
			mesh.colors = meshData.Colors;
			mesh.boneWeights = meshData.BoneWeights;
			mesh.bindposes = meshData.BindPoses;
			mesh.triangles = meshData.Triangles;
			meshData.Mesh = mesh;
		}

		protected virtual void TransformMaterialData(MaterialData materialData, AssetLoaderOptions options, string basePath = null)
		{
			bool checkAlphaChannel = false;
			bool checkAlphaChannel2 = options != null && options.ApplyAlphaMaterials && materialData.DiffuseInfoLoaded;
			Texture2D texture2D = (materialData.DiffuseInfoLoaded ? LoadTextureFromFile(materialData.DiffusePath, materialData.Name, options, materialData.DiffuseEmbeddedTextureData, materialData.DiffuseWrapMode, ref checkAlphaChannel2, isNormalMap: false, basePath) : null);
			Texture2D value = (materialData.EmissionInfoLoaded ? LoadTextureFromFile(materialData.EmissionPath, materialData.Name, options, materialData.EmissionEmbeddedTextureData, materialData.EmissionWrapMode, ref checkAlphaChannel, isNormalMap: false, basePath) : null);
			Texture2D value2 = (materialData.SpecularInfoLoaded ? LoadTextureFromFile(materialData.SpecularPath, materialData.Name, options, materialData.SpecularEmbeddedTextureData, materialData.SpecularWrapMode, ref checkAlphaChannel, isNormalMap: false, basePath) : null);
			Texture2D value3 = (materialData.NormalInfoLoaded ? LoadTextureFromFile(materialData.NormalPath, materialData.Name, options, materialData.NormalEmbeddedTextureData, materialData.NormalWrapMode, ref checkAlphaChannel, isNormalMap: true, basePath) : null);
			Texture2D value4 = (materialData.HeightInfoLoaded ? LoadTextureFromFile(materialData.HeightPath, materialData.Name, options, materialData.HeightEmbeddedTextureData, materialData.HeightWrapMode, ref checkAlphaChannel, isNormalMap: false, basePath) : null);
			bool hasAlpha = (materialData.AlphaLoaded && materialData.Alpha != 1f) || checkAlphaChannel2;
			bool hasSpecular = materialData.SpecularColorLoaded || !string.IsNullOrEmpty(materialData.SpecularPath);
			Material material = LoadMaterial(materialData.Name, options, hasAlpha, hasSpecular);
			string name = "_BaseColorMap";
			if (!material.HasProperty(name))
			{
				name = "_BaseMap";
			}
			material.SetTexture(name, texture2D);
			if (texture2D != null && texture2D.name.StartsWith("@"))
			{
				material.SetTextureScale(name, new Vector2(1f, -1f));
			}
			if (materialData.DiffuseColorLoaded)
			{
				Color diffuseColor = materialData.DiffuseColor;
				if (materialData.AlphaLoaded)
				{
					diffuseColor.a = materialData.Alpha;
				}
				material.SetColor("_BaseColor", diffuseColor);
			}
			material.SetTexture("_EmissionColorMap", value);
			if (materialData.EmissionColorLoaded)
			{
				material.SetColor("_EmissionColor", materialData.EmissionColor);
			}
			material.SetTexture("_SpecGlossMap", value2);
			if (materialData.SpecularColorLoaded)
			{
				material.SetColor("_SpecColor", materialData.SpecularColor);
			}
			material.SetTexture("_NormalMap", value3);
			material.SetTexture("_HeightMap", value4);
			if (materialData.BumpScaleLoaded)
			{
				material.SetFloat("_NormalScale", materialData.BumpScale);
			}
			materialData.Material = material;
		}

		protected virtual Material LoadMaterial(string name, AssetLoaderOptions options, bool hasAlpha, bool hasSpecular)
		{
			int num = name.LastIndexOf('_');
			string text = ((num >= 0) ? name.Remove(num) : name);
			Material value2;
			if (m_materialMapper.TryGetValue(text, out var value))
			{
				value2 = new Material(value);
				value2.name = " - " + text;
			}
			else if (!LoadedMaterials.TryGetValue(name, out value2))
			{
				value2 = ((!(options != null && !options.DisableAlphaMaterials && hasAlpha)) ? new Material((options != null && options.UseStandardSpecularMaterial && hasSpecular) ? StandardSpecularMaterial : StandardBaseMaterial) : (options.UseCutoutMaterials ? new Material((options.UseStandardSpecularMaterial && hasSpecular) ? StandardSpecularCutoutMaterial : StandardBaseCutoutMaterial) : new Material((options.UseStandardSpecularMaterial && hasSpecular) ? StandardSpecularAlphaMaterial : StandardBaseAlphaMaterial)));
				value2.name = name;
				LoadedMaterials.Add(name, value2);
			}
			if (HasOnMaterialCreated)
			{
				TriggerOnMaterialCreated(0u, isOverriden: false, value2);
			}
			return value2;
		}

		protected virtual Texture2D LoadTextureFromFile(string path, string name, AssetLoaderOptions options, EmbeddedTextureData embeddedTextureData, TextureWrapMode textureWrapMode, ref bool checkAlphaChannel, bool isNormalMap, string basePath)
		{
			if (!LoadedTextures.TryGetValue(path, out var value))
			{
				value = new __TextureInfo();
				value.tex2d = Texture2DUtils.LoadTextureFromFile(path, embeddedTextureData?.Width ?? 0, embeddedTextureData?.Height ?? 0, name, ref checkAlphaChannel, embeddedTextureData?.Data, embeddedTextureData?.IsRawData ?? false, isNormalMap, basePath, textureWrapMode, (!(options != null)) ? TextureCompression.NormalQuality : options.TextureCompression);
				if (value.tex2d != null)
				{
					value.alphaChecked = checkAlphaChannel;
					LoadedTextures.Add(path, value);
				}
			}
			if (value.tex2d != null && HasOnTextureLoaded)
			{
				TriggerOnTextureLoaded(path, null, null, value.tex2d);
			}
			checkAlphaChannel = value.alphaChecked;
			return value.tex2d;
		}

		private static uint GetDefaultPostProcessSteps()
		{
			return 20356687u;
		}

		private static IntPtr BuildPropertyStore(AssetLoaderOptions options)
		{
			IntPtr intPtr = AssimpInterop.ai_CreatePropertyStore();
			foreach (AssetAdvancedConfig advancedConfig in options.AdvancedConfigs)
			{
				AssetAdvancedPropertyMetadata.GetOptionMetadata(advancedConfig.Key, out var assetAdvancedConfigType, out var _, out var _, out var _, out var _, out var _, out var _, out var _, out var _, out var _);
				switch (assetAdvancedConfigType)
				{
				case AssetAdvancedConfigType.AiComponent:
					AssimpInterop.ai_SetImportPropertyInteger(intPtr, advancedConfig.Key, advancedConfig.IntValue << 1);
					break;
				case AssetAdvancedConfigType.AiPrimitiveType:
					AssimpInterop.ai_SetImportPropertyInteger(intPtr, advancedConfig.Key, advancedConfig.IntValue << 1);
					break;
				case AssetAdvancedConfigType.AiUVTransform:
					AssimpInterop.ai_SetImportPropertyInteger(intPtr, advancedConfig.Key, advancedConfig.IntValue << 1);
					break;
				case AssetAdvancedConfigType.Bool:
					AssimpInterop.ai_SetImportPropertyInteger(intPtr, advancedConfig.Key, advancedConfig.BoolValue ? 1 : 0);
					break;
				case AssetAdvancedConfigType.Integer:
					AssimpInterop.ai_SetImportPropertyInteger(intPtr, advancedConfig.Key, advancedConfig.IntValue);
					break;
				case AssetAdvancedConfigType.Float:
					AssimpInterop.ai_SetImportPropertyFloat(intPtr, advancedConfig.Key, advancedConfig.FloatValue);
					break;
				case AssetAdvancedConfigType.String:
					AssimpInterop.ai_SetImportPropertyString(intPtr, advancedConfig.Key, advancedConfig.StringValue);
					break;
				case AssetAdvancedConfigType.AiMatrix:
					AssimpInterop.ai_SetImportPropertyMatrix(intPtr, advancedConfig.Key, advancedConfig.TranslationValue, advancedConfig.RotationValue, advancedConfig.ScaleValue);
					break;
				}
			}
			return intPtr;
		}

		private static IntPtr ImportFileFromMemory(byte[] fileBytes, string fileHint, AssetLoaderOptions options)
		{
			IntPtr result;
			if (options != null && options.AdvancedConfigs != null)
			{
				IntPtr intPtr = BuildPropertyStore(options);
				result = AssimpInterop.ai_ImportFileFromMemoryWithProperties(fileBytes, (uint)options.PostProcessSteps, fileHint, intPtr);
				AssimpInterop.ai_CreateReleasePropertyStore(intPtr);
			}
			else
			{
				result = AssimpInterop.ai_ImportFileFromMemory(fileBytes, (options == null) ? GetDefaultPostProcessSteps() : ((uint)options.PostProcessSteps), fileHint);
			}
			return result;
		}

		private static IntPtr ImportFile(string filename, AssetLoaderOptions options)
		{
			IntPtr result;
			if (options != null && options.AdvancedConfigs != null)
			{
				IntPtr intPtr = BuildPropertyStore(options);
				result = AssimpInterop.ai_ImportFileEx(filename, (uint)options.PostProcessSteps, IntPtr.Zero, intPtr);
				AssimpInterop.ai_CreateReleasePropertyStore(intPtr);
			}
			else
			{
				result = AssimpInterop.ai_ImportFile(filename, (options == null) ? GetDefaultPostProcessSteps() : ((uint)options.PostProcessSteps));
			}
			return result;
		}

		private void LoadInternal(IntPtr scene, AssetLoaderOptions options, bool usesWrapperGameObject = false)
		{
			Dispose();
			if (AssimpInterop.aiScene_HasMaterials(scene) && (options == null || !options.DontLoadMaterials))
			{
				MaterialData = new MaterialData[AssimpInterop.aiScene_GetNumMaterials(scene)];
				BuildMaterials(scene);
			}
			if (AssimpInterop.aiScene_HasMeshes(scene) && (options == null || !options.DontLoadMeshes))
			{
				MeshData = new MeshData[AssimpInterop.aiScene_GetNumMeshes(scene)];
				BuildMeshes(scene);
				BuildBones(scene);
			}
			if (AssimpInterop.aiScene_HasAnimation(scene) && (options == null || !options.DontLoadAnimations))
			{
				AnimationData = new AnimationData[AssimpInterop.aiScene_GetNumAnimations(scene)];
				BuildAnimations(scene, options);
			}
			if (AssimpInterop.aiScene_HasCameras(scene) && (options == null || !options.DontLoadCameras))
			{
				CameraData = new CameraData[AssimpInterop.aiScene_GetNumCameras(scene)];
				BuildCameras(scene);
			}
			BuildObjects(scene, options, usesWrapperGameObject);
		}

		private void BuildObjects(IntPtr scene, AssetLoaderOptions options, bool usesWrapperGameObject = false)
		{
			NodesPath = new Dictionary<string, string>();
			IntPtr node = AssimpInterop.aiScene_GetRootNode(scene);
			RootNodeData = BuildObject(RootNodeData, node, options, usesWrapperGameObject);
		}

		private NodeData BuildObject(NodeData parentNodeData, IntPtr node, AssetLoaderOptions options, bool usesWrapperGameObject = false)
		{
			uint nodeId = NodeId++;
			string text = AssimpInterop.aiNode_GetName(node);
			string text2 = FixNodeName(text, nodeId);
			Matrix4x4 matrix = AssimpInterop.aiNode_GetTransformation(node);
			string text3 = ((parentNodeData != null) ? $"{parentNodeData.Path}/{text}" : string.Format((!usesWrapperGameObject) ? "/{0}" : "{0}", text));
			NodesPath.Add(text2, text3);
			uint num = AssimpInterop.aiNode_GetNumMeshes(node);
			NodeData nodeData = new NodeData
			{
				Name = text2,
				Path = text3,
				Matrix = matrix,
				Meshes = new uint[num]
			};
			for (uint num2 = 0u; num2 < num; num2++)
			{
				uint num3 = AssimpInterop.aiNode_GetMeshIndex(node, num2);
				nodeData.Meshes[num2] = num3;
			}
			uint num4 = AssimpInterop.aiNode_GetNumChildren(node);
			if (num4 != 0)
			{
				nodeData.Children = new NodeData[num4];
				for (uint num5 = 0u; num5 < num4; num5++)
				{
					IntPtr node2 = AssimpInterop.aiNode_GetChildren(node, num5);
					NodeData nodeData2 = BuildObject(nodeData, node2, options, usesWrapperGameObject);
					nodeData2.Parent = nodeData;
					nodeData.Children[num5] = nodeData2;
				}
			}
			return nodeData;
		}

		private void BuildMetadata(IntPtr scene)
		{
			if (HasOnMetadataProcessed)
			{
				uint num = AssimpInterop.aiScene_GetMetadataCount(scene);
				Metadata = new AssimpMetadata[num];
				for (uint num2 = 0u; num2 < num; num2++)
				{
					string metadataKey = AssimpInterop.aiScene_GetMetadataKey(scene, num2);
					AssimpMetadataType assimpMetadataType = AssimpInterop.aiScene_GetMetadataType(scene, num2);
					object metadataValue = assimpMetadataType switch
					{
						AssimpMetadataType.AI_BOOL => AssimpInterop.aiScene_GetMetadataBoolValue(scene, num2), 
						AssimpMetadataType.AI_INT32 => AssimpInterop.aiScene_GetMetadataInt32Value(scene, num2), 
						AssimpMetadataType.AI_UINT64 => AssimpInterop.aiScene_GetMetadataInt64Value(scene, num2), 
						AssimpMetadataType.AI_FLOAT => AssimpInterop.aiScene_GetMetadataFloatValue(scene, num2), 
						AssimpMetadataType.AI_DOUBLE => AssimpInterop.aiScene_GetMetadataDoubleValue(scene, num2), 
						AssimpMetadataType.AI_AIVECTOR3D => AssimpInterop.aiScene_GetMetadataVectorValue(scene, num2), 
						_ => AssimpInterop.aiScene_GetMetadataStringValue(scene, num2), 
					};
					Metadata[num] = new AssimpMetadata(assimpMetadataType, num2, metadataKey, metadataValue);
				}
			}
		}

		private void BuildMeshes(IntPtr scene)
		{
			uint num = AssimpInterop.aiScene_GetNumMeshes(scene);
			for (uint num2 = 0u; num2 < num; num2++)
			{
				MeshData meshData = new MeshData();
				IntPtr ptrMesh = AssimpInterop.aiScene_GetMesh(scene, num2);
				string name = AssimpInterop.aiMesh_GetName(ptrMesh);
				meshData.Name = FixName(name, num2);
				uint materialIndex = AssimpInterop.aiMesh_GetMatrialIndex(ptrMesh);
				meshData.MaterialIndex = materialIndex;
				uint num3 = AssimpInterop.aiMesh_VertexCount(ptrMesh);
				bool flag = AssimpInterop.aiMesh_HasNormals(ptrMesh);
				if (flag)
				{
					meshData.Normals = new Vector3[num3];
				}
				bool flag2 = AssimpInterop.aiMesh_HasTangentsAndBitangents(ptrMesh);
				if (flag2)
				{
					meshData.Tangents = new Vector4[num3];
					meshData.BiTangents = new Vector4[num3];
				}
				bool flag3 = AssimpInterop.aiMesh_HasTextureCoords(ptrMesh, 0u);
				if (flag3)
				{
					meshData.Uv = new Vector2[num3];
				}
				bool flag4 = AssimpInterop.aiMesh_HasTextureCoords(ptrMesh, 1u);
				if (flag4)
				{
					meshData.Uv1 = new Vector2[num3];
				}
				bool flag5 = AssimpInterop.aiMesh_HasTextureCoords(ptrMesh, 2u);
				if (flag5)
				{
					meshData.Uv2 = new Vector2[num3];
				}
				bool flag6 = AssimpInterop.aiMesh_HasTextureCoords(ptrMesh, 3u);
				if (flag6)
				{
					meshData.Uv3 = new Vector2[num3];
				}
				bool flag7 = AssimpInterop.aiMesh_HasVertexColors(ptrMesh, 0u);
				if (flag7)
				{
					meshData.Colors = new Color[num3];
				}
				meshData.Vertices = new Vector3[num3];
				for (uint num4 = 0u; num4 < num3; num4++)
				{
					meshData.Vertices[num4] = AssimpInterop.aiMesh_GetVertex(ptrMesh, num4);
					if (flag)
					{
						meshData.Normals[num4] = AssimpInterop.aiMesh_GetNormal(ptrMesh, num4);
					}
					if (flag2)
					{
						meshData.Tangents[num4] = AssimpInterop.aiMesh_GetTangent(ptrMesh, num4);
						meshData.BiTangents[num4] = AssimpInterop.aiMesh_GetBitangent(ptrMesh, num4);
					}
					if (flag3)
					{
						meshData.Uv[num4] = AssimpInterop.aiMesh_GetTextureCoord(ptrMesh, 0u, num4);
					}
					if (flag4)
					{
						meshData.Uv1[num4] = AssimpInterop.aiMesh_GetTextureCoord(ptrMesh, 1u, num4);
					}
					if (flag5)
					{
						meshData.Uv2[num4] = AssimpInterop.aiMesh_GetTextureCoord(ptrMesh, 2u, num4);
					}
					if (flag6)
					{
						meshData.Uv3[num4] = AssimpInterop.aiMesh_GetTextureCoord(ptrMesh, 3u, num4);
					}
					if (flag7)
					{
						meshData.Colors[num4] = AssimpInterop.aiMesh_GetVertexColor(ptrMesh, 0u, num4);
					}
				}
				if (AssimpInterop.aiMesh_HasFaces(ptrMesh))
				{
					uint num5 = AssimpInterop.aiMesh_GetNumFaces(ptrMesh);
					meshData.Triangles = new int[num5 * 3];
					for (uint num6 = 0u; num6 < num5; num6++)
					{
						IntPtr ptrFace = AssimpInterop.aiMesh_GetFace(ptrMesh, num6);
						uint num7 = AssimpInterop.aiFace_GetNumIndices(ptrFace);
						if (num7 > 3)
						{
							throw new UnityException("More than three face indices is not supported. Please enable \"Triangulate\" in your \"AssetLoaderOptions\" \"PostProcessSteps\" field");
						}
						for (uint num8 = 0u; num8 < num7; num8++)
						{
							meshData.Triangles[num6 * 3 + num8] = (int)AssimpInterop.aiFace_GetIndex(ptrFace, num8);
						}
					}
				}
				MeshData[num2] = meshData;
			}
		}

		private void BuildCameras(IntPtr scene)
		{
			for (uint num = 0u; num < AssimpInterop.aiScene_GetNumCameras(scene); num++)
			{
				IntPtr ptrCamera = AssimpInterop.aiScene_GetCamera(scene, num);
				string name = AssimpInterop.aiCamera_GetName(ptrCamera);
				CameraData cameraData = new CameraData
				{
					Name = name,
					Aspect = AssimpInterop.aiCamera_GetAspect(ptrCamera),
					NearClipPlane = AssimpInterop.aiCamera_GetClipPlaneNear(ptrCamera),
					FarClipPlane = AssimpInterop.aiCamera_GetClipPlaneFar(ptrCamera),
					FieldOfView = AssimpInterop.aiCamera_GetHorizontalFOV(ptrCamera),
					LocalPosition = AssimpInterop.aiCamera_GetPosition(ptrCamera),
					Forward = AssimpInterop.aiCamera_GetLookAt(ptrCamera),
					Up = AssimpInterop.aiCamera_GetUp(ptrCamera)
				};
				CameraData[num] = cameraData;
			}
		}

		private void BuildMaterials(IntPtr scene)
		{
			for (uint num = 0u; num < AssimpInterop.aiScene_GetNumMaterials(scene); num++)
			{
				MaterialData materialData = new MaterialData();
				IntPtr ptrMat = AssimpInterop.aiScene_GetMaterial(scene, num);
				string strName;
				if (AssimpInterop.aiMaterial_HasName(ptrMat))
				{
					if (!AssimpInterop.aiMaterial_GetName(ptrMat, out strName))
					{
						strName = "Material_" + StringUtils.GenerateUniqueName(num);
					}
				}
				else
				{
					strName = "Material_" + StringUtils.GenerateUniqueName(num);
				}
				materialData.Name = strName;
				bool alphaLoaded = false;
				if (AssimpInterop.aiMaterial_HasOpacity(ptrMat) && AssimpInterop.aiMaterial_GetOpacity(ptrMat, out var floatOut))
				{
					materialData.Alpha = floatOut;
					alphaLoaded = true;
				}
				materialData.AlphaLoaded = alphaLoaded;
				bool diffuseInfoLoaded = false;
				if (AssimpInterop.aiMaterial_GetNumTextureDiffuse(ptrMat) != 0 && AssimpInterop.aiMaterial_GetTextureDiffuse(ptrMat, 0u, out var strPath, out var _, out var _, out var floatBlend, out var uintOp, out var uintMapMode))
				{
					TextureWrapMode diffuseWrapMode = ((uintMapMode == 1) ? TextureWrapMode.Clamp : TextureWrapMode.Repeat);
					string diffuseName = StringUtils.GenerateUniqueName(strPath);
					materialData.DiffusePath = FixName(strPath);
					materialData.DiffuseWrapMode = diffuseWrapMode;
					materialData.DiffuseName = diffuseName;
					materialData.DiffuseBlendMode = floatBlend;
					materialData.DiffuseOp = uintOp;
					diffuseInfoLoaded = true;
					EmbeddedTextureData embeddedTextureData = null;
					if (this.EmbeddedTextureLoad != null)
					{
						embeddedTextureData = this.EmbeddedTextureLoad(strPath);
					}
					if (embeddedTextureData == null)
					{
						IntPtr intPtr = AssimpInterop.aiScene_GetEmbeddedTexture(scene, strPath);
						if (intPtr != IntPtr.Zero)
						{
							embeddedTextureData = BuildEmbeddedTextureData(scene, intPtr);
						}
					}
					materialData.DiffuseEmbeddedTextureData = embeddedTextureData;
				}
				materialData.DiffuseInfoLoaded = diffuseInfoLoaded;
				bool diffuseColorLoaded = false;
				if (AssimpInterop.aiMaterial_HasDiffuse(ptrMat) && AssimpInterop.aiMaterial_GetDiffuse(ptrMat, out var colorOut))
				{
					materialData.DiffuseColor = colorOut;
					diffuseColorLoaded = true;
				}
				materialData.DiffuseColorLoaded = diffuseColorLoaded;
				bool emissionColorLoaded = false;
				if (AssimpInterop.aiMaterial_HasEmissive(ptrMat) && AssimpInterop.aiMaterial_GetEmissive(ptrMat, out var colorOut2))
				{
					materialData.EmissionColor = colorOut2;
					emissionColorLoaded = true;
				}
				materialData.EmissionColorLoaded = emissionColorLoaded;
				bool emissionInfoLoaded = false;
				if (AssimpInterop.aiMaterial_GetNumTextureEmissive(ptrMat) != 0 && AssimpInterop.aiMaterial_GetTextureEmissive(ptrMat, 0u, out var strPath2, out var _, out var _, out var floatBlend2, out var uintOp2, out var uintMapMode2))
				{
					TextureWrapMode emissionWrapMode = ((uintMapMode2 == 1) ? TextureWrapMode.Clamp : TextureWrapMode.Repeat);
					string emissionName = StringUtils.GenerateUniqueName(strPath2);
					materialData.EmissionPath = FixName(strPath2);
					materialData.EmissionWrapMode = emissionWrapMode;
					materialData.EmissionName = emissionName;
					materialData.EmissionBlendMode = floatBlend2;
					materialData.EmissionOp = uintOp2;
					emissionInfoLoaded = true;
					EmbeddedTextureData embeddedTextureData2 = null;
					if (this.EmbeddedTextureLoad != null)
					{
						embeddedTextureData2 = this.EmbeddedTextureLoad(strPath2);
					}
					if (embeddedTextureData2 == null)
					{
						IntPtr intPtr2 = AssimpInterop.aiScene_GetEmbeddedTexture(scene, strPath2);
						if (intPtr2 != IntPtr.Zero)
						{
							embeddedTextureData2 = BuildEmbeddedTextureData(scene, intPtr2);
						}
					}
					materialData.EmissionEmbeddedTextureData = embeddedTextureData2;
				}
				materialData.EmissionInfoLoaded = emissionInfoLoaded;
				bool specularColorLoaded = false;
				if (AssimpInterop.aiMaterial_HasSpecular(ptrMat) && AssimpInterop.aiMaterial_GetSpecular(ptrMat, out var colorOut3))
				{
					materialData.SpecularColor = colorOut3;
					specularColorLoaded = true;
				}
				materialData.SpecularColorLoaded = specularColorLoaded;
				bool specularInfoLoaded = false;
				if (AssimpInterop.aiMaterial_GetNumTextureSpecular(ptrMat) != 0 && AssimpInterop.aiMaterial_GetTextureSpecular(ptrMat, 0u, out var strPath3, out var _, out var _, out var floatBlend3, out var uintOp3, out var uintMapMode3))
				{
					TextureWrapMode specularWrapMode = ((uintMapMode3 == 1) ? TextureWrapMode.Clamp : TextureWrapMode.Repeat);
					string specularName = StringUtils.GenerateUniqueName(strPath3);
					materialData.SpecularPath = FixName(strPath3);
					materialData.SpecularWrapMode = specularWrapMode;
					materialData.SpecularName = specularName;
					materialData.SpecularBlendMode = floatBlend3;
					materialData.SpecularOp = uintOp3;
					specularInfoLoaded = true;
					EmbeddedTextureData embeddedTextureData3 = null;
					if (this.EmbeddedTextureLoad != null)
					{
						embeddedTextureData3 = this.EmbeddedTextureLoad(strPath3);
					}
					if (embeddedTextureData3 == null)
					{
						IntPtr intPtr3 = AssimpInterop.aiScene_GetEmbeddedTexture(scene, strPath3);
						if (intPtr3 != IntPtr.Zero)
						{
							embeddedTextureData3 = BuildEmbeddedTextureData(scene, intPtr3);
						}
					}
					materialData.SpecularEmbeddedTextureData = embeddedTextureData3;
				}
				materialData.SpecularInfoLoaded = specularInfoLoaded;
				bool normalInfoLoaded = false;
				if (AssimpInterop.aiMaterial_GetNumTextureNormals(ptrMat) != 0 && AssimpInterop.aiMaterial_GetTextureNormals(ptrMat, 0u, out var strPath4, out var _, out var _, out var floatBlend4, out var uintOp4, out var uintMapMode4))
				{
					TextureWrapMode normalWrapMode = ((uintMapMode4 == 1) ? TextureWrapMode.Clamp : TextureWrapMode.Repeat);
					string normalName = StringUtils.GenerateUniqueName(strPath4);
					materialData.NormalPath = FixName(strPath4);
					materialData.NormalWrapMode = normalWrapMode;
					materialData.NormalName = normalName;
					materialData.NormalBlendMode = floatBlend4;
					materialData.NormalOp = uintOp4;
					normalInfoLoaded = true;
					EmbeddedTextureData embeddedTextureData4 = null;
					if (this.EmbeddedTextureLoad != null)
					{
						embeddedTextureData4 = this.EmbeddedTextureLoad(strPath4);
					}
					if (embeddedTextureData4 == null)
					{
						IntPtr intPtr4 = AssimpInterop.aiScene_GetEmbeddedTexture(scene, strPath4);
						if (intPtr4 != IntPtr.Zero)
						{
							embeddedTextureData4 = BuildEmbeddedTextureData(scene, intPtr4);
						}
					}
					materialData.NormalEmbeddedTextureData = embeddedTextureData4;
				}
				materialData.NormalInfoLoaded = normalInfoLoaded;
				bool heightInfoLoaded = false;
				if (AssimpInterop.aiMaterial_GetNumTextureHeight(ptrMat) != 0 && AssimpInterop.aiMaterial_GetTextureHeight(ptrMat, 0u, out var strPath5, out var _, out var _, out var floatBlend5, out var uintOp5, out var uintMapMode5))
				{
					TextureWrapMode heightWrapMode = ((uintMapMode5 == 1) ? TextureWrapMode.Clamp : TextureWrapMode.Repeat);
					string heightName = StringUtils.GenerateUniqueName(strPath5);
					materialData.HeightPath = FixName(strPath5);
					materialData.HeightWrapMode = heightWrapMode;
					materialData.HeightName = heightName;
					materialData.HeightBlendMode = floatBlend5;
					materialData.HeightOp = uintOp5;
					heightInfoLoaded = true;
					EmbeddedTextureData embeddedTextureData5 = null;
					if (this.EmbeddedTextureLoad != null)
					{
						embeddedTextureData5 = this.EmbeddedTextureLoad(strPath5);
					}
					if (embeddedTextureData5 == null)
					{
						IntPtr intPtr5 = AssimpInterop.aiScene_GetEmbeddedTexture(scene, strPath5);
						if (intPtr5 != IntPtr.Zero)
						{
							embeddedTextureData5 = BuildEmbeddedTextureData(scene, intPtr5);
						}
					}
					materialData.HeightEmbeddedTextureData = embeddedTextureData5;
				}
				materialData.HeightInfoLoaded = heightInfoLoaded;
				bool bumpScaleLoaded = false;
				if (AssimpInterop.aiMaterial_HasBumpScaling(ptrMat) && AssimpInterop.aiMaterial_GetBumpScaling(ptrMat, out var floatOut2))
				{
					if (Mathf.Approximately(floatOut2, 0f))
					{
						floatOut2 = 1f;
					}
					materialData.BumpScale = floatOut2;
					bumpScaleLoaded = true;
				}
				materialData.BumpScaleLoaded = bumpScaleLoaded;
				bool glossinessLoaded = false;
				if (AssimpInterop.aiMaterial_HasShininess(ptrMat) && AssimpInterop.aiMaterial_GetShininess(ptrMat, out var floatOut3))
				{
					materialData.Glossiness = floatOut3;
					glossinessLoaded = true;
				}
				materialData.GlossinessLoaded = glossinessLoaded;
				bool glossMapScaleLoaded = false;
				if (AssimpInterop.aiMaterial_HasShininessStrength(ptrMat) && AssimpInterop.aiMaterial_GetShininessStrength(ptrMat, out var floatOut4))
				{
					materialData.GlossMapScale = floatOut4;
					glossMapScaleLoaded = true;
				}
				materialData.GlossMapScaleLoaded = glossMapScaleLoaded;
				MaterialData[num] = materialData;
			}
		}

		private EmbeddedTextureData BuildEmbeddedTextureData(IntPtr scene, IntPtr texture)
		{
			bool flag = !AssimpInterop.aiMaterial_IsEmbeddedTextureCompressed(scene, texture);
			uint uintSize = AssimpInterop.aiMaterial_GetEmbeddedTextureDataSize(scene, texture, !flag);
			return new EmbeddedTextureData
			{
				IsRawData = flag,
				Data = AssimpInterop.aiMaterial_GetEmbeddedTextureData(scene, texture, uintSize),
				Width = AssimpInterop.aiMaterial_GetEmbeddedTextureWidth(texture),
				Height = AssimpInterop.aiMaterial_GetEmbeddedTextureHeight(texture)
			};
		}

		private void BuildBones(IntPtr scene)
		{
			uint num = AssimpInterop.aiScene_GetNumMeshes(scene);
			for (uint num2 = 0u; num2 < num; num2++)
			{
				MeshData meshData = MeshData[num2];
				IntPtr ptrMesh = AssimpInterop.aiScene_GetMesh(scene, num2);
				bool flag = (meshData.HasBoneInfo = AssimpInterop.aiMesh_HasBones(ptrMesh));
				if (flag)
				{
					HasBoneInfo = true;
				}
				if (!flag)
				{
					continue;
				}
				uint num3 = AssimpInterop.aiMesh_VertexCount(ptrMesh);
				uint num4 = AssimpInterop.aiMesh_GetNumBones(ptrMesh);
				meshData.BindPoses = new Matrix4x4[num4];
				meshData.BoneNames = new string[num4];
				meshData.BoneWeights = new BoneWeight[num3];
				int[] array = new int[num3];
				for (uint num5 = 0u; num5 < num4; num5++)
				{
					IntPtr ptrBone = AssimpInterop.aiMesh_GetBone(ptrMesh, num5);
					string text = AssimpInterop.aiBone_GetName(ptrBone);
					meshData.BoneNames[num5] = text;
					Matrix4x4 matrix4x = AssimpInterop.aiBone_GetOffsetMatrix(ptrBone);
					meshData.BindPoses[num5] = matrix4x;
					uint num6 = AssimpInterop.aiBone_GetNumWeights(ptrBone);
					for (uint num7 = 0u; num7 < num6; num7++)
					{
						int num8 = (int)num5;
						IntPtr ptrVweight = AssimpInterop.aiBone_GetWeights(ptrBone, num7);
						float num9 = AssimpInterop.aiVertexWeight_GetWeight(ptrVweight);
						uint num10 = AssimpInterop.aiVertexWeight_GetVertexId(ptrVweight);
						switch (array[num10])
						{
						case 0:
						{
							BoneWeight boneWeight = new BoneWeight
							{
								boneIndex0 = num8,
								weight0 = num9
							};
							meshData.BoneWeights[num10] = boneWeight;
							break;
						}
						case 1:
						{
							BoneWeight boneWeight = meshData.BoneWeights[num10];
							boneWeight.boneIndex1 = num8;
							boneWeight.weight1 = num9;
							meshData.BoneWeights[num10] = boneWeight;
							break;
						}
						case 2:
						{
							BoneWeight boneWeight = meshData.BoneWeights[num10];
							boneWeight.boneIndex2 = num8;
							boneWeight.weight2 = num9;
							meshData.BoneWeights[num10] = boneWeight;
							break;
						}
						case 3:
						{
							BoneWeight boneWeight = meshData.BoneWeights[num10];
							boneWeight.boneIndex3 = num8;
							boneWeight.weight3 = num9;
							meshData.BoneWeights[num10] = boneWeight;
							break;
						}
						default:
						{
							BoneWeight boneWeight = meshData.BoneWeights[num10];
							boneWeight.boneIndex3 = num8;
							boneWeight.weight3 = num9;
							meshData.BoneWeights[num10] = boneWeight;
							break;
						}
						}
						array[num10]++;
					}
				}
			}
		}

		private void BuildAnimations(IntPtr scene, AssetLoaderOptions options)
		{
			uint num = AssimpInterop.aiScene_GetNumAnimations(scene);
			for (uint num2 = 0u; num2 < num; num2++)
			{
				IntPtr ptrAnimation = AssimpInterop.aiScene_GetAnimation(scene, num2);
				float num3 = AssimpInterop.aiAnimation_GetTicksPerSecond(ptrAnimation);
				if (num3 <= 0f)
				{
					num3 = 60f;
				}
				float length = AssimpInterop.aiAnimation_GetDuraction(ptrAnimation) / num3;
				uint num4 = AssimpInterop.aiAnimation_GetNumChannels(ptrAnimation);
				string text = AssimpInterop.aiAnimation_GetName(ptrAnimation);
				AnimationData animationData = new AnimationData
				{
					Name = (string.IsNullOrEmpty(text) ? ("Animation_" + StringUtils.GenerateUniqueName(num2)) : text),
					Legacy = (options == null || options.UseLegacyAnimations),
					FrameRate = num3,
					Length = length,
					ChannelData = new AnimationChannelData[num4]
				};
				for (uint num5 = 0u; num5 < num4; num5++)
				{
					IntPtr ptrNodeAnim = AssimpInterop.aiAnimation_GetAnimationChannel(ptrAnimation, num5);
					string nodeName = AssimpInterop.aiNodeAnim_GetNodeName(ptrNodeAnim);
					AnimationChannelData animationChannelData = new AnimationChannelData
					{
						CurveData = new Dictionary<string, AnimationCurveData>(),
						NodeName = nodeName
					};
					uint num6 = AssimpInterop.aiNodeAnim_GetNumPositionKeys(ptrNodeAnim);
					if (num6 != 0)
					{
						AnimationCurveData animationCurveData = new AnimationCurveData(num6);
						AnimationCurveData animationCurveData2 = new AnimationCurveData(num6);
						AnimationCurveData animationCurveData3 = new AnimationCurveData(num6);
						for (uint num7 = 0u; num7 < num6; num7++)
						{
							IntPtr ptrVectorKey = AssimpInterop.aiNodeAnim_GetPositionKey(ptrNodeAnim, num7);
							float time = AssimpInterop.aiVectorKey_GetTime(ptrVectorKey) / num3;
							float[] array = AssimpInterop.aiVectorKey_GetValue(ptrVectorKey);
							animationCurveData.AddKey(time, array[0]);
							animationCurveData2.AddKey(time, array[1]);
							animationCurveData3.AddKey(time, array[2]);
						}
						animationChannelData.SetCurve("localPosition.x", animationCurveData);
						animationChannelData.SetCurve("localPosition.y", animationCurveData2);
						animationChannelData.SetCurve("localPosition.z", animationCurveData3);
					}
					uint num8 = AssimpInterop.aiNodeAnim_GetNumRotationKeys(ptrNodeAnim);
					if (num8 != 0)
					{
						AnimationCurveData animationCurveData4 = new AnimationCurveData(num8);
						AnimationCurveData animationCurveData5 = new AnimationCurveData(num8);
						AnimationCurveData animationCurveData6 = new AnimationCurveData(num8);
						AnimationCurveData animationCurveData7 = new AnimationCurveData(num8);
						for (uint num9 = 0u; num9 < num8; num9++)
						{
							IntPtr ptrQuatKey = AssimpInterop.aiNodeAnim_GetRotationKey(ptrNodeAnim, num9);
							float time2 = AssimpInterop.aiQuatKey_GetTime(ptrQuatKey) / num3;
							float[] array2 = AssimpInterop.aiQuatKey_GetValue(ptrQuatKey);
							animationCurveData4.AddKey(time2, array2[1]);
							animationCurveData5.AddKey(time2, array2[2]);
							animationCurveData6.AddKey(time2, array2[3]);
							animationCurveData7.AddKey(time2, array2[0]);
						}
						animationChannelData.SetCurve("localRotation.x", animationCurveData4);
						animationChannelData.SetCurve("localRotation.y", animationCurveData5);
						animationChannelData.SetCurve("localRotation.z", animationCurveData6);
						animationChannelData.SetCurve("localRotation.w", animationCurveData7);
					}
					uint num10 = AssimpInterop.aiNodeAnim_GetNumScalingKeys(ptrNodeAnim);
					if (num10 != 0)
					{
						AnimationCurveData animationCurveData8 = new AnimationCurveData(num10);
						AnimationCurveData animationCurveData9 = new AnimationCurveData(num10);
						AnimationCurveData animationCurveData10 = new AnimationCurveData(num10);
						for (uint num11 = 0u; num11 < num10; num11++)
						{
							IntPtr ptrVectorKey2 = AssimpInterop.aiNodeAnim_GetScalingKey(ptrNodeAnim, num11);
							float time3 = AssimpInterop.aiVectorKey_GetTime(ptrVectorKey2) / num3;
							float[] array3 = AssimpInterop.aiVectorKey_GetValue(ptrVectorKey2);
							animationCurveData8.AddKey(time3, array3[0]);
							animationCurveData9.AddKey(time3, array3[1]);
							animationCurveData10.AddKey(time3, array3[2]);
						}
						animationChannelData.SetCurve("localScale.x", animationCurveData8);
						animationChannelData.SetCurve("localScale.y", animationCurveData9);
						animationChannelData.SetCurve("localScale.z", animationCurveData10);
					}
					animationData.ChannelData[num5] = animationChannelData;
				}
				animationData.WrapMode = ((options != null) ? options.AnimationWrapMode : WrapMode.Loop);
				AnimationData[num2] = animationData;
			}
		}

		protected virtual string FixNodeName(string name, uint nodeId)
		{
			if (string.IsNullOrEmpty(name))
			{
				return nodeId.ToString();
			}
			if (NodesPath != null && NodesPath.ContainsKey(name))
			{
				return name + nodeId;
			}
			return name;
		}

		protected virtual string FixName(string name, uint id)
		{
			if (!string.IsNullOrEmpty(name))
			{
				return name;
			}
			return StringUtils.GenerateUniqueName(id);
		}

		protected virtual string FixName(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				return name;
			}
			return Guid.NewGuid().ToString();
		}

		public void Dispose()
		{
			RootNodeData = null;
			MaterialData = null;
			MeshData = null;
			AnimationData = null;
			CameraData = null;
			Metadata = null;
			NodesPath = null;
			LoadedMaterials = null;
			LoadedTextures = null;
			LoadedBoneNames = null;
			NodeId = 0u;
			HasBoneInfo = false;
		}
	}
}
