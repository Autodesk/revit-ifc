//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
// Copyright (C) 2016  Autodesk, Inc.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//

using Autodesk.Revit.WPFFramework;
using System;
using System.Windows;
using System.Windows.Controls;

namespace BIM.IFC.Export.UI
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class RenameExportSetupWindow : ChildWindow
    {
        private String m_newName;
        private IFCExportConfigurationsMap m_configurationMap;

        /// <summary>
        /// Constructs the rename dialog.
        /// </summary>
        /// <param name="configurationMap">The configuration map.</param>
        /// <param name="initialName">The initial name.</param>
        public RenameExportSetupWindow(IFCExportConfigurationsMap configurationMap, String initialName)
        {
            m_configurationMap = configurationMap;
            m_newName = initialName;           
            InitializeComponent();
            textBoxOriginalName.Text = m_newName;
            textBoxNewName.Text = m_newName;
        }

        /// <summary>
        /// The new name of the setup.
        /// </summary>
        /// <returns>The new name of the setup.</returns>
        public String GetName()
        {
            return m_newName;
        }

        /// <summary>
        /// OK button click.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            m_newName = textBoxNewName.Text;
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Cancel button click.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Updates ok button when the textbox changed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void textBoxNewName_TextChanged(object sender, TextChangedEventArgs e)
        {
            String textName = textBoxNewName.Text;
            if (String.IsNullOrWhiteSpace(textName) || m_configurationMap.HasName(textName))
            {
                buttonOK.IsEnabled = false;
            }
            else
            {
                buttonOK.IsEnabled = true;
            }
        }
    }
}
