using System;
using System.Runtime.InteropServices;

namespace SystemEx.Sleep
{
	public class PreventSleepGuard : IDisposable
	{
		public PreventSleepGuard()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				NativeFunctions.SetThreadExecutionState(ExecutionState.EsContinuous | ExecutionState.EsSystemRequired);
			}
		}

		public void Dispose()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				NativeFunctions.SetThreadExecutionState(ExecutionState.EsContinuous);
			}
		}
	}
}