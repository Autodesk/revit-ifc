using Autodesk.Revit.DB;
using Autodesk.UI.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for IFCPropertyFilter.xaml
   /// </summary>
   public partial class IFCPropertyFilter : ChildWindow
   {
      public string PropertySetFilter { get; set; }

      public string ParameterFilter { get; set; }

      public IFCPropertyFilter(string propertySetFilter, string parameterFilter)
      {
         DataContext = this;
         InitializeComponent();

         PropertySetFilter = propertySetFilter;
         ParameterFilter = parameterFilter;

         textBox_PropertySet.Text = PropertySetFilter;
         textBox_Parameter.Text = ParameterFilter;
      }

      private void button_Apply_Click(object sender, RoutedEventArgs e)
      {
         PropertySetFilter = textBox_PropertySet.Text;
         ParameterFilter = textBox_Parameter.Text;
      }

      private void button_Remove_Click(object sender, RoutedEventArgs e)
      {
         textBox_PropertySet.Text = string.Empty;
         textBox_Parameter.Text = string.Empty;
      }

      private void button_ParamaterClean_Click(object sender, RoutedEventArgs e)
      {
         textBox_Parameter.Text = string.Empty;
      }

      private void button_PropertySetClean_Click(object sender, RoutedEventArgs e)
      {
         textBox_PropertySet.Text = string.Empty;
      }
   }
}
