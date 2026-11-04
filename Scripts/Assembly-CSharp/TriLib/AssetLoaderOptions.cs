using System;
using System.Collections.Generic;
using UnityEngine;

namespace TriLib
{
	[Serializable]
	public class AssetLoaderOptions : ScriptableObject
	{
		public bool AddAssetUnloader;

		public bool DontLoadAnimations;

		public bool DontApplyAnimations;

		public bool DontLoadLights = true;

		public bool DontLoadCameras = true;

		public bool AutoPlayAnimations = true;

		public WrapMode AnimationWrapMode = WrapMode.Loop;

		public bool UseLegacyAnimations = true;

		public bool EnsureQuaternionContinuity = true;

		public RuntimeAnimatorController AnimatorController;

		public Avatar Avatar;

		public bool DontGenerateAvatar;

		public bool DontLoadMetadata;

		public bool DontLoadMaterials;

		public bool ApplyAlphaMaterials = true;

		public bool DisableAlphaMaterials;

		public bool UseCutoutMaterials = true;

		public bool UseStandardSpecularMaterial;

		public bool DontLoadMeshes;

		public bool CombineMeshes = true;

		public bool Use32BitsIndexFormat = true;

		public bool GenerateMeshColliders;

		public bool ConvexMeshColliders;

		[Obsolete("MaterialsOverride is not used anymore. If you want to change loaded materials source, override LoadMaterial method from AssetLoaderBase.")]
		public List<Material> MaterialsOverride = new List<Material>();

		public Vector3 RotationAngles = new Vector3(0f, 180f, 0f);

		public float Scale = 1f;

		public AssimpPostProcessSteps PostProcessSteps = AssimpPostProcessSteps.CalcTangentSpace | AssimpPostProcessSteps.JoinIdenticalVertices | AssimpPostProcessSteps.MakeLeftHanded | AssimpPostProcessSteps.Triangulate | AssimpPostProcessSteps.GenSmoothNormals | AssimpPostProcessSteps.LimitBoneWeights | AssimpPostProcessSteps.ValidateDataStructure | AssimpPostProcessSteps.ImproveCacheLocality | AssimpPostProcessSteps.RemoveRedundantMaterials | AssimpPostProcessSteps.SortByPType | AssimpPostProcessSteps.FindInvalidData | AssimpPostProcessSteps.GenUvCoords | AssimpPostProcessSteps.FindInstances | AssimpPostProcessSteps.OptimizeMeshes | AssimpPostProcessSteps.FlipWindingOrder;

		[Obsolete("TexturesPathOverride is not used anymore. If you want to change loaded textures path, override LoadTextureFromFile method from AssetLoaderBase.")]
		public string TexturesPathOverride;

		public TextureCompression TextureCompression = TextureCompression.NormalQuality;

		public List<AssetAdvancedConfig> AdvancedConfigs = new List<AssetAdvancedConfig>
		{
			AssetAdvancedConfig.CreateConfig(AssetAdvancedPropertyClassNames.SplitLargeMeshesVertexLimit, 65000),
			AssetAdvancedConfig.CreateConfig(AssetAdvancedPropertyClassNames.FBXImportReadLights, value: false),
			AssetAdvancedConfig.CreateConfig(AssetAdvancedPropertyClassNames.FBXImportReadCameras, value: false)
		};

		public static AssetLoaderOptions CreateInstance()
		{
			return ScriptableObject.CreateInstance<AssetLoaderOptions>();
		}

		public void Deserialize(string json)
		{
			JsonUtility.FromJsonOverwrite(json, this);
		}

		public string Serialize()
		{
			return JsonUtility.ToJson(this);
		}
	}
}
