using System.ComponentModel;
using System.Windows.Forms.Design;

namespace UE4AssistantCLI.UI
{
	public class MetaPropertyTab : PropertyTab
	{
		public MetaPropertyTab() {}

		/// <summary>
		/// extend everything
		/// </summary>
		public override bool CanExtend(object extendee) => true;

		/// <summary>
		/// the tab's iumage
		/// </summary>
		public override Bitmap Bitmap
			=> new Bitmap(
				Image.FromStream(
					typeof(MetaPropertyTab).Assembly.GetManifestResourceStream($"UE4AssistantCLI.UI.Resources.Members.png")
			));

		/// <summary>
		/// the tab's name
		/// </summary>
		public override string TabName => "meta";


		/// <summary>
		/// used to filter implemented interfaces in a type
		/// </summary>
		/// <returns>true if the requested interfaces are implemented</returns>
		protected static bool InterfaceFilter(Type typeObj, Object criteriaObj) => true;

		public override System.ComponentModel.PropertyDescriptorCollection GetProperties(object component) => this.GetProperties(component, null);
		public override PropertyDescriptorCollection GetProperties(object component, Attribute[] attributes)
		{
			if (component == null)
			{
				return new PropertyDescriptorCollection(null, true);
			}

			// list of the PropertyDescriptors that will be returned
			List<PropertyDescriptor> fields = new List<PropertyDescriptor>();
			return new PropertyDescriptorCollection(fields.ToArray());
		}
	}
}
