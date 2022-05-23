using DynamicDescriptors;
using System.ComponentModel;
using System.Drawing.Design;
using System.Xml.Linq;
using SystemEx;
using UE4Assistant;

namespace UE4AssistantCLI.UI
{
	public class CheckboxBoolEditor : UITypeEditor
	{
		public override bool GetPaintValueSupported(System.ComponentModel.ITypeDescriptorContext context) => true;
		public override void PaintValue(PaintValueEventArgs e)
		{
			var rect = e.Bounds;
			//rect.Inflate(-2, -2);
			ControlPaint.DrawCheckBox(e.Graphics, rect, ButtonState.Flat | (((bool)e.Value) ? ButtonState.Checked : ButtonState.Normal));
		}
	}

	public class SpecializerTypeDescriptor : DynamicTypeDescriptor
	{
		public Specifier specifier;

		public SpecializerTypeDescriptor(ICustomTypeDescriptor parent)
			: base(parent)
		{
		}

		public static SpecializerTypeDescriptor Create(Specifier specifier)
		{
			var items = specifier.ToModelDictionary("parameters");
			var dso_items = ToTypeDescriptionDictionary(specifier, items);

			var so = new SpecializerTypeDescriptor(new DictionaryTypeDescriptor(dso_items));
			so.specifier = specifier;

			foreach (var item in items)
			{
				var ri = item.Value[0];
				var dp = so.GetDynamicProperty(item.Key)
					.SetCategory(ri.category);

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

			return so;
		}

		public PropertyDescriptorCollection GetProperties(string name)
		{
			var items = specifier.ToModelDictionary(name);
			var dso_items = ToTypeDescriptionDictionary(specifier, items);

			foreach (var pair in dso_items)
			{		
				var propertyDescriptor = new DynamicPropertyDescriptor(
					new DictionaryPropertyDescriptor(dso_items, pair.Key, pair.Value.Item2));
				//propertyDescriptor.AddValueChanged(this, (s, e) => OnPropertyChanged(pair.Key));
				//_propertyDescriptors.Add(propertyDescriptor);
			}

			return new PropertyDescriptorCollection(null, true);
		}

		private static Dictionary<string, (object, Type)> ToTypeDescriptionDictionary(Specifier specifier, Dictionary<string, SpecifierParameterModel[]> items)
			=> items.ToDictionary(i => i.Key
				, i => i.Value.Length == 1
					? (specifier.data.GetValueOrDefault(i.Value[0].name, i.Value[0].DefaultValue), i.Value[0].Type)
					: (i.Value.Select(i => i.name).FirstOrDefault(i => specifier.data.ContainsKey(i), i.Value[0].name), typeof(string))
				);
	}
}
