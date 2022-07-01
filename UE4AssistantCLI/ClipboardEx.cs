using ClipboardEx.Win32;

namespace UE4AssistantCLI;

public static class ClipboardEx
{
	public static string GetConsoleOrClipboardText()
		=> GetConsoleOrClipboardText(out bool fromClipboard);

	public static string GetConsoleOrClipboardText(out bool fromClipboard)
	{
		if (Console.IsInputRedirected)
		{
			fromClipboard = false;
			return Console.In.ReadToEnd();
		}
		else
		{
			using (var clipboard = new Clipboard())
			{
				string text = clipboard.Text;
				fromClipboard = text != null;
				return fromClipboard ? text : Console.In.ReadToEnd();
			}
		}
	}
}
