using Autodesk.UI.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for IFCNotificationMessageBox.xaml
   /// </summary>   
   public partial class IFCNotificationMessageBox : ChildWindow
   {
      public IFCNotificationMessageBox(String messageText)
      {
         InitializeComponent();
         textBlock_MsgText.Text = messageText;
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
