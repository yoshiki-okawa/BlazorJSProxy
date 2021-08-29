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
		private static readonly bool isArray = interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IArray<>);
		private static readonly string constructorFunction = interfaceType.GetCustomAttribute<JSTargetAttribute>()?.ConstructorFunction ?? Regex.Replace(interfaceType.Name, "^I", String.Empty);
		private static readonly string globalVariable = interfaceType.GetCustomAttribute<JSTargetAttribute>()?.GlobalVariable;
		private IJSRuntime jsRuntime;
		private IJSObjectReference target;

		public static async ValueTask<TInterface> CreateAsync(IJSRuntime jsRuntime, params object[] parameters)
		{
			return await CreateAsyncInternal(jsRuntime, null, parameters);
		}

		internal static async ValueTask<TInterface> CreateAsyncInternal(IJSRuntime jsRuntime, ValueTask<IJSObjectReference>? getTarget, params object[] parameters)
		{
			await BlazorJSProxyInitializer.InitializeAsync(jsRuntime);
			TInterface proxy = Create<TInterface, BlazorJSProxy<TInterface>>();
			var blazorJSProxy = proxy as BlazorJSProxy<TInterface>;
			blazorJSProxy.jsRuntime = jsRuntime;
			if (getTarget.HasValue)
				blazorJSProxy.target = await getTarget.Value;
			else if (!String.IsNullOrWhiteSpace(globalVariable))
				blazorJSProxy.target = await jsRuntime.InvokeAsync<IJSObjectReference>("BlazorJSProxy.eval", $"return {globalVariable};");
			else
				blazorJSProxy.target = await jsRuntime.InvokeAsync<IJSObjectReference>("BlazorJSProxy.eval", $"return new (Function.prototype.bind.apply({constructorFunction}, arguments));", ConvertToJSObjectReference(parameters));
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
				var type = targetMethod.ReturnType.GenericTypeArguments[0];
				if (type.GetCustomAttribute<JSTargetAttribute>() != null || type.IsArray && type.GetElementType().GetCustomAttribute<JSTargetAttribute>() != null)
				{
					MethodInfo invokeAsyncMethodInfo2 = InvokeAsyncMethodInfoCache.GetJsRuntimeInvokeAsyncMethodInfo(typeof(IJSObjectReference));
					var getTarget = (ValueTask<IJSObjectReference>)invokeAsyncMethodInfo2.Invoke(jsRuntime, new object[] { "BlazorJSProxy.getProperty", new object[] { target, propertyName } });
					return CreateAsyncMethodInfoCache.GetCreateAsyncMethodInfo(type).Invoke(null, new object[] { jsRuntime, getTarget, null });
				}
				MethodInfo invokeAsyncMethodInfo = InvokeAsyncMethodInfoCache.GetJsRuntimeInvokeAsyncMethodInfo(type);
				return invokeAsyncMethodInfo.Invoke(jsRuntime, new object[] { "BlazorJSProxy.getProperty", new object[] { target, propertyName } });
			}
			else
			{
				return jsRuntime.InvokeVoidAsync("BlazorJSProxy.setProperty", target, propertyName, ConvertToJSObjectReference((object)((dynamic)args[0]).Result));
			}
		}

		private object InvokeMethod(MethodInfo targetMethod, object[] args)
		{
			string methodName = Char.ToLower(targetMethod.Name[0]) + targetMethod.Name[1..];

			if (targetMethod.ReturnType.IsGenericType)
			{
				Type returnType = targetMethod.ReturnType.GenericTypeArguments[0];
				Type invokeAsyncGenericType = returnType;
				bool isJSTarget = returnType.GetCustomAttribute<JSTargetAttribute>() != null || returnType.IsArray && returnType.GetElementType().GetCustomAttribute<JSTargetAttribute>() != null;
				if (isJSTarget)
					invokeAsyncGenericType = typeof(IJSObjectReference);

				object result;
				if (isArray && methodName == "getValue")
				{
					MethodInfo invokeAsyncMethodInfo = InvokeAsyncMethodInfoCache.GetJsRuntimeInvokeAsyncMethodInfo(invokeAsyncGenericType);
					result = invokeAsyncMethodInfo.Invoke(jsRuntime, new object[] { "BlazorJSProxy.getProperty", new object[] { target, args[0] } });
				}
				else
				{
					MethodInfo invokeAsyncMethodInfo = InvokeAsyncMethodInfoCache.GetJsObjectInvokeAsyncMethodInfo(invokeAsyncGenericType);
					result = invokeAsyncMethodInfo.Invoke(target, new object[] { methodName, ConvertToJSObjectReference(args) });
				}

				if (isJSTarget)
					return CreateAsyncMethodInfoCache.GetCreateAsyncMethodInfo(returnType).Invoke(null, new object[] { jsRuntime, result, null });

				return result;
			}
			if (isArray && methodName == "setValue")
				return jsRuntime.InvokeVoidAsync("BlazorJSProxy.setProperty", target, args[0], ConvertToJSObjectReference(args[1]));
			return target.InvokeVoidAsync(methodName, ConvertToJSObjectReference(args));
		}

		private static object[] ConvertToJSObjectReference(object[] arguments)
		{
			if (arguments == null)
				return arguments;

			var result = new object[arguments.Length];
			for (int i = 0; i < arguments.Length; i++)
				result[i] = ConvertToJSObjectReference(arguments[i]);
			return result;
		}

		private static object ConvertToJSObjectReference(object argument)
		{
			if (argument == null)
				return argument;

			Type type = argument.GetType().BaseType;
			if (type == null || !type.IsGenericType || type.GetGenericTypeDefinition() != typeof(BlazorJSProxy<>))
				return argument;

			return ((dynamic)argument).target;
		}
	}
}
