# BlazorJSProxy
Dynamic proxy of Microsoft.JSInterop.IJSRuntime which makes defining and invoking JS object much easier.\
All you need is to define interface for JavaScript class with matching properties and methods.\
[![NuGet](https://img.shields.io/nuget/v/BlazorJSProxy.svg)](https://www.nuget.org/packages/BlazorJSProxy/)

**Example**
```c#
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

[JSTarget(ConstructorFunction = "RTCPeerConnection")]
public interface IRTCPeerConnection : IAsyncDisposable
{
	ValueTask<string> ConnectionState { get; }
}

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
```
