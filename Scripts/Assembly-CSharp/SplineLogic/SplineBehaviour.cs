using UnityEngine;

namespace SplineLogic
{
	public abstract class SplineBehaviour : MonoBehaviour
	{
		public abstract ISpline GetBaseSpline();
	}
}
