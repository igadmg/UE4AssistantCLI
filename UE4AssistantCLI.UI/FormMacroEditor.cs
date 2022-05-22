using DynamicDescriptors;
using System.Reflection.Metadata;
using System.Reflection;
using SystemEx;
using UE4Assistant;
using System.Drawing.Design;

namespace UE4AssistantCLI.UI;

public partial class FormMacroEditor : Form
{
	string str;
	Specifier specifier;
	SpecifierTypeDescriptor so;

	public FormMacroEditor(string str)
	{
		this.str = str;

		InitializeComponent();
	}

	private void FormMacroEditor_Load(object sender, EventArgs e)
	{
		tabControlPages.ItemSize = new Size(0, 1);
		tabControlPages.SizeMode = TabSizeMode.Fixed;

		comboBoxMacro.Items.Clear();
		comboBoxMacro.Items.AddRange(SpecifierSchema.ReadAvailableTags().ToArray());

		Specifier.TryParse(str.tokenize(), out specifier);
		var model = SpecifierSchema.ReadSpecifierModel(specifier.tag.name);

		comboBoxMacro.SelectedIndex = comboBoxMacro.Items.Cast<TagModel>().ToList().FindIndex(i => i.name == specifier.tag.name);

		//var items = model.parameters.Where(p => p.group.IsNullOrWhiteSpace()).ToDictionary(p => p.name, p => (object)p);
		var groups = model.parameters
			.GroupBy(p => p.group
				, LambdaComparer.Create(
					(string a, string b) => a.IsNullOrWhiteSpace() || b.IsNullOrWhiteSpace() ? -1 : string.Compare(a, b))
			);
		var items = groups.ToDictionary(g => g.Key.IsNullOrWhiteSpace() ? g.First().name : g.Key, g => g.ToArray());

		var dso_items = items.ToDictionary(i => i.Key
			, i => i.Value.Length == 1
				? (specifier.data.GetValueOrDefault(i.Value[0].name, i.Value[0].DefaultValue), i.Value[0].Type)
				: (i.Value.Select(i => i.name).FirstOrDefault(i => specifier.data.ContainsKey(i), string.Empty), typeof(string))
			);
		var dso = DynamicDescriptor.CreateFromDictionary(dso_items);

		foreach (var item in items)
		{
			var dp = dso.GetDynamicProperty(item.Key);
			var ri = item.Value[0];

			dp.SetCategory(ri.category);
			if (ri.group.IsNullOrWhiteSpace())
			{
				if (ri.Type == typeof(bool))
					dp.SetEditor(typeof(UITypeEditor), new CheckboxBoolEditor());
			}
			else
			{
				dp.SetConverter(new StandardValuesStringConverter(item.Value.Select(i => i.name)));
			}
		}
		//	items.ToDictionary(i => i.Key, i => i.Value.Item1)
		//	, items.ToDictionary(i => i.Key, i => i.Value.Item2));
		//dso.GetDynamicProperty("").SetPropertyOrder

		so = new SpecifierTypeDescriptor(model.parameters);
		so.FromDictionary(specifier.data);
		propertyGridSpecifier.SelectedObject = dso;
	}

	private void comboBoxMacro_SelectedIndexChanged(object sender, EventArgs e)
	{

	}

	private void propertyGridSpecifier_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
	{
		so.ToDictionary(specifier.data);
	}
}
