using System;
using Revit.IFC.Export.Utility;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static BIM.IFC.Export.UI.IFCPropertySetType;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for IFCNewTemplate.xaml
   /// </summary>
   public partial class IFCNewTemplate : BaseTemplate
   {
      private IFCPropertySetType _selectedIfcPropertySetType;
      public IFCPropertySetType SelectedIfcPropertySetType
      {
         get { return _selectedIfcPropertySetType; }
         set
         {
            _selectedIfcPropertySetType = value;
            OnPropertyChanged(nameof(SelectedIfcPropertySetType));
         }
      }

      public IFCNewTemplate(IFCTemplateData data) : base(data)
      {
         InitializeComponent();
         base.OnInit(this);

         if (data?.DialogType != IFCTemplateData.DialogTypeEnum.PropertySet)
            comboBox_PropertySetType.Visibility = Visibility.Collapsed;
         else
            InitializePropertySetTypes();

         Title = GetDialogTitle();
         label_NewName.Content = GetLabelContent();
         textBox_NewName.ToolTip = GetToolTip();

         NewName = Data.MakeUniqueName(); 
         textBox_NewName.Focus();
         textBox_NewName.CaretIndex = textBox_NewName.Text.Length;
         textBox_NewName.Dispatcher.BeginInvoke(new Action(() => textBox_NewName.SelectAll()), DispatcherPriority.ContextIdle);
         
         // Set up event handlers
         SetupEnterKeyHandler(textBox_NewName, button_Save);
         SetupSaveClickHandler(button_Save, textBox_NewName);
         SetupCancelClickHandler(button_Cancel);
      }

      private void InitializePropertySetTypes()
      {
         if (!comboBox_PropertySetType.HasItems)
         {
            comboBox_PropertySetType.Items.Add(new IFCPropertySetType(PropertySetType.PropertySet));
            comboBox_PropertySetType.Items.Add(new IFCPropertySetType(PropertySetType.QuantitySet));
            comboBox_PropertySetType.Items.Add(new IFCPropertySetType(PropertySetType.IFCAttributes));
         }

         SelectedIfcPropertySetType = (IFCPropertySetType)comboBox_PropertySetType.Items[0];
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

      private string GetDialogTitle()
      {
         switch (Data.DialogType)
         {
            case IFCTemplateData.DialogTypeEnum.Template:
               return Properties.Resources.IFCNewTemplate;
            case IFCTemplateData.DialogTypeEnum.PropertySet:
               return Properties.Resources.IFCCreateNewPropertySet;
            default:
               return string.Empty;
         }
      }
   }

   /// <summary>
   /// Keeps data to initialize PropertySetType.
   /// </summary>
   public class IFCPropertySetType
   {
      public enum PropertySetType
      {
         PropertySet,
         QuantitySet,
         IFCAttributes
      }
      public PropertySetType CurrentPropertySetType { get; set; }

      public IFCPropertySetType(PropertySetType propertySetType)
      {
         CurrentPropertySetType = propertySetType;
      }

      public override string ToString()
      {
         switch (CurrentPropertySetType)
         {
            case PropertySetType.PropertySet:
               return Properties.Resources.PropertySet;
            case PropertySetType.QuantitySet:
               return Properties.Resources.QuantitySet;
            case PropertySetType.IFCAttributes:
               return Properties.Resources.IFCAttributes;
            default:
               return string.Empty;
         }
      }
   }
}
