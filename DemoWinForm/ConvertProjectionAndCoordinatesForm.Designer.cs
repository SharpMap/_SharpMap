namespace DemoWinForm
{
	partial class ConvertProjectionAndCoordinatesForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
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
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.MainSplitContainer = new System.Windows.Forms.SplitContainer();
			this.ConvertFromHeader = new System.Windows.Forms.Label();
			this.ConvertToHeader = new System.Windows.Forms.Label();
			this.ConvertToList = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.ConvertFromDatum = new System.Windows.Forms.GroupBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.MainSplitContainer.Panel1.SuspendLayout();
			this.MainSplitContainer.Panel2.SuspendLayout();
			this.MainSplitContainer.SuspendLayout();
			this.SuspendLayout();
			// 
			// MainSplitContainer
			// 
			this.MainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainSplitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.MainSplitContainer.IsSplitterFixed = true;
			this.MainSplitContainer.Location = new System.Drawing.Point(0, 0);
			this.MainSplitContainer.Name = "MainSplitContainer";
			// 
			// MainSplitContainer.Panel1
			// 
			this.MainSplitContainer.Panel1.Controls.Add(this.ConvertFromDatum);
			this.MainSplitContainer.Panel1.Controls.Add(this.label1);
			this.MainSplitContainer.Panel1.Controls.Add(this.ConvertFromHeader);
			// 
			// MainSplitContainer.Panel2
			// 
			this.MainSplitContainer.Panel2.Controls.Add(this.groupBox1);
			this.MainSplitContainer.Panel2.Controls.Add(this.ConvertToList);
			this.MainSplitContainer.Panel2.Controls.Add(this.ConvertToHeader);
			this.MainSplitContainer.Size = new System.Drawing.Size(500, 440);
			this.MainSplitContainer.SplitterDistance = 246;
			this.MainSplitContainer.SplitterWidth = 8;
			this.MainSplitContainer.TabIndex = 0;
			// 
			// ConvertFromHeader
			// 
			this.ConvertFromHeader.BackColor = System.Drawing.SystemColors.ControlDark;
			this.ConvertFromHeader.Dock = System.Windows.Forms.DockStyle.Top;
			this.ConvertFromHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ConvertFromHeader.Location = new System.Drawing.Point(0, 0);
			this.ConvertFromHeader.Name = "ConvertFromHeader";
			this.ConvertFromHeader.Size = new System.Drawing.Size(246, 23);
			this.ConvertFromHeader.TabIndex = 0;
			this.ConvertFromHeader.Text = "Convert From";
			this.ConvertFromHeader.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ConvertToHeader
			// 
			this.ConvertToHeader.BackColor = System.Drawing.SystemColors.ControlDark;
			this.ConvertToHeader.Dock = System.Windows.Forms.DockStyle.Top;
			this.ConvertToHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ConvertToHeader.Location = new System.Drawing.Point(0, 0);
			this.ConvertToHeader.Name = "ConvertToHeader";
			this.ConvertToHeader.Size = new System.Drawing.Size(246, 23);
			this.ConvertToHeader.TabIndex = 1;
			this.ConvertToHeader.Text = "Convert To";
			this.ConvertToHeader.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ConvertToList
			// 
			this.ConvertToList.Dock = System.Windows.Forms.DockStyle.Top;
			this.ConvertToList.FormattingEnabled = true;
			this.ConvertToList.Location = new System.Drawing.Point(0, 23);
			this.ConvertToList.Name = "ConvertToList";
			this.ConvertToList.Size = new System.Drawing.Size(246, 21);
			this.ConvertToList.TabIndex = 2;
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.SystemColors.Window;
			this.label1.Dock = System.Windows.Forms.DockStyle.Top;
			this.label1.Location = new System.Drawing.Point(0, 23);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(246, 21);
			this.label1.TabIndex = 1;
			// 
			// ConvertFromDatum
			// 
			this.ConvertFromDatum.Dock = System.Windows.Forms.DockStyle.Top;
			this.ConvertFromDatum.Location = new System.Drawing.Point(0, 44);
			this.ConvertFromDatum.Name = "ConvertFromDatum";
			this.ConvertFromDatum.Size = new System.Drawing.Size(246, 100);
			this.ConvertFromDatum.TabIndex = 2;
			this.ConvertFromDatum.TabStop = false;
			this.ConvertFromDatum.Text = "Datum";
			// 
			// groupBox1
			// 
			this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
			this.groupBox1.Location = new System.Drawing.Point(0, 44);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(246, 100);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Datum";
			// 
			// ConvertProjectionAndCoordinatesForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(500, 440);
			this.Controls.Add(this.MainSplitContainer);
			this.Name = "ConvertProjectionAndCoordinatesForm";
			this.Text = "ConvertProjectionAndCoordinatesForm";
			this.MainSplitContainer.Panel1.ResumeLayout(false);
			this.MainSplitContainer.Panel2.ResumeLayout(false);
			this.MainSplitContainer.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.SplitContainer MainSplitContainer;
		private System.Windows.Forms.Label ConvertFromHeader;
		private System.Windows.Forms.Label ConvertToHeader;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox ConvertToList;
		private System.Windows.Forms.GroupBox ConvertFromDatum;
		private System.Windows.Forms.GroupBox groupBox1;

	}
}