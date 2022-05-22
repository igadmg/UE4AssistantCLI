namespace UE4AssistantCLI.UI;

partial class FormMacroEditor
{
	/// <summary>
	///  Required designer variable.
	/// </summary>
	private System.ComponentModel.IContainer components = null;

	/// <summary>
	///  Clean up any resources being used.
	/// </summary>
	/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
	protected override void Dispose(bool disposing)
	{
		if (disposing && (components != null))
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	#region Windows Form Designer generated code

	/// <summary>
	///  Required method for Designer support - do not modify
	///  the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent()
	{
			this.comboBoxMacro = new System.Windows.Forms.ComboBox();
			this.buttonApply = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.tabControlPages = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.propertyGridSpecifier = new System.Windows.Forms.PropertyGrid();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.textBoxSpecifier = new System.Windows.Forms.TextBox();
			this.tabControlPages.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.SuspendLayout();
			// 
			// comboBoxMacro
			// 
			this.comboBoxMacro.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxMacro.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxMacro.FormattingEnabled = true;
			this.comboBoxMacro.Location = new System.Drawing.Point(125, 11);
			this.comboBoxMacro.Name = "comboBoxMacro";
			this.comboBoxMacro.Size = new System.Drawing.Size(215, 23);
			this.comboBoxMacro.TabIndex = 0;
			this.comboBoxMacro.SelectedIndexChanged += new System.EventHandler(this.comboBoxMacro_SelectedIndexChanged);
			// 
			// buttonApply
			// 
			this.buttonApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonApply.Location = new System.Drawing.Point(346, 11);
			this.buttonApply.Name = "buttonApply";
			this.buttonApply.Size = new System.Drawing.Size(75, 23);
			this.buttonApply.TabIndex = 1;
			this.buttonApply.Text = "Apply";
			this.buttonApply.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 15);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(75, 15);
			this.label1.TabIndex = 2;
			this.label1.Text = "Select Macro";
			// 
			// tabControlPages
			// 
			this.tabControlPages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControlPages.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
			this.tabControlPages.Controls.Add(this.tabPage1);
			this.tabControlPages.Controls.Add(this.tabPage2);
			this.tabControlPages.Location = new System.Drawing.Point(12, 69);
			this.tabControlPages.Name = "tabControlPages";
			this.tabControlPages.SelectedIndex = 0;
			this.tabControlPages.Size = new System.Drawing.Size(409, 558);
			this.tabControlPages.TabIndex = 3;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.propertyGridSpecifier);
			this.tabPage1.Location = new System.Drawing.Point(4, 27);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(401, 527);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "tabPage1";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// propertyGridSpecifier
			// 
			this.propertyGridSpecifier.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyGridSpecifier.Location = new System.Drawing.Point(3, 3);
			this.propertyGridSpecifier.Name = "propertyGridSpecifier";
			this.propertyGridSpecifier.Size = new System.Drawing.Size(395, 521);
			this.propertyGridSpecifier.TabIndex = 0;
			this.propertyGridSpecifier.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGridSpecifier_PropertyValueChanged);
			// 
			// tabPage2
			// 
			this.tabPage2.Location = new System.Drawing.Point(4, 27);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(401, 556);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "tabPage2";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// textBoxSpecifier
			// 
			this.textBoxSpecifier.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxSpecifier.Location = new System.Drawing.Point(16, 40);
			this.textBoxSpecifier.Name = "textBoxSpecifier";
			this.textBoxSpecifier.Size = new System.Drawing.Size(409, 23);
			this.textBoxSpecifier.TabIndex = 4;
			// 
			// FormMacroEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(433, 639);
			this.Controls.Add(this.textBoxSpecifier);
			this.Controls.Add(this.tabControlPages);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.buttonApply);
			this.Controls.Add(this.comboBoxMacro);
			this.Name = "FormMacroEditor";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.FormMacroEditor_Load);
			this.tabControlPages.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

	}

	#endregion

	private ComboBox comboBoxMacro;
	private Button buttonApply;
	private Label label1;
	private TabControl tabControlPages;
	private TabPage tabPage2;
	private TabPage tabPage1;
	private PropertyGrid propertyGridSpecifier;
	private TextBox textBoxSpecifier;
}
