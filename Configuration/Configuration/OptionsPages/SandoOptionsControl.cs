using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Sando.DependencyInjection;
using Sando.ExtensionContracts.IndexerContracts;

namespace Configuration.OptionsPages
{
	public class SandoOptionsControl : System.Windows.Forms.UserControl
	{
        private static readonly List<string> UnremovableFileExtensionsList = 
            new List<string> { ".cs", ".h", ".cpp", ".cxx", ".c" };
        public static readonly List<string> DefaultFileExtensionsList =
            new List<string> { ".xaml", ".xml", ".txt", ".text", ".lic", ".js", 
                               ".java", ".rb", ".py", ".vb", ".vbs", ".asp", ".aspx", 
                               ".asax", ".config", ".html", ".htm", ".css", ".fs", 
                               ".tcl", ".pl", ".pm", ".ps1", ".cls", ".frm", ".clj", 
                               ".go", ".resx", ".m" }.Concat(
                                UnremovableFileExtensionsList).ToList(); 

		#region Fields

		private SandoDialogPage customOptionsPage;
        private FolderBrowserDialog ExtensionPointsPluginDirectoryPathFolderBrowserDialog;
        //private Button ExtensionPointsPluginDirectoryPathButton;
		private GroupBox SearchResultsConfigurationGroupBox;
		private TextBox SearchResultsConfigurationNumberOfResultsReturnedTextBox;
		private Label NumberOfResultsReturnedLabel;
		private GroupBox ToggleLogCollectionGroupBox;
		private Label AllowCollectionLabel;
		private CheckBox AllowCollectionCheckBox;
        private GroupBox FileExtensionsGroupBox;
        private Button DeleteFileExtensionButton;
        private Button NewFileExtensionButton;
        private ListBox FileExtensionsListBox;
        private Button buttonDefault;

		/// <summary>  
		/// Required designer variable. 
		/// </summary> 
		private System.ComponentModel.Container components = null;

		#endregion

		#region Constructors

		public SandoOptionsControl()
		{
			// This call is required by the Windows.Forms Form Designer. 
			InitializeComponent();
		}

		#endregion

		#region Methods

