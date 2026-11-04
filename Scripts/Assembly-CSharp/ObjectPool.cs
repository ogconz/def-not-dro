using UnityEngine;

public class ObjectPool : MonoBehaviour, IPoolObject<string>
{
	public virtual string group => base.name;

	public virtual void Create()
	{
		base.gameObject.SetActive(value: true);
	}

	public void OnPush()
	{
		base.gameObject.SetActive(value: false);
	}

	public virtual void Push()
	{
		Singleton<PoolManager>.instance.Push(group, this);
	}

	public void OnFailedPush()
	{
		Debug.Log("Failed push to pool for: " + base.gameObject.name);
		Object.Destroy(base.gameObject);
	}
}
