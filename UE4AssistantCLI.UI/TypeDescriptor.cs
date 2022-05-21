using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UE4Assistant.Specifier;

namespace UE4AssistantCLI.UI
{
		public class SpecifierCustomTypeDescriptor : ICustomTypeDescriptor
		{
			public List<SpecifierParameterModel> model_ = null;
			protected AttributeCollection attributes_;
			protected PropertyDescriptorCollection properties_;

			public SpecifierCustomTypeDescriptor(List<SpecifierParameterModel> model)
			{
				model_ = model;

				var attributes = new List<Attribute>();
				var categories = new HashSet<string>();
				var properties = new List<BasePropertyDescriptor>();
				var groups = new Dictionary<string, List<SpecifierParameterModel>>();
				foreach (SpecifierParameterModel parameter in model_)
				{
					if (!string.IsNullOrWhiteSpace(parameter.group))
					{
						List<SpecifierParameterModel> group;
						if (!groups.TryGetValue(parameter.group, out group))
						{
							group = new List<SpecifierParameterModel>();
							groups.Add(parameter.group, group);
						}
						group.Add(parameter);
					}
					else
					{
						List<Attribute> propertyAttributes = new List<Attribute>();
						propertyAttributes.Add(new CategoryAttribute(!string.IsNullOrWhiteSpace(parameter.category) ? parameter.category : "Common"));
						categories.Add(!string.IsNullOrWhiteSpace(parameter.category) ? parameter.category : "Common");

						if (string.IsNullOrWhiteSpace(parameter.type))
						{
							properties.Add(
								new FlagPropertyDescriptor(GetType(), parameter.name, propertyAttributes)
								.SetDefaultValue(false));
						}
						else if (parameter.type.ToLower().Equals("string"))
						{
							properties.Add(
								new StringPropertyDescriptor(GetType(), parameter.name, propertyAttributes)
								.SetDefaultValue(""));
						}
						else if (parameter.type.ToLower().Equals("bool"))
						{
							properties.Add(
								new BoolPropertyDescriptor(GetType(), parameter.name, propertyAttributes)
								.SetDefaultValue(false));
						}
						else if (parameter.type.ToLower().Equals("integer"))
						{
							properties.Add(
								new IntegerPropertyDescriptor(GetType(), parameter.name, propertyAttributes)
								.SetDefaultValue(null));
						}
					}
				}

				foreach (var group in groups)
				{
					var parameter = group.Value[0];

					List<Attribute> propertyAttributes = new List<Attribute>();
					propertyAttributes.Add(new CategoryAttribute(!string.IsNullOrWhiteSpace(parameter.category) ? parameter.category : "Common"));
					propertyAttributes.Add(new EditorAttribute(typeof(GroupPropertyEditor), typeof(GroupPropertyEditor)));
					categories.Add(!string.IsNullOrWhiteSpace(parameter.category) ? parameter.category : "Common");

					properties.Add(new GroupPropertyDescriptor(GetType(), group.Key, propertyAttributes).SetDefaultValue(parameter, group.Value));
				}

				var categoriesModel = SpecifierSchema.ReadAvaliableCategories();
				foreach (var category in categories)
				{
					if (categoriesModel.TryGetValue(category, out int order))
					{
						attributes.Add(new CategoryOrderAttribute(category, order));
					}
				}

				attributes_ = new AttributeCollection(attributes.ToArray());
				properties_ = new PropertyDescriptorCollection(properties.ToArray());
			}

			public AttributeCollection GetAttributes()
			{
				return attributes_;
			}

			public string GetClassName()
			{
				return GetType().Name;
			}

			public string GetComponentName()
			{
				throw new NotImplementedException();
			}

			public TypeConverter GetConverter()
			{
				return null;
			}

			public EventDescriptor GetDefaultEvent()
			{
				throw new NotImplementedException();
			}

			public PropertyDescriptor GetDefaultProperty()
			{
				throw new NotImplementedException();
			}

			public object GetEditor(Type editorBaseType)
			{
				throw new NotImplementedException();
			}

			public EventDescriptorCollection GetEvents()
			{
				throw new NotImplementedException();
			}

			public EventDescriptorCollection GetEvents(Attribute[] attributes)
			{
				throw new NotImplementedException();
			}

			public PropertyDescriptorCollection GetProperties()
			{
				return properties_;
			}

			public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
			{
				throw new NotImplementedException();
			}

			public object GetPropertyOwner(PropertyDescriptor pd)
			{
				return this;
			}

			public void FromDictionary(Dictionary<string, object> values)
			{
				foreach (BasePropertyDescriptor pd in properties_)
				{
					pd.FromDictionary(values);
				}
			}

			public Dictionary<string, object> ToDictionary()
			{
				Dictionary<string, object> values = new Dictionary<string, object>();

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

			public virtual void FromDictionary(Dictionary<string, object> values)
			{
			}

			public virtual Dictionary<string, object> ToDictionary(Dictionary<string, object> values)
			{
				return values;
			}
		}

		internal class FlagPropertyDescriptor : BasePropertyDescriptor
		{
			protected bool isSet_;
			protected bool defaultIsSet_;

			public FlagPropertyDescriptor(Type componentType, string name, List<Attribute> attributes)
				: base(componentType, name, typeof(bool), attributes.ToArray())
			{
			}

			public FlagPropertyDescriptor SetDefaultValue(bool isSet)
			{
				defaultIsSet_ = isSet;
				isSet_ = isSet;

				return this;
			}

			public override bool CanResetValue(object component) { return true; }

			public override void ResetValue(object component)
			{
				isSet_ = defaultIsSet_;
			}

			public override object GetValue(object component)
			{
				return isSet_;
			}

