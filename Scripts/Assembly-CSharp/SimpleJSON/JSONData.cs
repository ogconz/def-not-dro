using System.Text;

namespace SimpleJSON
{
	internal class JSONData : JSONNode
	{
		private string m_Data;

		public override string Value
		{
			get
			{
				return m_Data;
			}
			set
			{
				m_Data = value;
			}
		}

		public void Set(string aData)
		{
			m_Data = aData;
		}

		public override void Free()
		{
			base.Free();
			m_Data = string.Empty;
		}

		public override void ToString(ref StringBuilder sb)
		{
			sb.Append("\"");
			JSONNode.Escape(ref sb, m_Data);
			sb.Append("\"");
		}

		public override void ToString(ref StringBuilder sb, string aPrefix)
		{
			sb.Append("\"");
			JSONNode.Escape(ref sb, m_Data);
			sb.Append("\"");
		}
	}
}
