using UnityEngine;

public class UILoadingProgress : LoadingProgressBar
{
	public Vector3 rotationsPerSecond = new Vector3(0f, 0f, 0.1f);

	public void ApplyDelta(float delta)
	{
		delta *= 360f;
		Quaternion quaternion = Quaternion.Euler(rotationsPerSecond * delta);
		base.transform.rotation = base.transform.rotation * quaternion;
	}

	protected override void OnChangeProgress()
	{
	}

	private void Update()
	{
		ApplyDelta(Time.deltaTime);
	}
}