		#region IDisposable implementation
		/// <summary>  
		/// Clean up any resources being used. 
		/// </summary> 
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(ExtensionPointsPluginDirectoryPathFolderBrowserDialog != null)
				{
					ExtensionPointsPluginDirectoryPathFolderBrowserDialog.Dispose();
					ExtensionPointsPluginDirectoryPathFolderBrowserDialog = null;
				}
				if(components != null)
				{
					components.Dispose();
				}
				GC.SuppressFinalize(this);
			}
			base.Dispose(disposing);
		}
		#endregion

		#region Component Designer generated code
		/// <summary>  
		/// Required method for Designer support - do not modify  
		/// the contents of this method with the code editor. 
		/// </summary> 
		private void InitializeComponent()
		{
            this.ExtensionPointsPluginDirectoryPathFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.SearchResultsConfigurationGroupBox = new System.Windows.Forms.GroupBox();
            this.SearchResultsConfigurationNumberOfResultsReturnedTextBox = new System.Windows.Forms.TextBox();
            this.NumberOfResultsReturnedLabel = new System.Windows.Forms.Label();
            this.ToggleLogCollectionGroupBox = new System.Windows.Forms.GroupBox();
            this.AllowCollectionCheckBox = new System.Windows.Forms.CheckBox();
            this.AllowCollectionLabel = new System.Windows.Forms.Label();
            this.FileExtensionsGroupBox = new System.Windows.Forms.GroupBox();
            this.buttonDefault = new System.Windows.Forms.Button();
            this.DeleteFileExtensionButton = new System.Windows.Forms.Button();
            this.NewFileExtensionButton = new System.Windows.Forms.Button();
            this.FileExtensionsListBox = new System.Windows.Forms.ListBox();
            this.SearchResultsConfigurationGroupBox.SuspendLayout();
            this.ToggleLogCollectionGroupBox.SuspendLayout();
            this.FileExtensionsGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // ExtensionPointsPluginDirectoryPathFolderBrowserDialog
            // 
            this.ExtensionPointsPluginDirectoryPathFolderBrowserDialog.ShowNewFolderButton = false;
            // 
            // SearchResultsConfigurationGroupBox
            // 
            this.SearchResultsConfigurationGroupBox.Controls.Add(this.SearchResultsConfigurationNumberOfResultsReturnedTextBox);
            this.SearchResultsConfigurationGroupBox.Controls.Add(this.NumberOfResultsReturnedLabel);
            this.SearchResultsConfigurationGroupBox.Location = new System.Drawing.Point(3, 169);
            this.SearchResultsConfigurationGroupBox.Name = "SearchResultsConfigurationGroupBox";
            this.SearchResultsConfigurationGroupBox.Size = new System.Drawing.Size(445, 60);
            this.SearchResultsConfigurationGroupBox.TabIndex = 7;
            this.SearchResultsConfigurationGroupBox.TabStop = false;
            this.SearchResultsConfigurationGroupBox.Text = "Search results configuration";
            // 
            // SearchResultsConfigurationNumberOfResultsReturnedTextBox
            // 
            this.SearchResultsConfigurationNumberOfResultsReturnedTextBox.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.SearchResultsConfigurationNumberOfResultsReturnedTextBox.Location = new System.Drawing.Point(310, 29);
            this.SearchResultsConfigurationNumberOfResultsReturnedTextBox.Name = "SearchResultsConfigurationNumberOfResultsReturnedTextBox";
            this.SearchResultsConfigurationNumberOfResultsReturnedTextBox.Size = new System.Drawing.Size(90, 20);
            this.SearchResultsConfigurationNumberOfResultsReturnedTextBox.TabIndex = 6;
            this.SearchResultsConfigurationNumberOfResultsReturnedTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.SearchResultsConfigurationNumberOfResultsReturnedTextBox.TextChanged += new System.EventHandler(this.SearchResultsConfigurationNumberOfResultsReturnedTextBox_TextChanged);
            // 
            // NumberOfResultsReturnedLabel
            // 
            this.NumberOfResultsReturnedLabel.AutoSize = true;
            this.NumberOfResultsReturnedLabel.Location = new System.Drawing.Point(10, 30);
            this.NumberOfResultsReturnedLabel.Name = "NumberOfResultsReturnedLabel";
            this.NumberOfResultsReturnedLabel.Size = new System.Drawing.Size(134, 13);
            this.NumberOfResultsReturnedLabel.TabIndex = 4;
            this.NumberOfResultsReturnedLabel.Text = "Number of results returned:";
            // 
            // ToggleLogCollectionGroupBox
            // 
            this.ToggleLogCollectionGroupBox.Controls.Add(this.AllowCollectionCheckBox);
            this.ToggleLogCollectionGroupBox.Controls.Add(this.AllowCollectionLabel);
            this.ToggleLogCollectionGroupBox.Location = new System.Drawing.Point(3, 235);
            this.ToggleLogCollectionGroupBox.Name = "ToggleLogCollectionGroupBox";
            this.ToggleLogCollectionGroupBox.Size = new System.Drawing.Size(445, 56);
            this.ToggleLogCollectionGroupBox.TabIndex = 8;
            this.ToggleLogCollectionGroupBox.TabStop = false;
            this.ToggleLogCollectionGroupBox.Text = "Logging";
            // 
            // AllowCollectionCheckBox
            // 
            this.AllowCollectionCheckBox.AutoSize = true;
            this.AllowCollectionCheckBox.Checked = true;
            this.AllowCollectionCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.AllowCollectionCheckBox.Location = new System.Drawing.Point(385, 29);
            this.AllowCollectionCheckBox.Name = "AllowCollectionCheckBox";
            this.AllowCollectionCheckBox.Size = new System.Drawing.Size(15, 14);
            this.AllowCollectionCheckBox.TabIndex = 5;
            this.AllowCollectionCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.AllowCollectionCheckBox.UseVisualStyleBackColor = true;
            this.AllowCollectionCheckBox.CheckedChanged += new System.EventHandler(this.AllowCollectionCheckBox_CheckedChanged);
            // 
            // AllowCollectionLabel
            // 
            this.AllowCollectionLabel.AutoSize = true;
            this.AllowCollectionLabel.Location = new System.Drawing.Point(10, 30);
            this.AllowCollectionLabel.Name = "AllowCollectionLabel";
            this.AllowCollectionLabel.Size = new System.Drawing.Size(206, 13);
            this.AllowCollectionLabel.TabIndex = 4;
            this.AllowCollectionLabel.Text = "Allow collection of anonymous usage logs:";
            // 
            // FileExtensionsGroupBox
            // 
            this.FileExtensionsGroupBox.Controls.Add(this.buttonDefault);
            this.FileExtensionsGroupBox.Controls.Add(this.DeleteFileExtensionButton);
            this.FileExtensionsGroupBox.Controls.Add(this.NewFileExtensionButton);
            this.FileExtensionsGroupBox.Controls.Add(this.FileExtensionsListBox);
            this.FileExtensionsGroupBox.Location = new System.Drawing.Point(3, 3);
            this.FileExtensionsGroupBox.Name = "FileExtensionsGroupBox";
            this.FileExtensionsGroupBox.Size = new System.Drawing.Size(445, 160);
            this.FileExtensionsGroupBox.TabIndex = 8;
            this.FileExtensionsGroupBox.TabStop = false;
            this.FileExtensionsGroupBox.Text = "File extensions to include in index";
            // 
            // buttonDefault
            // 
            this.buttonDefault.Location = new System.Drawing.Point(325, 77);
            this.buttonDefault.Name = "buttonDefault";
            this.buttonDefault.Size = new System.Drawing.Size(75, 23);
            this.buttonDefault.TabIndex = 3;
            this.buttonDefault.Text = "Default";
            this.buttonDefault.UseVisualStyleBackColor = true;
            this.buttonDefault.Click += new System.EventHandler(this.buttonDefault_Click);
            // 
            // DeleteFileExtensionButton
            // 
            this.DeleteFileExtensionButton.Location = new System.Drawing.Point(325, 48);
            this.DeleteFileExtensionButton.Name = "DeleteFileExtensionButton";
            this.DeleteFileExtensionButton.Size = new System.Drawing.Size(75, 23);
            this.DeleteFileExtensionButton.TabIndex = 2;
            this.DeleteFileExtensionButton.Text = "Delete";
            this.DeleteFileExtensionButton.UseVisualStyleBackColor = true;
            this.DeleteFileExtensionButton.Click += new System.EventHandler(this.DeleteFileExtensionButton_Click);
            // 
            // NewFileExtensionButton
            // 
            this.NewFileExtensionButton.Location = new System.Drawing.Point(325, 19);
            this.NewFileExtensionButton.Name = "NewFileExtensionButton";
            this.NewFileExtensionButton.Size = new System.Drawing.Size(75, 23);
            this.NewFileExtensionButton.TabIndex = 1;
            this.NewFileExtensionButton.Text = "New";
            this.NewFileExtensionButton.UseVisualStyleBackColor = true;
            this.NewFileExtensionButton.Click += new System.EventHandler(this.NewFileExtensionButton_Click);
            // 
            // FileExtensionsListBox
            // 
            this.FileExtensionsListBox.FormattingEnabled = true;
            this.FileExtensionsListBox.Location = new System.Drawing.Point(13, 19);
            this.FileExtensionsListBox.Name = "FileExtensionsListBox";
            this.FileExtensionsListBox.Size = new System.Drawing.Size(306, 134);
            this.FileExtensionsListBox.TabIndex = 0;
            // 
            // SandoOptionsControl
            // 
            this.AllowDrop = true;
            this.Controls.Add(this.FileExtensionsGroupBox);
            this.Controls.Add(this.ToggleLogCollectionGroupBox);
            this.Controls.Add(this.SearchResultsConfigurationGroupBox);
            this.Name = "SandoOptionsControl";
            this.Size = new System.Drawing.Size(465, 299);
            this.SearchResultsConfigurationGroupBox.ResumeLayout(false);
            this.SearchResultsConfigurationGroupBox.PerformLayout();
            this.ToggleLogCollectionGroupBox.ResumeLayout(false);
            this.ToggleLogCollectionGroupBox.PerformLayout();
            this.FileExtensionsGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion
		/// <summary> 
		/// Gets or Sets the reference to the underlying OptionsPage object. 
		/// </summary> 
		public SandoDialogPage OptionsPage
		{
			get
			{
				return customOptionsPage;
			}
			set
			{
				customOptionsPage = value;
			}
		}

		public string NumberOfSearchResultsReturned
		{
			get
			{
				return SearchResultsConfigurationNumberOfResultsReturnedTextBox.Text;
			}
			set
			{
				SearchResultsConfigurationNumberOfResultsReturnedTextBox.Text = value;
			}
		}

		public bool AllowDataCollectionLogging
		{
			get
			{
				return AllowCollectionCheckBox.Checked;
			}
			set
			{
				AllowCollectionCheckBox.Checked = value;
			}
		}

        public List<string> FileExtensionsList
        {
            get
            {
                return FileExtensionsListBox.DataSource as List<string>;
            }
            set
            {
                FileExtensionsListBox.DataSource = value;
            }
        }

		#endregion

		//private void ExtensionPointsPluginDirectoryPathButton_Click(object sender, EventArgs e)
		//{
            //ExtensionPointsPluginDirectoryPathFolderBrowserDialog = new FolderBrowserDialog();
            //if(ExtensionPointsPluginDirectoryPathFolderBrowserDialog != null &&
            //    DialogResult.OK == ExtensionPointsPluginDirectoryPathFolderBrowserDialog.ShowDialog())
            //{
            //    if(customOptionsPage != null)
            //    {
            //        customOptionsPage.ExtensionPointsPluginDirectoryPath = ExtensionPointsPluginDirectoryPathFolderBrowserDialog.SelectedPath;
            //    }
            //    ExtensionPointsPluginDirectoryPathValueTextBox.Text = ExtensionPointsPluginDirectoryPathFolderBrowserDialog.SelectedPath;
            //}
		//}

		private void SearchResultsConfigurationNumberOfResultsReturnedTextBox_TextChanged(object sender, EventArgs e)
		{
			int numberOfResultsReturned = 0;
			if(int.TryParse(SearchResultsConfigurationNumberOfResultsReturnedTextBox.Text, out numberOfResultsReturned))
			{
				customOptionsPage.NumberOfSearchResultsReturned = numberOfResultsReturned.ToString();
			}
			else
			{
				SearchResultsConfigurationNumberOfResultsReturnedTextBox.Text = "20";
				MessageBox.Show("You have to enter a positive number!");
			}
		}

        private void AllowCollectionCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (AllowCollectionCheckBox.Checked)
            {
                customOptionsPage.AllowDataCollectionLogging = Boolean.TrueString;
            }
            else
            {
                customOptionsPage.AllowDataCollectionLogging = Boolean.FalseString;
            }
        }

        private void NewFileExtensionButton_Click(object sender, EventArgs e)
        {
            //popup a popup
            var addExtensionForm = new AddFileExtensionForm();
            string newExtension = string.Empty;
            if (addExtensionForm.ShowDialog() == DialogResult.OK)
            {
                newExtension = addExtensionForm.textBoxNewExtension.Text;
            }
            if (addExtensionForm != null)
            {
                addExtensionForm.Dispose();
            }

            //add dot if missing
            if (!newExtension.StartsWith("."))
            {
                newExtension = "." + newExtension;
            }

            //validate entered text (should be dot followed by <3 >0 letters)
            if (newExtension.Length > 1 && newExtension.Length < 5 && newExtension.Skip(1).All(Char.IsLetter))
            {
                List<string> extensionsList;
                if (FileExtensionsList != null)
                {
                    extensionsList = new List<string>(FileExtensionsList);
                }
                else
                {
                    extensionsList = new List<string>();
                }

                if (!extensionsList.Contains(newExtension))
                {
                    extensionsList.Add(newExtension);
                    FileExtensionsList = null;
                    FileExtensionsList = extensionsList;
                    FileExtensionsListBox.Refresh();

                    customOptionsPage.FileExtensionsToIndexList = FileExtensionsList;

                    var missingFilesIncluder = ServiceLocator.Resolve<IMissingFilesIncluder>();
                    missingFilesIncluder.EnsureNoMissingFilesAndNoDeletedFiles();
                }
            }
        }

        private void DeleteFileExtensionButton_Click(object sender, EventArgs e)
        {
            var extensionsList = new List<string>(FileExtensionsList);
            foreach (var selected in FileExtensionsListBox.SelectedItems)
            {
                if (UnremovableFileExtensionsList.All(uExt => uExt != selected.ToString()))
                {
                    extensionsList.Remove(selected.ToString());
                }
                else
                {
                    MessageBox.Show("Unable the delete the required extension: " + selected.ToString());
                }
            }
            FileExtensionsList = null;
            FileExtensionsList = extensionsList;
            FileExtensionsListBox.Refresh();
            customOptionsPage.FileExtensionsToIndexList = FileExtensionsList;

            var missingFilesIncluder = ServiceLocator.Resolve<IMissingFilesIncluder>();
            missingFilesIncluder.EnsureNoMissingFilesAndNoDeletedFiles();
        }

        private void buttonDefault_Click(object sender, EventArgs e)
        {
            FileExtensionsList = null;
            FileExtensionsList = DefaultFileExtensionsList;
            FileExtensionsListBox.Refresh();
            customOptionsPage.FileExtensionsToIndexList = FileExtensionsList;

            var missingFilesIncluder = ServiceLocator.Resolve<IMissingFilesIncluder>();
            missingFilesIncluder.EnsureNoMissingFilesAndNoDeletedFiles();
        }
	} 
}
