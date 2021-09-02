using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BlazorJSProxy
{
	public class ActionBlazorJSProxyCallback<T>
	{
		private readonly Action<T> callback;

		public ActionBlazorJSProxyCallback(Action<T> callback)
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