using Autodesk.UI.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace BIM.IFC.Export.UI
{
   public abstract class BaseTemplate : ChildWindow, IDataErrorInfo, INotifyPropertyChanged
   {
      public IFCTemplateData Data { get; protected set; } = null;

      private readonly UserDefinedPropertySetValidator validator = new();

      public BaseTemplate(IFCTemplateData data)
      {
         Data = data;
         NewName = data.NewName;
      }

      internal void OnInit(object obj)
      {
         this.DataContext = obj;
      }

      private string m_newName = String.Empty;
      public string NewName
      {
         get { return m_newName; }
         set
         {
            m_newName = value;
            OnPropertyChanged();
         }
      }

      public event PropertyChangedEventHandler PropertyChanged;

      protected void OnPropertyChanged([CallerMemberName] string name = null)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }

      #region VALIDATION

      /// <summary>
      /// Error message indicating what's wrong with the object
      /// </summary>
      public string Error
      {
         get
         {
            return string.Empty;
         }
      }

      /// <summary>
      /// Error message for the property with the given name
      /// </summary>
      /// <param name="columnName">The name of the property</param>
      /// <returns></returns>
      public string this[string columnName]
      {
         get
         {
            string result = string.Empty;
            if (columnName == "NewName")
            {
               if (!Data.IsValidName(NewName))
               {
                  result = GetToolTip();
               }
            }

            return result;
         }
      }
      #endregion

      public string GetToolTip()
      {
         switch (Data.DialogType)
         {
            case IFCTemplateData.DialogTypeEnum.Template:
               if (IFCTemplateData.ContainsInvalidTemplateCharacters(NewName))
                  return Properties.Resources.TemplateNameInvalidTooltip;
               return Properties.Resources.TemplateNameTooltip;
            case IFCTemplateData.DialogTypeEnum.PropertySet:
               { 
                  if (UserDefinedPropertySetValidator.IsReserved(NewName))
                     return Properties.Resources.ReservedPropertySetTooltip;
                  else if (IFCTemplateData.ContainsInvalidTemplateCharacters(NewName))
                     return Properties.Resources.PropertySetInvalidNameTooltip;
                  return Properties.Resources.PropertySetNameTooltip;
               }
            default:
               return string.Empty;
         }
      }

      public string GetLabelContent()
      {
         switch (Data.DialogType)
         {
            case IFCTemplateData.DialogTypeEnum.Template:
               return Properties.Resources.TemplateName;
            case IFCTemplateData.DialogTypeEnum.PropertySet:
               return Properties.Resources.PropertySetName;
            default:
               return string.Empty;
         }
      }

      /// <summary>
      /// Sets up a KeyDown event handler on the textbox to ensure proper focus event order when pressing Enter.
      /// This fixes an issue where the journal record would get swapped
      /// if the button click event fired before the textbox lost keyboard focus.
      /// </summary>
      protected void SetupEnterKeyHandler(TextBox textBox, Button saveButton)
      {
         if (textBox == null || saveButton == null)
            return;

         textBox.KeyDown += (sender, e) =>
         {
            if (e.Key == Key.Enter)
            {
               saveButton.Focus();
            }
         };
      }
      
      protected void SetupSaveClickHandler(Button saveButton, TextBox textBox)
      {
         if (textBox == null || saveButton == null)
            return;
            
         saveButton.Click += (sender, e) =>
         {
            if (!Data.IsValidName(textBox.Text))
            {
               (textBox.Text, _) = validator.ExtendPropertySetNameIfNeeded(textBox.Text);
               return;
            }

            Data.UpdateName(textBox.Text);
            DialogResult = true;
            Close();
         };
      }
      
      protected void SetupCancelClickHandler(Button cancelButton)
      {
         if (cancelButton == null)
            return;
            
         cancelButton.Click += (sender, e) =>
         {
            DialogResult = false;
            Close();
         };
      }
  
   }

   /// <summary>
   /// Common data storage for IFCCategoryTemplate classes.
   /// </summary>
   public partial class IFCTemplateData
   {
      public enum DialogTypeEnum
      {
         Template,
         PropertySet
      }

      public DialogTypeEnum DialogType { get; set; } = DialogTypeEnum.Template;

      private IList<string> ExistingNames { get; set; } = null;

      private bool IsCategoryMapping { get; set; } = true;
      /// <summary>
      /// The new template name
      /// </summary>
      public string NewName { get; private set; } = null;

      public IFCTemplateData(string newName, IList<string> existingNames, bool isCategoryMapping, DialogTypeEnum dialogType)
      {
         NewName = newName;
         ExistingNames = existingNames;
         IsCategoryMapping = isCategoryMapping;
         DialogType = dialogType;
      }

      /// <summary>
      /// Check that a potential name is valid for a template.
      /// </summary>
      /// <param name="name">The potential name.</param>
      /// <returns>True if it is valid.</returns>
      public bool IsValidName(string name)
      {
         switch (DialogType)
         {
            case DialogTypeEnum.Template:
               {
                  if (ContainsInvalidTemplateCharacters(name))
                     return false;

                  return (IsCategoryMapping) ? IFCCategoryMapping.IsValidName(name, ExistingNames) :
                  IFCPropertyMapping.IsValidName(name, ExistingNames);
               }
            case DialogTypeEnum.PropertySet:
               {
                  return IFCUserDefinedPropertyMapping.IsValidPropertySetName(name, ExistingNames);
               }
            default:
               return false;
         }

      }

      private (string, int) RemoveDuplicateNumberIfItExists(string initName)
      {
         if (!initName.EndsWith(')'))
         {
            return (initName, 1);
         }

         int startParenloc = initName.LastIndexOf('(');
         if (startParenloc == -1)
         {
            return (initName, 1);
         }

         int startOfPotentialInt = startParenloc + 1;
         int endOfPotentialInt = initName.Length - 2;
         int potentialIntLen = endOfPotentialInt - startOfPotentialInt + 1;
         if (int.TryParse(initName.Substring(startOfPotentialInt, potentialIntLen), out int num))
         {
            return (initName.Substring(0, startOfPotentialInt - 1).TrimStart().TrimEnd(), num);
         }

         return (initName, 1);
      }

      public void UpdateName(string newName)
      {
         NewName = newName?.TrimStart()?.TrimEnd();
      }

      public string MakeUniqueName()
      {
         NewName = NewName?.TrimStart()?.TrimEnd();
         if (DialogType == DialogTypeEnum.Template)
            NewName = RemoveInvalidCharacters(NewName);
         if (string.IsNullOrWhiteSpace(NewName))
         {
            NewName = GetNewNameFromResources();
            if (DialogType == DialogTypeEnum.Template)
               NewName = RemoveInvalidCharacters(NewName);
         }

         if (IsValidName(NewName))
         {
            return NewName;
         }

         int numPasses = ExistingNames.Count + 1;

         (string baseName, int baseNum) = RemoveDuplicateNumberIfItExists(NewName);
         if (DialogType == DialogTypeEnum.Template)
            baseName = RemoveInvalidCharacters(baseName);
         for (int ii = 1; ii <= numPasses; ii++)
         {
            NewName = baseName + " (" + (ii+baseNum).ToString() + ")";
            if (DialogType == DialogTypeEnum.Template)
               NewName = RemoveInvalidCharacters(NewName);
            if (IsValidName(NewName))
            {
               return NewName;
            }
         }

         // We shouldn't ever get here. One of the names above is guaranteed to be unique.
         NewName = null;
         return null;
      }
      private string GetNewNameFromResources()
      {
         switch (DialogType)
         {
            case DialogTypeEnum.Template:
               return Properties.Resources.NewTemplateDefaultName;
            case DialogTypeEnum.PropertySet:
               return Properties.Resources.IFCNewPropertySet;
            default:
               return string.Empty;
         }
      }
   }

   public partial class IFCTemplateData
   {
      const string InvalidTemplateCharacters = "\\/:*?\"<>|";

      public static bool ContainsInvalidTemplateCharacters(string name)
      {
         if (string.IsNullOrEmpty(name))
            return false;

         foreach (char ch in name)
         {
            if (InvalidTemplateCharacters.IndexOf(ch) >= 0)
               return true;
         }

         return false;
      }

      public static string RemoveInvalidCharacters(string name)
      {
         if (string.IsNullOrEmpty(name))
            return name;

         if (!ContainsInvalidTemplateCharacters(name))
            return name;

         StringBuilder builder = new();
         foreach (char ch in name)
         {
            if (InvalidTemplateCharacters.IndexOf(ch) < 0)
               builder.Append(ch);
         }

         return builder.ToString();
      }
   }

   /// <summary>
   /// Converts ReservedPset validation result to bool
   /// </summary>
   public class ReservedPsetToLightErrorConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         var str = value as string;
         return string.IsNullOrEmpty(str) ? null : UserDefinedPropertySetValidator.IsReserved(str);
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         return value;
      }
   }
}
