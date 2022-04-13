//
// Revit IFC Import library: this library works with Autodesk(R) Revit(R) to import IFC files.
// Copyright (C) 2013  Autodesk, Inc.
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
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents IfcProductRepresentation and IfcMaterialDefinitionRepresentation.
   /// </summary>
   public class IFCProductRepresentation : IFCEntity
   {
      private string m_Name = null;

      private string m_Description = null;

      private IList<IFCRepresentation> m_Representations = null;

      // For IfcMaterialDefinitionRepresentation only.
      private IFCMaterial m_RepresentedMaterial = null;

      /// <summary>
      /// The optional name of the IfcProductRepresentation.
      /// </summary>
      public string Name
      {
         get { return m_Name; }
         protected set { m_Name = value; }
      }

      /// <summary>
      /// The optional description of the IfcProductRepresentation.
      /// </summary>
      public string Description
      {
         get { return m_Description; }
         protected set { m_Description = value; }
      }

      /// <summary>
      /// The representations of the IfcProductRepresentation.
      /// </summary>
      public IList<IFCRepresentation> Representations
      {
         get
         {
            if (m_Representations == null)
               m_Representations = new List<IFCRepresentation>();
            return m_Representations;
         }
      }

      public IFCMaterial RepresentedMaterial
      {
         get { return m_RepresentedMaterial; }
         protected set { m_RepresentedMaterial = value; }
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCProductRepresentation()
      {

      }

      protected IFCProductRepresentation(IFCAnyHandle ifcProductRepresentation)
      {
         Process(ifcProductRepresentation);
      }

      /// <summary>
      /// Processes IfcProductRepresentation attributes.
      /// </summary>
      /// <param name="ifcProductRepresentation">The IfcProductRepresentation handle.</param>
      protected override void Process(IFCAnyHandle ifcProductRepresentation)
      {
         base.Process(ifcProductRepresentation);

         Name = IFCImportHandleUtil.GetOptionalStringAttribute(ifcProductRepresentation, "Name", null);

         Description = IFCImportHandleUtil.GetOptionalStringAttribute(ifcProductRepresentation, "Description", null);

         List<IFCAnyHandle> representations =
             IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcProductRepresentation, "Representations");
         if (representations != null)
         {
            foreach (IFCAnyHandle representationHnd in representations)
            {
               try
               {
                  IFCRepresentation representation = IFCRepresentation.ProcessIFCRepresentation(representationHnd);
                  if (representation != null)
                  {
                     if (representation.RepresentationItems.Count > 0 || representation.BoundingBox != null)
                        Representations.Add(representation);
                  }
               }
               catch (Exception ex)
               {
                  string msg = ex.Message;
                  // Ignore some specific errors.
                  if (msg != null)
                  {
                     if (!msg.Contains("not imported"))
                        Importer.TheLog.LogError(Id, msg, false);
                  }
               }
            }
         }

         if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC2x3))
         {
            if (IFCAnyHandleUtil.IsSubTypeOf(ifcProductRepresentation, IFCEntityType.IfcMaterialDefinitionRepresentation))
            {
               IFCAnyHandle representedMaterial = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcProductRepresentation, "RepresentedMaterial", false);
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(representedMaterial))
                  RepresentedMaterial = IFCMaterial.ProcessIFCMaterial(representedMaterial);
            }
         }
      }

      /// <summary>
      /// Returns true if there is anything to create.
      /// </summary>
      /// <returns>Returns true if there is anything to create, false otherwise.</returns>
      public bool IsValid()
      {
         // TODO: We are not creating a shape if there is no representation for the shape.  We may allow this for specific entity types,
         // such as doors or windows.
         return (Representations != null && Representations.Count != 0);
      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      public void CreateProductRepresentation(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         // Partially sort the representations so that we create: Body, Box, then the rest of the representations in that order.
         // This allows us to skip Box representations if any of the Body representations create 3D geometry.  Until we have UI in place, 
         // this will disable creating extra 3D (bounding box) geometry that clutters the display, is only marginally useful and is hard to turn off.
         List<IFCRepresentation> sortedReps = new List<IFCRepresentation>(); // Double usage as body rep list.
         IList<IFCRepresentation> fallbackReps = new List<IFCRepresentation>();
         IList<IFCRepresentation> boxReps = new List<IFCRepresentation>();
         IList<IFCRepresentation> otherReps = new List<IFCRepresentation>();

         foreach (IFCRepresentation representation in Representations)
         {
            switch (representation.Identifier)
            {
               case IFCRepresentationIdentifier.Body:
                  sortedReps.Add(representation);
                  break;
               case IFCRepresentationIdentifier.BodyFallback:
                  fallbackReps.Add(representation);
                  break;
               case IFCRepresentationIdentifier.Box:
                  boxReps.Add(representation);
                  break;
               default:
                  otherReps.Add(representation);
                  break;
            }
         }

         // Add back the other representations.
         sortedReps.AddRange(fallbackReps);
         sortedReps.AddRange(boxReps);
         sortedReps.AddRange(otherReps);

         foreach (IFCRepresentation representation in sortedReps)
         {
            // Only process fallback geometry if we didn't process the Body geometry.
            if ((representation.Identifier == IFCRepresentationIdentifier.BodyFallback) &&
               shapeEditScope.Creator.Solids.Count > 0)
               continue;

            // Since we process all Body representations first, the misnamed "Solids" field will contain 3D geometry.
            // If this isn't empty, then we'll skip the bounding box, unless we are always importing bounding box geometry.
            // Note that we process Axis representations later since they create model geometry also,
            // but we don't consider Axis or 2D geometry in our decision to import bounding boxes.  
            // Note also that we will only read in the first bounding box, which is the maximum of Box representations allowed.
            if ((representation.Identifier == IFCRepresentationIdentifier.Box) &&
               IFCImportFile.TheFile.Options.ProcessBoundingBoxGeometry != IFCProcessBBoxOptions.Always &&
               shapeEditScope.Creator.Solids.Count > 0)
               continue;

            representation.CreateShape(shapeEditScope, lcs, scaledLcs, guid);
         }
      }

      /// <summary>
      /// Gets the IFCSurfaceStyle, if available, for an associated material.
      /// TODO: Make this generic, add warnings.
      /// </summary>
      /// <returns>The IFCSurfaceStyle.</returns>
      public IFCSurfaceStyle GetSurfaceStyle()
      {
         IList<IFCRepresentation> representations = Representations;
         if (representations != null && representations.Count > 0)
         {
            IFCRepresentation representation = representations[0];
            IList<IFCRepresentationItem> representationItems = representation.RepresentationItems;
            if (representationItems != null && representationItems.Count > 0 && (representationItems[0] is IFCStyledItem))
            {
               IFCStyledItem styledItem = representationItems[0] as IFCStyledItem;
               return styledItem.GetSurfaceStyle();
            }
         }

         return null;
      }

      /// <summary>
      /// Processes an IfcProductRepresentation object.
      /// </summary>
      /// <param name="ifcProductRepresentation">The IfcProductRepresentation handle.</param>
      /// <returns>The IFCProductRepresentation object.</returns>
      public static IFCProductRepresentation ProcessIFCProductRepresentation(IFCAnyHandle ifcProductRepresentation)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcProductRepresentation))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcProductRepresentation);
            return null;
         }

         IFCEntity cachedProductRepresentation;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcProductRepresentation.StepId, out cachedProductRepresentation))
            return (cachedProductRepresentation as IFCProductRepresentation);

         return new IFCProductRepresentation(ifcProductRepresentation);
      }
   }
}