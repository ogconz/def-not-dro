namespace JukeBox
{
	public class ChannelNode
	{
		public Node Info;

		public int Channel = -1;

		public int length;

		public float Volume;

		public float ChannelVolume
		{
			get
			{
				return Controller.GetChannelVolume(Channel);
			}
			set
			{
				Controller.SetChannelVolume(Channel, value);
			}
		}

		public ChannelNode(Node info)
		{
			Info = info;
		}

		public void ReleaseChannel()
		{
			Controller.ReleaseChannel(Channel);
			Volume = 0f;
			Channel = -1;
		}

		public bool AcquireChannel()
		{
			if (Channel != -1)
			{
				return true;
			}
			Channel = Controller.GetChannel(Info.Samples.RandomItem(), Info.Looped);
			Volume = 0f;
			return Channel != -1;
		}
	}
}
