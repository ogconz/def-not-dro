using Cysharp.Threading.Tasks;
using Minimaps;
using UnityEngine;

[AddComponentMenu("Scripts/Minimap/Icon")]
public class MinimapItem : MonoBehaviour
{
	[SerializeField]
	private int m_typeIndex;

	private Color? m_color;

	private int? m_depth;

	private int? m_size;

	private bool? m_isNetworkCar;

	private bool? m_isCheckpoint;

	public Transform cachedTransform { get; private set; }

	public Object host { get; set; }

	public Color color
	{
		get
		{
			if (isCheckpoint)
			{
				if (!(host as Checkpoint).isVisited)
				{
					return Color.grey;
				}
				return Color.green;
			}
			if (!m_color.HasValue || m_color.Value.a <= 0f)
			{
				m_color = (isNetworkCar ? (host as RaceCar).networkColor : Minimap.instance.GetIconColor(m_typeIndex));
			}
			return m_color.Value;
		}
	}

	public int depth
	{
		get
		{
			if (!m_depth.HasValue)
			{
				m_depth = Minimap.instance.GetIconDepth(m_typeIndex);
			}
			return m_depth.Value + ((!isNetworkCar) ? 1 : 0) + (isCheckpoint ? (-1) : 0);
		}
	}

	public bool isNetworkCar
	{
		get
		{
			if (!m_isNetworkCar.HasValue)
			{
				m_isNetworkCar = host is RaceCar raceCar && raceCar.isNetworkCar;
			}
			return m_isNetworkCar.Value;
		}
	}

	public int typeIndex
	{
		get
		{
			return m_typeIndex;
		}
		set
		{
			m_typeIndex = value;
		}
	}

	public int size
	{
		get
		{
			if (!m_size.HasValue)
			{
				m_size = Minimap.instance.GetIconSize(m_typeIndex);
			}
			return (int)((float)m_size.Value * (isNetworkCar ? 0.75f : 1f));
		}
	}

	public bool isCheckpoint
	{
		get
		{
			if (!m_isCheckpoint.HasValue)
			{
				m_isCheckpoint = host is Checkpoint;
			}
			return m_isCheckpoint.Value;
		}
	}

	private void Awake()
	{
		cachedTransform = base.transform;
	}

	private async void OnEnable()
	{
		await WaitCar();
	}

	protected virtual async UniTask WaitCar()
	{
		if (States.mainCurrent is SyncNetFreerideRaceModeState)
		{
			await UniTask.WaitUntil(() => host != null);
			if (!isCheckpoint)
			{
				await UniTask.WaitUntil(() => host is RaceCar);
			}
		}
		MinimapProvider.AddIcon(this);
	}

	private void OnDisable()
	{
		MinimapProvider.RemoveIcon(this);
	}
}
