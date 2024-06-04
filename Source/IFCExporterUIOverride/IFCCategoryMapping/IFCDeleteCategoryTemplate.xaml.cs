﻿using Autodesk.UI.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for IFCDeleteCategoryTemplate.xaml
   /// </summary>
   public partial class IFCDeleteCategoryTemplate : ChildWindow
   {
      public IFCDeleteCategoryTemplate(String templateName)
      {
         InitializeComponent();
         textBlock_MsgText.Text = string.Format(Properties.Resources.IFCDeleteCategoryTemplateMessage, templateName);
      }

      private void button_Ok_Click(object sender, RoutedEventArgs e)
      {
         DialogResult = true;
         Close();
      }

      private void button_Cancel_Click(object sender, RoutedEventArgs e)
      {
         DialogResult = false;
         Close();
      }
   }
}
