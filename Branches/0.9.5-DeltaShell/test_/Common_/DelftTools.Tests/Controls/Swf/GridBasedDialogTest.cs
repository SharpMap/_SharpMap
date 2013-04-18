using System;
using System.Linq;
using System.Reflection;
using DelftTools.Controls.Swf;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.Controls.Swf
{
    [TestFixture]
    public class GridBasedDialogTest
    {
        private PropertyInfo[] properties;

        [Test, Category("Windows.Forms")]
        public void ShowDialogWithStrings()
        {
            var dialog = new GridBasedDialog();

            dialog.MasterSelectionChanged += dialog_SelectionChanged;

            Type type = dialog.GetType();
            properties = type.GetProperties();
            dialog.MasterDataSource = properties;
            WindowsFormsTestHelper.ShowModal(dialog);
        }

        void dialog_SelectionChanged(object sender, EventArgs e)
        {
            GridBasedDialog gridBasedDialog = (GridBasedDialog)sender;

            if (1 == gridBasedDialog.MasterSelectedIndices.Count)
            {
                PropertyInfo propertyInfo = properties[gridBasedDialog.MasterSelectedIndices[0]];
                Type type = propertyInfo.GetType();
                PropertyInfo[] propertyInfoproperties = type.GetProperties();
                //, Value = propertyInfo.GetValue(pi, null).ToString()
                var ds = propertyInfoproperties.Select(pi => new { pi.Name }).ToArray();

                //Type type = propertyInfo.GetType();
                //PropertyInfo[] propertyInfoproperties = 
                //coverages.Select(c => new { c.Owner, Coverage = c }).ToArray();
                gridBasedDialog.SlaveDataSource = ds;
            }
        }
    }
}