using BlazorJSProxy;
using Microsoft.JSInterop;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClassLibrary1
{
	public class Class1
	{
		private IJSRuntime jsRuntime;
		public Class1(IJSRuntime jsRuntime)
		{
			this.jsRuntime = jsRuntime;
		}

		public async Task Test()
		{
			var window = await BlazorJSProxy<IWindow>.CreateAsync(jsRuntime);
			var aaa = await window.Eval2<string>("return 'aaa';");
			window.Onclick = async evt => {
				var a = evt.OffsetX;
				var b = evt.OffsetY;
			};

			var peer = await BlazorJSProxy<IRTCPeerConnection>.CreateAsync(jsRuntime);
			var a = await peer.ConnectionState;
			await peer.DisposeAsync();
			/*var a = await jsRuntime.InvokeAsync<IJSObjectReference>("eval", "new RTCPeerConnection()");
			var b = await jsRuntime.InvokeAsync<string>("getProperty", a, "connectionState");
			var t = b.GetType().FullName;
			var c = await a.InvokeAsync<IJSObjectReference>("getConfiguration");
			var d = await a.InvokeAsync<object>("getConfiguration");*/
		}
	}
}
