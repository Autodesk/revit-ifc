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
   public class IFCBooleanResult : IFCRepresentationItem, IIFCBooleanOperand
   {
      IFCBooleanOperator? m_BooleanOperator = null;

      IIFCBooleanOperand m_FirstOperand;

      IIFCBooleanOperand m_SecondOperand;

      /// <summary>
      /// The boolean operator.
      /// </summary>
      public IFCBooleanOperator? BooleanOperator
      {
         get { return m_BooleanOperator; }
         protected set { m_BooleanOperator = value; }
      }

      /// <summary>
      /// The first boolean operand.
      /// </summary>
      public IIFCBooleanOperand FirstOperand
      {
         get { return m_FirstOperand; }
         protected set { m_FirstOperand = value; }
      }

      /// <summary>
      /// The second boolean operand.
      /// </summary>
      public IIFCBooleanOperand SecondOperand
      {
         get { return m_SecondOperand; }
         protected set { m_SecondOperand = value; }
      }

      protected IFCBooleanResult()
      {
      }

      override protected void Process(IFCAnyHandle item)
      {
         base.Process(item);

         IFCBooleanOperator? booleanOperator = IFCEnums.GetSafeEnumerationAttribute<IFCBooleanOperator>(item, "Operator");
         if (booleanOperator.HasValue)
            BooleanOperator = booleanOperator.Value;

         IFCAnyHandle firstOperand = IFCImportHandleUtil.GetRequiredInstanceAttribute(item, "FirstOperand", true);
         FirstOperand = IFCBooleanOperand.ProcessIFCBooleanOperand(firstOperand);

         IFCAnyHandle secondOperand = IFCImportHandleUtil.GetRequiredInstanceAttribute(item, "SecondOperand", true);


         // We'll allow a solid to be created even if the second operand can't be properly handled.
         try
         {
            SecondOperand = IFCBooleanOperand.ProcessIFCBooleanOperand(secondOperand);
         }
         catch (Exception ex)
         {
            SecondOperand = null;
            Importer.TheLog.LogError(secondOperand.StepId, ex.Message, false);
         }
      }

      /// <summary>
      /// Get the styled item corresponding to the solid inside of an IFCRepresentationItem.
      /// </summary>
      /// <param name="repItem">The representation item.</param>
      /// <returns>The corresponding IFCStyledItem, or null if not found.</returns>
      /// <remarks>This function is intended to work on an IFCBooleanResult with an arbitrary number of embedded
      /// clipping operations.  We will take the first StyledItem that corresponds to either an IFCBooleanResult,
      /// or the contained solid.  We explicitly do not want any material associated specifically with the void.</remarks>
      private IFCStyledItem GetStyledItemFromOperand(IFCRepresentationItem repItem)
      {
         if (repItem == null)
            return null;

         if (repItem.StyledByItem != null)
            return repItem.StyledByItem;

         if (repItem is IFCBooleanResult)
         {
            IIFCBooleanOperand firstOperand = (repItem as IFCBooleanResult).FirstOperand;
            if (firstOperand is IFCRepresentationItem)
               return GetStyledItemFromOperand(firstOperand as IFCRepresentationItem);
         }

         return null;
      }

      /// <summary>
      /// Return geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      /// <returns>The created geometry.</returns>
      public IList<GeometryObject> CreateGeometry(
            IFCImportShapeEditScope shapeEditScope, Transform scaledLcs, string guid)
      {
         IList<GeometryObject> firstSolids = FirstOperand.CreateGeometry(shapeEditScope, scaledLcs, guid);

         if (firstSolids != null)
         {
            foreach (GeometryObject potentialSolid in firstSolids)
            {
               if (!(potentialSolid is Solid))
               {
                  Importer.TheLog.LogError((FirstOperand as IFCRepresentationItem).Id, "Can't perform Boolean operation on a Mesh.", false);
                  return firstSolids;
               }
            }
         }

         IList<GeometryObject> secondSolids = null;
         if ((firstSolids != null || BooleanOperator == IFCBooleanOperator.Union) && (SecondOperand != null))
         {
            try
            {
               using (IFCImportShapeEditScope.BuildPreferenceSetter setter =
                   new IFCImportShapeEditScope.BuildPreferenceSetter(shapeEditScope, IFCImportShapeEditScope.BuildPreferenceType.ForceSolid))
               {
                  // Before we process the second operand, we are going to see if there is a uniform material set for the first operand 
                  // (corresponding to the solid in the Boolean operation).  We will try to suggest the same material for the voids to avoid arbitrary
                  // setting of material information for the cut faces.
                  IFCStyledItem firstOperandStyledItem = GetStyledItemFromOperand(FirstOperand as IFCRepresentationItem);
                  using (IFCImportShapeEditScope.IFCMaterialStack stack =
                      new IFCImportShapeEditScope.IFCMaterialStack(shapeEditScope, firstOperandStyledItem, null))
                  {
                     secondSolids = SecondOperand.CreateGeometry(shapeEditScope, scaledLcs, guid);
                  }
               }
            }
            catch (Exception ex)
            {
               // We will allow something to be imported, in the case where the second operand is invalid.
               // If the first (base) operand is invalid, we will still fail the import of this solid.
               if (SecondOperand is IFCRepresentationItem)
                  Importer.TheLog.LogError((SecondOperand as IFCRepresentationItem).Id, ex.Message, false);
               else
                  throw;
               secondSolids = null;
            }
         }

         IList<GeometryObject> resultSolids = null;
         if (firstSolids == null)
         {
            if (BooleanOperator == IFCBooleanOperator.Union)
               resultSolids = secondSolids;
         }
         else if (secondSolids == null || BooleanOperator == null)
         {
            if (BooleanOperator == null)
               Importer.TheLog.LogError(Id, "Invalid BooleanOperationsType.", false);
            resultSolids = firstSolids;
         }
         else
         {
            BooleanOperationsType booleanOperationsType = BooleanOperationsType.Difference;
            switch (BooleanOperator)
            {
               case IFCBooleanOperator.Difference:
                  booleanOperationsType = BooleanOperationsType.Difference;
                  break;
               case IFCBooleanOperator.Intersection:
                  booleanOperationsType = BooleanOperationsType.Intersect;
                  break;
               case IFCBooleanOperator.Union:
                  booleanOperationsType = BooleanOperationsType.Union;
                  break;
               default:
                  Importer.TheLog.LogError(Id, "Invalid BooleanOperationsType.", true);
                  break;
            }

            resultSolids = new List<GeometryObject>();
            foreach (GeometryObject firstSolid in firstSolids)
            {
               Solid resultSolid = (firstSolid as Solid);

               int secondId = (SecondOperand == null) ? -1 : (SecondOperand as IFCRepresentationItem).Id;
               XYZ suggestedShiftDirection = GetSuggestedShiftDirection(scaledLcs);
               foreach (GeometryObject secondSolid in secondSolids)
               {
                  resultSolid = IFCGeometryUtil.ExecuteSafeBooleanOperation(Id, secondId, resultSolid, secondSolid as Solid, booleanOperationsType, suggestedShiftDirection);
                  if (resultSolid == null)
                     break;
               }

               if (resultSolid != null)
                  resultSolids.Add(resultSolid);
            }
         }

         return resultSolids;
      }

      /// <summary>
      /// Create geometry for a particular representation item, and add to scope.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform scaledLcs, string guid)
      {
         base.CreateShapeInternal(shapeEditScope, scaledLcs, guid);

         IList<GeometryObject> resultGeometries = CreateGeometry(shapeEditScope, scaledLcs, guid);
         if (resultGeometries != null)
         {
            foreach (GeometryObject resultGeometry in resultGeometries)
            {
               shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, resultGeometry));
            }
         }
      }

      protected IFCBooleanResult(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Create an IFCBooleanResult object from a handle of type IfcBooleanResult.
      /// </summary>
      /// <param name="ifcBooleanResult">The IFC handle.</param>
      /// <returns>The IFCBooleanResult object.</returns>
      public static IFCBooleanResult ProcessIFCBooleanResult(IFCAnyHandle ifcBooleanResult)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcBooleanResult))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcBooleanResult);
            return null;
         }

         IFCEntity booleanResult;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcBooleanResult.StepId, out booleanResult))
            booleanResult = new IFCBooleanResult(ifcBooleanResult);
         return (booleanResult as IFCBooleanResult);
      }

      /// <summary>
      /// In case of a Boolean operation failure, provide a recommended direction to shift the geometry in for a second attempt.
      /// </summary>
      /// <param name="lcs">The local transform for this entity.</param>
      /// <returns>An XYZ representing a unit direction vector, or null if no direction is suggested.</returns>
      /// <remarks>If the 2nd attempt fails, a third attempt will be done with a shift in the opposite direction.</remarks>
      public XYZ GetSuggestedShiftDirection(Transform lcs)
      {
         XYZ suggestedXYZ = (SecondOperand == null) ? null : SecondOperand.GetSuggestedShiftDirection(lcs);
         if (suggestedXYZ == null)
            suggestedXYZ = (FirstOperand == null) ? null : FirstOperand.GetSuggestedShiftDirection(lcs);
         return suggestedXYZ;

      }
   }
}