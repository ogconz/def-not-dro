using System;
using UnityEngine;

public class ObjectLoaderByDateRange : MonoBehaviour
{
	[Serializable]
	public class Date
	{
		public int Day;

		public int Month;

		public int Year;
	}

	public GameObject Prefab;

	public Vector3 Position;

	public Date FromDate;

	public Date ToDate;

	private void Start()
	{
		DateTime now = DateTime.Now;
		DateTime dateTime = new DateTime(FromDate.Year, FromDate.Month, FromDate.Day);
		DateTime dateTime2 = new DateTime(ToDate.Year, ToDate.Month, ToDate.Day);
		if (now > dateTime && now < dateTime2 && Prefab != null)
		{
			GameObject obj = UnityEngine.Object.Instantiate(Prefab);
			obj.transform.parent = base.transform;
			obj.transform.localPosition = Position;
		}
	}
}
