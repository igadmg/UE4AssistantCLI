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
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
			this.tabControlPages.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.SuspendLayout();
			// 
			// comboBox1
			// 
			this.comboBoxMacro.FormattingEnabled = true;
			this.comboBoxMacro.Location = new System.Drawing.Point(125, 11);
			this.comboBoxMacro.Name = "comboBox1";
			this.comboBoxMacro.Size = new System.Drawing.Size(322, 23);
			this.comboBoxMacro.TabIndex = 0;
			this.comboBoxMacro.SelectedValueChanged += new System.EventHandler(this.comboBoxMacro_SelectedValueChanged);
			// 
			// buttonApply
			// 
			this.buttonApply.Location = new System.Drawing.Point(453, 11);
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
			// tabControl1
			// 
			this.tabControlPages.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
			this.tabControlPages.Controls.Add(this.tabPage1);
			this.tabControlPages.Controls.Add(this.tabPage2);
			this.tabControlPages.Location = new System.Drawing.Point(12, 40);
			this.tabControlPages.Name = "tabControl1";
			this.tabControlPages.SelectedIndex = 0;
			this.tabControlPages.Size = new System.Drawing.Size(516, 398);
			this.tabControlPages.TabIndex = 3;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.propertyGrid1);
			this.tabPage1.Location = new System.Drawing.Point(4, 27);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(508, 367);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "tabPage1";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// tabPage2
			// 
			this.tabPage2.Location = new System.Drawing.Point(4, 27);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(373, 232);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "tabPage2";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// propertyGrid1
			// 
			this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyGrid1.Location = new System.Drawing.Point(3, 3);
			this.propertyGrid1.Name = "propertyGrid1";
			this.propertyGrid1.Size = new System.Drawing.Size(502, 361);
			this.propertyGrid1.TabIndex = 0;
			// 
			// FormMacroEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(540, 450);
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
	private PropertyGrid propertyGrid1;
}
