using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Static helper class for ComboBox filtering operations
   /// </summary>
   public static class ComboBoxFilteringUtilities
   {
      private static readonly Dictionary<ComboBox, ObservableCollection<string>> _collections = new();

      /// <summary>
      /// Gets or creates an individual filtered collection for the specified ComboBox.
      /// </summary>
      public static ObservableCollection<string> GetOrCreateCollection(ComboBox comboBox, ObservableCollection<string> masterData)
      {
         if (comboBox == null || masterData == null)
            return new();

         if (!_collections.ContainsKey(comboBox))
         {
            _collections[comboBox] = new(masterData);
            comboBox.ItemsSource = _collections[comboBox];
         }
         return _collections[comboBox];
      }

      /// <summary>
      /// Gets the current caret position in a ComboBox's internal TextBox
      /// </summary>
      public static int GetCaretPosition(ComboBox comboBox)
      {
         if (comboBox?.Template?.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
         {
            return textBox.CaretIndex;
         }
         return 0;
      }

      /// <summary>
      /// Sets the caret position in a ComboBox's internal TextBox
      /// </summary>
      public static void SetCaretPosition(ComboBox comboBox, int position)
      {
         if (comboBox?.Template?.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
         {
            int safePosition = Math.Max(0, Math.Min(position, textBox.Text?.Length ?? 0));
            textBox.CaretIndex = safePosition;
         }
      }

      /// <summary>
      /// Opens ComboBox dropdown while suppressing automatic text highlighting
      /// and preserving saved caret position
      /// </summary>
      public static void OpenDropDownSuppressingHighlight(ComboBox comboBox)
      {
         if (comboBox == null || comboBox.IsDropDownOpen)
            return;

         int savedCaretPosition = GetCaretPosition(comboBox);

         comboBox.IsDropDownOpen = true;

         if (comboBox.Template?.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
         {
            textBox.Select(savedCaretPosition, 0);
         }
      }

      /// <summary>
      /// Attaches TextChanged handler to ComboBox's internal TextBox
      /// </summary>
      public static void AttachTextChangedHandler(ComboBox comboBox, Action<ComboBox, TextChangedEventArgs> handler)
      {
         if (comboBox == null || handler == null)
            return;

         // Apply template to ensure visual tree is created
         comboBox.ApplyTemplate();

         if (comboBox.Template?.FindName("PART_EditableTextBox", comboBox) is not TextBox textBox)
            return;

         textBox.TextChanged += (sender, e) => handler(comboBox, e);
      }

      /// <summary>
      /// Filters a ComboBox collection based on text input with smart incremental updates
      /// </summary>
      /// <param name="comboBox">The ComboBox to filter</param>
      /// <param name="targetCollection">The collection bound to the ComboBox (will be modified)</param>
      /// <param name="masterData">The master data source to filter from</param>
      /// <param name="filterText">The text to filter by (case-insensitive substring match)</param>
      /// <param name="forceFullList">If true, shows full list regardless of filterText</param>
      public static void FilterCollection(ComboBox comboBox, ObservableCollection<string> targetCollection,
         ObservableCollection<string> masterData, string filterText, bool forceFullList)
      {
         if (comboBox == null || targetCollection == null || masterData == null)
            return;

         bool wantFullList = string.IsNullOrEmpty(filterText) || forceFullList;

         // If we want full list and already have it, skip the work
         if (wantFullList && targetCollection.Count == masterData.Count)
            return;

         // Determine what items should be in the filtered collection
         HashSet<string> targetItems = wantFullList
            ? new(masterData)
            : new(masterData.Where(item =>
               item?.IndexOf(filterText ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0));

         // Remove items that shouldn't be there
         string selectedItemText = comboBox?.SelectedItem?.ToString() ?? "";
         bool selectedItemWillBeRemoved = !string.IsNullOrEmpty(selectedItemText) && !targetItems.Contains(selectedItemText);
         if (selectedItemWillBeRemoved)
         {
            RemoveSelectedItemPreservingText(comboBox, targetCollection, targetItems);
         }

         // Remove remaining non-matching items 
         for (int i = targetCollection.Count - 1; i >= 0; i--)
         {
            if (!targetItems.Contains(targetCollection[i]))
            {
               targetCollection.RemoveAt(i);
            }
         }

         // Add missing items in correct sorted position
         foreach (var item in targetItems.OrderBy(x => x))
         {
            if (targetCollection.Contains(item))
               continue;

            // Find correct insertion position to maintain sort order
            int insertIndex = 0;
            for (int i = 0; i < targetCollection.Count; i++)
            {
               if (string.Compare(item, targetCollection[i], StringComparison.OrdinalIgnoreCase) < 0)
               {
                  insertIndex = i;
                  break;
               }
               insertIndex = i + 1;
            }
            targetCollection.Insert(insertIndex, item);
         }
      }

      /// <summary>
      /// Removes the selected item from collection while preserving ComboBox text and caret position
      /// </summary>
      /// <param name="comboBox">The ComboBox whose selected item will be removed</param>
      /// <param name="targetCollection">The collection to remove the item from</param>
      /// <param name="targetItems">The items that should remain (used for validation)</param>
      public static void RemoveSelectedItemPreservingText(ComboBox comboBox, ObservableCollection<string> targetCollection, HashSet<string> targetItems)
      {
         if (comboBox?.SelectedItem == null || targetCollection == null)
            return;

         string selectedItem = comboBox.SelectedItem.ToString() ?? string.Empty;
         string savedText = comboBox.Text ?? "";
         int savedCaretPosition = GetCaretPosition(comboBox);

         // Remove the selected item specifically
         targetCollection.Remove(selectedItem);

         // Restore text and caret position after ComboBox cleared it when selected item was removed
         if (string.IsNullOrEmpty(comboBox.Text) && !string.IsNullOrEmpty(savedText))
         {
            comboBox.Text = savedText;
            SetCaretPosition(comboBox, savedCaretPosition);
         }
      }
   }
}
