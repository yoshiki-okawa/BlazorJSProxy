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

	public class Location{
		public string Href {get; set; }
	}

	[JSTarget(GlobalVariable = "window")]
	public interface IWindow : IAsyncDisposable
	{
		ValueTask<Location> Location { get; set; }
		Func<MouseEvent, Task> Onclick { set; }
		Action<MouseEvent> Ondblclick { set; }
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
				Console.WriteLine("Clicked:" + evt.OffsetX + "," + evt.OffsetY);
				await Task.CompletedTask;
			};
			window.Ondblclick = evt =>
			{
				Console.WriteLine("Double clicked:" + evt.OffsetX + "," + evt.OffsetY);
			};
			var location = await window.Location;
			Console.WriteLine(location.Href);
			await window.DisposeAsync();

			var peer = await BlazorJSProxy<IRTCPeerConnection>.CreateAsync(jsRuntime);
			var connectionState = await peer.ConnectionState;
			Console.WriteLine(connectionState);
			await peer.DisposeAsync();
		}
	}
}
