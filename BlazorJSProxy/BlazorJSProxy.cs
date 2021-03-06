using Microsoft.JSInterop;
using System;
using System.Linq;
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
		private JSObjectReferenceWrapper target;

		public static async ValueTask<TInterface> CreateAsync(IJSRuntime jsRuntime, params object[] parameters)
		{
			return await CreateAsyncInternal(jsRuntime, null, parameters);
		}

		private static async ValueTask<TInterface> CreateAsyncInternal(IJSRuntime jsRuntime, ValueTask<JSObjectReferenceWrapper>? getTarget, params object[] parameters)
		{
			await BlazorJSProxyInitializer.InitializeAsync(jsRuntime);
			TInterface proxy = Create<TInterface, BlazorJSProxy<TInterface>>();
			var blazorJSProxy = proxy as BlazorJSProxy<TInterface>;
			blazorJSProxy.jsRuntime = jsRuntime;
			if (getTarget.HasValue)
				blazorJSProxy.target = await getTarget.Value;
			else if (!String.IsNullOrWhiteSpace(globalVariable))
				blazorJSProxy.target = await jsRuntime.InvokeAsync<JSObjectReferenceWrapper>("BlazorJSProxy.eval", $"return DotNet.createJSObjectReference({globalVariable});");
			else
				blazorJSProxy.target = await jsRuntime.InvokeAsync<JSObjectReferenceWrapper>("BlazorJSProxy.eval", $"return DotNet.createJSObjectReference(new (Function.prototype.bind.apply({constructorFunction}, arguments)));", PrepareArguments(jsRuntime, parameters));
			return proxy;
		}

		protected override object Invoke(MethodInfo targetMethod, object[] args)
		{
			return InvokeDisposeAsync(targetMethod, args) ??
				InvokeGetSet(targetMethod, args) ??
				InvokeMethod(targetMethod, args);
		}

		private object InvokeDisposeAsync(MethodInfo targetMethod, object[] args)
		{
			if (targetMethod.DeclaringType != typeof(IAsyncDisposable))
				return null;

			return jsRuntime.InvokeVoidAsync("DotNet.disposeJSObjectReference", target);
		}

		private object InvokeGetSet(MethodInfo targetMethod, object[] args)
		{
			if (!targetMethod.Name.StartsWith("get_") && !targetMethod.Name.StartsWith("set_"))
				return null;

			var prop = targetMethod.DeclaringType.GetProperties().FirstOrDefault(prop => prop.GetGetMethod() == targetMethod || prop.GetSetMethod() == targetMethod);
			var jsName = prop?.GetCustomAttribute<JSNameAttribute>()?.Name;
			var propertyName = targetMethod.Name[4..];
			propertyName = jsName ?? Char.ToLower(propertyName[0]) + propertyName[1..];
			bool isArrayItem = isArray && propertyName == "item";
			object arrayIndexOrPropertyName = isArrayItem ? args[0] : propertyName;
			if (targetMethod.Name.StartsWith("get_"))
			{
				return InvokeMethodInternal(targetMethod, "BlazorJSProxy.getProperty(...arguments)", arrayIndexOrPropertyName);
			}
			else
			{
				var arg = isArrayItem ? args[1] : args[0];
				if (arg is not Delegate)
					arg = (object)((dynamic)arg).Result;
				jsRuntime.InvokeVoidAsync("BlazorJSProxy.setProperty", target, propertyName, PrepareArgument(jsRuntime, arg));
				return null;
			}
		}

		private object InvokeMethod(MethodInfo targetMethod, object[] args)
		{
			var jsName = targetMethod.GetCustomAttribute<JSNameAttribute>()?.Name;
			string methodName = jsName ?? Char.ToLower(targetMethod.Name[0]) + targetMethod.Name[1..];
			string code = $"arguments[0]['{methodName}'](...arguments[1])";

			if (targetMethod.ReturnType.IsGenericType)
				return InvokeMethodInternal(targetMethod, code, PrepareArguments(jsRuntime, args));
			return jsRuntime.InvokeVoidAsync("BlazorJSProxy.eval", $"return {code};", new object[] { target, PrepareArguments(jsRuntime, args) });
		}

		private object InvokeMethodInternal(MethodInfo targetMethod, string code, object args)
		{
			Type returnType = targetMethod.ReturnType.GenericTypeArguments[0];
			Type invokeAsyncGenericType = returnType;
			bool isJSTarget = returnType.GetCustomAttribute<JSTargetAttribute>() != null || returnType.IsArray && returnType.GetElementType().GetCustomAttribute<JSTargetAttribute>() != null;
			if (isJSTarget)
				invokeAsyncGenericType = typeof(JSObjectReferenceWrapper);

			MethodInfo invokeAsyncMethodInfo = InvokeAsyncMethodInfoCache.GetJsRuntimeInvokeAsyncMethodInfo(invokeAsyncGenericType);
			string finalCode = isJSTarget ? $"return DotNet.createJSObjectReference({code});" : $"return {code};";
			object result = invokeAsyncMethodInfo.Invoke(jsRuntime, new object[] { "BlazorJSProxy.eval", new object[] { finalCode, new object[] { target, args } } });

			if (isJSTarget)
				return CreateAsyncMethodInfoCache.GetCreateAsyncMethodInfo(returnType).Invoke(null, new object[] { jsRuntime, result, null });

			return result;
		}

		private static object[] PrepareArguments(IJSRuntime jsRuntime, object[] arguments)
		{
			if (arguments == null || arguments.Length == 0)
				return arguments;

			var result = new object[arguments.Length];
			for (int i = 0; i < arguments.Length; i++)
				result[i] = PrepareArgument(jsRuntime, arguments[i]);
			return result;
		}

		private static object PrepareArgument(IJSRuntime jsRuntime, object argument)
		{
			if (argument == null)
				return argument;

			Type type = argument.GetType().BaseType;
			if (type == null)
				return argument;

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(BlazorJSProxy<>))
				return ((dynamic)argument).target;

			if (argument is Delegate del)
				return CallBackInteropWrapper.Create(jsRuntime, del);

			return argument;
		}
	}
}
