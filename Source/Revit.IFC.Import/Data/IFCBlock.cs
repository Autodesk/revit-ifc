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
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCBlock : IFCCsgPrimitive3D
   {
      /// <summary>
      /// The size of the block along the placement X axis.
      /// </summary>
      public double XLength { get; protected set; } = 0.0;

      /// <summary>
      /// The size of the block along the placement Y axis.
      /// </summary>
      public double YLength { get; protected set; } = 0.0;

      /// <summary>
      /// The size of the block along the placement Z axis.
      /// </summary>
      public double ZLength { get; protected set; } = 0.0;
      
      protected IFCBlock()
      {
      }

      protected override IList<GeometryObject> CreateGeometryInternal(
         IFCImportShapeEditScope shapeEditScope, Transform scaledLcs, string guid)
      {
         if (XLength < MathUtil.Eps() || YLength < MathUtil.Eps() || ZLength < MathUtil.Eps())
            return null;

         Transform scaledExtrusionPosition = (scaledLcs == null) ? Transform.Identity : scaledLcs;

         XYZ llPoint = scaledExtrusionPosition.OfPoint(XYZ.Zero);
         XYZ lrPoint = scaledExtrusionPosition.OfPoint(new XYZ(XLength, 0, 0));
         XYZ urPoint = scaledExtrusionPosition.OfPoint(new XYZ(XLength, YLength, 0));
         XYZ ulPoint = scaledExtrusionPosition.OfPoint(new XYZ(0, YLength, 0));

         CurveLoop outerLoop = new CurveLoop();
         outerLoop.Append(Line.CreateBound(llPoint, lrPoint));
         outerLoop.Append(Line.CreateBound(lrPoint, urPoint));
         outerLoop.Append(Line.CreateBound(urPoint, ulPoint));
         outerLoop.Append(Line.CreateBound(ulPoint, llPoint));

         IList<CurveLoop> loops = new List<CurveLoop>();
         loops.Add(outerLoop);

         XYZ scaledExtrusionDirection = scaledExtrusionPosition.OfVector(XYZ.BasisZ);
         SolidOptions solidOptions = new SolidOptions(GetMaterialElementId(shapeEditScope), shapeEditScope.GraphicsStyleId);

         GeometryObject block = null;
         try
         {
            block = GeometryCreationUtilities.CreateExtrusionGeometry(loops, scaledExtrusionDirection, ZLength, solidOptions);
         }
         catch (Exception)
         {
            if (shapeEditScope.MustCreateSolid())
               throw;

            Importer.TheLog.LogError(Id, "Block has an invalid definition for a solid; reverting to mesh.", false);

            MeshFromGeometryOperationResult meshResult = TessellatedShapeBuilder.CreateMeshByExtrusion(
               loops, scaledExtrusionDirection, ZLength, GetMaterialElementId(shapeEditScope));

            // will throw if mesh is not available
            block = meshResult.GetMesh();
         }

         IList<GeometryObject> blocks = new List<GeometryObject>();
         if (block != null)
            blocks.Add(block);

         return blocks;
      }

      /// <summary>
      /// Create geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, 
         Transform scaledLcs, string guid)
      {
         base.CreateShapeInternal(shapeEditScope, scaledLcs, guid);

         IList<GeometryObject> blockGeometries = CreateGeometryInternal(shapeEditScope, scaledLcs, guid);
         if (blockGeometries != null)
         {
            foreach (GeometryObject blockGeometry in blockGeometries)
            {
               shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, blockGeometry));
            }
         }
      }

      override protected void Process(IFCAnyHandle ifcBlock)
      {
         base.Process(ifcBlock);

         bool found = false;
         XLength = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcBlock, "XLength", out found);
         if (!found)
         {
            Importer.TheLog.LogError(ifcBlock.StepId, "Cannot find the X length of this block.", false);
            return;
         }

         YLength = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcBlock, "YLength", out found);
         if (!found)
         {
            Importer.TheLog.LogError(ifcBlock.StepId, "Cannot find the Y length of this block.", false);
            return;
         }

         ZLength = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(ifcBlock, "ZLength", out found);
         if (!found)
         {
            Importer.TheLog.LogError(ifcBlock.StepId, "Cannot find the Z length of this block.", false);
            return;
         }
      }

      protected IFCBlock(IFCAnyHandle ifcBlock)
      {
         Process(ifcBlock);
      }

      /// <summary>
      /// Create an IFCBlock object from a handle of type ifcBlock.
      /// </summary>
      /// <param name="ifcBlock">The IFC handle.</param>
      /// <returns>The IFCBlock object.</returns>
      public static IFCBlock ProcessIFCBlock(IFCAnyHandle ifcBlock)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcBlock))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcBlock);
            return null;
         }

         IFCEntity block;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcBlock.StepId, out block))
            block = new IFCBlock(ifcBlock);

         return (block as IFCBlock);
      }
   }
}