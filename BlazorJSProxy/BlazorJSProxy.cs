using Microsoft.JSInterop;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlazorJSProxy
{
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
			if (!String.IsNullOrWhiteSpace(globalVariable))
				Proxy.target = await jsRuntime.InvokeAsync<IJSObjectReference>("BlazorJSProxy.eval", $"return {globalVariable};");
			else
				Proxy.target = await jsRuntime.InvokeAsync<IJSObjectReference>("BlazorJSProxy.eval", $"return new (Function.prototype.bind.apply({constructorFunction}, arguments));", parameters);
			return proxy;
		}

		protected override object Invoke(MethodInfo targetMethod, object[] args)
		{
			return InvokeDisposeAsync(targetMethod, args) ??
				InvokeSetCallback(targetMethod, args) ??
				InvokeGetSet(targetMethod, args) ??
				InvokeMethod(targetMethod, args);
		}

		private object InvokeDisposeAsync(MethodInfo targetMethod, object[] args)
		{
			if (targetMethod.DeclaringType != typeof(IAsyncDisposable))
				return null;

			return target.DisposeAsync();
		}

		private object InvokeSetCallback(MethodInfo targetMethod, object[] args)
		{
			if (!targetMethod.Name.StartsWith("set_") || args.Length == 0 || !(args[0] is Delegate))
				return null;

			var propertyName = targetMethod.Name[4..];
			propertyName = Char.ToLower(propertyName[0]) + propertyName[1..];
			var arguments = args[0].GetType().GetGenericArguments();
			var callbackType = arguments[0];
			var isAsync = arguments.Length > 1;
			Type callbackClassType = CallbackClassTypeCache.GetCallbackClassType(isAsync, callbackType);
			var eventHandler = Activator.CreateInstance(callbackClassType, args[0]);
			var objectRef = DotNetObjectReference.Create(eventHandler);
			return jsRuntime.InvokeVoidAsync("BlazorJSProxy.setEventHandler", target, propertyName, objectRef);
		}

		private object InvokeGetSet(MethodInfo targetMethod, object[] args)
		{
			if (!targetMethod.Name.StartsWith("get_") && !targetMethod.Name.StartsWith("set_"))
				return null;

			var propertyName = targetMethod.Name[4..];
			propertyName = Char.ToLower(propertyName[0]) + propertyName[1..];
			if (targetMethod.Name.StartsWith("get_"))
			{
				MethodInfo invokeAsyncMethodInfo = InvokeAsyncMethodInfoCache.GetJsRuntimeInvokeAsyncMethodInfo(targetMethod.ReturnType.GenericTypeArguments[0]);
				return invokeAsyncMethodInfo.Invoke(jsRuntime, new object[] { "BlazorJSProxy.getProperty", new object[] { target, propertyName } });
			}
			else
				return jsRuntime.InvokeVoidAsync("BlazorJSProxy.setProperty", target, propertyName, args[0]);
		}

		private object InvokeMethod(MethodInfo targetMethod, object[] args)
		{
			string methodName = Char.ToLower(targetMethod.Name[0]) + targetMethod.Name[1..];
			if (targetMethod.ReturnType.IsGenericType)
				return InvokeAsyncMethodInfoCache.GetJsObjectInvokeAsyncMethodInfo(targetMethod.ReturnType.GenericTypeArguments[0]).Invoke(target, new object[] { methodName, args });
			return target.InvokeVoidAsync(methodName, args);
		}
	}
}
