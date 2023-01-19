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
   public class IFCRevolvedAreaSolid : IFCSweptAreaSolid
   {
      Transform m_Axis = null;

      double m_Angle = 0.0;

      /// <summary>
      /// The axis of rotation for the revolved area solid in the local coordinate system.
      /// </summary>
      public Transform Axis
      {
         get { return m_Axis; }
         protected set { m_Axis = value; }
      }

      /// <summary>
      /// The angle of the sweep.  The sweep will go from 0 to angle in the local coordinate system.
      /// </summary>
      public double Angle
      {
         get { return m_Angle; }
         protected set { m_Angle = value; }
      }

      protected IFCRevolvedAreaSolid()
      {
      }

      override protected void Process(IFCAnyHandle solid)
      {
         base.Process(solid);

         // We will not fail if the axis is not given, but instead assume it to be the identity in the LCS.
         IFCAnyHandle axis = IFCImportHandleUtil.GetRequiredInstanceAttribute(solid, "Axis", false);
         if (axis != null)
            Axis = IFCLocation.ProcessIFCAxis1Placement(axis);
         else
            Axis = Transform.Identity;

         bool found = false;
         Angle = IFCImportHandleUtil.GetRequiredScaledAngleAttribute(solid, "Angle", out found);
         // TODO: IFCImportFile.TheFile.Document.Application.IsValidAngle(Angle)
         if (!found || Angle < MathUtil.Eps())
            Importer.TheLog.LogError(solid.StepId, "revolve angle is invalid, aborting.", true);
      }

      private XYZ GetValidXVectorFromLoop(CurveLoop curveLoop, XYZ zVec, XYZ origin)
      {
         foreach (Curve curve in curveLoop)
         {
            IList<XYZ> pointsToCheck = new List<XYZ>();

            // If unbound, must be cyclic.
            if (!curve.IsBound)
            {
               pointsToCheck.Add(curve.Evaluate(0, false));
               pointsToCheck.Add(curve.Evaluate(Math.PI / 2.0, false));
               pointsToCheck.Add(curve.Evaluate(Math.PI, false));
            }
            else
            {
               pointsToCheck.Add(curve.Evaluate(0, true));
               pointsToCheck.Add(curve.Evaluate(1.0, true));
               if (curve.IsCyclic)
                  pointsToCheck.Add(curve.Evaluate(0.5, true));
            }

            foreach (XYZ pointToCheck in pointsToCheck)
            {
               XYZ possibleVec = (pointToCheck - origin);
               XYZ yVec = zVec.CrossProduct(possibleVec).Normalize();
               if (yVec.IsZeroLength())
                  continue;
               return yVec.CrossProduct(zVec);
            }
         }

         return null;
      }

      /// <summary>
      /// Return geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The shape edit scope.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      /// <returns>One or more created Solids.</returns>
      protected override IList<GeometryObject> CreateGeometryInternal(
         IFCImportShapeEditScope shapeEditScope, Transform scaledLcs, string guid)
      {
         Transform scaledOrigLCS = (scaledLcs == null) ? Transform.Identity : scaledLcs;
         Transform scaledRevolvePosition = (Position == null) ? scaledOrigLCS : scaledOrigLCS.Multiply(Position);

         ISet<IList<CurveLoop>> disjointLoops = GetTransformedCurveLoops(scaledRevolvePosition);
         if (disjointLoops == null || disjointLoops.Count() == 0)
            return null;

         XYZ frameOrigin = scaledRevolvePosition.OfPoint(Axis.Origin);
         XYZ frameZVec = scaledRevolvePosition.OfVector(Axis.BasisZ);
         SolidOptions solidOptions = new SolidOptions(GetMaterialElementId(shapeEditScope), shapeEditScope.GraphicsStyleId);

         IList<GeometryObject> myObjs = new List<GeometryObject>();

         foreach (IList<CurveLoop> loops in disjointLoops)
         {
            XYZ frameXVec = GetValidXVectorFromLoop(loops[0], frameZVec, frameOrigin);
            if (frameXVec == null)
            {
               Importer.TheLog.LogError(Id, "Couldn't generate valid frame for IfcRevolvedAreaSolid.", false);
               return null;
            }

            XYZ frameYVec = frameZVec.CrossProduct(frameXVec);
            Frame coordinateFrame = new Frame(frameOrigin, frameXVec, frameYVec, frameZVec);

            try
            {
               GeometryObject myObj = GeometryCreationUtilities.CreateRevolvedGeometry(coordinateFrame, loops, 0, Angle, solidOptions);
               myObjs?.Add(myObj);
            }
            catch
            {
               Importer.TheLog.LogError(Id, "Couldn't generate valid IfcRevolvedAreaSolid.", false);
            }
         }

         return myObjs;
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

         IList<GeometryObject> revolvedGeometries = CreateGeometryInternal(shapeEditScope, scaledLcs, guid);
         if (revolvedGeometries != null)
         {
            foreach (GeometryObject revolvedGeometry in revolvedGeometries)
            {
               shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, revolvedGeometry));
            }
         }
      }

      protected IFCRevolvedAreaSolid(IFCAnyHandle solid)
      {
         Process(solid);
      }

      /// <summary>
      /// Create an IFCRevolvedAreaSolid object from a handle of type IfcRevolvedAreaSolid.
      /// </summary>
      /// <param name="ifcSolid">The IFC handle.</param>
      /// <returns>The IFCRevolvedAreaSolid object.</returns>
      public static IFCRevolvedAreaSolid ProcessIFCRevolvedAreaSolid(IFCAnyHandle ifcSolid)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSolid))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcRevolvedAreaSolid);
            return null;
         }

         IFCEntity solid;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSolid.StepId, out solid))
            solid = new IFCRevolvedAreaSolid(ifcSolid);
         return (solid as IFCRevolvedAreaSolid);
      }
   }
}