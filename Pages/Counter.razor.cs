using ClassLibrary1;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorJSProxy.Pages
{
	public partial class Counter
	{
		[Inject]
		private Class1 Class1 { get; set; }

		protected override async Task OnInitializedAsync()
		{
			await base.OnInitializedAsync();
			await Class1.Test();
		}
	}
}
