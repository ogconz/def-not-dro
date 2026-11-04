using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DI;
using MessagePack;
using UnityEngine;

namespace SyncMultiplayer.NetGameSystems
{
	public class NetGameTextureTransferSystem : BaseNetGameSystem
	{
		private Dictionary<NetworkPlayer, CancellationTokenSource> m_tokenSources = new Dictionary<NetworkPlayer, CancellationTokenSource>();

		private NetGamePlayerStateSystem m_playerState;

		private NetworkGame m_game;

		private CarVisualManager m_visualManager;

		private MessagePackSerializerOptions m_lz4Options;

		private CarTextureType[] m_carTextureTypes;

		public override void Attach(NetGameSystemsManager owner)
		{
			base.Attach(owner);
			m_game = owner.host;
			m_playerState = owner.Get<NetGamePlayerStateSystem>();
			m_visualManager = DependencyInjector.Resolve<CarVisualManager>();
			m_carTextureTypes = Enum.GetValues(typeof(CarTextureType)) as CarTextureType[];
			m_lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
		}

		public override void OnReconnect()
		{
			base.OnReconnect();
			UniTaskScheduler.UnobservedExceptionWriteLogType = LogType.Log;
			DisposeTokenSources();
			if (m_game.Players.Count < 2)
			{
				return;
			}
			foreach (NetworkPlayer player in m_game.Players)
			{
				DownloadUserLivery(player);
			}
		}

		public override void OnDisconnect()
		{
			base.OnDisconnect();
			DisposeTokenSources();
			m_game.Players?.ForEach(delegate(NetworkPlayer p)
			{
				p?.ClearTextures(m_carTextureTypes);
			});
		}

		public override void DoPlayerEnter(NetworkPlayer player)
		{
			base.DoPlayerEnter(player);
			DownloadUserLivery(player);
		}

		private void SetLoadingLiveryState(NetworkPlayer networkPlayer, bool isLoading)
		{
			if (networkPlayer != null)
			{
				if (isLoading)
				{
					m_playerState.AddStateForPlayer(networkPlayer, PlayerCarState.LoadLivery);
				}
				else
				{
					m_playerState.RemoveStateForPlayer(networkPlayer, PlayerCarState.LoadLivery);
				}
			}
		}

		public bool PlayerHasLivery(NetworkPlayer player)
		{
			if (!m_visualManager.CanApplyVinylsForNetworkCar(player.userCar))
			{
				return false;
			}
			if (player.userCar.vinylsNotSupport)
			{
				return false;
			}
			return player.PlayerProperties.HasLivery;
		}

		public void DownloadUserLivery(NetworkPlayer player)
		{
			if (player != null && !player.IsLocal)
			{
				DownloadUserLiveryAsync(player).Forget();
			}
		}

		private void FillCtsDictionary(NetworkPlayer player)
		{
			DisposeTokenSource(player);
			m_tokenSources[player] = new CancellationTokenSource();
		}

