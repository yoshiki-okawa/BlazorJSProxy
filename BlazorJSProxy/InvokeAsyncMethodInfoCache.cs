using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.JSInterop;

namespace BlazorJSProxy
{
	internal static class InvokeAsyncMethodInfoCache
	{
		private static MethodInfo jsRuntimeInvokeAsyncMethodInfo = typeof(IJSRuntime).GetMethod(nameof(IJSRuntime.InvokeAsync), new Type[] { typeof(string), typeof(object[]) });
		private static Dictionary<Type, MethodInfo> jsRuntimeInvokeAsyncMethodInfos = new Dictionary<Type, MethodInfo>();

		internal static MethodInfo GetJsRuntimeInvokeAsyncMethodInfo(Type genericTypeArgument)
		{
			if (!jsRuntimeInvokeAsyncMethodInfos.TryGetValue(genericTypeArgument, out MethodInfo methodinfo))
			{
				methodinfo = jsRuntimeInvokeAsyncMethodInfo.MakeGenericMethod(genericTypeArgument);
				jsRuntimeInvokeAsyncMethodInfos.Add(genericTypeArgument, methodinfo);
			}

			return methodinfo;
		}
	}
}