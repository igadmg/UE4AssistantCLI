using DynamicDescriptors;
using System.ComponentModel;
using System.Drawing.Design;
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

#if false
	public class SpeсifierTypeDescriptor : DictionaryTypeDescriptor
	{
		private const string DefaultSectionName = "parameters";

		public SpeсifierTypeDescriptor(IDictionary<string, object> data) : base(data) { }
		public SpeсifierTypeDescriptor(IDictionary<string, (object, Type)> data) : base(data) { }

		public SpeсifierTypeDescriptor(Specifier specifier)
			: base(specifier.Let(ToTypeDescriptionDictionary))
		{
		}

		private static Dictionary<string, (object, Type, Attribute[])> ToTypeDescriptionDictionary(Specifier specifier)
			=> specifier.GroupProperties(DefaultSectionName).ToDictionary(i => i.Key
				, i => i.Count() == 1
					? (specifier.data.GetValueOrDefault(i.First().name, i.First().DefaultValue), i.First().Type, GetAttributes(i.First()))
					: (i.Select(i => i.name).FirstOrDefault(i => specifier.data.ContainsKey(i), i.First().name), typeof(string), GetAttributes(i.First()))
				);

		private static Attribute[] GetAttributes(SpecifierParameterModel i)
		{
			var result = new List<Attribute>();
			result.Add(new CategoryAttribute(i.category));

			if (i.group.IsNullOrWhiteSpace())
			{
				if (i.Type == typeof(bool))
					result.Add(new EditorAttribute(typeof(CheckboxBoolEditor), typeof(UITypeEditor)));
			}
			else
			{
				// ERROR NOT DOABLE unreasonable effort.
				//result.Add(new TypeConverterAttribute(i.type));
				//dp.SetConverter(new StandardValuesStringConverter(item.Value.Select(i => i.name)));
			}

			return result.ToArray();
		}
	}
#endif

	public class SpecializerTypeDescriptor : DynamicTypeDescriptor
	{
		public Specifier specifier;

		public SpecializerTypeDescriptor(Specifier specifier)
			: base(new DictionaryTypeDescriptor(ToTypeDescriptionDictionary(specifier)))
		{
			this.specifier = specifier;

			foreach (var item in specifier.GroupProperties(""))
			{
				var dp = GetDynamicProperty(item.Key);
				DecorateProperty(dp, item);
			}
		}

		public PropertyDescriptorCollection GetProperties(string name)
		{
			var items = specifier.GroupProperties(name).ToDictionary(g => g.Key, g => g);
			var dso_items = ToTypeDescriptionDictionary(specifier, name);

			return new PropertyDescriptorCollection(dso_items.Select(pair => {
				var dp = new DynamicPropertyDescriptor(
					new DictionaryPropertyDescriptor(specifier.GetData(name), pair.Key, pair.Value.Item2));
				dp.SetValue(this, pair.Value.Item1);
				DecorateProperty(dp, items[dp.Name]);
				return dp;
			}).ToArray(), true);
		}

		private static Dictionary<string, (object, Type)> ToTypeDescriptionDictionary(Specifier specifier, string name = "")
			=> specifier.GroupProperties(name).ToDictionary(i => i.Key
				, i => i.Count() == 1
					? (specifier.GetData(name).GetValueOrDefault(i.First().name, i.First().DefaultValue), i.First().Type)
					: (i.Select(i => i.name).FirstOrDefault(i => specifier.GetData(name).ContainsKey(i), i.First().name), typeof(string))
				);

		private static void DecorateProperty(DynamicPropertyDescriptor dp, IEnumerable<SpecifierParameterModel> items)
		{
			var ri = items.First();
			dp.SetCategory(ri.category);

			if (ri.group.IsNullOrWhiteSpace())
			{
				if (ri.Type == typeof(bool))
					dp.SetEditor(typeof(UITypeEditor), new CheckboxBoolEditor());
			}
			else
			{
				dp.SetConverter(new StandardValuesStringConverter(items.Select(i => i.name)));
			}
		}
	}
}
