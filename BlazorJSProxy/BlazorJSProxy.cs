using Microsoft.JSInterop;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlazorJSProxy
{
	[AttributeUsage(AttributeTargets.Interface)]
	public class JSTargetAttribute : Attribute
	{
		public string ConstructorFunction { get; set; }
		public string GlobalVariable { get; set; }
	}

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

	static class BlazorJSProxyInitializer
	{
		public static MethodInfo JsRuntimeInvokeAsyncMethodInfo = typeof(IJSRuntime).GetMethod(nameof(IJSObjectReference.InvokeAsync), new Type[] { typeof(string), typeof(object[]) });
		public static MethodInfo JsObjectInvokeAsyncMethodInfo = typeof(IJSObjectReference).GetMethod(nameof(IJSObjectReference.InvokeAsync), new Type[] { typeof(string), typeof(object[]) });
		private static bool init = false;
		public static async Task InitializeAsync(IJSRuntime jsRuntime)
		{
			if (!init)
			{
				await jsRuntime.InvokeVoidAsync("eval", "window.eval2 = function() { return new Function(arguments[0]).apply(null, arguments[1]); }");
				await jsRuntime.InvokeVoidAsync("eval2", "window.getProperty = function(x, y) { return x[y]; }");
				await jsRuntime.InvokeVoidAsync("eval2", "window.setProperty = function(x, y, z) { return x[y] = z; }");
				await jsRuntime.InvokeVoidAsync("eval2", @"window.cloneObjWithFilter = function (obj, filter)
{
	if (Object(obj) !== obj)
		return obj;
	else if (Array.isArray(obj))
		return obj.reduce((previousValue, currentValue) =>
		{
			if (filter(undefined, currentValue))
				return previousValue.push(cloneObjWithFilter(currentValue, filter));
			return previousValue;
		}, []);

	let newObj = {};
	for (let k in obj)
	{
		let v = obj[k];
		if (filter(k, v))
			newObj[k] = cloneObjWithFilter(obj[k], filter);
	}
	return newObj;
}");
				await jsRuntime.InvokeVoidAsync("eval2", @"window.prepareObjectForCallback = (obj) =>
{
					console.log(cloneObjWithFilter);
					return cloneObjWithFilter(obj, (k, v) => v !== window && typeof v !== 'function' && !(v instanceof Element) && !(v instanceof HTMLDocument));
				}
				");
				await jsRuntime.InvokeVoidAsync("eval2", "window.setEventHandler = function(x, y, z) { console.log(z);return x[y] = function() { z.invokeMethodAsync('InvokeAsync', prepareObjectForCallback(arguments[0])) }; }");
			}
			init = true;
		}
	}

	public class BlazorJSProxy<TInterface> : DispatchProxy where TInterface : IAsyncDisposable
	{
		private static readonly Type interfaceType = typeof(TInterface);
		private static readonly string constructorFunction = interfaceType.GetCustomAttribute<JSTargetAttribute>()?.ConstructorFunction ?? Regex.Replace(interfaceType.Name, "^I", String.Empty);
		private static readonly string globalVariable = interfaceType.GetCustomAttribute<JSTargetAttribute>()?.GlobalVariable;
		private IJSRuntime jsRuntime;
		private IJSObjectReference target;

		public static async Task<TInterface> CreateAsync(IJSRuntime jsRuntime, params object[] parameters)
		{
			await BlazorJSProxyInitializer.InitializeAsync(jsRuntime);
			TInterface proxy = Create<TInterface, BlazorJSProxy<TInterface>>();
			var Proxy = proxy as BlazorJSProxy<TInterface>;
			Proxy.jsRuntime = jsRuntime;
			if(!String.IsNullOrWhiteSpace(globalVariable))
				Proxy.target = await jsRuntime.InvokeAsync<IJSObjectReference>("eval2", $"return {globalVariable};");
			else
				Proxy.target = await jsRuntime.InvokeAsync<IJSObjectReference>("eval2", $"return new (Function.prototype.bind.apply({constructorFunction}, arguments));", parameters);
			return proxy;
		}

		protected override object Invoke(MethodInfo targetMethod, object[] args)
		{
			if (targetMethod.DeclaringType == typeof(IAsyncDisposable))
				return target.DisposeAsync();

			if (targetMethod.Name.StartsWith("get_") || targetMethod.Name.StartsWith("set_"))
			{
				var propertyName = targetMethod.Name[4..];
				propertyName = Char.ToLower(propertyName[0]) + propertyName[1..];
				if (args.Length > 0 && args[0] is Delegate)
				{
					var arguments = args[0].GetType().GetGenericArguments();
					var callbackType = arguments[0];
					var isAsync = arguments.Length > 1;
					var eventHandler = (isAsync ? typeof(AsyncBlazorJSProxyCallback<>) : typeof(SyncBlazorJSProxyCallback<>)).MakeGenericType(callbackType).GetConstructors()[0].Invoke(new object[] { args[0] });
					var objectRef = typeof(DotNetObjectReference).GetMethod("Create").MakeGenericMethod(eventHandler.GetType()).Invoke(null, new object[] { eventHandler });
					return jsRuntime.InvokeVoidAsync("setEventHandler", target, propertyName, objectRef);
				}
				if (targetMethod.Name.StartsWith("get_"))
				{
					MethodInfo invokeAsyncMethodInfo = BlazorJSProxyInitializer.JsRuntimeInvokeAsyncMethodInfo.MakeGenericMethod(targetMethod.ReturnType.GenericTypeArguments[0]);
					return invokeAsyncMethodInfo.Invoke(jsRuntime, new object[] { "getProperty", new object[] { target, propertyName } });
				}
				else
					return jsRuntime.InvokeVoidAsync("setProperty", target, propertyName, args[0]);
			}

			string methodName = Char.ToLower(targetMethod.Name[0]) + targetMethod.Name[1..];
			if (targetMethod.ReturnType.IsGenericType)
				return BlazorJSProxyInitializer.JsObjectInvokeAsyncMethodInfo.MakeGenericMethod(targetMethod.ReturnType.GenericTypeArguments[0]).Invoke(target, new object[] { methodName, args });
			return target.InvokeVoidAsync(methodName, args);
		}
	}
}
