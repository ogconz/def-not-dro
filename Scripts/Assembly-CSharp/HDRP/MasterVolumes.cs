using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using HDRP.Scalability;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace HDRP
{
	public static class MasterVolumes
	{
		private static int s_lastAnisotropicLevel = -1;

		private static float s_motionBlurIntensity;

		private static MotionBlur s_motionBlurComponent;

		private static VolumeProfile s_qualityVolumeProfile;

		[RuntimeInitializeOnLoadMethod]
		private static void OnLoad()
		{
			GameObject gameObject = new GameObject("master_volume");
			gameObject.hideFlags = HideFlags.HideAndDontSave;
			VolumeProfile[] array = Resources.LoadAll<VolumeProfile>("MasterProfiles");
			if (array.Length != 0)
			{
				foreach (VolumeProfile sharedProfile in array)
				{
					Volume volume = gameObject.AddComponent<Volume>();
					volume.isGlobal = true;
					volume.sharedProfile = sharedProfile;
					volume.priority = -2f;
				}
			}
			Volume volume2 = gameObject.AddComponent<Volume>();
			volume2.isGlobal = true;
			volume2.priority = 1f;
			if (!Application.isPlaying)
			{
				volume2.sharedProfile = Resources.Load<VolumeProfile>("QualityProfiles/QualityProfile_Default");
			}
			else
			{
				s_qualityVolumeProfile = volume2.profile;
			}
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
		}

		private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			if (!camera.CompareTag("MainCamera"))
			{
				return;
			}
			HDCamera orCreate = HDCamera.GetOrCreate(camera);
			if (orCreate == null || orCreate.volumeStack == null)
			{
				return;
			}
			VolumeStack volumeStack = orCreate.volumeStack;
			AntiAliasingComponent component = volumeStack.GetComponent<AntiAliasingComponent>();
			if (component != null && component.active && camera.cameraType == CameraType.Game)
			{
				HDAdditionalCameraData component2 = camera.GetComponent<HDAdditionalCameraData>();
				if (component2 != null)
				{
					component2.antialiasing = component.mode.value;
					component2.SMAAQuality = component.smaaQuality.value;
					component2.TAAQuality = component.taaQuality.value;
					component2.taaSharpenStrength = component.taaSharpenStrength.value;
					component2.taaHistorySharpening = component.taaHistorySharpening.value;
					component2.taaAntiFlicker = component.taaAntiFlicker.value;
					component2.taaMotionVectorRejection = component.taaMotionVectorRejection.value;
					component2.taaAntiHistoryRinging = component.taaAntiHistoryRinging.value;
				}
			}
			TextureQualityComponent component3 = volumeStack.GetComponent<TextureQualityComponent>();
			if (component3 != null && component3.active && component3.value.value != (TextureQualityComponent.TextureQuality)QualitySettings.globalTextureMipmapLimit)
			{
				QualitySettings.globalTextureMipmapLimit = (int)component3.value.value;
				GraphicsSettings.renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
			}
			AnisotropicLevelComponent component4 = volumeStack.GetComponent<AnisotropicLevelComponent>();
			if (component4 != null && component4.active && component4.value.value != s_lastAnisotropicLevel)
			{
				s_lastAnisotropicLevel = component4.value.value;
				Texture.SetGlobalAnisotropicFilteringLimits(s_lastAnisotropicLevel, s_lastAnisotropicLevel);
			}
			float num = 1f;
			LODBiasComponent component5 = volumeStack.GetComponent<LODBiasComponent>();
			if (component5 != null && component5.active)
			{
				num *= component5.value.value;
			}
			LODScaleComponent component6 = volumeStack.GetComponent<LODScaleComponent>();
			if (component6 != null && component6.active)
			{
				num *= component6.value.value;
			}
			if (Math.Abs(num - QualitySettings.lodBias) > Mathf.Epsilon)
			{
				QualitySettings.lodBias = num;
			}
			VSyncComponent component7 = volumeStack.GetComponent<VSyncComponent>();
			if (component7 != null && component7.active && component7.value.value != (VSyncComponent.VSyncMode)QualitySettings.vSyncCount)
			{
				QualitySettings.vSyncCount = (int)component7.value.value;
			}
		}

		public static void OverrideComponent<T>(T state) where T : VolumeComponent
		{
			if (state == null)
			{
				s_qualityVolumeProfile.Remove<T>();
				return;
			}
			if (!s_qualityVolumeProfile.TryGet<T>(out var component))
			{
				component = s_qualityVolumeProfile.Add<T>();
			}
			component.SetAllOverridesTo(state: false);
			ReadOnlyCollection<VolumeParameter> parameters = component.parameters;
			ReadOnlyCollection<VolumeParameter> parameters2 = state.parameters;
			int i = 0;
			for (int count = parameters.Count; i < count; i++)
			{
				VolumeParameter volumeParameter = parameters2[i];
				VolumeParameter volumeParameter2 = parameters[i];
				if (volumeParameter.overrideState)
				{
					volumeParameter2.overrideState = true;
					volumeParameter2.SetValue(volumeParameter);
				}
			}
		}

		public static void LogQualityStates()
		{
			if (s_qualityVolumeProfile == null)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(">>> QualityStates");
			foreach (VolumeComponent component in s_qualityVolumeProfile.components)
			{
				if (!component.active)
				{
					continue;
				}
				Type type = component.GetType();
				FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
				stringBuilder.AppendLine(" " + type.Name);
				for (int i = 0; i < component.parameters.Count; i++)
				{
					if (component.parameters[i].overrideState && i < fields.Length)
					{
						stringBuilder.AppendLine($"   - {fields[i].Name} : {component.parameters[i]}");
					}
				}
			}
			stringBuilder.AppendLine("> QualitySetting");
			stringBuilder.AppendLine($" - vSyncCount: {QualitySettings.vSyncCount}");
			stringBuilder.AppendLine($" - lodBias: {QualitySettings.lodBias}");
			stringBuilder.AppendLine($" - masterTextureLimit: {QualitySettings.globalTextureMipmapLimit}");
			Debug.Log(stringBuilder.ToString());
		}
	}
}
