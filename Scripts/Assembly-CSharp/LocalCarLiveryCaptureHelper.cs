using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameOverlay;
using MessagePack;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using VinylSystem;

public class LocalCarLiveryCaptureHelper
{
	public const int TexBodyMaxSize = 2048;

	public const int TexGlassMaxSize = 1024;

	private bool m_notValidAtLeastOneTexture;

	private CarTextureType[] m_textureTypes;

	private Dictionary<CarTextureType, Texture2D> m_textureCacheDict = new Dictionary<CarTextureType, Texture2D>(4);

	public bool liveryNetworkDataReadyToUpload => !m_notValidAtLeastOneTexture;

	public string carLiveryHash { get; private set; }

	public byte[] liveryNetworkData { get; private set; }

	public LocalCarLiveryCaptureHelper()
	{
		m_textureCacheDict[CarTextureType.TextureBody] = new Texture2D(2048, 2048, TextureFormat.RGBA32, 0, linear: false, createUninitialized: true);
		m_textureCacheDict[CarTextureType.MaterialBody] = new Texture2D(2048, 2048, TextureFormat.RGBA32, 0, linear: false, createUninitialized: true);
		m_textureCacheDict[CarTextureType.TextureGlass] = new Texture2D(1024, 1024, TextureFormat.RGBA32, 0, linear: false, createUninitialized: true);
		m_textureCacheDict[CarTextureType.MaterialGlass] = new Texture2D(1024, 1024, TextureFormat.RGBA32, 0, linear: false, createUninitialized: true);
	}

	~LocalCarLiveryCaptureHelper()
	{
		foreach (KeyValuePair<CarTextureType, Texture2D> item in m_textureCacheDict)
		{
			UnityEngine.Object.Destroy(item.Value);
		}
		m_textureCacheDict.Clear();
		m_textureCacheDict = null;
	}

	public async UniTask CaptureLocalCarLivery(RaceCar car)
	{
		if (car == null || car.carModel == null)
		{
			return;
		}
		if (m_textureTypes == null)
		{
			m_textureTypes = Enum.GetValues(typeof(CarTextureType)) as CarTextureType[];
		}
		if (m_textureTypes == null)
		{
			return;
		}
		Livery livery = car.carModel.livery;
		await UniTask.WaitUntil(() => livery != null && livery.IsReady);
		bool vinylsNotSupport = car.vinylsNotSupport;
		m_notValidAtLeastOneTexture = false;
		TextureNetworkData textureData = default(TextureNetworkData);
		CarTextureType[] textureTypes = m_textureTypes;
		foreach (CarTextureType textureType in textureTypes)
		{
			if (textureType == CarTextureType.Unknown)
			{
				continue;
			}
			if (m_notValidAtLeastOneTexture)
			{
				CallEmptyCallback(textureType);
				continue;
			}
			Texture texture = GetTexture(textureType, livery);
			if (texture == null)
			{
				CallEmptyCallback(textureType);
				continue;
			}
			if (!vinylsNotSupport)
			{
				List<VinylLayer> layers = livery.layers;
				if ((layers == null || layers.Count != 0) && !IsTextureNotValid(textureType, texture.width, texture.height) && m_textureCacheDict.TryGetValue(textureType, out var result))
				{
					bool doneDuplicateTextureRequest = false;
					await DuplicateTexture(texture, delegate(AsyncGPUReadbackRequest request)
					{
						if (request.hasError)
						{
							CallEmptyCallback(textureType);
						}
						else
						{
							NativeArray<Color32> data = request.GetData<Color32>();
							result.Reinitialize(request.width, request.height, TextureFormat.RGBA32, hasMipMap: false);
							result.LoadRawTextureData(data);
							result.Apply();
							data.Dispose();
						}
					}, delegate
					{
						doneDuplicateTextureRequest = true;
					});
					while (!doneDuplicateTextureRequest)
					{
						await UniTask.Yield();
					}
					result.Compress(highQuality: true);
					byte[] array = await CompressTextureAsync(result);
					if (array == null)
					{
						CallEmptyCallback(textureType);
						return;
					}
					switch (textureType)
					{
					case CarTextureType.TextureBody:
						textureData.bodyTex = array;
						textureData.bodySize = result.width;
						textureData.bodyFormat = (int)result.format;
						break;
					case CarTextureType.TextureGlass:
						textureData.glassTex = array;
						textureData.glassSize = result.width;
						textureData.glassFormat = (int)result.format;
						break;
					case CarTextureType.MaterialBody:
						textureData.bodyMat = array;
						break;
					case CarTextureType.MaterialGlass:
						textureData.glassMat = array;
						break;
					}
					continue;
				}
			}
			CallEmptyCallback(textureType);
		}
		if (!m_notValidAtLeastOneTexture)
		{
			liveryNetworkData = MessagePackSerializer.Serialize(textureData);
			carLiveryHash = $"{Overlay.instance.player.id.ToString().ToLower()}_{car.carId}_{CalculateHash(livery)}";
			await UniTask.NextFrame(PlayerLoopTiming.LastPostLateUpdate);
		}
		void CallEmptyCallback(CarTextureType carTextureType)
		{
			liveryNetworkData = null;
			m_notValidAtLeastOneTexture = true;
		}
	}

