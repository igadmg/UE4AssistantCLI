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

	public class SpecifierTypeDescriptor : ICustomTypeDescriptor
	{
		public List<SpecifierParameterModel> model_ = null;

		protected AttributeCollection attributes_;
		protected PropertyDescriptorCollection properties_;

		List<Attribute> CreateAttributes(SpecifierParameterModel parameter, params Attribute[] attributes)
		{
			List<Attribute> propertyAttributes = new List<Attribute>();
			propertyAttributes.Add(new BrowsableAttribute(true));
			propertyAttributes.Add(new CategoryAttribute(parameter.category));
			propertyAttributes.AddRange(attributes);

			return propertyAttributes;
		}

		public SpecifierTypeDescriptor(List<SpecifierParameterModel> model)
		{
			model_ = model;

			var attributes = new List<Attribute>();
			var categories = new HashSet<string>();
			var properties = new List<BasePropertyDescriptor>();
			var groups = new Dictionary<string, List<SpecifierParameterModel>>();
			foreach (SpecifierParameterModel pm in model_)
			{
				var parameter = pm.FixCategory("Common");

				if (!parameter.group.IsNullOrWhiteSpace())
				{
					groups.GetOrAdd(parameter.group, k => new List<SpecifierParameterModel>()).Add(parameter);
				}
				else
				{
					List<Attribute> propertyAttributes = CreateAttributes(parameter);
					if (parameter.type.IsNullOrWhiteSpace() || parameter.type == "bool")
						propertyAttributes.Add(new EditorAttribute(typeof(CheckboxBoolEditor), typeof(UITypeEditor)));
					categories.Add(parameter.category);

					properties.Add(parameter.type switch {
						"string" => new BasePropertyDescriptor<string>(GetType(), propertyAttributes, parameter.name),
						"bool" => new BasePropertyDescriptor<bool>(GetType(), propertyAttributes, parameter.name),
						"integer" => new BasePropertyDescriptor<int>(GetType(), propertyAttributes, parameter.name),
						_ => new FlagPropertyDescriptor(GetType(), propertyAttributes, parameter.name),
					});
				}
			}

			foreach (var group in groups)
			{
				var parameter = group.Value[0];

				List<Attribute> propertyAttributes = CreateAttributes(parameter, new TypeConverterAttribute(typeof(GroupPropertyEditor)));
				categories.Add(parameter.category);

				properties.Add(new GroupPropertyDescriptor(GetType(), group.Key, propertyAttributes, group.Value));
			}


#if false
			var categoriesModel = SpecifierSchema.ReadAvaliableCategories();
			foreach (var category in categories)
			{
				if (categoriesModel.TryGetValue(category, out int order))
				{
					//attributes.Add(new CategoryOrderAttribute(category, order));
				}
			}
#endif

			attributes_ = new AttributeCollection(attributes.ToArray());
			properties_ = new PropertyDescriptorCollection(properties.ToArray());
		}

		public AttributeCollection GetAttributes() => attributes_;
		public string GetClassName() => GetType().Name;
		public string GetComponentName() => null;
		public TypeConverter GetConverter() => null;
		public EventDescriptor GetDefaultEvent() => null;
		public PropertyDescriptor GetDefaultProperty() => null;
		public object GetEditor(Type editorBaseType) => null;
		public EventDescriptorCollection GetEvents() => null;
		public EventDescriptorCollection GetEvents(Attribute[]? attributes) => null;

		public PropertyDescriptorCollection GetProperties() => properties_;

		public PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
			=> new PropertyDescriptorCollection(
				GetProperties().Cast<BasePropertyDescriptor>()
					.Where(p => p.Attributes.Cast<Attribute>().Any(v => attributes.Contains(v)))
					.ToArray());

		public object? GetPropertyOwner(PropertyDescriptor? pd) => this;

		public void FromDictionary(Dictionary<string, object> values)
		{
			foreach (BasePropertyDescriptor pd in properties_)
			{
				pd.FromDictionary(values);
			}
		}

		public Dictionary<string, object> ToDictionary(Dictionary<string, object> values)
		{
			foreach (BasePropertyDescriptor pd in properties_)
			{
				pd.ToDictionary(values);
			}

			return values;
		}
	}

	internal abstract class BasePropertyDescriptor : PropertyDescriptor
	{
		Type componentType_;
		Type propertyType_;

		public BasePropertyDescriptor(Type componentType, string name, Type propertyType, params Attribute[] attributes)
			: base(name, attributes)
		{
			componentType_ = componentType;
			propertyType_ = propertyType;
		}

		public override Type ComponentType { get { return componentType_; } }
		public override bool IsReadOnly { get { return false; } }
		public override Type PropertyType { get { return propertyType_; } }

		public override bool ShouldSerializeValue(object component) { return false; }

		public virtual void FromDictionary(Dictionary<string, object> values) {}
		public virtual Dictionary<string, object> ToDictionary(Dictionary<string, object> values) => values;
	}

	internal class BasePropertyDescriptor<T> : BasePropertyDescriptor
	{
		protected T value_;
		protected T defaultValue_ = default;

		public BasePropertyDescriptor(Type componentType, List<Attribute> attributes, string name)
			: base(componentType, name, typeof(T), attributes.ToArray())
		{
		}

		public BasePropertyDescriptor<T> SetDefaultValue(T value)
		{
			value_ = value;
			defaultValue_ = value;

			return this;
		}

		public override bool CanResetValue(object component) { return true; }

		public override void ResetValue(object component)
		{
			value_ = defaultValue_;
		}

		public override object GetValue(object component)
		{
			return value_;
		}

		public override void SetValue(object component, object value)
		{
			value_ = (T)value;
		}

		public override void FromDictionary(Dictionary<string, object> values)
		{
			if (values.TryGetValue(Name, out object v))
			{
				value_ = (T)v;
			}
		}

		public override Dictionary<string, object> ToDictionary(Dictionary<string, object> values)
		{
			if (!Equals(value_, defaultValue_))
			{
				values[Name] = value_;
			}

			return values;
		}
	}

	internal class GroupPropertyDescriptor : BasePropertyDescriptor<string>
	{
		public List<SpecifierParameterModel> values_;

		public GroupPropertyDescriptor(Type componentType, string name, List<Attribute> attributes, List<SpecifierParameterModel> values)
			: base(componentType, attributes, name)
		{
			values_ = values;
		}

		public override void FromDictionary(Dictionary<string, object> values)
			=> values_.FirstOrDefault(v => values.ContainsKey(v.name), values_[0])
				.Also(v => value_ = v.name);

		public override Dictionary<string, object> ToDictionary(Dictionary<string, object> values)
		{
			if (Equals(value_, defaultValue_))
			{
				values[Name] = value_;
			}

			return values;
		}
	}

	internal class FlagPropertyDescriptor : BasePropertyDescriptor<bool>
	{
		public FlagPropertyDescriptor(Type componentType, List<Attribute> attributes, string name)
			: base(componentType, attributes, name)
		{
		}

		public override void FromDictionary(Dictionary<string, object> values)
		{
			value_ = values.ContainsKey(Name);
		}

		public override Dictionary<string, object> ToDictionary(Dictionary<string, object> values)
		{
			if (value_)
				values[Name] = true;

			return values;
		}
	}



	public class GroupPropertyEditor : StringConverter
	{
		public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)
		{
			return true;
		}

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
		{
			var gtd = (GroupPropertyDescriptor)context.PropertyDescriptor;
			return new StandardValuesCollection(gtd.values_.Select(v => v.name).ToList());
		}
	}
}
