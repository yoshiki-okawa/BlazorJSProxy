using BlazorJSProxy;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace ClassLibrary1
{
	[JSTarget(ConstructorFunction = "Parent")]
	public interface IParent : IAsyncDisposable
	{
		ValueTask<string> Name { get; set; }
		ValueTask<IChild> GetChild();
		ValueTask<IArray<IChild>> GetChildren();
		ValueTask<IChild> Child { get; set; }
		ValueTask<IArray<IChild>> Children { get; set; }
	}
	[JSTarget(ConstructorFunction = "Child")]
	public interface IChild : IAsyncDisposable
	{
		ValueTask<string> Name { get; set; }
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
			var window = await BlazorJSProxy<IWindow>.CreateAsync(jsRuntime, null);
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
			await jsRuntime.InvokeVoidAsync("eval", @"
window.Parent = class Parent {
  name = 'Parent';
  child = this.getChild();
  children = this.getChildren();
  
  getChild() {
	  this.child1 = new Child();
	  this.child1.name = 'Child1';
    return this.child1;
  }
  getChildren() {
	  this.child1 = new Child();
	  this.child1.name = 'Child1';
	  this.child2 = new Child();
	  this.child2.name = 'Child2';
    return [this.child1, this.child2];
  }
};
window.Child = class Child {
  name = 'Child';
};");
			await TestProxyReturnType(x => x.GetChild(), x => x.GetChildren());
			await TestProxyReturnType(x => x.Child, x => x.Children);
		}

		public async Task TestProxyReturnType(Func<IParent, ValueTask<IChild>> getChild, Func<IParent, ValueTask<IArray<IChild>>> getChildren)
		{
			var parent = await BlazorJSProxy<IParent>.CreateAsync(jsRuntime, null);
			var name = await parent.Name;
			Console.WriteLine(name);
			var child1 = await getChild(parent);
			name = await child1.Name;
			Console.WriteLine(name);
			child1.Name = new ValueTask<string>("Child3");
			name = await child1.Name;
			Console.WriteLine(name);
			var children = await getChildren(parent);
			Console.WriteLine(await children.Length);
			name = await (await children.GetValue(0)).Name;
			Console.WriteLine(name);
			name = await (await children.GetValue(1)).Name;
			Console.WriteLine(name);
			(await children.GetValue(0)).Name = new ValueTask<string>("Child4");
			name = await (await children.GetValue(0)).Name;
			Console.WriteLine(name);
			await children.SetValue(0, await children.GetValue(1));
			name = await (await children.GetValue(0)).Name;
			Console.WriteLine(name);
			await parent.DisposeAsync();
		}
	}
}
