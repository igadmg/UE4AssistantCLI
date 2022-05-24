﻿using DynamicDescriptors;
using System.ComponentModel;
using System.Drawing.Design;
using SystemEx;
using UE4Assistant;

namespace UE4AssistantCLI.UI;

public partial class FormMacroEditor : Form
{
	string str;
	Specifier specifier;
	DynamicTypeDescriptor so;

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
		comboBoxMacro.Items.AddRange(SpecifierSchema.ReadAvailableTags().Cast<object>().ToArray());

		Specifier.TryParse(str.tokenize(), out specifier);

		comboBoxMacro.SelectedIndex = comboBoxMacro.Items.Cast<TagModel>().ToList().FindIndex(i => i.name == specifier.tag.name);

		so = new SpecializerTypeDescriptor(specifier);

		propertyGridSpecifier.PropertyTabs.AddTabType(typeof(MetaPropertyTab), PropertyTabScope.Static);
		propertyGridSpecifier.SelectedObject = so;
		//propertyGridSpecifier.SelectedObject = new SpeсifierTypeDescriptor(specifier);
	}

	private void comboBoxMacro_SelectedIndexChanged(object sender, EventArgs e)
	{

	}

	private void propertyGridSpecifier_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
	{
		bool tabSelected = propertyGridSpecifier.SelectedTab is MetaPropertyTab;
		var cn = tabSelected ? "meta" : "parameters";
		var collection = specifier.model.collections[cn];
		var data = specifier.GetData(cn);

		foreach (var p in tabSelected
			? propertyGridSpecifier.SelectedTab.GetProperties(so).Cast<DynamicPropertyDescriptor>()
			: so.GetDynamicProperties())
		{
			var v = p.GetValue(so);
			var sp = collection.Find(mp => mp.name == p.Name);

			if (!sp.IsEmpty)
			{
				if (!Equals(v, sp.DefaultValue))
				{
					data[p.Name] = sp.type.IsNullOrWhiteSpace() ? null : v;
				}
				else
				{
					data.Remove(p.Name); 
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
					else
					{
						data.Remove(i.name);
					}
				}
			}
		}

		textBoxResult.Text = specifier.ToString();
	}
}
