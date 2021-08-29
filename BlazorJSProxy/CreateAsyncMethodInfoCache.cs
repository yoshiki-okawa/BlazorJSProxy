using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BlazorJSProxy
{
	internal static class CreateAsyncMethodInfoCache
	{
		private static Dictionary<Type, MethodInfo> createAsyncMethodInfos = new Dictionary<Type, MethodInfo>();

		internal static MethodInfo GetCreateAsyncMethodInfo(Type genericTypeArgument)
		{
			if (!createAsyncMethodInfos.TryGetValue(genericTypeArgument, out MethodInfo methodinfo))
			{
				methodinfo = typeof(BlazorJSProxy<>).MakeGenericType(genericTypeArgument).GetMethod("CreateAsyncInternal", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(IJSRuntime), typeof(ValueTask<IJSObjectReference>), typeof(object[]) }, null);
				createAsyncMethodInfos.Add(genericTypeArgument, methodinfo);
			}

			return methodinfo;
		}
	}
}