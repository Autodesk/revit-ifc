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

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Utility
{
   /// <summary>
   /// Class to encapsulate information about Site Export.
   /// This includes both SiteHandle (STEP in IFC File) and SiteElementId (Site within Revit).
   /// </summary>
   public class SiteExportInfo
   {

      /// <summary>
      /// Handle for IfcSite, once it has been exported.
      /// </summary>
      public IFCAnyHandle SiteHandle { get; set; } = null;

      /// <summary>
      /// ElementId corresponding to IfcSite.  If IfcSite corresponds to no Element (i.e., the "Default Site), then this should
      /// remain ElementId.InvalidElementId.
      /// </summary>
      public ElementId SiteElementId { get; set; } = ElementId.InvalidElementId;

      /// <summary>
      /// ElementId that can potentially be the Main Site Element.  Only one is checked at a given time.
      /// </summary>
      public ElementId PotentialSiteElementId { get; set;} = ElementId.InvalidElementId;

      /// <summary>
      /// Helper method to indicate if IfcSite has been exported.
      /// </summary>
      /// <returns>True if IfcSite exported, false otherwise.</returns>
      public bool IsSiteExported() => !IFCAnyHandleUtil.IsNullOrHasNoValue(SiteHandle);

      /// <summary>
      /// Helper method to indicate if IFC File is using the "Default" IfcSite.
      /// </summary>
      /// <returns>True if using "Default" IfcSite, false otherwise.</returns>
      public bool UsingDefaultSite() => IsSiteExported() && SiteElementId == ElementId.InvalidElementId;

      /// <summary>
      /// Helper method to indicate if IFC File is using an IfcSite corresponding to an Element (i.e., not the "Default" IfcSite).
      /// </summary>
      /// <returns>True if corresponds to an Element, false otherwise.</returns>
      public bool UsingElementSite() => IsSiteExported() && SiteElementId != ElementId.InvalidElementId;

      /// <summary>
      /// Convenience Function to indicate whether the given ElementId is the SiteElementId (positive identificatioN) or not.
      /// </summary>
      /// <param name="elementId">Element to check.</param>
      /// <returns>True if positively Element has been positively identified as the main Site, False otherwise.</returns>
      public bool IsSiteElementId(ElementId elementId) => SiteElementId == elementId;

      /// <summary>
      /// Convenience Function to indicate whether the given ElementId can potentially be the Main Site.
      /// </summary>
      /// <param name="elementId">ElementId to check.</param>
      /// <returns>True if the Element can be the Main Site, False otherwise.</returns>
      public bool IsPotentialSiteElementId(ElementId elementId) => PotentialSiteElementId == elementId;

      /// <summary>
      /// Establishes the current "Potential" Main Site Element as the Main Site Element.
      /// </summary>
      public void EstablishPotentialSiteElement()
      {
         SiteElementId = PotentialSiteElementId;
         PotentialSiteElementId = ElementId.InvalidElementId;
      }

      /// <summary>
      /// Resets the Site Information for another Export.
      /// </summary>
      public void Clear()
      {
         SiteHandle = null;
         SiteElementId = ElementId.InvalidElementId;
         PotentialSiteElementId = ElementId.InvalidElementId;
      }
   }
}
