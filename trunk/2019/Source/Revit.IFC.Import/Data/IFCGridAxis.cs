﻿//
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
   /// Represents an IfcGridAxis, which corresponds to a Revit Grid element.
   /// </summary>
   /// <remarks>This will translate into a Revit Grid element, that will use the default
   /// Grid type from the template file associated with this import.  As such, we do
   /// not guarantee that the grid lines will look the same as in the original application,
   /// but they should be in the right place and orientation.</remarks>
   public class IFCGridAxis : IFCEntity
   {
      private string m_AxisTag = null;

      private IFCCurve m_AxisCurve = null;

      private bool m_SameSense = true;

      // If this value is set, then this axis is actually a duplicate of an already created axis.  
      // Use the original axis instead.
      private int m_DuplicateAxisId = -1;

      private ElementId m_CreatedElementId = ElementId.InvalidElementId;

      /// <summary>
      /// The optional tag for this grid line.
      /// </summary>
      public string AxisTag
      {
         get { return m_AxisTag; }
         protected set { m_AxisTag = value; }
      }

      /// <summary>
      /// The underlying curve for the grid line.
      /// </summary>
      public IFCCurve AxisCurve
      {
         get { return m_AxisCurve; }
         protected set { m_AxisCurve = value; }
      }

      /// <summary>
      /// Whether or not the grid line orientation is the same as the underlying curve, or reversed.
      /// This will determine the default position of the grid head.
      /// </summary>
      public bool SameSense
      {
         get { return m_SameSense; }
         protected set { m_SameSense = value; }
      }

      public int DuplicateAxisId
      {
         get { return m_DuplicateAxisId; }
         protected set { m_DuplicateAxisId = value; }
      }

      /// <summary>
      /// Returns the main element id associated with this object.  Only valid after the call to Create(Document).
      /// </summary>
      public ElementId CreatedElementId
      {
         get { return m_CreatedElementId; }
         protected set { m_CreatedElementId = value; }
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCGridAxis()
      {

      }

      /// <summary>
      /// Constructs an IFCGridAxis from the IfcGridAxis handle.
      /// </summary>
      /// <param name="ifcGridAxis">The IfcGridAxis handle.</param>
      protected IFCGridAxis(IFCAnyHandle ifcGridAxis)
      {
         Process(ifcGridAxis);
      }

      private bool AreLinesEqual(Line line1, Line line2)
      {
         if (!line1.IsBound || !line2.IsBound)
         {
            // Two unbound lines are equal if they are going in the same direction and the origin
            // of one lies on the other one.
            return line1.Direction.IsAlmostEqualTo(line2.Direction) &&
               MathUtil.IsAlmostZero(line1.Project(line2.Origin).Distance);
         }

         for (int ii = 0; ii < 2; ii++)
         {
            if (line1.GetEndPoint(0).IsAlmostEqualTo(line2.GetEndPoint(ii)) &&
                line1.GetEndPoint(1).IsAlmostEqualTo(line2.GetEndPoint(1 - ii)))
               return true;
         }

         return false;
      }

      private bool AreArcsEqual(Arc arc1, Arc arc2)
      {
         if (!arc1.Center.IsAlmostEqualTo(arc2.Center))
            return false;

         double dot = arc1.Normal.DotProduct(arc2.Normal);
         if (!MathUtil.IsAlmostEqual(Math.Abs(dot), 1.0))
            return false;

         int otherIdx = (dot > 0.0) ? 0 : 1;
         if (arc1.GetEndPoint(0).IsAlmostEqualTo(arc2.GetEndPoint(otherIdx)))
            return true;

         return false;
      }

      private int FindMatchingGrid(IList<Curve> otherCurves, int id, ref IList<Curve> curves, ref int curveCount)
      {
         if (curves == null)
         {
            curves = AxisCurve.GetCurves();
            curveCount = curves.Count;
         }

         // Check that the base curves are the same type.
         int otherCurveCount = otherCurves.Count;

         if (curveCount != otherCurveCount)
            return -1;

         bool sameCurves = true;
         for (int ii = 0; (ii < curveCount) && sameCurves; ii++)
         {
            if ((curves[ii] is Line) && (otherCurves[ii] is Line))
               sameCurves = AreLinesEqual(curves[ii] as Line, otherCurves[ii] as Line);
            else if ((curves[ii] is Arc) && (otherCurves[ii] is Arc))
               sameCurves = AreArcsEqual(curves[ii] as Arc, otherCurves[ii] as Arc);
            else
            {
               // No supported.
               sameCurves = false;
            }
         }

         return sameCurves ? id : -1;
      }

      private int FindMatchingGrid(IFCGridAxis gridAxis, ref IList<Curve> curves, ref int curveCount)
      {
         IList<Curve> otherCurves = gridAxis.AxisCurve.GetCurves();
         int id = gridAxis.Id;
         return FindMatchingGrid(otherCurves, id, ref curves, ref curveCount);
      }

      // Revit doesn't allow grid lines to have the same name.  This routine makes a unique variant.
      private string MakeAxisTagUnique(string axisTag)
      {
         // Don't set the name.
         if (string.IsNullOrWhiteSpace(axisTag))
            return null;

         int counter = 2;

         IDictionary<string, IFCGridAxis> gridAxes = IFCImportFile.TheFile.IFCProject.GridAxes;
         do
         {
            string uniqueAxisTag = axisTag + "-" + counter;
            if (!gridAxes.ContainsKey(uniqueAxisTag))
               return uniqueAxisTag;
            counter++;
         }
         while (counter < 1000);

         // Give up; use default name.
         return null;
      }

      // This routine should be unnecessary if we called MakeAxisTagUnique correctly, but better to be safe.
      private void SetAxisTagUnique(Grid grid, string axisTag)
      {
         if (grid != null && axisTag != null)
         {
            int counter = 1;
            do
            {
               try
               {
                  grid.Name = (counter == 1) ? axisTag : axisTag + "-" + counter;
                  break;
               }
               catch
               {
                  counter++;
               }
            }
            while (counter < 1000);

            if (counter >= 1000)
               Importer.TheLog.LogWarning(Id, "Couldn't set name: '" + axisTag + "' for Grid, reverting to default.", false);
         }
      }

      /// <summary>
      /// Processes IfcGridAxis attributes.
      /// </summary>
      /// <param name="ifcGridAxis">The IfcGridAxis handle.</param>
      protected override void Process(IFCAnyHandle ifcGridAxis)
      {
         base.Process(ifcGridAxis);

         AxisTag = IFCImportHandleUtil.GetOptionalStringAttribute(ifcGridAxis, "AxisTag", null);
         if (AxisTag == null)
            AxisTag = "Z";    // arbitrary; all Revit Grids have names.

         IFCAnyHandle axisCurve = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcGridAxis, "AxisCurve", true);
         AxisCurve = IFCCurve.ProcessIFCCurve(axisCurve);

         bool found = false;
         bool sameSense = IFCImportHandleUtil.GetRequiredBooleanAttribute(ifcGridAxis, "SameSense", out found);
         SameSense = found ? sameSense : true;

         // We are going to check if this grid axis is a vertical duplicate of any existing axis.
         // If so, we will throw an exception so that we don't create duplicate grids.
         // We will only initialize these values if we actually intend to use them below.
         IList<Curve> curves = null;
         int curveCount = 0;

         ElementId gridId = ElementId.InvalidElementId;
         if (Importer.TheCache.GridNameToElementMap.TryGetValue(AxisTag, out gridId))
         {
            Grid grid = IFCImportFile.TheFile.Document.GetElement(gridId) as Grid;
            if (grid != null)
            {
               IList<Curve> otherCurves = new List<Curve>();
               Curve gridCurve = grid.Curve;
               if (gridCurve != null)
               {
                  otherCurves.Add(gridCurve);
                  int matchingGridId = FindMatchingGrid(otherCurves, grid.Id.IntegerValue, ref curves, ref curveCount);

                  if (matchingGridId != -1)
                  {
                     Importer.TheCache.UseGrid(grid);
                     CreatedElementId = grid.Id;
                     return;
                  }
               }
            }
         }

         IDictionary<string, IFCGridAxis> gridAxes = IFCImportFile.TheFile.IFCProject.GridAxes;
         IFCGridAxis gridAxis = null;
         if (gridAxes.TryGetValue(AxisTag, out gridAxis))
         {
            int matchingGridId = FindMatchingGrid(gridAxis, ref curves, ref curveCount);
            if (matchingGridId != -1)
            {
               DuplicateAxisId = matchingGridId;
               return;
            }
            else
            {
               // Revit doesn't allow grid lines to have the same name.  If it isn't a duplicate, rename it.
               // Note that this will mean that we may miss some "duplicate" grid lines because of the renaming.
               AxisTag = MakeAxisTagUnique(AxisTag);
            }
         }

         gridAxes.Add(new KeyValuePair<string, IFCGridAxis>(AxisTag, this));
      }

      private Grid CreateArcGridAxis(Document doc, Arc curve)
      {
         if (doc == null || curve == null)
            return null;

         if (curve.IsBound)
            return Grid.Create(doc, curve);

         // Create almost-closed grid line.
         Arc copyCurve = curve.Clone() as Arc;
         copyCurve.MakeBound(0, 2 * Math.PI * (359.0 / 360.0));
         return Grid.Create(doc, copyCurve);
      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="lcs">The local coordinate system transform.</param>
      public void Create(Document doc, Transform lcs)
      {
         if (!IsValidForCreation)
            return;

         // These are hardwired values to ensure that the Grid is visible in the
         // current view, in feet.  Note that there is an assumption that building storeys
         // would not be placed too close to one another; if they are, and they use different
         // grid structures, then the plan views may have overlapping grid lines.  This seems
         // more likely in theory than practice.
         const double bottomOffset = -1.0 / 12.0;    // 1" =   2.54 cm
         const double topOffset = 4.0;               // 4' = 121.92 cm

         double originalZ = (lcs != null) ? lcs.Origin.Z : 0.0;

         if (CreatedElementId != ElementId.InvalidElementId)
         {
            Grid existingGrid = doc.GetElement(CreatedElementId) as Grid;
            if (existingGrid != null)
            {
               Outline outline = existingGrid.GetExtents();
               existingGrid.SetVerticalExtents(Math.Min(originalZ - bottomOffset, outline.MinimumPoint.Z),
                   Math.Max(originalZ + topOffset, outline.MaximumPoint.Z));
            }
            return;
         }

         if (AxisCurve == null)
         {
            Importer.TheLog.LogError(Id, "Couldn't find axis curve for grid line, ignoring.", false);
            IsValidForCreation = false;
            return;
         }

         IList<Curve> curves = AxisCurve.GetCurves();
         int numCurves = curves.Count;
         if (numCurves == 0)
         {
            Importer.TheLog.LogError(AxisCurve.Id, "Couldn't find axis curve for grid line, ignoring.", false);
            IsValidForCreation = false;
            return;
         }

         if (numCurves > 1)
            Importer.TheLog.LogError(AxisCurve.Id, "Found multiple curve segments for grid line, ignoring all but first.", false);

         Grid grid = null;

         Curve curve = curves[0].CreateTransformed(lcs);
         if (curve == null)
         {
            Importer.TheLog.LogError(AxisCurve.Id, "Couldn't create transformed axis curve for grid line, ignoring.", false);
            IsValidForCreation = false;
            return;
         }

         if (!curve.IsBound)
         {
            curve.MakeBound(-100, 100);
            Importer.TheLog.LogWarning(AxisCurve.Id, "Creating arbitrary bounds for unbounded grid line.", false);
         }

         // Grid.create can throw, so catch the exception if it does.
         try
         {
            if (curve is Arc)
            {
               // This will potentially make a small modification in the curve if it is unbounded,
               // as Revit doesn't allow unbounded grid lines.
               grid = CreateArcGridAxis(doc, curve as Arc);
            }
            else if (curve is Line)
               grid = Grid.Create(doc, curve as Line);
            else
            {
               Importer.TheLog.LogError(AxisCurve.Id, "Couldn't create grid line from curve of type " + curve.GetType().ToString() + ", expected line or arc.", false);
               IsValidForCreation = false;
               return;
            }
         }
         catch (Exception ex)
         {
            Importer.TheLog.LogError(AxisCurve.Id, ex.Message, false);
            IsValidForCreation = false;
            return;
         }

         if (grid != null)
         {
            SetAxisTagUnique(grid, AxisTag);

            // We will try to "grid match" as much as possible to avoid duplicate grid lines.  As such,
            // we want the remaining grid lines to extend to the current level.
            // A limitation here is that if a grid axis in the IFC file were visible on Level 1 and Level 3
            // but not Level 2, this will make it visibile on Level 2 also.  As above, this seems
            // more likely in theory than practice.
            grid.SetVerticalExtents(originalZ - bottomOffset, originalZ + topOffset);

            CreatedElementId = grid.Id;
         }
      }

      /// <summary>
      /// Processes an IfcGridAxis object.
      /// </summary>
      /// <param name="ifcGridAxis">The IfcGridAxis handle.</param>
      /// <returns>The IFCGridAxis object.</returns>
      public static IFCGridAxis ProcessIFCGridAxis(IFCAnyHandle ifcGridAxis)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcGridAxis))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcGridAxis);
            return null;
         }

         IFCEntity gridAxis;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcGridAxis.StepId, out gridAxis))
         {
            try
            {
               gridAxis = new IFCGridAxis(ifcGridAxis);
            }
            catch (Exception ex)
            {
               Importer.TheLog.LogError(ifcGridAxis.StepId, ex.Message, false);
               return null;
            }
         }

         return (gridAxis as IFCGridAxis);
      }
   }
}