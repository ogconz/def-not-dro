using System;
using System.Threading.Tasks;

namespace TriLib
{
	public static class ThreadUtils
	{
		public static Task RunThread(Action action, Action onComplete)
		{
			Dispatcher.CheckInstance();
			return Task.Run(delegate
			{
				try
				{
					action();
					Dispatcher.InvokeAsync(onComplete);
				}
				catch (Exception ex)
				{
					Exception ex2 = ex;
					Exception exception = ex2;
					Dispatcher.InvokeAsync(delegate
					{
						throw exception;
					});
				}
			});
		}
	}
}
