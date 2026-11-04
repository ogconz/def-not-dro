using System.Collections.Generic;
using System.Linq;
using GameInput;
using HUD;
using UnityEngine;

namespace Minimaps
{
	public class GlobalMinimapVisualizer : BaseMinimapVisualizer
	{
		private const float defaultMapScale = 1024f;

		private const float maxSizeScale = 3f;

		private const float zoomSpeed = 30f;

		private const float minSizeScale = 0.7f;

		private const float panSpeed = 7f;

		private const float mousePanSpeed = 2f;

		private const int namePoolSize = 16;

		private readonly Vector2 m_mapBoundsMin = new Vector2(-1000f, -1000f);

		private readonly Vector2 m_mapBoundsMax = new Vector2(1000f, 1000f);

		private readonly Vector2 m_nicknameOffset = new Vector2(0f, 5f);

		private readonly Vector2 m_defaultPlayerNamePrefScale = new Vector2(0.6f, 0.6f);

		[SerializeField]
		private RectTransform m_maskTransform;

		[SerializeField]
		private RectTransform m_networkPlayerNamesPool;

		[SerializeField]
		private UICarBasePlayerName m_nickNamePrefab;

		[SerializeField]
		private RectTransform m_visualizerRect;

		private Dictionary<UIMinimapItem, UICarBasePlayerName> m_networkPlayerCarNameDictionary = new Dictionary<UIMinimapItem, UICarBasePlayerName>();

		private List<UICarBasePlayerName> m_playerNamePool = new List<UICarBasePlayerName>();

		private Vector3 m_lastMousePosition;

		private Vector2 m_mapMoveDirection;

		private float m_defaultMapScale;

		private bool m_isPanning;

		private bool m_playersNamesVisibility = true;

		public RectTransform rootTransform => m_mapRoot;

		private bool m_isMouseScrollZoom => Input.mouseScrollDelta.y != 0f;

		protected override void OnEnable()
		{
			base.OnEnable();
			MinimapProvider.instance.OnCreateNickName += CreateNickNameHandler;
			MinimapProvider.instance.OnDisponseCar += DisposeNickHandle;
			MinimapProvider.instance.OnUpdateNickName += UpdateNickNameHandle;
			m_mapImage.rectTransform.SetWidth(1024f);
			m_mapImage.rectTransform.SetHeight(1024f);
			m_maskTransform.localPosition = Vector2.zero;
			m_defaultMapScale = m_mapImage.GetComponent<RectTransform>().GetWidth();
			ActivatePlayerNames();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			ClearUserNamesPool();
			ClearUserNamesDictionary();
			MinimapProvider.instance.OnCreateNickName -= CreateNickNameHandler;
			MinimapProvider.instance.OnDisponseCar -= DisposeNickHandle;
			MinimapProvider.instance.OnUpdateNickName -= UpdateNickNameHandle;
		}

		private void ActivatePlayerNames()
		{
			UpdatePlayerNamesPool();
			foreach (var cars in m_carsList)
			{
				CreateNickNameHandler(cars.Item1, cars.Item2);
			}
		}

		private void UpdatePlayerNamesPool()
		{
			GOPool.Allocate(m_nickNamePrefab, ref m_playerNamePool, 16, delegate(UICarBasePlayerName item)
			{
				item.transform.SetParent(m_networkPlayerNamesPool);
				item.Release();
			}, delegate(UICarBasePlayerName item)
			{
				item.Release();
			});
		}

		private void ClearUserNamesPool()
		{
			foreach (UICarBasePlayerName item in m_playerNamePool)
			{
				Object.Destroy(item.gameObject);
			}
			m_playerNamePool.Clear();
		}

		private void ClearUserNamesDictionary()
		{
			foreach (UICarBasePlayerName value in m_networkPlayerCarNameDictionary.Values)
			{
				Object.Destroy(value.gameObject);
			}
			m_networkPlayerCarNameDictionary.Clear();
		}

		private void Update()
		{
			if (!(Minimap.instance == null))
			{
				UpdateIcons();
				UpdateMinimapTexture();
				UpdateNetworkPlayersCarName();
			}
		}

		private void LateUpdate()
		{
			UpdateZooming();
			MouseNavigate();
			NavigateViaDirection();
		}

