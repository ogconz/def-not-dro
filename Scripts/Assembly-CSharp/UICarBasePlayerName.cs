using System.Collections;
using Binding;
using Binding.Base;
using GameOverlay;
using SyncMultiplayer;
using UnityEngine;

public class UICarBasePlayerName : BehaviourBindingTarget
{
	public Color colorInSubroom = UIUtils.HexToColor(4290380031u);

	public Color colorNormalPlayer = UIUtils.HexToColor(8242431u);

	public Color colorFriendPlayer = UIUtils.HexToColor(998572287u);

	public Color colorCollisionOff = UIUtils.HexToColor(255u);

	public Color colorTurningCollision = UIUtils.HexToColor(3758096639u);

	public Color colorDownloadLivery = UIUtils.HexToColor(937747967u);

	public Color colorInPause = UIUtils.HexToColor(2240978943u);

	public Color colorInDyno = UIUtils.HexToColor(3882494719u);

	public Color colorDefault = UIUtils.HexToColor(64214015u);

	protected string m_playerName;

	protected float m_time;

	protected RaceCar m_host;

	protected BoolProperty m_isFriend = new BoolProperty();

	protected NetGamePlayerStateSystem m_playerState;

	protected UIPlayerCarState m_playerCarState;

	protected StringProperty m_nickName = new StringProperty();

	private BoolProperty m_isReleased = new BoolProperty(false);

	private ColorProperty m_backColor = new ColorProperty();

	private IntProperty m_number = new IntProperty();

	private RectTransform m_rect;

	private Coroutine m_refreshNumberCoroutine;

	public RaceCar host => m_host;

	public bool isReleased => m_isReleased.value;

	public bool isVisible
	{
		get
		{
			return !m_isReleased.value;
		}
		set
		{
			m_isReleased.value = !value;
		}
	}

	protected virtual void Awake()
	{
		m_playerState = NetworkController.InstanceGame.systems.Get<NetGamePlayerStateSystem>();
		m_playerCarState = GetComponentInChildren<UIPlayerCarState>();
		m_rect = GetComponent<RectTransform>();
	}

	protected void OnEnable()
	{
		m_refreshNumberCoroutine = TimeController.StartNow(RefreshNumberCoroutine(0f, 1f));
	}

	protected void OnDisable()
	{
		TimeController.StopNow(m_refreshNumberCoroutine);
	}

	public virtual void UpdatePlayerStatus()
	{
		Color color = m_playerState.GetUIState(m_host.networkPlayer, m_isFriend.value, out m_time) switch
		{
			UIPlayerStateType.CollisionOn => colorNormalPlayer, 
			UIPlayerStateType.CollisionTimer => colorTurningCollision, 
			UIPlayerStateType.CollisionOff => colorCollisionOff, 
			UIPlayerStateType.Pause => colorInPause, 
			UIPlayerStateType.Dyno => colorInDyno, 
			UIPlayerStateType.LoadLivery => colorDownloadLivery, 
			UIPlayerStateType.InSubroom => colorInSubroom, 
			UIPlayerStateType.IsFriend => colorFriendPlayer, 
			_ => colorDefault, 
		};
		if (m_playerCarState != null)
		{
			m_playerCarState.current = m_playerState.GetCurrent(m_host.networkPlayer);
		}
		if (m_backColor.value != color)
		{
			m_backColor.value = Color.Lerp(m_backColor.value, color, Time.deltaTime * 10f);
		}
		m_nickName.value = m_playerName;
	}

	protected IEnumerator RefreshNumberCoroutine(float time, float repeatRate)
	{
		yield return new WaitForSeconds(time);
		while (true)
		{
			RefreshNumber();
			yield return new WaitForSeconds(repeatRate);
		}
	}

	public virtual void Set(RaceCar host, string name)
	{
		SetNameLabelData(host, name);
		UpdatePlayerStatus();
	}

	public void SetLocalPosition(Vector2 position)
	{
		m_rect.localPosition = position;
	}

	public void SetLocalScale(Vector3 scale)
	{
		m_rect.localScale = scale;
	}

	public void SetLocalRotation(Quaternion orientation)
	{
		m_rect.localRotation = orientation;
	}

	protected void SetNameLabelData(RaceCar host, string name)
	{
		UpdateNickName(name);
		SetHostPlayerCar(host);
		SetFriendStatus();
	}

	private void SetFriendStatus()
	{
		m_isFriend.value = Overlay.instance.IsFriend(host.networkPlayer.PlayerId);
	}

	public void UpdateNickName(string name)
	{
		m_playerName = name;
		m_nickName.value = m_playerName;
	}

	private void SetHostPlayerCar(RaceCar host)
	{
		m_host = host;
	}

	public void RefreshNumber()
	{
		if (m_host != null && m_host.networkPlayer != null)
		{
			m_number.value = NetworkController.InstanceGame.Players.IndexOf(m_host.networkPlayer) + 1;
		}
	}

	public void Release()
	{
		m_isReleased.value = true;
	}
}
