namespace UE4AssistantCLI.UI;

using ConsoleAppFramework;
using UE4Assistant;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
internal class CLI : ConsoleAppBase
{
	[Command("parse")]
	public async void Parse([Option("ci")] int caret_index = 0)
	{
		var result = await Program.ParseAsync(Console.ReadLine() ?? string.Empty, caret_index);
		Console.WriteLine(result);
	}

	[Command("update")]
	public async void Update()
	{
		SpecifierSchema.UpdateBenuiSpecifiers();
	}
}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
