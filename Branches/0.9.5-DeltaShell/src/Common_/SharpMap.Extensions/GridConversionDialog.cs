using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace SharpMap.Extensions
{
    public partial class GridConversionDialog : Form
    {
        public GridConversionDialog()
        {
            InitializeComponent();

            this.convertTabPage.Text = "Convert";
            this.resampleTabPage.Text = "Resample";

            this.attributeSelectionGroupBox.Text = "Destination attributes";
            this.inputFileButton.Text = "Browse ..";
            this.outputFileButton.Text = "Browse ..";
            this.inputFileLabel.Text = "File name";
            this.outputFileLabel.Text = "Destination path";
            this.dataTypeComboBox.DataSource = new BindingList<string>
                                            {
                                                "Byte (8 bits)",
                                                "Integer (16 bits)",
                                                "Single (32 bits)",
                                                "Double(64 bits)"
                                            };
            this.dataTypeLabel.Text = "Data type";
            this.blockWidthLabel.Text = "Block width";
            this.validCellsLabel.Text = "Nr valid cells";
            this.generateStatisticsLabel.Text = "Generate statistics";
            this.optionLabel.Text = "Option";
            generateStatisticsCheckBox.Text = "";
            startLabel.Text = "Start";
            endLabel.Text = "End";
            columnLabel.Text = "Column";
            rowLabel.Text = "Row";


            this.dataTypeComboBox1.DataSource = new BindingList<string>
                                            {
                                                "Unknown",
                                                "Byte (8 bits)",
                                                "Integer (16 bits)",
                                                "Single (32 bits)",
                                                "Double(64 bits)"
                                            };

            resampleMethodComboBox.DataSource = new BindingList<string>
                                                    {
                                                        "Unknown",
                                                        "Triangulation",
                                                        "Hab area-area "

                                                    };
    
            
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void RowsLabel_Click(object sender, EventArgs e)
        {

        }

        private void ColumnsLabel_Click(object sender, EventArgs e)
        {

        }
    }
}
