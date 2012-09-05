using System;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;

namespace DelftTools.Tests.Controls.Swf.Table
{
    /// <summary>
    /// Helper class to populate comboboxes for xtragrid and datatable
    /// </summary>
    public static class XtraGridComboBoxHelper
    {
        public static void Populate(DevExpress.XtraEditors.ImageComboBoxEdit comboBox, Type enumType, params System.Enum[] excludeValues)
        {
            Populate(comboBox.Properties, enumType, excludeValues);
        }

        public static void Populate(RepositoryItemImageComboBox comboBox, Type enumType, params System.Enum[] excludeValues)
        {
            Populate(comboBox.Items, enumType, excludeValues);
        }

        public static void Populate(ImageComboBoxItemCollection items, Type enumType, params System.Enum[] excludeValues)
        {
            Array enumValues = Enum.GetValues(enumType);
            if (enumValues.Length > 0)
            {
                items.BeginUpdate();

                foreach (Enum enumValue in enumValues)
                {
                    if (Array.BinarySearch(excludeValues, enumValue) >= 0)
                        continue;

                    items.Add(new ImageComboBoxItem(enumValue.ToString(), Convert.ToInt32(enumValue), -1));
                }

                items.EndUpdate();
            }
        }
    }
}