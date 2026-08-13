//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Class that manages information for the current link instance being exported.
   /// </summary>
   public class FederatedLinkManager
   {
      public FederatedLinkManager()
      {
         // Set up the mirror transform here, for use in mapped items.
         MirrorTransform = Transform.CreateReflection(Plane.CreateByNormalAndOrigin(XYZ.BasisY, XYZ.Zero));
      }

      /// <summary>
      /// Update the values of the current LinkInformation.
      /// </summary>
      /// <param name="linkId">The id of the linked instance.</param>
      public void Update(ElementId linkId)
      {
         LinkId = linkId;
         IsMirrored = false;
         BaseLinkTransform = Transform.Identity;
      }

      /// <summary>
      /// Update the values of the current LinkInformation.
      /// </summary>
      /// <param name="linkId">The id of the linked instance.</param>
      /// <param name="totalTransform">The transform (potentially with reflection> of the linked instance.</param>
      public void Update(ElementId linkId, Transform totalTransform)
      {
         LinkId = linkId;
         IsMirrored = totalTransform.HasReflection;
         if (IsMirrored)
         {
            BaseLinkTransform = totalTransform.Multiply(MirrorTransform);
         }
         else
         {
            BaseLinkTransform = totalTransform;
         }
      }

      /// <summary>
      /// Checks if we are currently exporting a linked instance.
      /// </summary>
      /// <returns>True if we are.</returns>
      public bool ExportingLink()
      {
         return !MathUtil.IsInvalidElementId(LinkId);
      }

      /// <summary>
      /// Creates a filter for the current link instance, if it exists.
      /// </summary>
      /// <param name="filterView">The current view for the filter.</param>
      /// <returns>A FilteredElementCollector if we are exporting a link, or null if not.</returns>
      public FilteredElementCollector CreateFilter(View filterView)
      {
         if (MathUtil.IsInvalidElementId(LinkId) || filterView == null)
            return null;

         return new FilteredElementCollector(filterView.Document, filterView.Id, LinkId);
      }

      /// <summary>
      /// Creates a LinkElementId that identifies an element of the current linked document from
      /// the host document's point of view.
      /// </summary>
      /// <param name="linkedElementId">The id of the element in the linked document.</param>
      /// <returns>The LinkElementId, or null if we are not currently exporting a link.</returns>
      /// <remarks>Used to access information that the host document stores for linked elements,
      /// such as extended properties.</remarks>
      public LinkElementId GetLinkElementId(ElementId linkedElementId)
      {
         if (!ExportingLink() || MathUtil.IsInvalidElementId(linkedElementId))
            return null;

         return new LinkElementId(LinkId, linkedElementId);
      }

      /// <summary>
      /// The id of the link instance.
      /// </summary>
      /// <remarks>This is private to discourage direct access to element information.</remarks>
      private ElementId LinkId { get; set; } = ElementId.InvalidElementId;

      /// <summary>
      /// The transform associated with this link without reflection.
      /// </summary>
      /// <remarks>If IsMirrored is true, the original transform is the BaseLinkTransform multiplied
      /// by the MirrorTransform.  If IsMirrored is false, the original transform is the BaseLinkTransform.</remarks>
      public Transform BaseLinkTransform { get; private set; } = Transform.Identity;

      /// <summary>
      /// True if the link has a reflection component, false otherwise.
      /// </summary>
      public bool IsMirrored { get; private set; } = false;

      /// <summary>
      /// Static access to the mirrored transform to apply to elements inside of a mirrored link instance.
      /// </summary>
      public static Transform MirrorTransform { get; set; } = null;
   }

   /// <summary>
   /// Manages state information for the current export session.  Intended to eventually replace 
   /// ExporterIFC for most state operations.
   /// </summary>
   public class ExporterStateManager
   {
      static IList<string> CADLayerOverrides { get; set; } = new List<string>();

      static int RangeIndex { get; set; }

      /// <summary>
      /// A utility class that manages keeping track of a sub-element index for ranges for splitting walls and columns.  
      /// Intended to be using with "using" keyword.
      /// </summary>
      public class RangeIndexSetter : IDisposable
      {
         /// <summary>
         /// Increment the range index.
         /// </summary>
         public void IncreaseRangeIndex()
         {
            RangeIndex++;
         }

         /// <summary>
         /// Return the maximum allowed number of stable GUIDs for elements split by range.
         /// </summary>
         /// <returns>The maximum allowed number of stable GUIDs for elements split by range. </returns>
         static public int GetMaxStableGUIDs()
         {
            const int maxSplitIndices = IFCGenericSubElements.SplitInstanceEnd - IFCGenericSubElements.SplitInstanceStart + 1;
            return maxSplitIndices;
         }

         #region IDisposable Members

         /// <summary>
         /// Reset the range index.
         /// </summary>
         public void Dispose()
         {
            RangeIndex = 0;
         }

         #endregion
      }

      /// <summary>
      /// Skip the "CanElementBeExported" function for cached elements that have already passed the test.
      /// </summary>
      public static bool CanExportElementOverride { get; private set; } = false;

      /// <summary>
      /// A utility class that skips the "CanElementBeExported" function for cached elements that have already passed the test.
      /// </summary>
      public class ForceElementExport : IDisposable
      {
         bool OldCanExportElementOverride { get; set; } = false;

         /// <summary>
         /// The constructor that sets forced element export to be true.
         /// </summary>
         public ForceElementExport()
         {
            OldCanExportElementOverride = CanExportElementOverride;
            CanExportElementOverride = true;
         }

         /// <summary>
         /// The destructor that sets forced element export to be false.
         /// </summary>
         public void Dispose()
         {
            CanExportElementOverride = OldCanExportElementOverride;
         }
      }

      public static FederatedLinkManager FederatedLinkManager { get; } = new();

      public class FederatedLinkManagerSetter : IDisposable
      {
         public FederatedLinkManagerSetter(ElementId linkId, Transform linkTransform)
         {
            FederatedLinkManager.Update(linkId, linkTransform);
         }

         public void Dispose()
         {
            FederatedLinkManager.Update(ElementId.InvalidElementId);
         }
      }

      /// <summary>
      /// A utility class that manages pushing and popping CAD layer overrides for containers.  Intended to be using with "using" keyword.
      /// </summary>
      public class CADLayerOverrideSetter : IDisposable
      {
         bool ValidString = false;

         /// <summary>
         /// The constructor that sets the current CAD layer override string.  Will do nothing if the string in invalid or null.
         /// </summary>
         /// <param name="overrideString">The value.</param>
         public CADLayerOverrideSetter(string overrideString)
         {
            if (!string.IsNullOrWhiteSpace(overrideString))
            {
               ExporterStateManager.PushCADLayerOverride(overrideString);
               ValidString = true;
            }
         }

         #region IDisposable Members

         /// <summary>
         /// Pop the current CAD layer override string, if valid.
         /// </summary>
         public void Dispose()
         {
            if (ValidString)
            {
               ExporterStateManager.PopCADLayerOverride();
            }
         }

         #endregion
      }

      static private void PushCADLayerOverride(string overrideString)
      {
         CADLayerOverrides.Add(overrideString);
      }

      static private void PopCADLayerOverride()
      {
         int size = CADLayerOverrides.Count;
         if (size > 0)
            CADLayerOverrides.RemoveAt(size - 1);
      }

      /// <summary>
      /// Get the current CAD layer override string.
      /// </summary>
      /// <returns>The CAD layer override string, or null if not set.</returns>
      static public string GetCurrentCADLayerOverride()
      {
         if (CADLayerOverrides.Count > 0)
         {
            // Should this be 0 or Count-1?
            return CADLayerOverrides[0];
         }
         return null;
      }

      /// <summary>
      /// Get the current range index.
      /// </summary>
      /// <returns>The current range index, or 0 if there are no ranges.</returns>
      static public int GetCurrentRangeIndex()
      {
         return RangeIndex;
      }

      /// <summary>
      /// Resets the state manager.
      /// </summary>
      static public void Clear()
      {
         CADLayerOverrides.Clear();
         RangeIndex = 0;
         FederatedLinkManager.Update(ElementId.InvalidElementId);
      }
   }
}
