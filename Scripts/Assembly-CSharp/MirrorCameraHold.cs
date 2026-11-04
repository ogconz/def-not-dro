using UnityEngine;

public class MirrorCameraHold : MonoBehaviour
{
	private void LateUpdate()
	{
		base.transform.rotation = Quaternion.Euler(0f, base.transform.parent.rotation.eulerAngles.y + 180f, 0f);
	}
}
