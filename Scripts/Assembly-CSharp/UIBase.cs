using Binding.Base;
using UnityEngine;

public abstract class UIBase<T> : BaseBindingBehaviourTarget where T : MonoBehaviour
{
	[SerializeField]
	private bool m_hideOnAwake = true;

	protected static T instance { get; private set; }

	public static bool isAvailable => instance != null;

	public static bool isVisible { get; set; }

	public static void Show(bool show)
	{
		if ((bool)instance)
		{
			isVisible = show;
			UIUtils.SetActive(instance.gameObject, show);
		}
	}

	protected virtual void Awake()
	{
		isVisible = !m_hideOnAwake;
		instance = this as T;
		if (m_hideOnAwake)
		{
			UIUtils.SetActive(base.gameObject, state: false);
		}
	}

	protected override void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
		}
	}
}
