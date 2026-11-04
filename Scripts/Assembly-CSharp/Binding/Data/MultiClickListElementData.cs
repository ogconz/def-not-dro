using System;

namespace Binding.Data
{
	public class MultiClickListElementData : BaseListElementData
	{
		public event Action<MultiClickListElementData> OnClick1;

		public event Action<MultiClickListElementData> OnClick2;

		public event Action<MultiClickListElementData> OnClick3;

		public event Action<MultiClickListElementData> OnClick4;

		public event Action<MultiClickListElementData> OnClick5;

		public void Click1()
		{
			this.OnClick1.Call(this);
		}

		public void Click2()
		{
			this.OnClick2.Call(this);
		}

		public void Click3()
		{
			this.OnClick3.Call(this);
		}

		public void Click4()
		{
			this.OnClick4.Call(this);
		}

		public void Click5()
		{
			this.OnClick5.Call(this);
		}
	}
}
