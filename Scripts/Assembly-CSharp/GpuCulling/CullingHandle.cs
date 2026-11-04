using System;

namespace GpuCulling
{
	public struct CullingHandle : IEquatable<CullingHandle>
	{
		private const int InvalidHandle = -1;

		private readonly int? m_handle;

		public static CullingHandle Invalid => new CullingHandle(-1);

		public CullingHandle(int handle)
		{
			m_handle = handle;
		}

		public bool IsValid()
		{
			if (m_handle.HasValue)
			{
				return m_handle.Value != -1;
			}
			return false;
		}

		public static implicit operator int(CullingHandle handle)
		{
			if (!handle.IsValid())
			{
				return -1;
			}
			return handle.m_handle.Value;
		}

		public bool Equals(CullingHandle other)
		{
			return m_handle == other.m_handle;
		}

		public override bool Equals(object obj)
		{
			if (obj is CullingHandle other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return this;
		}
	}
}
