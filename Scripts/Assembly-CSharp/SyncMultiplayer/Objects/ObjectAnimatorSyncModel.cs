using System;
using System.IO;
using UnityEngine;

namespace SyncMultiplayer.Objects
{
	public class ObjectAnimatorSyncModel : BaseObjectSyncModel
	{
		private const int SYNC_HZ = 1;

		public Animator animator;

		private NetGameSyncTimeSystem m_timeSync;

		private ApproxTimeKeeper m_syncAnim = new ApproxTimeKeeper(1000L);

		public override void Attach(NetGameSystemsManager systems)
		{
			m_timeSync = systems.Get<NetGameSyncTimeSystem>();
		}

		public override bool Init(NetworkObject netObject)
		{
			if (netObject.gameObject.TryGetComponent<Animator>(out animator))
			{
				ToggleAnimation(enable: false);
				return true;
			}
			return false;
		}

		public override void Update()
		{
			if (m_syncAnim.ShouldExecute(PreciseTime.unscaledDeltaTimeMs / 2) && m_timeSync.initialSyncDone)
			{
				SetAnimTime(m_timeSync.GetModelTime());
				m_syncAnim.Reset();
			}
		}

		public override void ReadPackage(byte[] bytes, ref int offset, SyncPacketType type)
		{
			if (type.IsInit())
			{
				animator.rootPosition = CustomTypes.DeserializeVector3(bytes, offset);
				offset += 12;
				animator.rootRotation = CustomTypes.DeserializeQuaternion(bytes, offset);
				offset += 16;
			}
		}

		public override void WritePackage(MemoryStream stream, SyncPacketType type)
		{
			if (type.IsId())
			{
				float length = GetCurrentState().length;
				stream.Write(BitConverter.GetBytes(length), 0, 4);
				ToggleAnimation(enable: true);
			}
			if (type.IsInit())
			{
				CustomTypes.SerializeVector3(animator.rootPosition, stream);
				CustomTypes.SerializeQuaternion(animator.rootRotation, stream);
			}
		}

		private void SetAnimTime(float time)
		{
			AnimatorStateInfo currentState = GetCurrentState();
			float normalizedTime = time % currentState.length / currentState.length;
			animator.Play(currentState.fullPathHash, 0, normalizedTime);
		}

		private void ToggleAnimation(bool enable)
		{
			animator.enabled = enable;
			if (!enable)
			{
				animator.Rebind();
				animator.Update(0f);
			}
		}

		private AnimatorStateInfo GetCurrentState()
		{
			return animator.GetCurrentAnimatorStateInfo(0);
		}
	}
}