	private int CalculateHash(Livery livery)
	{
		if (livery == null)
		{
			return 0;
		}
		int num = 0;
		int num2 = 0;
		foreach (VinylLayer layer in livery.layers)
		{
			int num3 = layer.rawId * ++num2;
			num += layer.GetHashCode() * num3;
		}
		return num;
	}

	private async UniTask<byte[]> CompressTextureAsync(Texture2D texture2D)
	{
		if (!texture2D)
		{
			return null;
		}
		byte[] textureRawData = texture2D.GetRawTextureData();
		byte[] compressed = null;
		await UniTask.RunOnThreadPool(delegate
		{
			MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
			compressed = MessagePackSerializer.Serialize(textureRawData, options);
		});
		while (compressed == null)
		{
			await UniTask.Yield();
		}
		textureRawData = null;
		return compressed;
	}

	private async UniTask DuplicateTexture(Texture source, Action<AsyncGPUReadbackRequest> callback, Action onDone)
	{
		RenderTexture renderTex = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
		Graphics.Blit(source, renderTex);
		await AsyncGPUReadback.Request(renderTex, 0, TextureFormat.RGBA32, callback);
		RenderTexture.ReleaseTemporary(renderTex);
		onDone.SafeCall();
	}

	private bool IsTextureNotValid(CarTextureType textureType, int width, int height)
	{
		switch (textureType)
		{
		case CarTextureType.TextureBody:
		case CarTextureType.MaterialBody:
			if (width <= 2048)
			{
				return height > 2048;
			}
			return true;
		case CarTextureType.TextureGlass:
		case CarTextureType.MaterialGlass:
			if (width <= 1024)
			{
				return height > 1024;
			}
			return true;
		default:
			return true;
		}
	}

	private Texture GetTexture(CarTextureType textureType, Livery livery)
	{
		if (livery == null || !IsLiveryTexturesValid(livery.TextureArray) || !IsLiveryTexturesValid(livery.MaterialPropertiesArray))
		{
			return null;
		}
		return textureType switch
		{
			CarTextureType.TextureBody => livery.TextureArray[0], 
			CarTextureType.TextureGlass => livery.TextureArray[1], 
			CarTextureType.MaterialBody => livery.MaterialPropertiesArray[0], 
			CarTextureType.MaterialGlass => livery.MaterialPropertiesArray[1], 
			_ => null, 
		};
		static bool IsLiveryTexturesValid(Texture[] textures)
		{
			if (textures == null)
			{
				return false;
			}
			return textures.Length > 1;
		}
	}
}
