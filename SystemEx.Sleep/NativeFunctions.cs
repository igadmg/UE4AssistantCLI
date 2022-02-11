using System;
using System.Runtime.InteropServices;

namespace SystemEx.Sleep
{
	[Flags]
	public enum ExecutionState : uint
	{
		EsAwaymodeRequired = 0x00000040,
		EsContinuous = 0x80000000,
		EsDisplayRequired = 0x00000002,
		EsSystemRequired = 0x00000001
	}

	internal class NativeFunctions
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);
	}
}
