//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012-2016  Autodesk, Inc.
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
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export ramps
   /// </summary>
   class RampExporter
   {
      /// <summary>
      /// Checks if exporting an element of Ramp category.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <returns>True if element is of category OST_Ramps.</returns>
      static public bool IsRamp(Element element)
      {
         // FaceWall should be exported as IfcWall.
         return (CategoryUtil.GetSafeCategoryId(element) == new ElementId(BuiltInCategory.OST_Ramps));
      }

      static private double GetDefaultHeightForRamp()
      {
         // The default height for ramps is 3'.
         return UnitUtil.ScaleLength(3.0);
      }

      /// <summary>
      /// Gets the ramp height for a ramp.
      /// </summary>
      /// <param name="exporterIFC">
      /// The exporter.
      /// </param>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <returns>
      /// The unscaled height.
      /// </returns>
      static public double GetRampHeight(ExporterIFC exporterIFC, Element element)
      {
         // Re-use the code for stairs height for legacy stairs.
         return StairsExporter.GetStairsHeightForLegacyStair(exporterIFC, element, GetDefaultHeightForRamp());
      }

      /// <summary>
      /// Gets the number of flights of a multi-story ramp.
      /// </summary>
      /// <param name="exporterIFC">
      /// The exporter.
      /// </param>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <returns>
      /// The number of flights (at least 1.)
      /// </returns>
      static public int GetNumFlightsForRamp(ExporterIFC exporterIFC, Element element)
      {
         return StairsExporter.GetNumFlightsForLegacyStair(exporterIFC, element, GetDefaultHeightForRamp());
      }

      /// <summary>
      /// Gets IFCRampType from ramp type name.
      /// </summary>
      /// <param name="rampTypeName">The ramp type name.</param>
      /// <returns>The IFCRampType.</returns>
      public static string GetIFCRampType(string rampTypeName)
      {
         string typeName = NamingUtil.RemoveSpacesAndUnderscores(rampTypeName);

         if (String.Compare(typeName, "StraightRun", true) == 0 ||
             String.Compare(typeName, "StraightRunRamp", true) == 0)
            return "Straight_Run_Ramp";
         if (String.Compare(typeName, "TwoStraightRun", true) == 0 ||
             String.Compare(typeName, "TwoStraightRunRamp", true) == 0)
            return "Two_Straight_Run_Ramp";
         if (String.Compare(typeName, "QuarterTurn", true) == 0 ||
             String.Compare(typeName, "QuarterTurnRamp", true) == 0)
            return "Quarter_Turn_Ramp";
         if (String.Compare(typeName, "TwoQuarterTurn", true) == 0 ||
             String.Compare(typeName, "TwoQuarterTurnRamp", true) == 0)
            return "Two_Quarter_Turn_Ramp";
         if (String.Compare(typeName, "HalfTurn", true) == 0 ||
             String.Compare(typeName, "HalfTurnRamp", true) == 0)
            return "Half_Turn_Ramp";
         if (String.Compare(typeName, "Spiral", true) == 0 ||
             String.Compare(typeName, "SpiralRamp", true) == 0)
            return "Spiral_Ramp";
         if (String.Compare(typeName, "UserDefined", true) == 0)
            return "UserDefined";

         return "NotDefined";
      }

      /// <summary>
      /// Exports the top stories of a multistory ramp.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="ramp">The ramp element.</param>
      /// <param name="numFlights">The number of flights for a multistory ramp.</param>
      /// <param name="rampHnd">The stairs container handle.</param>
      /// <param name="components">The components handles.</param>
      /// <param name="ecData">The extrusion creation data.</param>
      /// <param name="componentECData">The extrusion creation data for the components.</param>
      /// <param name="placementSetter">The placement setter.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportMultistoryRamp(ExporterIFC exporterIFC, Element ramp, int numFlights,
          IFCAnyHandle rampHnd, IList<IFCAnyHandle> components, IList<IFCExtrusionCreationData> componentECData,
          PlacementSetter placementSetter, ProductWrapper productWrapper)
      {
         if (numFlights < 2)
            return;

         double heightNonScaled = GetRampHeight(exporterIFC, ramp);
         if (heightNonScaled < MathUtil.Eps())
            return;

         if (IFCAnyHandleUtil.IsNullOrHasNoValue(rampHnd))
            return;

         IFCAnyHandle localPlacement = IFCAnyHandleUtil.GetObjectPlacement(rampHnd);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(localPlacement))
            return;

         IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;

         IFCFile file = exporterIFC.GetFile();

         IFCAnyHandle relPlacement = GeometryUtil.GetRelativePlacementFromLocalPlacement(localPlacement);
         IFCAnyHandle ptHnd = IFCAnyHandleUtil.GetLocation(relPlacement);
         IList<double> origCoords = IFCAnyHandleUtil.GetCoordinates(ptHnd);

         IList<IFCAnyHandle> rampLocalPlacementHnds = new List<IFCAnyHandle>();
         IList<IFCLevelInfo> levelInfos = new List<IFCLevelInfo>();
         for (int ii = 0; ii < numFlights - 1; ii++)
         {
            IFCAnyHandle newLevelHnd = null;

            // We are going to avoid internal scaling routines, and instead scale in .NET.
            double newOffsetUnscaled = 0.0;
            IFCLevelInfo currLevelInfo =
                placementSetter.GetOffsetLevelInfoAndHandle(heightNonScaled * (ii + 1), 1.0, ramp.Document, out newLevelHnd, out newOffsetUnscaled);
            double newOffsetScaled = UnitUtil.ScaleLength(newOffsetUnscaled);

            if (currLevelInfo != null)
               levelInfos.Add(currLevelInfo);
            else
               levelInfos.Add(placementSetter.LevelInfo);

            XYZ orig;
            if (ptHnd.HasValue)
               orig = new XYZ(origCoords[0], origCoords[1], newOffsetScaled);
            else
               orig = new XYZ(0.0, 0.0, newOffsetScaled);

            rampLocalPlacementHnds.Add(ExporterUtil.CreateLocalPlacement(file, newLevelHnd, orig, null, null));
         }

         IList<List<IFCAnyHandle>> newComponents = new List<List<IFCAnyHandle>>();
         for (int ii = 0; ii < numFlights - 1; ii++)
            newComponents.Add(new List<IFCAnyHandle>());

         int compIdx = 0;
         ElementId catId = CategoryUtil.GetSafeCategoryId(ramp);

         foreach (IFCAnyHandle component in components)
         {
            string componentName = IFCAnyHandleUtil.GetStringAttribute(component, "Name");
            IFCAnyHandle componentProdRep = IFCAnyHandleUtil.GetInstanceAttribute(component, "Representation");

            IList<string> localComponentNames = new List<string>();
            IList<IFCAnyHandle> componentPlacementHnds = new List<IFCAnyHandle>();

            IFCAnyHandle localLocalPlacement = IFCAnyHandleUtil.GetObjectPlacement(component);
            IFCAnyHandle localRelativePlacement =
                (localLocalPlacement == null) ? null : IFCAnyHandleUtil.GetInstanceAttribute(localLocalPlacement, "RelativePlacement");

            bool isSubRamp = component.IsSubTypeOf(IFCEntityType.IfcRamp.ToString());
            for (int ii = 0; ii < numFlights - 1; ii++)
            {
               localComponentNames.Add((componentName == null) ? (ii + 2).ToString() : (componentName + ":" + (ii + 2)));
               if (isSubRamp)
                  componentPlacementHnds.Add(ExporterUtil.CopyLocalPlacement(file, rampLocalPlacementHnds[ii]));
               else
                  componentPlacementHnds.Add(IFCInstanceExporter.CreateLocalPlacement(file, rampLocalPlacementHnds[ii], localRelativePlacement));
            }

            IList<IFCAnyHandle> localComponentHnds = new List<IFCAnyHandle>();
            if (isSubRamp)
            {
               string componentType = IFCAnyHandleUtil.GetEnumerationAttribute(component, ExporterCacheManager.ExportOptionsCache.ExportAs4 ? "PredefinedType" : "ShapeType");
               string localRampType = GetIFCRampType(componentType);

               for (int ii = 0; ii < numFlights - 1; ii++)
               {
                  IFCAnyHandle representationCopy =
                      ExporterUtil.CopyProductDefinitionShape(exporterIFC, ramp, catId, componentProdRep);

                  IFCAnyHandle localComponent = IFCInstanceExporter.CreateRamp(exporterIFC, ramp, GUIDUtil.CreateGUID(), ownerHistory,
                      componentPlacementHnds[ii], representationCopy, localRampType);

                  localComponentHnds.Add(localComponent);
                  IFCAnyHandleUtil.OverrideNameAttribute(localComponent, localComponentNames[ii]);

               }
            }
            else if (IFCAnyHandleUtil.IsSubTypeOf(component, IFCEntityType.IfcRampFlight))
            {
               for (int ii = 0; ii < numFlights - 1; ii++)
               {
                  IFCAnyHandle representationCopy =
                      ExporterUtil.CopyProductDefinitionShape(exporterIFC, ramp, catId, componentProdRep);

                  IFCAnyHandle localComponent = IFCInstanceExporter.CreateRampFlight(exporterIFC, ramp, GUIDUtil.CreateGUID(), ownerHistory,
                      componentPlacementHnds[ii], representationCopy, "NOTDEFINED");

                  IFCAnyHandleUtil.OverrideNameAttribute(localComponent, localComponentNames[ii]);
                  localComponentHnds.Add(localComponent);
               }
            }
            else if (IFCAnyHandleUtil.IsSubTypeOf(component, IFCEntityType.IfcSlab))
            {
               string componentType = IFCAnyHandleUtil.GetEnumerationAttribute(component, "PredefinedType");
               IFCSlabType localLandingType = FloorExporter.GetIFCSlabType(componentType);

               for (int ii = 0; ii < numFlights - 1; ii++)
               {
                  IFCAnyHandle representationCopy =
                      ExporterUtil.CopyProductDefinitionShape(exporterIFC, ramp, catId, componentProdRep);

                  IFCAnyHandle localComponent = IFCInstanceExporter.CreateSlab(exporterIFC, ramp, GUIDUtil.CreateGUID(), ownerHistory,
                      componentPlacementHnds[ii], representationCopy, localLandingType.ToString());
                  localComponentHnds.Add(localComponent);
                  IFCAnyHandleUtil.OverrideNameAttribute(localComponent, localComponentNames[ii]);
               }
            }
            else if (IFCAnyHandleUtil.IsSubTypeOf(component, IFCEntityType.IfcMember))
            {
               for (int ii = 0; ii < numFlights - 1; ii++)
               {
                  IFCAnyHandle representationCopy =
                      ExporterUtil.CopyProductDefinitionShape(exporterIFC, ramp, catId, componentProdRep);

                  IFCAnyHandle localComponent = IFCInstanceExporter.CreateMember(exporterIFC, ramp, GUIDUtil.CreateGUID(), ownerHistory,
                componentPlacementHnds[ii], representationCopy, "STRINGER");
                  localComponentHnds.Add(localComponent);
                  IFCAnyHandleUtil.OverrideNameAttribute(localComponent, localComponentNames[ii]);
               }
            }

            for (int ii = 0; ii < numFlights - 1; ii++)
            {
               if (localComponentHnds[ii] != null)
               {
                  newComponents[ii].Add(localComponentHnds[ii]);
                  productWrapper.AddElement(null, localComponentHnds[ii], levelInfos[ii], componentECData[compIdx], false);
               }
            }
            compIdx++;
         }

         // finally add a copy of the container.
         IList<IFCAnyHandle> rampCopyHnds = new List<IFCAnyHandle>();
         for (int ii = 0; ii < numFlights - 1; ii++)
         {
            string rampTypeAsString = null;
            if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
               rampTypeAsString = IFCAnyHandleUtil.GetEnumerationAttribute(rampHnd, "PredefinedType");
            else
               rampTypeAsString = IFCAnyHandleUtil.GetEnumerationAttribute(rampHnd, "ShapeType");
            string rampType = GetIFCRampType(rampTypeAsString);

            string containerRampName = IFCAnyHandleUtil.GetStringAttribute(rampHnd, "Name") + ":" + (ii + 2);
            IFCAnyHandle rampCopyHnd = IFCInstanceExporter.CreateRamp(exporterIFC, ramp, GUIDUtil.CreateGUID(), ownerHistory,
                rampLocalPlacementHnds[ii], null, rampType);

            rampCopyHnds.Add(rampCopyHnd);
            IFCAnyHandleUtil.OverrideNameAttribute(rampCopyHnd, containerRampName);

            productWrapper.AddElement(ramp, rampCopyHnds[ii], levelInfos[ii], null, true);
         }

         for (int ii = 0; ii < numFlights - 1; ii++)
         {
            StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(rampCopyHnds[ii], newComponents[ii],
                rampLocalPlacementHnds[ii]);
            ExporterCacheManager.StairRampContainerInfoCache.AppendStairRampContainerInfo(ramp.Id, stairRampInfo);
         }
      }

      /// <summary>
      /// Exports a ramp to IfcRamp, without decomposing into separate runs and landings.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="ifcEnumType">The ramp type.</param>
      /// <param name="ramp">The ramp element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="numFlights">The number of flights for a multistory ramp.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportRamp(ExporterIFC exporterIFC, string ifcEnumType, Element ramp, GeometryElement geometryElement,
          int numFlights, ProductWrapper productWrapper)
      {
         if (ramp == null || geometryElement == null)
            return;

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcRamp;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, ramp))
            {
               using (IFCExtrusionCreationData ecData = new IFCExtrusionCreationData())
               {
                  ecData.SetLocalPlacement(placementSetter.LocalPlacement);
                  ecData.ReuseLocalPlacement = false;

                  GeometryElement rampGeom = GeometryUtil.GetOneLevelGeometryElement(geometryElement, numFlights);

                  BodyData bodyData;
                  ElementId categoryId = CategoryUtil.GetSafeCategoryId(ramp);

                  BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                  IFCAnyHandle representation = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC,
                      ramp, categoryId, rampGeom, bodyExporterOptions, null, ecData, out bodyData);

                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(representation))
                  {
                     ecData.ClearOpenings();
                     return;
                  }

                  string containedRampGuid = GUIDUtil.CreateSubElementGUID(ramp, (int)IFCRampSubElements.ContainedRamp);
                  IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
                  IFCAnyHandle containedRampLocalPlacement = ExporterUtil.CreateLocalPlacement(file, ecData.GetLocalPlacement(), null);
                  string rampType = GetIFCRampType(ifcEnumType);

                  List<IFCAnyHandle> components = new List<IFCAnyHandle>();
                  IList<IFCExtrusionCreationData> componentExtrusionData = new List<IFCExtrusionCreationData>();
                  IFCAnyHandle containedRampHnd = IFCInstanceExporter.CreateRamp(exporterIFC, ramp, containedRampGuid, ownerHistory,
                      containedRampLocalPlacement, representation, rampType);
                  components.Add(containedRampHnd);
                  componentExtrusionData.Add(ecData);
                  //productWrapper.AddElement(containedRampHnd, placementSetter.LevelInfo, ecData, false);
                  CategoryUtil.CreateMaterialAssociation(exporterIFC, containedRampHnd, bodyData.MaterialIds);

                  string guid = GUIDUtil.CreateGUID(ramp);
                  IFCAnyHandle localPlacement = ecData.GetLocalPlacement();

                  IFCAnyHandle rampHnd = IFCInstanceExporter.CreateRamp(exporterIFC, ramp, guid, ownerHistory,
                      localPlacement, null, rampType);

                  productWrapper.AddElement(ramp, rampHnd, placementSetter.LevelInfo, ecData, true);

                  StairRampContainerInfo stairRampInfo = new StairRampContainerInfo(rampHnd, components, localPlacement);
                  ExporterCacheManager.StairRampContainerInfoCache.AddStairRampContainerInfo(ramp.Id, stairRampInfo);

                  ExportMultistoryRamp(exporterIFC, ramp, numFlights, rampHnd, components, componentExtrusionData, placementSetter,
                      productWrapper);
               }
               tr.Commit();
            }
         }
      }

      /// <summary>
      /// Exports a ramp to IfcRamp.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The ramp element.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void Export(ExporterIFC exporterIFC, Element element, GeometryElement geometryElement, ProductWrapper productWrapper)
      {
         string ifcEnumType = ExporterUtil.GetIFCTypeFromExportTable(exporterIFC, element);
         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            StairsExporter.ExportLegacyStairOrRampAsContainer(exporterIFC, ifcEnumType, element, geometryElement, productWrapper);

            // If we didn't create a handle here, then the element wasn't a "native" Ramp, and is likely a FamilyInstance or a DirectShape.
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(productWrapper.GetAnElement()))
            {
               int numFlights = GetNumFlightsForRamp(exporterIFC, element);
               if (numFlights > 0)
                  ExportRamp(exporterIFC, ifcEnumType, element, geometryElement, numFlights, productWrapper);
            }

            tr.Commit();
         }
      }
   }
}