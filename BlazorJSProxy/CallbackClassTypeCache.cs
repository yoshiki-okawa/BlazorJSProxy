using System;
using System.Collections.Generic;

namespace BlazorJSProxy
{
	internal static class CallbackClassTypeCache
	{
		private static Dictionary<Type, Type> asyncCallbackClassTypes = new Dictionary<Type, Type>();
		private static Dictionary<Type, Type> syncCallbackClassTypes = new Dictionary<Type, Type>();

		internal static Type GetCallbackClassType(bool isAsync, Type callbackType)
		{
			var callbackClassTypes = isAsync ? asyncCallbackClassTypes : syncCallbackClassTypes;

			if (!callbackClassTypes.TryGetValue(callbackType, out Type callbackClassType))
			{
				callbackClassType = (isAsync ? typeof(FuncBlazorJSProxyCallback<>) : typeof(ActionBlazorJSProxyCallback<>)).MakeGenericType(callbackType);
				callbackClassTypes.Add(callbackType, callbackClassType);
			}

			return callbackClassType;
		}
	}
}