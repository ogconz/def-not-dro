using Cysharp.Threading.Tasks;
using Minimaps;
using UnityEngine;

public class WaypointMinimapItem : MinimapItem
{
	private readonly (Vector3, Quaternion) defaultPosRot = (Vector3.zero, Quaternion.identity);

	public (Vector3 position, Quaternion rotation) waypointScenePosRot
	{
		get
		{
			base.transform.GetPositionAndRotation(out var position, out var rotation);
			return (position: position, rotation: rotation);
		}
		set
		{
			base.transform.SetPositionAndRotation(value.position, value.rotation);
		}
	}

	public void SetDefaultPosRot()
	{
		waypointScenePosRot = defaultPosRot;
	}

	protected override async UniTask WaitCar()
	{
		if (States.mainCurrent is SyncNetFreerideRaceModeState)
		{
			await UniTask.WaitUntil(() => base.host is RaceCar);
		}
		MinimapProvider.AddIcon(this);
	}
}
