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

		/// <summary>
		/// JS property or method name.
		/// </summary>
		public string Name { get; set; }
	}
}