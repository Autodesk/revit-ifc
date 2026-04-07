using System.Windows;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for IFCCopyTemplate.xaml
   /// </summary>
   public partial class IFCCopyTemplate : BaseTemplate
   {
      public IFCCopyTemplate(IFCTemplateData data):base(data)
      {
         InitializeComponent();
         base.OnInit(this);

         Title = GetDialogTitle();
         label_NewName.Content = GetLabelContent();

         textBox_NewName.Focus();
         textBox_NewName.CaretIndex = textBox_NewName.Text.Length;
         textBox_NewName.ToolTip = GetToolTip();
         
         // Set up event handlers
         SetupEnterKeyHandler(textBox_NewName, button_Save);
         SetupSaveClickHandler(button_Save, textBox_NewName);
         SetupCancelClickHandler(button_Cancel);
      }

      private string GetDialogTitle()
      {
         switch (Data.DialogType)
         {
            case IFCTemplateData.DialogTypeEnum.Template:
               return Properties.Resources.IFCCopyTemplate;
            case IFCTemplateData.DialogTypeEnum.PropertySet:
               return Properties.Resources.IFCCreateDuplicate;
            default:
               return string.Empty;
         }
      }
   }
}
