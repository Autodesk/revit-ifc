using Autodesk.UI.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BIM.IFC.Export.UI
{
   public abstract class BaseCategoryTemplate : ChildWindow, IDataErrorInfo, INotifyPropertyChanged
   {
      public IFCCategoryTemplateData Data { get; protected set; } = null;

      public BaseCategoryTemplate(IFCCategoryTemplateData data)
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
            string result = String.Empty;
            if (columnName == "NewName")
            {
               if (!Data.IsValidName(NewName))
               {
                  result = Properties.Resources.TemplateNameTooltip;
               }
            }

            return result;
         }
      }
      #endregion
   }

   /// <summary>
   /// Common data storage for IFCCategoryTemplate classes.
   /// </summary>
   public partial class IFCCategoryTemplateData
   {
      private IList<string> ExistingNames { get; set; } = null;

      /// <summary>
      /// The new template name
      /// </summary>
      public string NewName { get; private set; } = null;

      public IFCCategoryTemplateData(string newName, IList<string> existingNames)
      {
         NewName = newName;
         ExistingNames = existingNames;
      }

      /// <summary>
      /// Check that a potential name is valid for a template.
      /// </summary>
      /// <param name="name">The potential name.</param>
      /// <returns>True if it is valid.</returns>
      public bool IsValidName(string name)
      {
         return IFCCategoryMapping.IsValidName(name, ExistingNames);
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

      public string MakeUniqueTemplateName()
      {
         NewName = NewName?.TrimStart()?.TrimEnd();
         if (string.IsNullOrWhiteSpace(NewName))
         {
            NewName = Properties.Resources.NewTemplateDefaultName;
         }

         if (IsValidName(NewName))
         {
            return NewName;
         }

         int numPasses = ExistingNames.Count + 1;

         (string baseName, int baseNum) = RemoveDuplicateNumberIfItExists(NewName);
         for (int ii = 1; ii <= numPasses; ii++)
         {
            NewName = baseName + " (" + (ii+baseNum).ToString() + ")";
            if (IsValidName(NewName))
            {
               return NewName;
            }
         }

         // We shouldn't ever get here. One of the names above is guaranteed to be unique.
         NewName = null;
         return null;
      }
   }
}
