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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcDistributionPort.
   /// </summary>
   public class IFCDistributionPort : IFCPort
   {
      /// <summary>
      /// The flow direction of this port.
      /// </summary>
      public IFCFlowDirection FlowDirection { get; protected set; } = IFCFlowDirection.NotDefined;

      /// <summary>
      /// The system type of this port.
      /// </summary>
      public IFCDistributionSystemEnum SystemType { get; protected set; } = IFCDistributionSystemEnum.NotDefined;

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCDistributionPort()
      {

      }

      protected IFCDistributionPort(IFCAnyHandle ifcDistributionPort)
      {
         Process(ifcDistributionPort);
      }

      /// <summary>
      /// Processes IfcDistributionPort attributes.
      /// </summary>
      /// <param name="ifcDistributionPort">The IfcDistributionPort handle.</param>
      protected override void Process(IFCAnyHandle ifcDistributionPort)
      {
         base.Process(ifcDistributionPort);

         FlowDirection = IFCEnums.GetSafeEnumerationAttribute<IFCFlowDirection>(ifcDistributionPort, "FlowDirection", IFCFlowDirection.NotDefined);
         SystemType = IFCEnums.GetSafeEnumerationAttribute<IFCDistributionSystemEnum>(ifcDistributionPort, "SystemType", IFCDistributionSystemEnum.NotDefined);
      }

      /// <summary>
      /// Creates or populates Revit element params based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      protected override void CreateParametersInternal(Document doc, Element element)
      {
         base.CreateParametersInternal(doc, element);

         if (element != null)
         {
            Category category = IFCPropertySet.GetCategoryForParameterIfValid(element, Id);

            ParametersToSet.AddStringParameter(doc, element, category, this, "Flow Direction", FlowDirection.ToString(), Id);
            ParametersToSet.AddStringParameter(doc, element, category, this, "System Type", SystemType.ToString(), Id);
         }
      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      protected override void Create(Document doc)
      {
         // Try to get the location:
         // 1. From the ObjectLocation, if it exists.  This should be exact.
         // 2. From the ObjectLocation of the element that the port is associated to, if it exists.
         // This should be approximate.
         // 3. Default to the origin.
         Transform lcs = ObjectLocation?.TotalTransform;
         if (lcs == null)
         {
            if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC4))
               lcs = (NestsWhole as IFCProduct)?.ObjectLocation?.TotalTransform;
            else
               lcs = ContainedIn?.ObjectLocation?.TotalTransform;

         }
         if (lcs == null)
            lcs = Transform.Identity;

         // 2016+ only.
         XYZ origin = lcs.Origin;

         ElementId graphicsStyleId = GetGraphicsStyleId(doc);
         Point point = XYZ.IsWithinLengthLimits(origin) ? Point.Create(origin, graphicsStyleId) : null;

         // 2015+: create cone(s) for the direction of flow.
         CurveLoop rightTrangle = new CurveLoop();
         const double radius = 0.04;
         const double height = 0.12;

         SolidOptions solidOptions = new SolidOptions(ElementId.InvalidElementId, graphicsStyleId);

         Frame coordinateFrame = new Frame(lcs.Origin, lcs.BasisX, lcs.BasisY, lcs.BasisZ);

         // The origin is at the base of the cone for everything but source - then it is at the top of the cone.
         XYZ pt1 = FlowDirection == IFCFlowDirection.Source ? lcs.Origin - height * lcs.BasisZ : lcs.Origin;
         XYZ pt2 = pt1 + radius * lcs.BasisX;
         XYZ pt3 = pt1 + height * lcs.BasisZ;

         rightTrangle.Append(Line.CreateBound(pt1, pt2));
         rightTrangle.Append(Line.CreateBound(pt2, pt3));
         rightTrangle.Append(Line.CreateBound(pt3, pt1));
         IList<CurveLoop> curveLoops = new List<CurveLoop>();
         curveLoops.Add(rightTrangle);

         Solid portArrow = GeometryCreationUtilities.CreateRevolvedGeometry(coordinateFrame, curveLoops, 0.0, Math.PI * 2.0, solidOptions);

         Solid oppositePortArrow = null;
         if (FlowDirection == IFCFlowDirection.SourceAndSink)
         {
            Frame oppositeCoordinateFrame = new Frame(lcs.Origin, -lcs.BasisX, lcs.BasisY, -lcs.BasisZ);
            CurveLoop oppositeRightTrangle = new CurveLoop();

            XYZ oppPt2 = pt1 - radius * lcs.BasisX;
            XYZ oppPt3 = pt1 - height * lcs.BasisZ;
            oppositeRightTrangle.Append(Line.CreateBound(pt1, oppPt2));
            oppositeRightTrangle.Append(Line.CreateBound(oppPt2, oppPt3));
            oppositeRightTrangle.Append(Line.CreateBound(oppPt3, pt1));
            IList<CurveLoop> oppositeCurveLoops = new List<CurveLoop>() { oppositeRightTrangle };

            oppositePortArrow = GeometryCreationUtilities.CreateRevolvedGeometry(oppositeCoordinateFrame, oppositeCurveLoops, 0.0, Math.PI * 2.0, solidOptions);
         }

         if (portArrow != null)
         {
            IList<GeometryObject> geomObjs = new List<GeometryObject>();

            if (point != null)
               geomObjs.Add(point);
            geomObjs.Add(portArrow);
            if (oppositePortArrow != null)
               geomObjs.Add(oppositePortArrow);

            DirectShape directShape = IFCElementUtil.CreateElement(doc, GetCategoryId(doc), GlobalId, geomObjs, Id, EntityType);
            if (directShape != null)
            {
               CreatedGeometry = geomObjs;
               CreatedElementId = directShape.Id;
            }
            else
               Importer.TheLog.LogCreationError(this, null, false);
         }
      }

      /// <summary>
      /// Processes an IfcDistributionPort object.
      /// </summary>
      /// <param name="ifcDistributionPort">The IfcDistributionPort handle.</param>
      /// <returns>The IFCDistributionPort object.</returns>
      public static IFCDistributionPort ProcessIFCDistributionPort(IFCAnyHandle ifcDistributionPort)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcDistributionPort))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcDistributionPort);
            return null;
         }

         try
         {
            IFCEntity cachedDistributionPort;
            if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcDistributionPort.StepId, out cachedDistributionPort))
               return (cachedDistributionPort as IFCDistributionPort);

            return new IFCDistributionPort(ifcDistributionPort);
         }
         catch (Exception ex)
         {
            HandleError(ex.Message, ifcDistributionPort, true);
            return null;
         }
      }
   }
}