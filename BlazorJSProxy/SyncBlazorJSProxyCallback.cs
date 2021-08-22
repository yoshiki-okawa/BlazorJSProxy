using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BlazorJSProxy
{
	public class SyncBlazorJSProxyCallback<T>
	{
		private readonly Action<T> callback;

		public SyncBlazorJSProxyCallback(Action<T> callback)
		{
			this.callback = callback;
		}

		[JSInvokable]
		public async Task InvokeAsync(T evt)
		{
			callback.Invoke(evt);
			await Task.CompletedTask;
		}
	}
}