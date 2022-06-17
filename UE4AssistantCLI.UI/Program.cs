namespace UE4AssistantCLI.UI;

using ConsoleAppFramework;
using System.Runtime.InteropServices;
using SystemEx;
using UE4Assistant;

static class Program
{
	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	static extern bool AllocConsole();



	internal static async Task<string> ParseAsync(string str, int caret_index)
	{
		var result = str;

		var signal = new SemaphoreSlim(0, 1);
		Application.Run(MacroForm(str, caret_index, str => { result = str; signal.Release(); }));
		await signal.WaitAsync();

		return result;
	}

	/// <summary>
	///  The main entry point for the application.
	/// </summary>
	[STAThread]
	static void Main()
	{
		ApplicationConfiguration.Initialize();
		AllocConsole();

		Console.SetIn(new StringReader("	UPROPERTY(Category = \"Default\", VisibleAnywhere, BlueprintReadOnly) int i = 0;\n"));

		if (!SpecifierSchema.HaveBenuiSpecifiers)
			SpecifierSchema.UpdateBenuiSpecifiers();

		ConsoleApp.CreateBuilder(Environment.GetCommandLineArgs().Skip(1)
				, options => {
					options.HelpSortCommandsByFullName = true;
				})
			.Build()
			.AddCommands<CLI>()
			.Run();
	}

	public static FormMacroEditor MacroForm(string str, int caret_index, Action<string> a)
		=> Specifier.FindAll(str, caret_index).First()
			.Let(specifier => new FormMacroEditor(specifier.s)
				.Also(_ => {
					_.FormClosed += (object? sender, FormClosedEventArgs e) => {
						var form = (FormMacroEditor)sender!;
						if (form.DialogResult == DialogResult.OK)
						{
							str = str
								.Remove(specifier.si, specifier.ei - specifier.si)
								.Insert(specifier.si, form.specifier.ToString());
						}

						a(str);
					};
				}));
}

public static class StreamEx
{
	public static Stream Write(this Stream stream, string str)
		=> stream.Also(_ => {
			var data = str.ToByte();
			stream.Write(data, 0, data.Length);
		});
	public static Task WriteAsync(this Stream stream, string str)
		=> stream.Let(_ => {
			var data = str.ToByte();
			return stream.WriteAsync(data, 0, data.Length);
		});
}
