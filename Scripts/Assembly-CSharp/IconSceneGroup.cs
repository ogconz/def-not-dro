using UnityEngine;

public class IconSceneGroup : MonoBehaviour
{
	public Transform cameraPoint;

	[Range(0.1f, 10f)]
	public float sizeBound;

	[SerializeField]
	private int[] m_carsId;

	private IconGeneratorContext m_iconGeneratorContext;

	private GarageGUIState m_garageGuiState;

	public void Start()
	{
		m_garageGuiState = States.State<GarageGUIState>();
		if (!(m_garageGuiState == null))
		{
			m_iconGeneratorContext = m_garageGuiState.contextOwner.Get<IconGeneratorContext>();
			if (!(m_iconGeneratorContext == null))
			{
				m_iconGeneratorContext.AddCategory(this, m_carsId);
			}
		}
	}
}