		private void UpdateZooming()
		{
			float num = (m_isMouseScrollZoom ? GetMouseScrollDelta() : GetGamepadZoomDelta());
			if (num != 0f)
			{
				float deltaSize = num * 0.2f * 30f;
				RectTransformUtility.ScreenPointToLocalPointInRectangle(m_visualizerRect, GetScreenPoint(), SetCameraForZooming(), out var localPoint);
				SetZoom(localPoint, deltaSize);
			}
		}

		private Vector2 GetScreenPoint()
		{
			Vector2 result = new Vector2(Screen.width / 2, Screen.height / 2);
			if (!m_isMouseScrollZoom)
			{
				return result;
			}
			return Input.mousePosition;
		}

		private Camera SetCameraForZooming()
		{
			if (!XRModeManager.instance.isHMDWearing)
			{
				return null;
			}
			return Camera.main;
		}

		private float GetMouseScrollDelta()
		{
			return Input.mouseScrollDelta.y;
		}

		private float GetGamepadZoomDelta()
		{
			InputManager instance = InputManager.instance;
			return (0f - instance.GetActionValue(ActionType.ZoomOut) + instance.GetActionValue(ActionType.ZoomIn)) * 0.5f;
		}

		private void SetZoom(Vector2 zoomPoint, float deltaSize)
		{
			Vector2 currectAxisSizes = new Vector2(m_mapImage.rectTransform.rect.width, m_mapImage.rectTransform.rect.height);
			Vector2 vector = MinimapHelper.CalculateNewAxisScaleValue(currectAxisSizes, deltaSize, m_defaultMapScale, 0.7f, 3f);
			float widthRatio = vector.x / currectAxisSizes.x;
			float heightRatio = vector.y / currectAxisSizes.y;
			Vector2 vector2 = MinimapHelper.CalculatePivotOffset(zoomPoint, m_maskTransform, widthRatio, heightRatio);
			MinimapHelper.ZoomAnimation((Vector2)m_maskTransform.localPosition + vector2, m_maskTransform, m_mapImage.rectTransform, vector.x, vector.y);
		}

		private void MouseNavigate()
		{
			if (Input.GetMouseButtonDown(0))
			{
				m_isPanning = true;
				m_lastMousePosition = Input.mousePosition;
			}
			if (Input.GetMouseButton(0) && m_isPanning)
			{
				Vector3 vector = Input.mousePosition - m_lastMousePosition;
				Vector2 mapPosition = (Vector2)m_maskTransform.localPosition + (Vector2)vector * 2f;
				SetMapPosition(mapPosition);
				m_lastMousePosition = Input.mousePosition;
			}
			if (Input.GetMouseButtonUp(0))
			{
				m_isPanning = false;
			}
		}

		private void NavigateViaDirection()
		{
			m_mapMoveDirection = GetMapMoveDirection();
			if (!(m_mapMoveDirection == Vector2.zero))
			{
				Vector2 mapPosition = (Vector2)m_maskTransform.localPosition + m_mapMoveDirection * 7f;
				SetMapPosition(mapPosition);
			}
		}

		private void SetMapPosition(Vector2 newPosition)
		{
			newPosition.x = Mathf.Clamp(newPosition.x, m_mapBoundsMin.x, m_mapBoundsMax.x);
			newPosition.y = Mathf.Clamp(newPosition.y, m_mapBoundsMin.y, m_mapBoundsMax.y);
			MinimapHelper.DoMapMove(newPosition, m_maskTransform);
		}

		private Vector2 GetMapMoveDirection()
		{
			float actionValue = InputManager.instance.GetActionValue(ActionType.NavLeft);
			float actionValue2 = InputManager.instance.GetActionValue(ActionType.NavRight);
			float x = actionValue - actionValue2;
			float actionValue3 = InputManager.instance.GetActionValue(ActionType.NavUp);
			float y = InputManager.instance.GetActionValue(ActionType.NavDown) - actionValue3;
			return new Vector2(x, y);
		}

		private void UpdateNetworkPlayersCarName()
		{
			foreach (KeyValuePair<UIMinimapItem, UICarBasePlayerName> item in m_networkPlayerCarNameDictionary)
			{
				UICarBasePlayerName value = item.Value;
				if (!(value == null))
				{
					Vector2 localPosition = item.Key.position + m_nicknameOffset;
					value.SetLocalPosition(localPosition);
					value.SetLocalScale(m_defaultPlayerNamePrefScale);
					value.UpdatePlayerStatus();
				}
			}
		}

