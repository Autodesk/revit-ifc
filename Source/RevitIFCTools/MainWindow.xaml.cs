﻿//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
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

namespace RevitIFCTools
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      public MainWindow()
      {
         InitializeComponent();
      }

      private void button_GenerateIFCEntityList_Click(object sender, RoutedEventArgs e)
      {
         RevitIFCTools.IFCEntityListWin ifcEntWnd = new RevitIFCTools.IFCEntityListWin();
         ifcEntWnd.ShowDialog();
      }

      private void button_GeneratePsetDefs_Click(object sender, RoutedEventArgs e)
      {
         RevitIFCTools.GeneratePsetDefWin psetWin = new RevitIFCTools.GeneratePsetDefWin();
         psetWin.ShowDialog();
      }

      private void button_Cancel_Click(object sender, RoutedEventArgs e)
      {
         Close();
      }

      private void button_paramexpr_Click(object sender, RoutedEventArgs e)
      {
         RevitIFCTools.ParameterExpr.ExprTester exprTest = new ParameterExpr.ExprTester();
         exprTest.ShowDialog();
      }

      private void Button_GenGUID_Click(object sender, RoutedEventArgs e)
      {
         Guid newGuid = Guid.NewGuid();
         TextBox_GUID.Text = newGuid.ToString();
      }
   }
}
