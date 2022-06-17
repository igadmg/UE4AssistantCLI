using DynamicDescriptors;
using System.ComponentModel;
using SystemEx;
using UE4Assistant;

namespace UE4AssistantCLI.UI;

public partial class FormMacroEditor : Form
{
	DynamicTypeDescriptor? so = null;
	Specifier specifier_;

	public Specifier specifier {
		get {
			bool tabSelected = propertyGridSpecifier.SelectedTab is MetaPropertyTab;
			var cn = tabSelected ? "meta" : "parameters";
			var collection = specifier_.model.collections[cn];

			var data = new Dictionary<string, object?>();
			foreach (var p in tabSelected
				? propertyGridSpecifier.SelectedTab.GetProperties(so!).Cast<DynamicPropertyDescriptor>()
				: so!.GetDynamicProperties())
			{
				var v = p.GetValue(so);
				var sp = collection.Find(mp => mp.name == p.Name);

				if (!sp.IsEmpty)
				{
					if (!Equals(v, sp.DefaultValue))
					{
						data[p.Name] = sp.type.IsNullOrWhiteSpace() ? null : v;
					}
				}
				else
				{
					var group = collection.FindAll(mp => mp.group == p.Name);
					foreach (var i in group.Skip(1))
					{
						if (Equals(v, i.name))
						{
							data[i.name] = null;
						}
					}
				}
			}

			return specifier_.Swap(tabSelected ? "meta" : "", data);
		}
		set {
			specifier_ = value;
			if (!specifier_.IsEmpty)
			{
				comboBoxMacro.SelectedIndex = comboBoxMacro.Items.Cast<TagModel>().ToList().FindIndex(i => i.name == specifier_.tag.name);
				textBoxResult.Text = specifier_.ToString();
				so = new SpecializerTypeDescriptor(specifier_);
			}

			propertyGridSpecifier.SelectedObject = so;

			propertyGridSpecifier.PropertyTabs.Clear(PropertyTabScope.Component);
			propertyGridSpecifier.PropertyTabs.AddTabType(typeof(MetaPropertyTab), PropertyTabScope.Component);
		}
	}

	public FormMacroEditor(Specifier specifier)
	{
		InitializeComponent();

		specifier_ = specifier;
	}

	private void FormMacroEditor_Load(object sender, EventArgs e)
	{
		tabControlPages.ItemSize = new Size(0, 1);
		tabControlPages.SizeMode = TabSizeMode.Fixed;

		comboBoxMacro.Items.Clear();
		comboBoxMacro.Items.AddRange(SpecifierSchema.ReadAvailableTags().Cast<object>().ToArray());

		specifier = specifier_;
	}

	private void comboBoxMacro_SelectedIndexChanged(object sender, EventArgs e)
	{
		var tag = (TagModel)comboBoxMacro.SelectedItem;

		if (tag.type == "specifier")
		{
			if (specifier_.type != tag.ToString())
				specifier = Specifier.Create(tag);
		}
	}

	private void propertyGridSpecifier_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
	{
		textBoxResult.Text = specifier.ToString();
	}

	private void buttonApply_Click(object sender, EventArgs e)
	{
		DialogResult = DialogResult.OK;
		Close();
	}
}