			public override void SetValue(object component, object value)
			{
				isSet_ = (bool)value;
			}

			public override void FromDictionary(Dictionary<string, object> values)
			{
				isSet_ = values.ContainsKey(Name);
			}

			public override Dictionary<string, object> ToDictionary(Dictionary<string, object> values)
			{
				if (isSet_)
				{
					values.Add(Name, null);
				}

				return values;
			}
		}

		internal class StringPropertyDescriptor : BasePropertyDescriptor
		{
			protected string value_;
			protected string defaultValue_;

			public StringPropertyDescriptor(Type componentType, string name, List<Attribute> attributes)
				: base(componentType, name, typeof(string), attributes.ToArray())
			{
			}

			public StringPropertyDescriptor SetDefaultValue(string value)
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
				value_ = (string)value;
			}

			public override void FromDictionary(Dictionary<string, object> values)
			{
				if (values.TryGetValue(Name, out object v))
				{
					value_ = v as string;
				}
			}

			public override Dictionary<string, object> ToDictionary(Dictionary<string, object> values)
			{
				if (!string.IsNullOrWhiteSpace(value_))
				{
					values.Add(Name, value_);
				}

				return values;
			}
		}

		internal class BoolPropertyDescriptor : BasePropertyDescriptor
		{
			protected bool value_;
			protected bool defaultValue_;

			public BoolPropertyDescriptor(Type componentType, string name, List<Attribute> attributes)
				: base(componentType, name, typeof(bool), attributes.ToArray())
			{
			}

			public BoolPropertyDescriptor SetDefaultValue(bool value)
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
				value_ = (bool)value;
			}

			public override void FromDictionary(Dictionary<string, object> values)
			{
				if (values.TryGetValue(Name, out object v))
				{
					value_ = (bool)v;
				}
			}

			public override Dictionary<string, object> ToDictionary(Dictionary<string, object> values)
			{
				if (value_ != defaultValue_)
				{
					values.Add(Name, value_);
				}

				return values;
			}
		}

		internal class IntegerPropertyDescriptor : BasePropertyDescriptor
		{
			protected int? value_;
			protected int? defaultValue_;

			public IntegerPropertyDescriptor(Type componentType, string name, List<Attribute> attributes)
				: base(componentType, name, typeof(int?), attributes.ToArray())
			{
			}

			public IntegerPropertyDescriptor SetDefaultValue(int? value)
			{
				value_ = null;
				defaultValue_ = value;

				return this;
			}

			public override bool CanResetValue(object component) { return true; }

			public override void ResetValue(object component)
			{
				value_ = null;
			}

			public override object GetValue(object component)
			{
				return value_;
			}

			public override void SetValue(object component, object value)
			{
				value_ = (int?)value;
			}

			public override void FromDictionary(Dictionary<string, object> values)
			{
				if (values.TryGetValue(Name, out object v))
				{
					value_ = (int)v;
				}
			}

			public override Dictionary<string, object> ToDictionary(Dictionary<string, object> values)
			{
				if (value_ != defaultValue_)
				{
					values.Add(Name, value_);
				}

				return values;
			}
		}

		internal class GroupPropertyDescriptor : BasePropertyDescriptor
		{
			protected SpecifierParameterModel value_;
			protected SpecifierParameterModel defaultvalue_;
			public List<SpecifierParameterModel> values_;

			public GroupPropertyDescriptor(Type componentType, string name, List<Attribute> attributes)
				: base(componentType, name, typeof(string), attributes.ToArray())
			{
			}

			public GroupPropertyDescriptor SetDefaultValue(SpecifierParameterModel value, List<SpecifierParameterModel> values)
			{
				defaultvalue_ = value;
				value_ = value;
				values_ = values;

				return this;
			}

			public override bool CanResetValue(object component) { return true; }

			public override void ResetValue(object component)
			{
				value_ = defaultvalue_;
			}

			public override object GetValue(object component)
			{
				return value_.name;
			}

			public override void SetValue(object component, object value)
			{
				value_ = values_.Find(i => i.name == (string)value);
			}

			public override void FromDictionary(Dictionary<string, object> values)
			{
				foreach (var v in values_)
				{
					if (values.TryGetValue(v.name, out object o))
					{
						value_ = v;
						break;
					}
				}
			}

			public override Dictionary<string, object> ToDictionary(Dictionary<string, object> values)
			{
				if (value_.type != "empty")
				{
					values.Add(value_.name, value_.type == "bool" ? (object)true : null);
				}

				return values;
			}
		}

		public class GroupPropertyEditor : TypeEditor<System.Windows.Controls.ComboBox>
		{
			List<SpecifierParameterModel> values_;

			public GroupPropertyEditor()
			{
			}

			protected override void SetValueDependencyProperty()
			{
				ValueProperty = System.Windows.Controls.ComboBox.SelectedValueProperty;
			}

			protected override System.Windows.Controls.ComboBox CreateEditor()
			{
				return new PropertyGridEditorComboBox();
			}

			protected override void ResolveValueBinding(PropertyItem propertyItem)
			{
				if (propertyItem.PropertyDescriptor is GroupPropertyDescriptor pd)
				{
					Editor.ItemsSource = pd.values_.Select(v => v.name);
				}

				base.ResolveValueBinding(propertyItem);
			}

			protected override void SetControlProperties(PropertyItem propertyItem)
			{
				/*
				Editor.DisplayMemberPath = "DisplayName";
				Editor.SelectedValuePath = "Value";
				if (propertyItem != null)
				{
					Editor.IsEnabled = !propertyItem.IsReadOnly;
				}
				*/
			}

			private void SetItemsSource()
			{
				Editor.ItemsSource = values_;
			}
		}
}
