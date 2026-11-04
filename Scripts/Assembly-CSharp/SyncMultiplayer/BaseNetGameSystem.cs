namespace SyncMultiplayer
{
	public class BaseNetGameSystem
	{
		public NetGameSystemsManager owner { get; private set; }

		public virtual void Attach(NetGameSystemsManager owner)
		{
			this.owner = owner;
		}

		public virtual void OnGameConnect()
		{
		}

		public virtual void OnReconnect()
		{
		}

		public virtual void OnDisconnect()
		{
		}

		public virtual void OnRaceLoaded()
		{
		}

		public virtual void Update()
		{
		}

		public virtual void LateUpdate()
		{
		}

		public virtual void FixedUpdate()
		{
		}

		public virtual void OnDrawGizmos()
		{
		}

		public virtual void DoPlayerEnter(NetworkPlayer player)
		{
		}

		public virtual void DoPlayerLeave(NetworkPlayer player)
		{
		}

		public virtual void PostPlayerLeave(NetworkPlayer player)
		{
		}

		public virtual void ChangeMasterState(bool isMaster)
		{
		}

		public static implicit operator bool(BaseNetGameSystem x)
		{
			return x != null;
		}
	}
}
