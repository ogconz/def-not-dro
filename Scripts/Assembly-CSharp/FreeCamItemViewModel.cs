using UnityEngine;

public class FreeCamItemViewModel : BaseListItem
{
	private UIFreeCamContext m_context;

	public string title = "";

	public int index;

	public Vector3 position;

	public Transform transformCamera;

	public FreeCamItemViewModel(UIFreeCamContext context, string titleCamera, int indexCamera, Vector3 pos, Transform transform)
	{
		m_context = context;
		title = titleCamera;
		index = indexCamera;
		position = pos;
		transformCamera = transform;
	}

	public void MoveToPoint()
	{
		if (!(m_context == null))
		{
			m_context.mainCamera.transform.position = transformCamera.position;
			m_context.mainCamera.transform.LookAt(m_context.mainCamera.GetCar().carX.transform);
		}
	}

	public override void OnClick()
	{
		MoveToPoint();
		m_context.cameraList.selectedIndex = m_context.cameraList.IndexOf(this);
	}

	public void SelfRemove()
	{
		if (!(m_context == null))
		{
			m_context.RemoveCameraPoint(this);
			m_context.cameraList.ApplyChangeList();
		}
	}
}
