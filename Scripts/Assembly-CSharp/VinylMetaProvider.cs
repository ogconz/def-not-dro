using System;
using System.Collections.Generic;
using DB.Meta;
using DI;
using GameOverlay;
using UnityEngine;
using VinylSystem;

[DefaultExecutionOrder(-65536)]
public class VinylMetaProvider : Singleton<VinylMetaProvider>, IAlwaysAccessibleOnQuit
{
	public const string CategoryPath = "Vinyls/CategoryIcons/";

	[SerializeField]
	private VinylsSystemMeta m_meta;

	[SerializeField]
	private VinylsSystemMeta m_metaConsoles;

	private static Texture2D m_previewDefaultTexture;

	public VinylsSystemMeta meta => m_meta;

	public static VinylSystemConfig config
	{
		get
		{
			if (VinylSystemConfig.config == null)
			{
				VinylSystemConfig.InitConfig(ResourcesWrapper.Load<VinylSystemConfig>("VinylSystemConfig"));
			}
			return VinylSystemConfig.config;
		}
	}

	public List<VinylCategorysMeta> GetCategorys()
	{
		return meta.categorys;
	}

	protected override void Initialization()
	{
		base.Initialization();
		VinylSystemKernel.onTryGetRawLayerAction = (VinylSystemKernel.TryGetRawLayerAction)Delegate.Combine(VinylSystemKernel.onTryGetRawLayerAction, new VinylSystemKernel.TryGetRawLayerAction(OnTryGetRawLayerAction));
		VinylSystemKernel.isValidLayer = (Func<VinylLayer, bool>)Delegate.Combine(VinylSystemKernel.isValidLayer, new Func<VinylLayer, bool>(IsValidLayer));
		VinylSystemKernel.requestCompressionQualityMode = (Action)Delegate.Combine(VinylSystemKernel.requestCompressionQualityMode, new Action(OnVinylCompressionModeRequest));
		Overlay.OnReady = (Action)Delegate.Combine(Overlay.OnReady, new Action(OnOverlayLoaded));
	}

	private bool OnTryGetRawLayerAction(int id, out string texturePath, out string flippedTexturePath, out Color[] defaultColors, out int[] defaultMaterialProps)
	{
		VinylMeta vinylById = meta.GetVinylById(id);
		if (vinylById == null)
		{
			texturePath = null;
			flippedTexturePath = null;
			defaultColors = null;
			defaultMaterialProps = null;
			return false;
		}
		texturePath = vinylById.texturePath;
		flippedTexturePath = vinylById.flippedTexturePath;
		defaultColors = vinylById.defaultColors;
		defaultMaterialProps = vinylById.defaultMaterialProps;
		return true;
	}

	protected override void Finalization()
	{
		base.Finalization();
		VinylSystemKernel.onTryGetRawLayerAction = (VinylSystemKernel.TryGetRawLayerAction)Delegate.Remove(VinylSystemKernel.onTryGetRawLayerAction, new VinylSystemKernel.TryGetRawLayerAction(OnTryGetRawLayerAction));
		VinylSystemKernel.isValidLayer = (Func<VinylLayer, bool>)Delegate.Remove(VinylSystemKernel.isValidLayer, new Func<VinylLayer, bool>(IsValidLayer));
		VinylSystemKernel.requestCompressionQualityMode = (Action)Delegate.Remove(VinylSystemKernel.requestCompressionQualityMode, new Action(OnVinylCompressionModeRequest));
		GraphicsQuality graphicsQuality = DependencyInjector.Resolve<GraphicsQuality>();
		if ((bool)graphicsQuality)
		{
			graphicsQuality.OnVinylCompressionModeChange = (Action<bool>)Delegate.Remove(graphicsQuality.OnVinylCompressionModeChange, new Action<bool>(EnableVinylCompression));
		}
		Overlay.OnReady = (Action)Delegate.Remove(Overlay.OnReady, new Action(OnVinylCompressionModeRequest));
	}

	public bool IsValidLayer(VinylLayer layer)
	{
		return meta.GetVinylById(layer.rawId) != null;
	}

	public static Sprite LoadCategorySprite(string path)
	{
		return ResourcesWrapper.Load<Sprite>("Vinyls/CategoryIcons/" + path);
	}

	public static Sprite LoadVinylPreviewSprite(string texturePath)
	{
		return ResourcesWrapper.Load<Sprite>(config.previewsLocation + texturePath);
	}

	public static Texture2D LoadPreviewTexture(string texturePath)
	{
		return ResourcesWrapper.Load<Texture2D>(config.previewsLocation + texturePath);
	}

	public static Texture2D LoadTexture(string texturePath)
	{
		return ResourcesWrapper.Load<Texture2D>(config.texturesLocation + texturePath);
	}

	public static Texture2D GetPreviewDefaultTexture()
	{
		if (m_previewDefaultTexture == null)
		{
			m_previewDefaultTexture = LoadPreviewTexture("vinyl_empty2");
		}
		return m_previewDefaultTexture;
	}

	private static void OnOverlayLoaded()
	{
		GraphicsQuality graphicsQuality = DependencyInjector.Resolve<GraphicsQuality>();
		if ((bool)graphicsQuality)
		{
			graphicsQuality.OnVinylCompressionModeChange = (Action<bool>)Delegate.Combine(graphicsQuality.OnVinylCompressionModeChange, new Action<bool>(EnableVinylCompression));
			EnableVinylCompression(graphicsQuality.compressVinyls);
		}
	}

	private static void OnVinylCompressionModeRequest()
	{
		GraphicsQuality graphicsQuality = DependencyInjector.Resolve<GraphicsQuality>();
		if ((bool)graphicsQuality)
		{
			EnableVinylCompression(graphicsQuality.compressVinyls);
		}
	}

	private static void EnableVinylCompression(bool on)
	{
		if ((bool)VinylSystemKernel.instance)
		{
			VinylSystemKernel.instance.worker?.EnableVinylCompression(on);
		}
	}
}
