using UnityEngine;

public class CameraPartMarker : MonoBehaviour
{
	[SerializeField]
	private string m_placePointTag = "Untagged";

	public bool invert;

	public string placePointTag => m_placePointTag;

	private void Awake()
	{
		CarCameraSwitchHandler componentInParent = GetComponentInParent<CarCameraSwitchHandler>();
		if (componentInParent != null)
		{
			componentInParent.AddCameraPlaceObject(this);
		}
	}

	public void SetActive(string cameraPlacePointTag)
	{
		bool flag = m_placePointTag.Equals(cameraPlacePointTag);
		flag = (invert ? (!flag) : flag);
		base.gameObject.SetActive(flag);
	}

	public void SetActive(bool active)
	{
		active = (invert ? (!active) : active);
		base.gameObject.SetActive(active);
	}
}
