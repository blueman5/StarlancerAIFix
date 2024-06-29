using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x02000005 RID: 5
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	internal sealed class IgnoresAccessChecksToAttribute : Attribute
	{
		// Token: 0x06000014 RID: 20 RVA: 0x00002768 File Offset: 0x00000968
		public IgnoresAccessChecksToAttribute(string assemblyName)
		{
		}
	}
}
