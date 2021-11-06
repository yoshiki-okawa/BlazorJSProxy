using BlazorJSProxy;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace ClassLibrary1
{
	// Use JSTarget with ConstructorFunction as Parent is a custom class defined.
	[JSTarget(ConstructorFunction = "Parent")]
	public interface IParent : IAsyncDisposable
	{
		ValueTask<string> Name { get; set; }
		ValueTask<IChild> GetChild();
		// IArray is a simple Array JS proxy so that no parsing from JS and .NET vice versa are required until absolutely needed.
		ValueTask<IArray<IChild>> GetChildren();
		ValueTask<IChild> Child { get; set; }
		// IArray is a simple Array JS proxy so that no parsing from JS and .NET vice versa are required until absolutely needed.
		ValueTask<IArray<IChild>> Children { get; set; }
	}
	[JSTarget(ConstructorFunction = "Child")]
	public interface IChild : IAsyncDisposable
	{
		ValueTask<string> Name { get; set; }
	}

	[JSTarget(ConstructorFunction = "MouseEvent")]
	public interface IMouseEvent : IAsyncDisposable
	{
		ValueTask<int> OffsetX { get; set; }
		ValueTask<int> OffsetY { get; set; }
	}

	[JSTarget(GlobalVariable = "window.location")]
	public interface ILocation : IAsyncDisposable
	{
		ValueTask<string> Href { get; set; }
	}

	// Use JSTarget with GlobalVariable as window is a global variable in JS.
	[JSTarget(GlobalVariable = "window")]
	public interface IWindow : IAsyncDisposable
	{
		ValueTask<ILocation> Location { get; set; }
		// Use JSName to use custom propery name instead.
		[JSName("onclick")]
		Func<IMouseEvent, Task> OnClick { get; set; }
		// Use JSName to use custom propery name instead.
		[JSName("ondblclick")]
		Action<IMouseEvent> OnDoubleClick { set; }
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
			await TestWindow();
			await TestCustomParentChildClasses();
		}

		private async Task TestWindow()
		{
			// Create a JS proxy of window.
			await using var window = await BlazorJSProxy<IWindow>.CreateAsync(jsRuntime, null);
			// Get current URL.
			var location = await window.Location;
			Console.WriteLine(await location.Href); // https://localhost:5001/counter
			// Register a callback function to onclick with Func<IMouseEvent, Task>.
			window.OnClick = async evt =>
			{
				Console.WriteLine("Clicked:" + await evt.OffsetX + "," + await evt.OffsetY); //Clicked:99,18
				await Task.CompletedTask;
			};
			// Register a callback function to ondblclick with Action<IMouseEvent>.
			window.OnDoubleClick = async evt =>
			{
				Console.WriteLine("Double clicked:" + await evt.OffsetX + "," + await evt.OffsetY); //Double clicked:99,18
			};
			// window object is disposed automatically with await using from both .NET and JS.
		}

		private async Task TestCustomParentChildClasses()
		{
			await DefineCustomParentChildClasses();
			await TestProxyReturnType(x => x.GetChild(), x => x.GetChildren());
			await TestProxyReturnType(x => x.Child, x => x.Children);
		}

		private async Task DefineCustomParentChildClasses()
		{
			await jsRuntime.InvokeVoidAsync("eval", @"
window.Parent = class Parent {
	name = 'Parent';
	child = this.getChild();
	children = this.getChildren();

	getChild() {
		return new Child('Child1');
	}

	getChildren() {
		return [new Child('Child1'), new Child('Child2')];
	}
};
window.Child = class Child {
	constructor(name)
	{
		this.name = name;
	}
};");
		}

		private async Task TestProxyReturnType(Func<IParent, ValueTask<IChild>> getChild, Func<IParent, ValueTask<IArray<IChild>>> getChildren)
		{
			// Create a JS proxy of Parent. Every time this is called, new instance of Parent is created in JS.
			await using var parent = await BlazorJSProxy<IParent>.CreateAsync(jsRuntime, null);
			// Get the parent name.
			Console.WriteLine(await parent.Name); // Parent
			// Get a child.
			await using var child1 = await getChild(parent);
			// Get the child name.
			Console.WriteLine(await child1.Name); // Child1
			// Set the child name to Child3
			child1.Name = new ValueTask<string>("Child3");
			// Get the child's Name again.
			Console.WriteLine(await child1.Name); // Child3
			// Get children.
			await using var children = await getChildren(parent);
			// Get a number of children.
			Console.WriteLine(await children.Length); // 2
			// Get the first child name.
			Console.WriteLine(await (await children[0]).Name); // Child1
			// Get the second child name.
			Console.WriteLine(await (await children[1]).Name); // Child2
			// Set the first child name to Child4
			(await children[0]).Name = new ValueTask<string>("Child4");
			// Get the first child name again.
			Console.WriteLine(await (await children[0]).Name); // Child4
			// Set the first child = the second child.
			children[0] = new ValueTask<IChild>(await children[1]);
			// Get the first child name again.
			Console.WriteLine(await (await children[0]).Name); // Child2
			// Get the second child name again.
			Console.WriteLine(await (await children[1]).Name); // Child2
		}
	}
}
