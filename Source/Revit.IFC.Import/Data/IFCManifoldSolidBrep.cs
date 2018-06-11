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
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCManifoldSolidBrep : IFCSolidModel, IIFCBooleanOperand
   {
      IFCClosedShell m_Outer = null;

      /// <summary>
      /// The outer shell of the solid.
      /// </summary>
      public IFCClosedShell Outer
      {
         get { return m_Outer; }
         protected set { m_Outer = value; }
      }

      protected IFCManifoldSolidBrep()
      {
      }

      /// <summary>
      /// Return geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The shape edit scope.</param>
      /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      /// <returns>The created geometry.</returns>
      protected override IList<GeometryObject> CreateGeometryInternal(
         IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         if (Outer == null || Outer.Faces.Count == 0)
            return null;

         IList<GeometryObject> geomObjs = null;
         bool canRevertToMesh = false;

         using (BuilderScope bs = shapeEditScope.InitializeBuilder(IFCShapeBuilderType.TessellatedShapeBuilder))
         {
            TessellatedShapeBuilderScope tsBuilderScope = bs as TessellatedShapeBuilderScope;

            tsBuilderScope.StartCollectingFaceSet();
            Outer.CreateShape(shapeEditScope, lcs, scaledLcs, guid);

            if (tsBuilderScope.CreatedFacesCount == Outer.Faces.Count)
            {
               geomObjs = tsBuilderScope.CreateGeometry(guid);
            }

            canRevertToMesh = tsBuilderScope.CanRevertToMesh();
         }


         if (geomObjs == null || geomObjs.Count == 0)
         {
            if (canRevertToMesh)
            {
               using (IFCImportShapeEditScope.BuildPreferenceSetter setter =
                   new IFCImportShapeEditScope.BuildPreferenceSetter(shapeEditScope, IFCImportShapeEditScope.BuildPreferenceType.AnyMesh))
               {
                  using (BuilderScope newBuilderScope = shapeEditScope.InitializeBuilder(IFCShapeBuilderType.TessellatedShapeBuilder))
                  {
                     TessellatedShapeBuilderScope newTsBuilderScope = newBuilderScope as TessellatedShapeBuilderScope;
                     // Let's see if we can loosen the requirements a bit, and try again.
                     newTsBuilderScope.StartCollectingFaceSet();

                     Outer.CreateShape(shapeEditScope, lcs, scaledLcs, guid);

                     // This needs to be in scope so that we keep the mesh tolerance for vertices.
                     if (newTsBuilderScope.CreatedFacesCount != 0)
                     {
                        if (newTsBuilderScope.CreatedFacesCount != Outer.Faces.Count)
                           Importer.TheLog.LogWarning
                               (Outer.Id, "Processing " + newTsBuilderScope.CreatedFacesCount + " valid faces out of " + Outer.Faces.Count + " total.", false);

                        geomObjs = newTsBuilderScope.CreateGeometry(guid);
                     }

                  }
               }
            }
         }

         if (geomObjs == null || geomObjs.Count == 0)
         {
            // Couldn't use fallback, or fallback didn't work.
            Importer.TheLog.LogWarning(Id, "Couldn't create any geometry.", false);
            return null;
         }

         return geomObjs;
      }

      override protected void Process(IFCAnyHandle ifcManifoldSolidBrep)
      {
         base.Process(ifcManifoldSolidBrep);

         // We will not fail if the transform is not given, but instead assume it to be the identity.
         IFCAnyHandle ifcOuter = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcManifoldSolidBrep, "Outer", true);
         Outer = IFCClosedShell.ProcessIFCClosedShell(ifcOuter);


      }

      /// <summary>
      /// Create geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, guid);

         // Ignoring Inner shells for now.
         if (Outer != null)
         {
            try
            {
               IList<GeometryObject> solids = CreateGeometry(shapeEditScope, lcs, scaledLcs, guid);
               if (solids != null)
               {
                  foreach (GeometryObject solid in solids)
                  {
                     shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, solid));
                  }
               }
               else
                  Importer.TheLog.LogError(Outer.Id, "cannot create valid solid, ignoring.", false);
            }
            catch (Exception ex)
            {
               Importer.TheLog.LogError(Outer.Id, ex.Message, false);
            }
         }
      }

      protected IFCManifoldSolidBrep(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Create an IFCManifoldSolidBrep object from a handle of type IfcManifoldSolidBrep.
      /// </summary>
      /// <param name="ifcManifoldSolidBrep">The IFC handle.</param>
      /// <returns>The IFCManifoldSolidBrep object.</returns>
      public static IFCManifoldSolidBrep ProcessIFCManifoldSolidBrep(IFCAnyHandle ifcManifoldSolidBrep)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcManifoldSolidBrep))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcManifoldSolidBrep);
            return null;
         }

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcManifoldSolidBrep, IFCEntityType.IfcFacetedBrep))
            return IFCFacetedBrep.ProcessIFCFacetedBrep(ifcManifoldSolidBrep);
         if (IFCImportFile.TheFile.SchemaVersion > IFCSchemaVersion.IFC2x3 && IFCAnyHandleUtil.IsSubTypeOf(ifcManifoldSolidBrep, IFCEntityType.IfcAdvancedBrep))
            return IFCAdvancedBrep.ProcessIFCAdvancedBrep(ifcManifoldSolidBrep);

         IFCEntity manifoldSolidBrep;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcManifoldSolidBrep.StepId, out manifoldSolidBrep))
            manifoldSolidBrep = new IFCManifoldSolidBrep(ifcManifoldSolidBrep);
         return (manifoldSolidBrep as IFCManifoldSolidBrep);
      }
   }
}