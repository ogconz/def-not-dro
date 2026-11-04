using UnityEngine;

namespace TrackLayout.Behaviours
{
	public interface IMovable
	{
		Vector3 position { get; set; }

		Quaternion rotation { get; set; }

		bool visible { get; set; }

		float crossFadeDuration { get; set; }
	}
}