		public void TogglePlayerNamesVisibility()
		{
			m_playersNamesVisibility = !m_playersNamesVisibility;
			SetNetworkPlayersNamesVisualValid();
		}

		private void SetNetworkPlayersNamesVisualValid()
		{
			foreach (UICarBasePlayerName value in m_networkPlayerCarNameDictionary.Values)
			{
				value.isVisible = m_playersNamesVisibility;
			}
		}

		protected override bool IsNeedActivate(MinimapItem item, bool defaultShowValue)
		{
			if (item.isCheckpoint)
			{
				if (States.current is BaseRaceModeState baseRaceModeState)
				{
					return baseRaceModeState.raceMode != RaceMode.Freeride;
				}
				return false;
			}
			return defaultShowValue;
		}

		private void CreateNickNameHandler(RaceCar car, string name)
		{
			if (!(car == null))
			{
				UIMinimapItem uIMinimapItem = m_iconsList.Find((UIMinimapItem icon) => icon.item.host == car && icon.item.isNetworkCar);
				if (uIMinimapItem == null)
				{
					GameLog.Print(AlertLevel.Error, "Minimap", "There is no icon for " + car.name + " name element");
				}
				else if (!m_networkPlayerCarNameDictionary.ContainsKey(uIMinimapItem))
				{
					SetNetworkPlayerNameEnable(car, name, uIMinimapItem);
					SetNetworkPlayersNamesVisualValid();
				}
			}
		}

		private void SetNetworkPlayerNameEnable(RaceCar car, string name, UIMinimapItem vKey)
		{
			UICarBasePlayerName uICarBasePlayerName = m_playerNamePool.Find((UICarBasePlayerName item) => item.isReleased);
			if (uICarBasePlayerName == null)
			{
				GameLog.Print(AlertLevel.Error, "Minimap", "There is no free name element in name pool");
				return;
			}
			uICarBasePlayerName.Set(car, name);
			uICarBasePlayerName.SetLocalRotation(Quaternion.identity);
			m_networkPlayerCarNameDictionary[vKey] = uICarBasePlayerName;
		}

		protected override void OnItemListUpdated()
		{
			base.OnItemListUpdated();
			RefrechAllNumbers();
			List<UICarBasePlayerName> list = m_networkPlayerCarNameDictionary.Values.ToList();
			m_networkPlayerCarNameDictionary.Clear();
			foreach (UIMinimapItem icon in m_iconsList)
			{
				if (!(icon == null) && !(icon.item == null) && icon.item.isNetworkCar)
				{
					UICarBasePlayerName uICarBasePlayerName = list.Find((UICarBasePlayerName item) => item != null && item.host == icon.item.host);
					if (!(uICarBasePlayerName == null))
					{
						m_networkPlayerCarNameDictionary[icon] = uICarBasePlayerName;
						list.Remove(uICarBasePlayerName);
					}
				}
			}
		}

		private void RefrechAllNumbers()
		{
			foreach (UICarBasePlayerName value in m_networkPlayerCarNameDictionary.Values)
			{
				value.RefreshNumber();
			}
		}

		private void DisposeNickHandle(RaceCar car)
		{
			m_dirtyList = true;
			UIMinimapItem uIMinimapItem = m_iconsList.Find((UIMinimapItem icon) => icon.item.host == car && icon.item.isNetworkCar);
			if (!(uIMinimapItem == null) && m_networkPlayerCarNameDictionary.TryGetValue(uIMinimapItem, out var value))
			{
				DeactivatePlayerName((uIMinimapItem, value));
				ReinitIconsIfDirtyList();
			}
		}

		private void DeactivatePlayerName((UIMinimapItem, UICarBasePlayerName) element)
		{
			m_networkPlayerCarNameDictionary.Remove(element.Item1);
			element.Item2.Release();
		}

		private void UpdateNickNameHandle(RaceCar car, string name)
		{
			if (!(car == null))
			{
				UIMinimapItem uIMinimapItem = m_iconsList.Find((UIMinimapItem icon) => icon.item.host == car && icon.item.isNetworkCar);
				if (!(uIMinimapItem == null) && m_networkPlayerCarNameDictionary.TryGetValue(uIMinimapItem, out var value))
				{
					value.UpdateNickName(name);
				}
			}
		}
	}
}
