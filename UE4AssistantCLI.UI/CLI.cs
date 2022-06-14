namespace UE4AssistantCLI.UI;

using ConsoleAppFramework;

internal class CLI : ConsoleAppBase
{
	[Command("parse")]
	public async void Parse([Option("ci")] int caret_index = 0)
	{
		var result = await Program.ParseAsync(Console.ReadLine(), caret_index);
		Console.WriteLine(result);
	}
}