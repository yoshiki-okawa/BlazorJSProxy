using System;

namespace BlazorJSProxy
{
	/// <summary>
	/// Specifies JS property or method name.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
	public class JSNameAttribute : Attribute
	{
		public JSNameAttribute(string name)
		{
			Name = name;
		}

		public string Name { get; set; }
	}
}