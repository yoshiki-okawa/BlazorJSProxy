using BlazorJSProxy;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace ClassLibrary1
{
	[JSTarget(ConstructorFunction = "RTCPeerConnection")]
	public interface IRTCPeerConnection : IAsyncDisposable
	{
		ValueTask<string> ConnectionState { get; }
	}

	public class MouseEvent
	{
		public int OffsetX { get; set; }
		public int OffsetY { get; set; }
	}

	[JSTarget(GlobalVariable = "window")]
	public interface IWindow : IAsyncDisposable
	{
		Func<MouseEvent, Task> Onclick { set; }
	}

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
			window.Onclick = async evt =>
			{
				var a = evt.OffsetX;
				var b = evt.OffsetY;
			};

			var peer = await BlazorJSProxy<IRTCPeerConnection>.CreateAsync(jsRuntime);
			var a = await peer.ConnectionState;
			await peer.DisposeAsync();
		}
	}
}
