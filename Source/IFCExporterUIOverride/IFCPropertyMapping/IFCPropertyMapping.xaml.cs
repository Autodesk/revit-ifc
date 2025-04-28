using Autodesk.Revit.DB;
using Autodesk.UI.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for IFCPropertyMapping.xaml
   /// </summary>
   public partial class IFCPropertyMapping : ChildWindow, INotifyPropertyChanged
   {
      /// <summary>
      /// The Mapping Model
      /// </summary>
      private IFCPropertyMappingModel _model = new();

      /// <summary>
      /// Property Setups list.
      /// </summary>
      private IFCPropertySetups _selectedPropertySetup;
      public IFCPropertySetups SelectedPropertySetup
      {
         get { return _selectedPropertySetup; }
         set
         {
            _selectedPropertySetup = value;
            OnPropertyChanged(nameof(SelectedPropertySetup));
         }
      }
      private List<IFCPropertySetups> _propertySetups = new();
      public List<IFCPropertySetups> PropertySetups
      {
         get { return _propertySetups; }
      }

      /// <summary>
      /// Property Set list.
      /// </summary>
      private PSetMappingInfo _selectedPropertySet;
      public PSetMappingInfo SelectedPropertySet
      {
         get { return _selectedPropertySet; }
         set
         {
            _selectedPropertySet = value;
            OnPropertyChanged(nameof(SelectedPropertySet));
         }
      }

      /// <summary>
      /// Property sets
      /// </summary>
      private ObservableCollection<PSetMappingInfo> ObservablePropertySets { get; set; } = new();
      
      /// <summary>
      /// Property mappings
      /// </summary>
      private ObservableCollection<PropertyMappingInfo> ObservableProperties { get; set; } = new();

      /// <summary>
      /// IFC schema list.
      /// </summary>
      private IFCVersion _selectedIfcSchema;
      public IFCVersion SelectedIfcSchema
      {
         get { return _selectedIfcSchema; }
         set
         {
            _selectedIfcSchema = value;
            OnPropertyChanged(nameof(SelectedIfcSchema));
         }
      }
      private List<IFCVersion> _ifcSchemas;
      public List<IFCVersion> IfcSchemas
      {
         get { return _ifcSchemas; }
      }

      #region Filtering members

      public ICollectionView PropertySetCollection => CollectionViewSource.GetDefaultView(ObservablePropertySets);

      private string m_filterTextPropertySet;
      public string FilterTextPropertySet
      {
         get { return m_filterTextPropertySet; }
         set
         {
            m_filterTextPropertySet = value;
            OnPropertyChanged(nameof(FilterTextPropertySet));
            PropertySetCollection.Refresh();
         }
      }

      public ICollectionView PropertyCollection => CollectionViewSource.GetDefaultView(ObservableProperties);

      private string m_filterTextParameter;
      public string FilterTextParameter
      {
         get { return m_filterTextParameter; }
         set
         {
            m_filterTextParameter = value;
            OnPropertyChanged(nameof(FilterTextParameter));
            PropertyCollection.Refresh();
         }
      }
      #endregion

      private bool? m_ExportFlagAll = true;

      /// <summary>
      /// Flag to determine if all properties are exported or not.
      /// </summary>
      public bool? ExportFlagAll
      {
         get { return m_ExportFlagAll; }
         set
         {
            m_ExportFlagAll = value;
            OnPropertyChanged(nameof(ExportFlagAll));
            ExportFlagAllClick();
         }
      }

      public event PropertyChangedEventHandler PropertyChanged;
      protected void OnPropertyChanged([CallerMemberName] string name = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }


      /// <summary>
      /// Constructor.
      /// </summary>
      public IFCPropertyMapping()
      {
         InitializeComponent();
         DataContext = this;

         InitializeSchemaList();
         InitializePropertySetupsList();

         IFCParameterTemplate currentTemplate = GetCurrentTemplate();
         if (currentTemplate?.IsInSessionTemplate() ?? false)
            currentTemplate.SetInSessionTemplateDocument(IFCCommandOverrideApplication.TheDocument);

         _model.InitializeSetupIfNeeded(SelectedPropertySetup.Setup, SelectedIfcSchema, currentTemplate);

         PropertySetCollection.Filter = FilterPropertySet;
         PropertyCollection.Filter = FilterProperty;
      }

      private void InitializeSchemaList()
      {
         _ifcSchemas = new()
         {
         IFCVersion.IFC2x2,
         IFCVersion.IFC2x3,
         IFCVersion.IFCCOBIE,
         IFCVersion.IFC2x3FM,
         IFCVersion.IFC2x3BFM,
         IFCVersion.IFC2x3CV2,
         IFCVersion.IFC4,
         IFCVersion.IFC4RV,
         IFCVersion.IFC4DTV,
         IFCVersion.IFCSG,
         IFCVersion.IFC4x3
         };
         SelectedIfcSchema = IFCVersion.IFC2x3CV2;
      }

      /// <summary>
      /// Initialize listBox with Property Setups list
      /// </summary>
      private void InitializePropertySetupsList()
      {
         foreach (IFCPropertySetups.PropertySetup propertySetup in Enum.GetValues(typeof(IFCPropertySetups.PropertySetup)))
         {
            _propertySetups.Add(new IFCPropertySetups(propertySetup));
         }
         SelectedPropertySetup = _propertySetups.FirstOrDefault();
      }

      #region Visibility states
      public enum VisibilityState
      {
         // Show all controls except empty states
         Default = 0,
         // Display empty state image
         EmptyState = 1,
         // Schema selection is allowed
         VisibleSchema = 2,
         // Schema selection is not needed
         HiddenSchema
      }

      private void UpdateControlsVisibilityState(VisibilityState state)
      {
         switch (state)
         {
            case VisibilityState.Default:
               {
                  dataGrid_PropertyMapping.Visibility = System.Windows.Visibility.Visible;
                  comboBox_IFCSchema.Visibility = System.Windows.Visibility.Visible;
                  textBlock_Schema.Visibility = System.Windows.Visibility.Visible;
                  button_Filter.Visibility = System.Windows.Visibility.Visible;
                  image_EmptyState.Visibility = System.Windows.Visibility.Collapsed;
                  break;
               }
            case VisibilityState.VisibleSchema:
               {
                  comboBox_IFCSchema.Visibility = System.Windows.Visibility.Visible;
                  textBlock_Schema.Visibility = System.Windows.Visibility.Visible;
                  break;
               }
            case VisibilityState.HiddenSchema:
               {
                  comboBox_IFCSchema.Visibility = System.Windows.Visibility.Collapsed;
                  textBlock_Schema.Visibility = System.Windows.Visibility.Collapsed;
                  break;
               }
            case VisibilityState.EmptyState:
               {
                  dataGrid_PropertyMapping.Visibility = System.Windows.Visibility.Collapsed;
                  button_Filter.Visibility = System.Windows.Visibility.Collapsed;
                  image_EmptyState.Visibility = System.Windows.Visibility.Visible;
                  break;
               }
            default:
               break;
         }
      }
      #endregion

      private void InitializePropertySetList()
      {
         if (SelectedPropertySetup == null)
            return;

         UpdateControlsVisibilityState(VisibilityState.Default);

         ObservablePropertySets.Clear();
         List<PSetMappingInfo> psetList = _model.GetPropertySetList(SelectedPropertySetup.Setup);
         foreach(PSetMappingInfo psetInfo in psetList)
            ObservablePropertySets.Add(psetInfo);

         switch (SelectedPropertySetup.Setup)
         {
            case IFCPropertySetups.PropertySetup.IFCCommonPropertySets:
               {
                  UpdateControlsVisibilityState(VisibilityState.VisibleSchema);
                  break;
               }
            case IFCPropertySetups.PropertySetup.MaterialPropertySets:
            case IFCPropertySetups.PropertySetup.Schedules:
               {
                  UpdateControlsVisibilityState(VisibilityState.HiddenSchema);
                  if (ObservablePropertySets.Count == 0)
                     UpdateControlsVisibilityState(VisibilityState.EmptyState);
                  break;
               }
            default:
               break;
         }

         SelectedPropertySet = ObservablePropertySets.FirstOrDefault();
      }

      private void InitializePropertyDataGrid()
      {
         if (SelectedPropertySet == null)
            return;

         if (ObservableProperties.Count > 0)
            ObservableProperties.Clear();

         List<PropertyMappingInfo> propertyInfoList = _model.GetPropertyList(SelectedPropertySetup.Setup, SelectedPropertySet.Name);
         if (propertyInfoList == null)
            return;
         
         foreach(PropertyMappingInfo propertyInfo in propertyInfoList)
            ObservableProperties.Add(propertyInfo); 
      }

      /// <summary>
      /// Returns true is the name is equal to in-session template name
      /// </summary>
      static bool IsInSessionTemplate(string templateName)
      {
         string inSessionName = GetInSessionTemplateName();
         return templateName.Equals(inSessionName);
      }

      /// <summary>
      /// Returns in-session template name
      /// </summary>
      static string GetInSessionTemplateName()
      {
         IFCParameterTemplate inSessionTemplate = IFCParameterTemplate.GetOrCreateInSessionTemplate(IFCCommandOverrideApplication.TheDocument);
         return inSessionTemplate?.Name ?? string.Empty;
      }

      /// <summary>
      /// Returns the list of templates in the document including the in-session one
      /// </summary>
      static IList<string> GetAllTemplateNames()
      {
         IList<string> templateNames = IFCParameterTemplate.ListNames(IFCCommandOverrideApplication.TheDocument) ?? new List<string>();
         templateNames.Add(GetInSessionTemplateName());

         return templateNames;
      }

      /// <summary>
      /// Get template active in list
      /// </summary>
      private IFCParameterTemplate GetCurrentTemplate()
      {
         return GetTemplateByName(GetInSessionTemplateName());
      }

      private IFCParameterTemplate GetTemplateByName(string templateName)
      {
         if (string.IsNullOrEmpty(templateName))
            return null;

         IFCParameterTemplate foundTemplate = null;
         if (IsInSessionTemplate(templateName))
            foundTemplate = IFCParameterTemplate.GetOrCreateInSessionTemplate(IFCCommandOverrideApplication.TheDocument);
         else
            foundTemplate = IFCParameterTemplate.FindByName(IFCCommandOverrideApplication.TheDocument, templateName);

         return foundTemplate;
      }

      private void UpdateTemplateFromGrid(string templateName)
      {
         IFCParameterTemplate templateToUpdate = null;

         // Update Current template is the name isn't specified
         if (templateName == null)
            templateToUpdate = GetCurrentTemplate();
         else
            templateToUpdate = GetTemplateByName(templateName);

         if (templateToUpdate == null)
            return;

         foreach (IFCPropertySetups.PropertySetup propertySetup in Enum.GetValues(typeof(IFCPropertySetups.PropertySetup)))
         {
            _model.WriteToTemplate(propertySetup, templateToUpdate);
         }
      }

      private void button_Ok_Click(object sender, RoutedEventArgs e)
      {
         // The name isn't specified, InSession template will be used
         UpdateTemplateFromGrid(null);
         // Save changes here
         Close();
      }

      private void button_Cancel_Click(object sender, RoutedEventArgs e)
      {
         Close();
      }

      private void listBox_PropertySetups_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         _model.InitializeSetupIfNeeded(SelectedPropertySetup.Setup, SelectedIfcSchema, GetCurrentTemplate());
         textBlock_SelectedSetupName.Text = SelectedPropertySetup.ToString();
         InitializePropertySetList();
      }

      private void listBox_PropertySets_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         InitializePropertyDataGrid();
      }

      private void comboBox_IFCSchema_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         // TODO how should it even work?
         //InitializePropertySetList();
      }

      private bool FilterPropertySet(object obj)
      {
         if (!string.IsNullOrEmpty(FilterTextPropertySet) ||
            !string.IsNullOrEmpty(FilterTextParameter))
         {
            PSetMappingInfo psetItem = obj as PSetMappingInfo;
            if (psetItem != null)
            {
               // Process Property Set Filter text
               if (!string.IsNullOrEmpty(FilterTextPropertySet))
               {
                  bool passPSetFilter = psetItem.Name.Contains(FilterTextPropertySet, StringComparison.OrdinalIgnoreCase);
                  if (!passPSetFilter)
                     return false;
               }

               // Process Parameter Filter text (Hide empty Property Sets)
               if (!string.IsNullOrEmpty(FilterTextParameter))
               {
                  List<string> propertyList = GetContainedProperties(psetItem.Name);
                  if ((propertyList?.Count ?? 0) == 0)
                     return false;

                  foreach (string propertyName in propertyList)
                  {
                     if (FilterProperty(propertyName))
                        return true;
                  }
                  return false;
               }
            }
         }
         return true;
      }

      private List<string> GetContainedProperties(string propertySetName)
      {
         List<PropertyMappingInfo> propertyInfos = _model.GetPropertyList(SelectedPropertySetup.Setup, propertySetName);
         if (propertyInfos == null)
            return null;

         List<string> propertyList = propertyInfos.Select(x =>
            (x.Type == IFCPropertyMappingModel.MappingType.IfcToRevit) ?
            x.IFCPropertyName : x.RevitPropertyName).ToList();

         return propertyList;
      }

      private bool FilterProperty(object obj)
      {
         if (!string.IsNullOrEmpty(FilterTextParameter) &&
            obj is PropertyMappingInfo)
         {
            PropertyMappingInfo propertyInfo = (PropertyMappingInfo)obj;
            if (propertyInfo == null)
               return false;

            string nameToFilter = (propertyInfo.Type == IFCPropertyMappingModel.MappingType.IfcToRevit) ?
               propertyInfo.IFCPropertyName : propertyInfo.RevitPropertyName;

            return FilterProperty(nameToFilter); 
         }

         return true;
      }

      private bool FilterProperty(string propertyName)
      {
         if (!string.IsNullOrEmpty(FilterTextParameter) &&
            !string.IsNullOrEmpty(propertyName))
         {
            return propertyName.Contains(FilterTextParameter, StringComparison.OrdinalIgnoreCase);
         }
         return true;
      }

      private void button_Filter_Click(object sender, RoutedEventArgs e)
      {
         IFCPropertyFilter filterDialog = new(FilterTextPropertySet, FilterTextParameter);
         filterDialog.Owner = this;
         filterDialog.ShowDialog();

         bool filterChanged =
            FilterTextParameter != filterDialog.ParameterFilter ||
            FilterTextPropertySet != filterDialog.PropertySetFilter;

         if (filterChanged)
         {
            FilterTextParameter = filterDialog.ParameterFilter;
            FilterTextPropertySet = filterDialog.PropertySetFilter;

            listBox_PropertySets.SelectedIndex = 0;
         }
      }

      /// <summary>
      /// This modifies ExportFlag for all PropertyMappingInfos 
      /// </summary>
      private void ExportFlagAllClick()
      {
         bool? newState = ExportFlagAll;
         if (!newState.HasValue)
            return;

         foreach (PropertyMappingInfo currMappingInfo in PropertyCollection)
         {
            if (currMappingInfo == null)
               continue;

            currMappingInfo.ExportFlag = newState.Value;
         }
      }

      private void button_Reset_Click(object sender, RoutedEventArgs e)
      {
         PropertyMappingInfo propertyMappingInfo = dataGrid_PropertyMapping.SelectedItem as PropertyMappingInfo;
         if (propertyMappingInfo == null || propertyMappingInfo.IsDefault())
            return;

         // Resets one property mapping raw
         propertyMappingInfo.ResetToDefault();
      }

      private void button_ResetAll_Click(object sender, RoutedEventArgs e)
      {
         if (SelectedPropertySet == null)
            return;

         _model.ResetAll(SelectedPropertySetup.Setup, SelectedPropertySet.Name);

         ExportFlagAll = UpdateCheckboxFlag(ObservableProperties);
      }

      /// <summary>
      /// Updates indeterminate state if needed.
      /// </summary>
      private bool? UpdateCheckboxFlag(ObservableCollection<PropertyMappingInfo> nodesList)
      {
         bool existsUnchecked = nodesList?.Any(node => (node.ExportFlag == false)) ?? false;
         if (existsUnchecked)
            return null;

         return true;
      }
   }

   

   #region Converters
   /// <summary>
   /// Converts IFCPropertySetups to string
   /// </summary>
   public class SetupNameConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         if (value is IFCPropertySetups)
            return (value as IFCPropertySetups).ToString();

         return null;
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }
   #endregion
}
