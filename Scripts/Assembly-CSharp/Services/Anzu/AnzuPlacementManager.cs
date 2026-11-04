using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using anzu;

namespace Services.Anzu
{
	public class AnzuPlacementManager : MonoBehaviour
	{
		public string additiveSceneName;

		private static bool s_isServicesReady;

		private AnzuAdditiveSceneController m_sceneLoadController;

		private AnzuPropsHolder m_placementsHolder;

		private AnzuCoreStateActivityHandler m_coreActivityHandler;

		private CancellationTokenSource m_ctsInitServices;

		public static AnzuPlacementManager instance { get; private set; }

		public static bool isReady
		{
			get
			{
				if (instance != null)
				{
					return s_isServicesReady;
				}
				return false;
			}
		}

		private void Awake()
		{
			instance = this;
		}

		private void Start()
		{
			InitServicesAsync().Forget();
		}

		private void Update()
		{
			m_sceneLoadController?.Update();
		}

		private void OnDestroy()
		{
			DisposeInitToken();
			DisposeServices();
		}

		private async UniTaskVoid InitServicesAsync()
		{
			m_ctsInitServices = new CancellationTokenSource();
			AnzuUnityWrapper anzuInstance = AnzuUnityWrapper.Instance;
			await UniTask.WaitUntil(() => anzuInstance != null && anzuInstance.isStarted, PlayerLoopTiming.Update, m_ctsInitServices.Token, cancelImmediately: true);
			StartServicesAsync().AttachExternalCancellation(m_ctsInitServices.Token).Forget();
		}

		private async UniTask StartServicesAsync()
		{
			m_sceneLoadController = new AnzuAdditiveSceneController(additiveSceneName);
			await m_sceneLoadController.SetActiveSceneAsync(activity: true);
			m_placementsHolder = new AnzuPropsHolder(m_sceneLoadController);
			m_placementsHolder.FillItems();
			m_coreActivityHandler = new AnzuCoreStateActivityHandler();
			m_coreActivityHandler.Initialize();
			s_isServicesReady = true;
		}

		private void DisposeInitToken()
		{
			if (m_ctsInitServices != null && !m_ctsInitServices.IsCancellationRequested)
			{
				m_ctsInitServices?.Cancel();
			}
			m_ctsInitServices?.Dispose();
			m_ctsInitServices = null;
		}

		private void DisposeServices()
		{
			m_sceneLoadController?.Dispose();
			m_sceneLoadController = null;
			m_placementsHolder?.Dispose();
			m_placementsHolder = null;
			m_coreActivityHandler?.Dispose();
			m_coreActivityHandler = null;
			s_isServicesReady = false;
		}

		public void UpdateDebugStatsVisibility()
		{
			m_placementsHolder?.UpdatePropsAdStatsVisibility();
		}

		private void OnGUI()
		{
			if (!s_isServicesReady || !GameConsole.Bool["anzuStats"])
			{
				return;
			}
			GUIStyle gUIStyle = new GUIStyle(GUI.skin.label);
			gUIStyle.normal.textColor = Color.red;
			gUIStyle.fontStyle = FontStyle.Bold;
			GUIStyle gUIStyle2 = new GUIStyle(GUI.skin.label);
			gUIStyle2.normal.textColor = Color.green;
			gUIStyle2.fontStyle = FontStyle.Bold;
			float yStart = (float)Screen.height * 0.15f;
			float xStart = (float)Screen.width * 0.7f;
			float btnWidth = 600f;
			float btnHeight = 22f;
			ShowLabel("AnzuChannels on scene:");
			foreach (KeyValuePair<AnzuChannelSO, bool> item in AnzuPlacement.channelsPerImperssionGained)
			{
				ShowLabel($"{item.Key.name}, impression - {item.Value}", item.Value ? gUIStyle2 : gUIStyle);
			}
			ShowLabel("\n");
			ShowLabel("Placements:");
			foreach (AnzuPlacement propsPlacement in m_placementsHolder.GetPropsPlacements())
			{
				if (propsPlacement.nativeAd.isActiveAndEnabled)
				{
					ShowLabel(propsPlacement.nativeAd.name ?? "");
				}
			}
			void ShowLabel(string label, GUIStyle style = null)
			{
				yStart += 18f;
				Rect position = new Rect(new Vector2(xStart, yStart), new Vector2(btnWidth, btnHeight));
				if (style != null)
				{
					GUI.Label(position, label, style);
				}
				else
				{
					GUI.Label(position, label);
				}
			}
		}
	}
}
