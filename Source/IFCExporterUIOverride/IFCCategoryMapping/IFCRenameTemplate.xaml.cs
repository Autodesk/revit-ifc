using System;
using System.Windows;
using System.Windows.Threading;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for IFCRenameTemplate.xaml
   /// </summary>
   public partial class IFCRenameTemplate : BaseTemplate
   {
      public IFCRenameTemplate(IFCTemplateData data) : base(data)
      {
         InitializeComponent();
         base.OnInit(this);

         textBox_PreviousName.Text = data.NewName;
         NewName = data.MakeUniqueName();
         textBox_NewName.Focus();
         textBox_NewName.CaretIndex = textBox_NewName.Text.Length;
         textBox_NewName.Dispatcher.BeginInvoke(new Action(() => textBox_NewName.SelectAll()), DispatcherPriority.ContextIdle);
         textBox_NewName.ToolTip = GetToolTip();
         
         // Set up event handlers
         SetupEnterKeyHandler(textBox_NewName, button_Save);
         SetupSaveClickHandler(button_Save, textBox_NewName);
         SetupCancelClickHandler(button_Cancel);
      }
   }
}
