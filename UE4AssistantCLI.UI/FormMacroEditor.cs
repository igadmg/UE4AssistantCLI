using System.Windows.Forms;

namespace UE4AssistantCLI.UI;

public partial class FormMacroEditor : Form
{
	public FormMacroEditor()
	{
		InitializeComponent();
	}

	private void comboBoxMacro_SelectedValueChanged(object sender, EventArgs e)
{
	}

    record class PropertyGridSimpleDemoClass(int Value = 0, string Song = "")
    {
        int m_DisplayInt;
        public int DisplayInt {
            get { return m_DisplayInt; }
            set { m_DisplayInt = value; }
        }

        string m_DisplayString;
        public string DisplayString {
            get { return m_DisplayString; }
            set { m_DisplayString = value; }
        }

        bool m_DisplayBool;
        public bool DisplayBool {
            get { return m_DisplayBool; }
            set { m_DisplayBool = value; }
        }

        Color m_DisplayColors;
        public Color DisplayColors {
            get { return m_DisplayColors; }
            set { m_DisplayColors = value; }
        }
    }

    private void FormMacroEditor_Load(object sender, EventArgs e)
	{
		tabControlPages.ItemSize = new Size(0, 1);
		tabControlPages.SizeMode = TabSizeMode.Fixed;

		dynamic so = new PropertyGridSimpleDemoClass();
		propertyGrid1.SelectedObject = so; 
	}
}
