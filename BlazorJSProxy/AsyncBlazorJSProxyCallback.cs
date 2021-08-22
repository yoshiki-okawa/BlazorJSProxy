using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BlazorJSProxy
{
	public class AsyncBlazorJSProxyCallback<T>
	{
		private readonly Func<T, Task> callback;

		public AsyncBlazorJSProxyCallback(Func<T, Task> callback)
		{
			this.callback = callback;
		}

		[JSInvokable]
		public async Task InvokeAsync(T evt)
		{
			await callback.Invoke(evt);
		}
	}
}