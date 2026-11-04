using UnityEngine;

namespace CarX
{
	[AddComponentMenu("")]
	public abstract class BaseCamera : MonoBehaviour
	{
		public const string UNTAGGED = "Untagged";

		[SerializeField]
		private string m_identifier;

		public string label;

		public string identifier => m_identifier;

		public bool needRefreshTarget { get; protected set; }

		public abstract void SetTarget(RaceCar target);

		public virtual void SetFOV(float fov)
		{
		}

		public virtual void ResetInterpolation()
		{
		}

		protected bool IsTargetValid(RaceCar target)
		{
			if (target != null && target.IsVisualLoaded)
			{
				return !target.IsPhysicsBroken();
			}
			return false;
		}

		protected virtual void SetDefaults()
		{
		}

		public virtual void ResetToDefaults()
		{
		}

		public void RefreshTargetIfNeeded(ref Transform target, string tag)
		{
			if (needRefreshTarget && tag != "Untagged")
			{
				Transform transform = target.FindChildWithTag(tag);
				if ((bool)transform)
				{
					target = transform;
					needRefreshTarget = false;
				}
				else
				{
					Debug.LogError("BaseCamera.RefreshTargetIfNeeded >>> no find tag:" + tag);
				}
			}
		}
	}
}
