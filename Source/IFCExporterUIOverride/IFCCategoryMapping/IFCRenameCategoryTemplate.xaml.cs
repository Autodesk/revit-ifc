using System.Windows;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for IFCRenameCategoryTemplate.xaml
   /// </summary>
   public partial class IFCRenameCategoryTemplate : BaseCategoryTemplate
   {
      public IFCRenameCategoryTemplate(IFCCategoryTemplateData data) : base(data)
      {
         InitializeComponent();
         base.OnInit(this);

         textBox_PreviousName.Text = data.NewName;
         NewName = data.MakeUniqueTemplateName();
         textBox_NewName.Focus();
         textBox_NewName.CaretIndex = textBox_NewName.Text.Length;
      }

      private void button_Save_Click(object sender, RoutedEventArgs e)
      {
         if (!Data.IsValidName(textBox_NewName.Text))
            return;

         Data.UpdateName(textBox_NewName.Text);
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
