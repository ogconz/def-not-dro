namespace JukeBox
{
	public static class ChannelNodeHelper
	{
		public static bool IsValid(this ChannelNode node)
		{
			if (node != null)
			{
				return node.Channel != -1;
			}
			return false;
		}
	}
}