		public async UniTask DownloadUserLiveryAsync(NetworkPlayer player)
		{
			if (player == null)
			{
				return;
			}
			FillCtsDictionary(player);
			while (!player.userCar || !player.userCar.IsLiveryReady)
			{
				await UniTask.Yield();
			}
			if (!m_tokenSources.TryGetValue(player, out var value))
			{
				return;
			}
			CancellationToken cancelToken = value.Token;
			if (cancelToken.IsCancellationRequested || !PlayerHasLivery(player))
			{
				return;
			}
			SetLoadingLiveryState(player, isLoading: true);
			byte[] data = await m_game.carLiveryUploadService.DownloadLiveryData(player.PlayerId);
			if (data == null || cancelToken.IsCancellationRequested)
			{
				SetLoadingLiveryState(player, isLoading: false);
				GameLog.Print(AlertLevel.Error, "LiveryTransferSystem", "Failed to download livery data for " + player.OriginaNickName);
				return;
			}
			TextureNetworkData result = default(TextureNetworkData);
			bool errorCatched = false;
			await UniTask.RunOnThreadPool(delegate
			{
				try
				{
					result = MessagePackSerializer.Deserialize<TextureNetworkData>(data);
				}
				catch
				{
					errorCatched = true;
				}
			}, configureAwait: true, cancelToken);
			if (errorCatched || cancelToken.IsCancellationRequested)
			{
				SetLoadingLiveryState(player, isLoading: false);
				GameLog.Print(AlertLevel.Error, "LiveryTransferSystem", "Failed to deserizalize livery data for " + player.OriginaNickName);
				return;
			}
			int bodySize = result.bodySize;
			int glassSize = result.glassSize;
			TextureFormat bodyFormat = (TextureFormat)result.bodyFormat;
			TextureFormat glassFormat = (TextureFormat)result.glassFormat;
			await CreateAndSetTextures();
			await UniTask.WaitUntil(() => player.userCar.IsVisualLoaded);
			player.ApplyTextures();
			SetLoadingLiveryState(player, isLoading: false);
			async UniTask CreateAndSetTextures()
			{
				if (m_carTextureTypes != null)
				{
					CarTextureType[] carTextureTypes = m_carTextureTypes;
					foreach (CarTextureType carTextureType in carTextureTypes)
					{
						if (carTextureType != CarTextureType.Unknown)
						{
							int size = GetBodySize(carTextureType);
							TextureFormat format = GetTextureFormat(carTextureType);
							byte[] pngData = GetTextureData(carTextureType);
							await CreateAndSetTextureAsync(carTextureType, player, size, format, pngData, cancelToken);
						}
					}
				}
			}
			int GetBodySize(CarTextureType carTextureType)
			{
				if (carTextureType == CarTextureType.TextureBody || carTextureType == CarTextureType.MaterialBody)
				{
					return bodySize;
				}
				return glassSize;
			}
			byte[] GetTextureData(CarTextureType carTextureType)
			{
				return carTextureType switch
				{
					CarTextureType.TextureBody => result.bodyTex, 
					CarTextureType.TextureGlass => result.glassTex, 
					CarTextureType.MaterialBody => result.bodyMat, 
					CarTextureType.MaterialGlass => result.glassMat, 
					_ => null, 
				};
			}
			TextureFormat GetTextureFormat(CarTextureType carTextureType)
			{
				if (carTextureType == CarTextureType.TextureBody || carTextureType == CarTextureType.MaterialBody)
				{
					return bodyFormat;
				}
				return glassFormat;
			}
		}

		private async UniTask CreateAndSetTextureAsync(CarTextureType carTextureType, NetworkPlayer networkPlayer, int size, TextureFormat format, byte[] pngData, CancellationToken cancelToken)
		{
			Texture2D result = new Texture2D(size, size, format, mipChain: false);
			byte[] resultData = null;
			bool errorCatched = false;
			await UniTask.RunOnThreadPool(delegate
			{
				try
				{
					resultData = MessagePackSerializer.Deserialize<byte[]>(pngData, m_lz4Options);
				}
				catch
				{
					errorCatched = true;
				}
			}, configureAwait: true, cancelToken);
			if (errorCatched || cancelToken.IsCancellationRequested)
			{
				GameLog.Print(AlertLevel.Error, "LiveryTransferSystem", "Failed to deserizalize livery data for " + networkPlayer.OriginaNickName);
				UnityEngine.Object.Destroy(result);
				return;
			}
			bool makeNoLongerReadable = false;
			try
			{
				result.LoadRawTextureData(resultData);
				result.Apply(updateMipmaps: false, makeNoLongerReadable);
			}
			catch (Exception message)
			{
				Debug.LogError(message);
				errorCatched = true;
			}
			if (networkPlayer == null || cancelToken.IsCancellationRequested || errorCatched)
			{
				UnityEngine.Object.Destroy(result);
			}
			else
			{
				networkPlayer.SetPlayerTexture(carTextureType, result);
			}
		}

		public override void DoPlayerLeave(NetworkPlayer player)
		{
			base.DoPlayerLeave(player);
			DisposeTokenSource(player);
			player.ClearTextures(m_carTextureTypes);
		}

		public void DisposeTokenSource(NetworkPlayer player)
		{
			if (m_tokenSources.TryGetValue(player, out var value))
			{
				value?.Cancel();
				value?.Dispose();
			}
			m_tokenSources.Remove(player);
		}

		private void DisposeTokenSources()
		{
			foreach (CancellationTokenSource value in m_tokenSources.Values)
			{
				value?.Cancel();
				value?.Dispose();
			}
			m_tokenSources.Clear();
		}
	}
}
