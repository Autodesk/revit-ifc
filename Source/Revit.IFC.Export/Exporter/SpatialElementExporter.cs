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
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;


namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export spatial elements.
   /// </summary>
   class SpatialElementExporter
   {
      /// <summary>
      /// The SpatialElementGeometryCalculator object that contains some useful results from previous calculator.
      /// </summary>
      private static SpatialElementGeometryCalculator s_SpatialElementGeometryCalculator = null;

      /// <summary>
      /// Initializes SpatialElementGeometryCalculator object.
      /// </summary>
      /// <param name="document">
      /// The Revit document.
      /// </param>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      public static void InitializeSpatialElementGeometryCalculator(Document document, ExporterIFC exporterIFC)
      {
         SpatialElementBoundaryOptions options = GetSpatialElementBoundaryOptions(null);
         s_SpatialElementGeometryCalculator = new SpatialElementGeometryCalculator(document, options);
      }

      /// <summary>
      /// Destroys SpatialElementGeometryCalculator object.
      /// </summary>
      public static void DestroySpatialElementGeometryCalculator()
      {
         if (s_SpatialElementGeometryCalculator != null)
         {
            s_SpatialElementGeometryCalculator.Dispose();
            s_SpatialElementGeometryCalculator = null;
         }
      }

      /// <summary>
      /// Exports spatial elements, including rooms, areas and spaces. 1st level space boundaries.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="spatialElement">
      /// The spatial element.
      /// </param>
      /// <param name="productWrapper">
      /// The ProductWrapper.
      /// </param>
      public static void ExportSpatialElement(ExporterIFC exporterIFC, SpatialElement spatialElement, ProductWrapper productWrapper)
      {
         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcSpace;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return;

         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, spatialElement, null, null))
            {
               SpatialElementGeometryResults spatialElemGeomResult = null;
               if (!CreateIFCSpace(exporterIFC, spatialElement, productWrapper, setter, out spatialElemGeomResult))
                  return;

               bool isArea = (spatialElement is Area);

               // Do not create boundary information for areas.
               if (!isArea && (ExporterCacheManager.ExportOptionsCache.SpaceBoundaryLevel == 1))
               {
                  Document document = spatialElement.Document;
                  ElementId levelId = spatialElement.LevelId;
                  IFCLevelInfo levelInfo = exporterIFC.GetLevelInfo(levelId);
                  double baseHeightNonScaled = (levelInfo != null) ? levelInfo.Elevation : 0.0;

                  try
                  {
                     // This can throw an exception.  If it does, continue to export element without boundary information.
                     // We will re-use the previously generated value, if we have it.
                     // TODO: warn user.
                     if (spatialElemGeomResult == null)
                        spatialElemGeomResult = s_SpatialElementGeometryCalculator.CalculateSpatialElementGeometry(spatialElement);

                     Solid spatialElemGeomSolid = spatialElemGeomResult.GetGeometry();
                     FaceArray faces = spatialElemGeomSolid.Faces;
                     foreach (Face face in faces)
                     {
                        IList<SpatialElementBoundarySubface> spatialElemBoundarySubfaces = spatialElemGeomResult.GetBoundaryFaceInfo(face);
                        foreach (SpatialElementBoundarySubface spatialElemBSubface in spatialElemBoundarySubfaces)
                        {
                           if (spatialElemBSubface.SubfaceType == SubfaceType.Side)
                              continue;

                           if (spatialElemBSubface.GetSubface() == null)
                              continue;

                           ElementId elemId = spatialElemBSubface.SpatialBoundaryElement.LinkInstanceId;
                           if (elemId == ElementId.InvalidElementId)
                           {
                              elemId = spatialElemBSubface.SpatialBoundaryElement.HostElementId;
                           }

                           Element boundingElement = document.GetElement(elemId);
                           if (boundingElement == null)
                              continue;

                           bool isObjectExt = CategoryUtil.IsElementExternal(boundingElement);

                           IFCGeometryInfo info = IFCGeometryInfo.CreateSurfaceGeometryInfo(spatialElement.Document.Application.VertexTolerance);

                           Face subFace = spatialElemBSubface.GetSubface();
                           ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, subFace, XYZ.Zero, false);

                           foreach (IFCAnyHandle surfaceHnd in info.GetSurfaces())
                           {
                              IFCAnyHandle connectionGeometry = IFCInstanceExporter.CreateConnectionSurfaceGeometry(file, surfaceHnd, null);

                              SpaceBoundary spaceBoundary = new SpaceBoundary(spatialElement.Id, boundingElement.Id, setter.LevelId, connectionGeometry, IFCPhysicalOrVirtual.Physical,
                                  isObjectExt ? IFCInternalOrExternal.External : IFCInternalOrExternal.Internal);

                              if (!ProcessIFCSpaceBoundary(exporterIFC, spaceBoundary, file))
                                 ExporterCacheManager.SpaceBoundaryCache.Add(spaceBoundary);
                           }
                        }
                     }
                  }
                  catch
                  {
                  }

                  IList<IList<BoundarySegment>> roomBoundaries = spatialElement.GetBoundarySegments(GetSpatialElementBoundaryOptions(spatialElement));
                  double scaledRoomHeight = GetScaledHeight(spatialElement, levelId, levelInfo);
                  double unscaledHeight = UnitUtil.UnscaleLength(scaledRoomHeight);

                  Transform lcs = Transform.Identity;

                  foreach (IList<BoundarySegment> roomBoundaryList in roomBoundaries)
                  {
                     foreach (BoundarySegment roomBoundary in roomBoundaryList)
                     {
                        Element boundingElement = document.GetElement(roomBoundary.ElementId);

                        if (boundingElement == null)
                           continue;

                        ElementId buildingElemId = boundingElement.Id;
                        Curve trimmedCurve = roomBoundary.GetCurve();

                        if (trimmedCurve == null)
                           continue;

                        //trimmedCurve.Visibility = Visibility.Visible; readonly
                        IFCAnyHandle connectionGeometry = ExtrusionExporter.CreateConnectionSurfaceGeometry(
                           exporterIFC, trimmedCurve, lcs, scaledRoomHeight, baseHeightNonScaled);

                        IFCPhysicalOrVirtual physOrVirt = IFCPhysicalOrVirtual.Physical;
                        if (boundingElement is CurveElement)
                           physOrVirt = IFCPhysicalOrVirtual.Virtual;
                        else if (boundingElement is Autodesk.Revit.DB.Architecture.Room)
                           physOrVirt = IFCPhysicalOrVirtual.NotDefined;

                        bool isObjectExt = CategoryUtil.IsElementExternal(boundingElement);
                        bool isObjectPhys = (physOrVirt == IFCPhysicalOrVirtual.Physical);

                        ElementId actualBuildingElemId = isObjectPhys ? buildingElemId : ElementId.InvalidElementId;

                        SpaceBoundary spaceBoundary = new SpaceBoundary(spatialElement.Id, actualBuildingElemId, setter.LevelId, !IFCAnyHandleUtil.IsNullOrHasNoValue(connectionGeometry) ? connectionGeometry : null,
                            physOrVirt, isObjectExt ? IFCInternalOrExternal.External : IFCInternalOrExternal.Internal);

                        if (!ProcessIFCSpaceBoundary(exporterIFC, spaceBoundary, file))
                           ExporterCacheManager.SpaceBoundaryCache.Add(spaceBoundary);

                        // try to add doors and windows for host objects if appropriate.
                        if (isObjectPhys && boundingElement is HostObject)
                        {
                           HostObject hostObj = boundingElement as HostObject;
                           HashSet<ElementId> elemIds = new HashSet<ElementId>();
                           elemIds.UnionWith(hostObj.FindInserts(false, false, false, false));
                           if (elemIds.Count == 0)
                           {
                              CurtainGridSet curtainGridSet = CurtainSystemExporter.GetCurtainGridSet(hostObj);
                              if (curtainGridSet != null)
                              {
                                 foreach (CurtainGrid curtainGrid in curtainGridSet)
                                    elemIds.UnionWith(CurtainSystemExporter.GetVisiblePanelsForGrid(curtainGrid));
                              }
                           }

                           foreach (ElementId elemId in elemIds)
                           {
                              // we are going to do a simple bbox export, not complicated geometry.
                              Element instElem = document.GetElement(elemId);
                              if (instElem == null)
                                 continue;

                              BoundingBoxXYZ instBBox = instElem.get_BoundingBox(null);
                              if (instBBox == null)
                                 continue;

                              // make copy of original trimmed curve.
                              Curve instCurve = trimmedCurve.Clone();
                              XYZ instOrig = instCurve.GetEndPoint(0);

                              // make sure that the insert is on this level.
                              if (instBBox.Max.Z < instOrig.Z)
                                 continue;
                              if (instBBox.Min.Z > instOrig.Z + unscaledHeight)
                                 continue;

                              double insHeight = Math.Min(instBBox.Max.Z, instOrig.Z + unscaledHeight) - Math.Max(instOrig.Z, instBBox.Min.Z);
                              if (insHeight < (1.0 / (12.0 * 16.0)))
                                 continue;

                              // move base curve to bottom of bbox.
                              XYZ moveDir = new XYZ(0.0, 0.0, instBBox.Min.Z - instOrig.Z);
                              Transform moveTrf = Transform.CreateTranslation(moveDir);
                              instCurve = instCurve.CreateTransformed(moveTrf);

                              bool isHorizOrVert = false;
                              if (instCurve is Line)
                              {
                                 Line instLine = instCurve as Line;
                                 XYZ lineDir = instLine.Direction;
                                 if (MathUtil.IsAlmostEqual(Math.Abs(lineDir.X), 1.0) || (MathUtil.IsAlmostEqual(Math.Abs(lineDir.Y), 1.0)))
                                    isHorizOrVert = true;
                              }

                              double[] parameters = new double[2];
                              double[] origEndParams = new double[2];
                              bool paramsSet = false;

                              if (!isHorizOrVert)
                              {
                                 FamilyInstance famInst = instElem as FamilyInstance;
                                 if (famInst == null)
                                    continue;

                                 ElementType elementType = document.GetElement(famInst.GetTypeId()) as ElementType;
                                 if (elementType == null)
                                    continue;

                                 BoundingBoxXYZ symBBox = elementType.get_BoundingBox(null);
                                 if (symBBox != null)
                                 {
                                    Curve symCurve = trimmedCurve.Clone();
                                    Transform trf = famInst.GetTransform();
                                    Transform invTrf = trf.Inverse;
                                    Curve trfCurve = symCurve.CreateTransformed(invTrf);
                                    parameters[0] = trfCurve.Project(symBBox.Min).Parameter;
                                    parameters[1] = trfCurve.Project(symBBox.Max).Parameter;
                                    paramsSet = true;
                                 }
                              }

                              if (!paramsSet)
                              {
                                 parameters[0] = instCurve.Project(instBBox.Min).Parameter;
                                 parameters[1] = instCurve.Project(instBBox.Max).Parameter;
                              }

                              // ignore if less than 1/16".
                              if (Math.Abs(parameters[1] - parameters[0]) < 1.0 / (12.0 * 16.0))
                                 continue;
                              if (parameters[0] > parameters[1])
                              {
                                 //swap
                                 double tempParam = parameters[0];
                                 parameters[0] = parameters[1];
                                 parameters[1] = tempParam;
                              }

                              origEndParams[0] = instCurve.GetEndParameter(0);
                              origEndParams[1] = instCurve.GetEndParameter(1);

                              if (origEndParams[0] > parameters[1] - (1.0 / (12.0 * 16.0)))
                                 continue;
                              if (origEndParams[1] < parameters[0] + (1.0 / (12.0 * 16.0)))
                                 continue;

                              instCurve.MakeBound(parameters[0] > origEndParams[0] ? parameters[0] : origEndParams[0],
                                                  parameters[1] < origEndParams[1] ? parameters[1] : origEndParams[1]);

                              double insHeightScaled = UnitUtil.ScaleLength(insHeight);
                              IFCAnyHandle insConnectionGeom = ExtrusionExporter.CreateConnectionSurfaceGeometry(exporterIFC, instCurve, lcs,
                                 insHeightScaled, baseHeightNonScaled);

                              SpaceBoundary instBoundary = new SpaceBoundary(spatialElement.Id, elemId, setter.LevelId, !IFCAnyHandleUtil.IsNullOrHasNoValue(insConnectionGeom) ? insConnectionGeom : null, physOrVirt,
                                  isObjectExt ? IFCInternalOrExternal.External : IFCInternalOrExternal.Internal);
                              if (!ProcessIFCSpaceBoundary(exporterIFC, instBoundary, file))
                                 ExporterCacheManager.SpaceBoundaryCache.Add(instBoundary);
                           }
                        }
                     }
                  }
               }

               CreateZoneInfos(exporterIFC, file, spatialElement, productWrapper);
               CreateSpaceOccupantInfo(exporterIFC, file, spatialElement, productWrapper);
            }
            transaction.Commit();
         }
      }

      /// <summary>
      /// Exports spatial elements, including rooms, areas and spaces. 2nd level space boundaries.
      /// </summary>
      /// <param name="ifcExporter">The Exporter object.</param>
      /// <param name="exporterIFC"> The ExporterIFC object.</param>
      /// <param name="document">The Revit document.</param>
      /// <returns>The set of exported spaces.  This is used to try to export using the standard routine for spaces that failed.</returns>
      public static ISet<ElementId> ExportSpatialElement2ndLevel(Revit.IFC.Export.Exporter.Exporter ifcExporter, ExporterIFC exporterIFC, Document document)
      {
         ISet<ElementId> exportedSpaceIds = new HashSet<ElementId>();

         using (SubTransaction st = new SubTransaction(document))
         {
            st.Start();

            EnergyAnalysisDetailModel model = null;
            try
            {
               View filterView = ExporterCacheManager.ExportOptionsCache.FilterViewForExport;
               IFCFile file = exporterIFC.GetFile();
               using (IFCTransaction transaction = new IFCTransaction(file))
               {

                  EnergyAnalysisDetailModelOptions options = new EnergyAnalysisDetailModelOptions();
                  options.Tier = EnergyAnalysisDetailModelTier.SecondLevelBoundaries; //2nd level space boundaries
                  options.SimplifyCurtainSystems = true;
                  try
                  {
                     model = EnergyAnalysisDetailModel.Create(document, options);
                  }
                  catch (System.Exception)
                  {
                     return exportedSpaceIds;
                  }

                  IList<EnergyAnalysisSpace> spaces = model.GetAnalyticalSpaces();
                  foreach (EnergyAnalysisSpace space in spaces)
                  {
                     SpatialElement spatialElement = document.GetElement(space.CADObjectUniqueId) as SpatialElement;

                     if (spatialElement == null)
                        continue;

                     //current view only
                     if (!ElementFilteringUtil.IsElementVisible(spatialElement))
                        continue;

                     if (!ElementFilteringUtil.ShouldElementBeExported(exporterIFC, spatialElement, false))
                        continue;

                     if (ElementFilteringUtil.IsRoomInInvalidPhase(spatialElement))
                        continue;

                     Options geomOptions = GeometryUtil.GetIFCExportGeometryOptions();
                     View ownerView = spatialElement.Document.GetElement(spatialElement.OwnerViewId) as View;
                     if (ownerView != null)
                        geomOptions.View = ownerView;
                     GeometryElement geomElem = spatialElement.get_Geometry(geomOptions);

                     try
                     {
                        exporterIFC.PushExportState(spatialElement, geomElem);

                        using (ProductWrapper productWrapper = ProductWrapper.Create(exporterIFC, true))
                        {
                           using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, spatialElement))
                           {
                              // We won't use the SpatialElementGeometryResults, as these are 1st level boundaries, not 2nd level.
                              SpatialElementGeometryResults results = null;
                              if (!CreateIFCSpace(exporterIFC, spatialElement, productWrapper, setter, out results))
                                 continue;

                              exportedSpaceIds.Add(spatialElement.Id);

                              XYZ offset = GetSpaceBoundaryOffset(setter);

                              //get boundary information from surfaces
                              IList<EnergyAnalysisSurface> surfaces = space.GetAnalyticalSurfaces();
                              foreach (EnergyAnalysisSurface surface in surfaces)
                              {
                                 Element boundingElement = GetBoundaryElement(document, surface.CADLinkUniqueId, surface.CADObjectUniqueId);

                                 IList<EnergyAnalysisOpening> openings = surface.GetAnalyticalOpenings();
                                 IFCAnyHandle connectionGeometry = CreateConnectionSurfaceGeometry(exporterIFC, surface, openings, offset);
                                 CreateIFCSpaceBoundary(file, exporterIFC, spatialElement, boundingElement, setter.LevelId, connectionGeometry);

                                 // try to add doors and windows for host objects if appropriate.
                                 if (boundingElement is HostObject)
                                 {
                                    foreach (EnergyAnalysisOpening opening in openings)
                                    {
                                       Element openingBoundingElem = GetBoundaryElement(document, opening.CADLinkUniqueId, opening.CADObjectUniqueId);
                                       IFCAnyHandle openingConnectionGeom = CreateConnectionSurfaceGeometry(exporterIFC, opening, offset);
                                       CreateIFCSpaceBoundary(file, exporterIFC, spatialElement, openingBoundingElem, setter.LevelId, openingConnectionGeom);
                                    }
                                 }
                              }
                              CreateZoneInfos(exporterIFC, file, spatialElement, productWrapper);
                              CreateSpaceOccupantInfo(exporterIFC, file, spatialElement, productWrapper);

                              ExporterUtil.ExportRelatedProperties(exporterIFC, spatialElement, productWrapper);
                           }
                        }
                     }
                     catch (Exception ex)
                     {
                        ifcExporter.HandleUnexpectedException(ex, exporterIFC, spatialElement);
                     }
                     finally
                     {
                        exporterIFC.PopExportState();
                     }
                  }
                  transaction.Commit();
               }
            }
            finally
            {
               if (model != null)
                  document.Delete(model.Id);
            }

            st.RollBack();
            return exportedSpaceIds;
         }
      }

      /// <summary>
      /// Creates SpaceBoundary from a bounding element.
      /// </summary>
      /// <param name="file">
      /// The IFC file.
      /// </param>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="spatialElement">
      /// The spatial element.
      /// </param>
      /// <param name="boundingElement">
      /// The bounding element.
      /// </param>
      /// <param name="levelId">
      /// The level id.
      /// </param>
      /// <param name="connectionGeometry">
      /// The connection geometry handle.
      /// </param>
      static void CreateIFCSpaceBoundary(IFCFile file, ExporterIFC exporterIFC, SpatialElement spatialElement, Element boundingElement, ElementId levelId, IFCAnyHandle connectionGeometry)
      {
         IFCPhysicalOrVirtual physOrVirt = IFCPhysicalOrVirtual.Physical;
         if (boundingElement == null || boundingElement is CurveElement)
            physOrVirt = IFCPhysicalOrVirtual.Virtual;
         else if (boundingElement is Autodesk.Revit.DB.Architecture.Room)
            physOrVirt = IFCPhysicalOrVirtual.NotDefined;

         bool isObjectExt = CategoryUtil.IsElementExternal(boundingElement);

         SpaceBoundary spaceBoundary = new SpaceBoundary(spatialElement.Id, boundingElement != null ? boundingElement.Id : ElementId.InvalidElementId,
             levelId, connectionGeometry, physOrVirt, isObjectExt ? IFCInternalOrExternal.External : IFCInternalOrExternal.Internal);

         if (!ProcessIFCSpaceBoundary(exporterIFC, spaceBoundary, file))
            ExporterCacheManager.SpaceBoundaryCache.Add(spaceBoundary);
      }

      /// <summary>
      /// Gets element from a CAD Link's UniqueId and a CAD Object's UniqueId.
      /// </summary>
      /// <param name="document">
      /// The Revit document.
      /// </param>
      /// <param name="CADLinkUniqueId">
      /// The CAD Link's unique id.
      /// </param>
      /// <param name="CADObjectUniqueId">
      /// The CAD Object's unique id.
      /// </param>
      /// <returns>
      /// The element.
      /// </returns>
      static Element GetBoundaryElement(Document document, string CADLinkUniqueId, string CADObjectUniqueId)
      {
         Document documentToUse = document;
         if (!String.IsNullOrEmpty(CADLinkUniqueId))
         {
            RevitLinkInstance CADLinkInstance = document.GetElement(CADLinkUniqueId) as RevitLinkInstance;
            if (CADLinkInstance != null)
            {
               documentToUse = CADLinkInstance.GetLinkDocument();
            }
         }

         return documentToUse.GetElement(CADObjectUniqueId);
      }

      /// <summary>
      /// Gets the boundary options of a spatial element.
      /// </summary>
      /// <param name="spatialElement">The spatial element. null to get the default options.</param>
      /// <returns>The SpatialElementBoundaryOptions.</returns>
      static SpatialElementBoundaryOptions GetSpatialElementBoundaryOptions(SpatialElement spatialElement)
      {
         SpatialElementBoundaryOptions spatialElementBoundaryOptions = new SpatialElementBoundaryOptions();
         spatialElementBoundaryOptions.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish;

         if (spatialElement == null)
            return spatialElementBoundaryOptions;

         SpatialElementType spatialElementType = SpatialElementType.Room;
         if (spatialElement is Autodesk.Revit.DB.Architecture.Room)
         {
            spatialElementType = SpatialElementType.Room;
         }
         else if (spatialElement is Area)
         {
            spatialElementType = SpatialElementType.Area;
         }
         else if (spatialElement is Autodesk.Revit.DB.Mechanical.Space)
         {
            spatialElementType = SpatialElementType.Space;
         }
         else
            return spatialElementBoundaryOptions;

         AreaVolumeSettings areaSettings = AreaVolumeSettings.GetAreaVolumeSettings(spatialElement.Document);
         if (areaSettings != null)
         {
            spatialElementBoundaryOptions.SpatialElementBoundaryLocation = areaSettings.GetSpatialElementBoundaryLocation(spatialElementType);
         }

         return spatialElementBoundaryOptions;
      }

      /// <summary>Creates IFC connection surface geometry from a surface object.</summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="surface">The EnergyAnalysisSurface.</param>
      /// <param name="openings">List of EnergyAnalysisOpenings.</param>
      /// <param name="offset">The offset of the geometry.</param>
      /// <returns>The connection geometry handle.</returns>
      static IFCAnyHandle CreateConnectionSurfaceGeometry(ExporterIFC exporterIFC, EnergyAnalysisSurface surface, IList<EnergyAnalysisOpening> openings, XYZ offset)
      {
         IFCFile file = exporterIFC.GetFile();

         Polyloop outerLoop = surface.GetPolyloop();
         IList<XYZ> outerLoopPoints = outerLoop.GetPoints();

         IList<XYZ> newOuterLoopPoints = new List<XYZ>();
         foreach (XYZ point in outerLoopPoints)
         {
            newOuterLoopPoints.Add(UnitUtil.ScaleLength(point.Subtract(offset)));
         }

         IList<IList<XYZ>> innerLoopPoints = new List<IList<XYZ>>();
         foreach (EnergyAnalysisOpening opening in openings)
         {
            IList<XYZ> openingPoints = opening.GetPolyloop().GetPoints();
            List<XYZ> newOpeningPoints = new List<XYZ>();
            foreach (XYZ openingPoint in openingPoints)
            {
               newOpeningPoints.Add(UnitUtil.ScaleLength(openingPoint.Subtract(offset)));
            }
            innerLoopPoints.Add(newOpeningPoints);
         }

         IFCAnyHandle hnd = ExporterUtil.CreateCurveBoundedPlane(file, newOuterLoopPoints, innerLoopPoints);

         return IFCInstanceExporter.CreateConnectionSurfaceGeometry(file, hnd, null);
      }

      /// <summary>
      /// Creates IFC connection surface geometry from an opening object.
      /// </summary>
      /// <param name="file">
      /// The IFC file.
      /// </param>
      /// <param name="opening">
      /// The EnergyAnalysisOpening.
      /// </param>
      /// <param name="offset">
      /// The offset of opening.
      /// </param>
      /// <returns>
      /// The connection surface geometry handle.
      /// </returns>
      static IFCAnyHandle CreateConnectionSurfaceGeometry(ExporterIFC exporterIFC, EnergyAnalysisOpening opening, XYZ offset)
      {
         IFCFile file = exporterIFC.GetFile();

         Polyloop outerLoop = opening.GetPolyloop();
         IList<XYZ> outerLoopPoints = outerLoop.GetPoints();

         List<XYZ> newOuterLoopPoints = new List<XYZ>();
         foreach (XYZ point in outerLoopPoints)
         {
            newOuterLoopPoints.Add(UnitUtil.ScaleLength(point.Subtract(offset)));
         }

         IList<IList<XYZ>> innerLoopPoints = new List<IList<XYZ>>();

         IFCAnyHandle hnd = ExporterUtil.CreateCurveBoundedPlane(file, newOuterLoopPoints, innerLoopPoints);

         return IFCInstanceExporter.CreateConnectionSurfaceGeometry(file, hnd, null);
      }

      /// <summary>
      /// Gets the height of a spatial element.
      /// </summary>
      /// <param name="spatialElement">The spatial element.</param>
      /// <param name="levelId">The level id.</param>
      /// <param name="levelInfo">The level info.</param>
      /// <returns>
      /// The height, scaled in IFC units.
      /// </returns>
      static double GetScaledHeight(SpatialElement spatialElement, ElementId levelId, IFCLevelInfo levelInfo)
      {
         Document document = spatialElement.Document;
         bool isArea = spatialElement is Area;

         ElementId topLevelId = ElementId.InvalidElementId;
         double topOffset = 0.0;

         // These values are internally set for areas, but are invalid.  Ignore them and just use the level height.
         if (!isArea)
         {
            ParameterUtil.GetElementIdValueFromElement(spatialElement, BuiltInParameter.ROOM_UPPER_LEVEL, out topLevelId);
            ParameterUtil.GetDoubleValueFromElement(spatialElement, BuiltInParameter.ROOM_UPPER_OFFSET, out topOffset);
         }

         double bottomOffset;
         ParameterUtil.GetDoubleValueFromElement(spatialElement, BuiltInParameter.ROOM_LOWER_OFFSET, out bottomOffset);

         Level bottomLevel = document.GetElement(levelId) as Level;
         Level topLevel =
            (levelId == topLevelId) ? bottomLevel : document.GetElement(topLevelId) as Level;

         double roomHeight = 0.0;
         if (bottomLevel != null && topLevel != null)
         {
            roomHeight = (topLevel.Elevation - bottomLevel.Elevation) + (topOffset - bottomOffset);
            roomHeight = UnitUtil.ScaleLength(roomHeight);
         }

         if (MathUtil.IsAlmostZero(roomHeight))
         {
            double levelHeight = ExporterCacheManager.LevelInfoCache.FindHeight(levelId);
            if (levelHeight < 0.0)
               levelHeight = LevelUtil.CalculateDistanceToNextLevel(document, levelId, levelInfo);

            roomHeight = UnitUtil.ScaleLength(levelHeight);
         }

         // For area spaces, we assign a dummy height (1 unit), as we are not allowed to export IfcSpaces without a volumetric representation.
         if (MathUtil.IsAlmostZero(roomHeight) && spatialElement is Area)
         {
            roomHeight = 1.0;
         }

         return roomHeight;
      }

      /// <summary>
      /// Gets the offset of the space boundary.
      /// </summary>
      /// <param name="setter">The placement settter.</param>
      /// <returns>The offset.</returns>
      static XYZ GetSpaceBoundaryOffset(PlacementSetter setter)
      {
         IFCAnyHandle localPlacement = setter.LocalPlacement;
         double zOffset = setter.Offset;

         IFCAnyHandle relPlacement = GeometryUtil.GetRelativePlacementFromLocalPlacement(localPlacement);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(relPlacement))
         {
            IFCAnyHandle ptHnd = IFCAnyHandleUtil.GetLocation(relPlacement);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(ptHnd))
            {
               IList<double> addToCoords = IFCAnyHandleUtil.GetCoordinates(ptHnd);
               return new XYZ(addToCoords[0], addToCoords[1], addToCoords[2] + zOffset);
            }
         }

         return new XYZ(0, 0, zOffset);
      }

      /// <summary>
      /// Creates space boundary.
      /// </summary>
      /// <param name="exporterIFC">
      /// The ExporterIFC object.
      /// </param>
      /// <param name="boundary">
      /// The space boundary object.
      /// </param>
      /// <param name="file">
      /// The IFC file.
      /// </param>
      /// <returns>
      /// True if processed successfully, false otherwise.
      /// </returns>
      public static bool ProcessIFCSpaceBoundary(ExporterIFC exporterIFC, SpaceBoundary boundary, IFCFile file)
      {
         string spaceBoundaryName = String.Empty;
         if (ExporterCacheManager.ExportOptionsCache.SpaceBoundaryLevel == 1)
            spaceBoundaryName = "1stLevel";
         else if (ExporterCacheManager.ExportOptionsCache.SpaceBoundaryLevel == 2)
            spaceBoundaryName = "2ndLevel";

         IFCAnyHandle spatialElemHnd = ExporterCacheManager.SpaceInfoCache.FindSpaceHandle(boundary.SpatialElementId);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(spatialElemHnd))
            return false;

         IFCPhysicalOrVirtual boundaryType = boundary.SpaceBoundaryType;
         IFCAnyHandle buildingElemHnd = null;
         if (boundaryType == IFCPhysicalOrVirtual.Physical)
         {
            buildingElemHnd = exporterIFC.FindSpaceBoundingElementHandle(boundary.BuildingElementId, boundary.LevelId);
            if (IFCAnyHandleUtil.IsNullOrHasNoValue(buildingElemHnd))
               return false;
         }

         IFCInstanceExporter.CreateRelSpaceBoundary(file, GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle, spaceBoundaryName, null,
            spatialElemHnd, buildingElemHnd, boundary.ConnectGeometryHandle, boundaryType, boundary.InternalOrExternal);

         return true;
      }

      /// <summary>
      /// Creates COBIESpaceClassifications.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="file">The file.</param>
      /// <param name="spaceHnd">The space handle.</param>
      /// <param name="projectInfo">The project info.</param>
      /// <param name="spatialElement">The spatial element.</param>
      private static void CreateCOBIESpaceClassifications(ExporterIFC exporterIFC, IFCFile file, IFCAnyHandle spaceHnd,
          ProjectInfo projectInfo, SpatialElement spatialElement)
      {
         HashSet<IFCAnyHandle> spaceHnds = new HashSet<IFCAnyHandle>();
         spaceHnds.Add(spaceHnd);

         string bimStandardsLocation = null;
         if (projectInfo != null)
            ParameterUtil.GetStringValueFromElement(projectInfo, "BIM Standards URL", out bimStandardsLocation);

         // OCCS - Space by Function.
         string itemReference = "";
         if (ParameterUtil.GetStringValueFromElement(spatialElement, "OmniClass Number", out itemReference) != null)
         {
            string itemName;
            ParameterUtil.GetStringValueFromElement(spatialElement, "OmniClass Title", out itemName);

            IFCAnyHandle classification;
            if (!ExporterCacheManager.ClassificationCache.ClassificationHandles.TryGetValue("OmniClass", out classification))
            {
               classification = IFCInstanceExporter.CreateClassification(file, "http://www.omniclass.org", "v 1.0", null, "OmniClass");
               ExporterCacheManager.ClassificationCache.ClassificationHandles.Add("OmniClass", classification);
            }

            IFCAnyHandle classificationReference = IFCInstanceExporter.CreateClassificationReference(file,
              "http://www.omniclass.org/tables/OmniClass_13_2006-03-28.pdf", itemReference, itemName, classification);
            IFCAnyHandle relAssociates = IFCInstanceExporter.CreateRelAssociatesClassification(file, GUIDUtil.CreateGUID(),
               ExporterCacheManager.OwnerHistoryHandle, "OmniClass", null, spaceHnds, classificationReference);
         }

         // Space Type (Owner)
         itemReference = "";
         if (ParameterUtil.GetStringValueFromElement(spatialElement, "Space Type (Owner) Reference", out itemReference) != null)
         {
            string itemName;
            ParameterUtil.GetStringValueFromElement(spatialElement, "Space Type (Owner) Name", out itemName);

            IFCAnyHandle classificationReference = IFCInstanceExporter.CreateClassificationReference(file,
              bimStandardsLocation, itemReference, itemName, null);
            IFCAnyHandle relAssociates = IFCInstanceExporter.CreateRelAssociatesClassification(file, GUIDUtil.CreateGUID(),
               ExporterCacheManager.OwnerHistoryHandle, "Space Type (Owner)", null, spaceHnds, classificationReference);
         }

         // Space Category (Owner)
         itemReference = "";
         if (ParameterUtil.GetStringValueFromElement(spatialElement, "Space Category (Owner) Reference", out itemReference) != null)
         {
            string itemName;
            ParameterUtil.GetStringValueFromElement(spatialElement, "Space Category (Owner) Name", out itemName);

            IFCAnyHandle classificationReference = IFCInstanceExporter.CreateClassificationReference(file,
              bimStandardsLocation, itemReference, itemName, null);
            IFCAnyHandle relAssociates = IFCInstanceExporter.CreateRelAssociatesClassification(file, GUIDUtil.CreateGUID(),
               ExporterCacheManager.OwnerHistoryHandle, "Space Category (Owner)", null, spaceHnds, classificationReference);
         }

         // Space Category (BOMA)
         itemReference = "";
         if (ParameterUtil.GetStringValueFromElement(spatialElement, "Space Category (BOMA) Reference", out itemReference) != null)
         {
            string itemName;
            ParameterUtil.GetStringValueFromElement(spatialElement, "Space Category (BOMA) Name", out itemName);

            IFCAnyHandle classificationReference = IFCInstanceExporter.CreateClassificationReference(file,
              "http://www.BOMA.org", itemReference, itemName, null);
            IFCAnyHandle relAssociates = IFCInstanceExporter.CreateRelAssociatesClassification(file, GUIDUtil.CreateGUID(),
               ExporterCacheManager.OwnerHistoryHandle, "Space Category (BOMA)", "", spaceHnds, classificationReference);
         }
      }

      /// <summary>
      /// Creates IFC room/space/area item, not include boundaries. 
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="spatialElement">The spatial element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <param name="setter">The PlacementSetter.</param>
      /// <returns>True if created successfully, false otherwise.</returns>
      static bool CreateIFCSpace(ExporterIFC exporterIFC, SpatialElement spatialElement, ProductWrapper productWrapper,
          PlacementSetter setter, out SpatialElementGeometryResults results)
      {
         results = null;

         IList<CurveLoop> curveLoops = null;
         try
         {
            // Avoid throwing for a spatial element with no location.
            if (spatialElement.Location == null)
               return false;

            SpatialElementBoundaryOptions options = GetSpatialElementBoundaryOptions(spatialElement);
            curveLoops = ExporterIFCUtils.GetRoomBoundaryAsCurveLoopArray(spatialElement, options, true);
         }
         catch (Autodesk.Revit.Exceptions.InvalidOperationException)
         {
            //Some spatial elements are not placed that have no boundary loops. Don't export them.
            return false;
         }

         Autodesk.Revit.DB.Document document = spatialElement.Document;
         ElementId levelId = spatialElement.LevelId;

         ElementId catId = spatialElement.Category != null ? spatialElement.Category.Id : ElementId.InvalidElementId;

         double dArea = 0.0;
         if (ParameterUtil.GetDoubleValueFromElement(spatialElement, BuiltInParameter.ROOM_AREA, out dArea) != null)
            dArea = UnitUtil.ScaleArea(dArea);

         IFCLevelInfo levelInfo = exporterIFC.GetLevelInfo(levelId);



         IFCFile file = exporterIFC.GetFile();

         IFCAnyHandle localPlacement = setter.LocalPlacement;
         ElementType elemType = document.GetElement(spatialElement.GetTypeId()) as ElementType;
         IFCInternalOrExternal internalOrExternal = CategoryUtil.IsElementExternal(spatialElement) ? IFCInternalOrExternal.External : IFCInternalOrExternal.Internal;

         double scaledRoomHeight = GetScaledHeight(spatialElement, levelId, levelInfo);
         if (scaledRoomHeight <= 0.0)
            return false;

         double bottomOffset;
         ParameterUtil.GetDoubleValueFromElement(spatialElement, BuiltInParameter.ROOM_LOWER_OFFSET, out bottomOffset);

         GeometryElement geomElem = null;
         bool isArea = (spatialElement is Area);
         Area spatialElementAsArea = isArea ? (spatialElement as Area) : null;

         if (spatialElement is Autodesk.Revit.DB.Architecture.Room)
         {
            Autodesk.Revit.DB.Architecture.Room room = spatialElement as Autodesk.Revit.DB.Architecture.Room;
            geomElem = room.ClosedShell;
         }
         else if (spatialElement is Autodesk.Revit.DB.Mechanical.Space)
         {
            Autodesk.Revit.DB.Mechanical.Space space = spatialElement as Autodesk.Revit.DB.Mechanical.Space;
            geomElem = space.ClosedShell;
         }
         else if (isArea)
         {
            Options geomOptions = GeometryUtil.GetIFCExportGeometryOptions();
            geomElem = spatialElementAsArea.get_Geometry(geomOptions);
         }

         IFCAnyHandle spaceHnd = null;
         using (IFCExtrusionCreationData extraParams = new IFCExtrusionCreationData())
         {
            extraParams.SetLocalPlacement(localPlacement);
            extraParams.PossibleExtrusionAxes = IFCExtrusionAxes.TryZ;

            using (IFCTransaction transaction2 = new IFCTransaction(file))
            {
               IFCAnyHandle repHnd = null;
               if (!ExporterCacheManager.ExportOptionsCache.Use2DRoomBoundaryForRoomVolumeCreation && geomElem != null)
               {
                  BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.Medium);
                  repHnd = RepresentationUtil.CreateAppropriateProductDefinitionShape(exporterIFC, spatialElement,
                      catId, geomElem, bodyExporterOptions, null, extraParams, false);
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(repHnd))
                     extraParams.ClearOpenings();
               }
               else
               {
                  double elevation = (levelInfo != null) ? levelInfo.Elevation : 0.0;
                  XYZ orig = new XYZ(0, 0, elevation + bottomOffset);
                  Transform lcs = Transform.CreateTranslation(orig); // room calculated as level offset.

                  IFCAnyHandle shapeRep = ExtrusionExporter.CreateExtrudedSolidFromCurveLoop(exporterIFC, null, curveLoops, lcs, XYZ.BasisZ, scaledRoomHeight, true);
                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(shapeRep))
                     return false;
                  BodyExporter.CreateSurfaceStyleForRepItem(exporterIFC, document, shapeRep, ElementId.InvalidElementId);

                  HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
                  bodyItems.Add(shapeRep);
                  shapeRep = RepresentationUtil.CreateSweptSolidRep(exporterIFC, spatialElement, catId, exporterIFC.Get3DContextHandle("Body"), bodyItems, null);
                  IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
                  shapeReps.Add(shapeRep);

                  IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geomElem, Transform.Identity);
                  if (boundingBoxRep != null)
                     shapeReps.Add(boundingBoxRep);

                  repHnd = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps);
               }

               extraParams.ScaledHeight = scaledRoomHeight;
               extraParams.ScaledArea = dArea;



               spaceHnd = IFCInstanceExporter.CreateSpace(exporterIFC, spatialElement, GUIDUtil.CreateGUID(spatialElement),
                                             ExporterCacheManager.OwnerHistoryHandle,
                                             extraParams.GetLocalPlacement(), repHnd, IFCElementComposition.Element,
                                             internalOrExternal);

               transaction2.Commit();
            }

            if (spaceHnd != null)
            {
               productWrapper.AddSpace(spatialElement, spaceHnd, levelInfo, extraParams, true);
               if (isArea)
               {
                  Element areaScheme = spatialElementAsArea.AreaScheme;
                  if (areaScheme != null)
                  {
                     ElementId areaSchemeId = areaScheme.Id;
                     HashSet<IFCAnyHandle> areas = null;
                     if (!ExporterCacheManager.AreaSchemeCache.TryGetValue(areaSchemeId, out areas))
                     {
                        areas = new HashSet<IFCAnyHandle>();
                        ExporterCacheManager.AreaSchemeCache[areaSchemeId] = areas;
                     }
                     areas.Add(spaceHnd);
                  }
               }
            }
         }

         // Save room handle for later use/relationships
         ExporterCacheManager.SpaceInfoCache.SetSpaceHandle(spatialElement, spaceHnd);

         // Find Ceiling as a Space boundary and keep the relationship in a cache for use later
         bool ret = GetCeilingSpaceBoundary(spatialElement, out results);

         if (!MathUtil.IsAlmostZero(dArea))
         {
            // TODO: Determine if we even need this for IFC2x2, or just IFC2x3.  This is a workaround for the pre-2010 GSA requirements, that don't have their own MVD.
            bool mvdSupportDesignGrossArea = ExporterCacheManager.ExportOptionsCache.ExportAs2x2 || ExporterCacheManager.ExportOptionsCache.ExportAs2x3CoordinationView1;
            bool addonMVDSupportDesignGrossArea = !ExporterCacheManager.ExportOptionsCache.ExportBaseQuantities;
            if (mvdSupportDesignGrossArea && addonMVDSupportDesignGrossArea)
            {
               string strSpaceNumber = null;
               if (ParameterUtil.GetStringValueFromElement(spatialElement, BuiltInParameter.ROOM_NUMBER, out strSpaceNumber) == null)
                  strSpaceNumber = null;

               string spatialElementName = NamingUtil.GetNameOverride(spatialElement, strSpaceNumber);

               bool isDesignGrossArea = (string.Compare(spatialElementName, "GSA Design Gross Area") > 0);
               PropertyUtil.CreatePreCOBIEGSAQuantities(exporterIFC, spaceHnd, "GSA Space Areas", (isDesignGrossArea ? "GSA Design Gross Area" : "GSA BIM Area"), dArea);
            }
         }

         // Export Classifications for SpatialElement for GSA/COBIE.
         if (ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE)
         {
            ProjectInfo projectInfo = document.ProjectInformation;
            if (projectInfo != null)
               CreateCOBIESpaceClassifications(exporterIFC, file, spaceHnd, projectInfo, spatialElement);
         }

         return true;
      }

      /// <summary>
      /// Collect relationship information from Ceiling to Room to be used later to determine whether a Ceiling can be contained in a Room
      /// </summary>
      /// <param name="spatialElement">The revit spatial object to process</param>
      /// <param name="results">The results of the CalculateSpatialElementGeometry call, for caching for later use.</param>
      /// <returns>True if it found a ceiling, false otherwise.</returns>
      static private bool GetCeilingSpaceBoundary(SpatialElement spatialElement, out SpatialElementGeometryResults results)
      {
         results = null;

         // Areas don't have a 3D rep, so no ceiling space boundaries.
         if (spatialElement is Area)
            return false;

         // Represents the criteria for boundary elements to be considered bounding Ceiling
         LogicalOrFilter categoryFilter = new LogicalOrFilter(new ElementCategoryFilter(BuiltInCategory.OST_Ceilings),
                                                     new ElementCategoryFilter(BuiltInCategory.OST_Ceilings));

         try
         {
            if (s_SpatialElementGeometryCalculator == null)
               return false;
            results = s_SpatialElementGeometryCalculator.CalculateSpatialElementGeometry(spatialElement);
         }
         catch
         {
            return false;
         }

         Solid geometry = results.GetGeometry();

         // Go through the boundary faces to identify whether it is bounded by a Ceiling. If it is Ceiling, add into the Cache
         foreach (Face face in geometry.Faces)
         {
            IList<SpatialElementBoundarySubface> boundaryFaces = results.GetBoundaryFaceInfo(face);
            foreach (SpatialElementBoundarySubface boundaryFace in boundaryFaces)
            {
               // Get boundary element
               LinkElementId boundaryElementId = boundaryFace.SpatialBoundaryElement;

               // Only considering local file room bounding elements
               ElementId localElementId = boundaryElementId.HostElementId;
               // Evaluate if element meets criteria using PassesFilter()
               if (localElementId != ElementId.InvalidElementId && categoryFilter.PassesFilter(spatialElement.Document, localElementId))
               {
                  if (ExporterCacheManager.CeilingSpaceRelCache.ContainsKey(localElementId))
                  {
                     // The ceiling already exists in the Dictionary, add the Space into list
                     IList<ElementId> roomlist = ExporterCacheManager.CeilingSpaceRelCache[localElementId];
                     roomlist.Add(spatialElement.Id);
                  }
                  else
                  {
                     // The first time this Ceiling Id appears
                     IList<ElementId> roomlist = new List<ElementId>();
                     roomlist.Add(spatialElement.Id);
                     ExporterCacheManager.CeilingSpaceRelCache.Add(localElementId, roomlist);
                  }

               }
            }
         }

         return true;
      }

      /// <summary>
      /// Creates spatial zone energy analysis property set.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC.</param>
      /// <param name="file">The file.</param>
      /// <param name="element">The element.</param>
      /// <returns>The handle.</returns>
      static private IFCAnyHandle CreateSpatialZoneEnergyAnalysisPSet(ExporterIFC exporterIFC, IFCFile file, Element element)
      {
         // Property Sets.  We don't use the generic Property Set mechanism because Zones aren't "real" elements.
         HashSet<IFCAnyHandle> properties = new HashSet<IFCAnyHandle>();

         string paramValue = "";
         if (ParameterUtil.GetStringValueFromElement(element, "Spatial Zone Conditioning Requirement", out paramValue) != null)
         {
            IFCData paramVal = Revit.IFC.Export.Toolkit.IFCDataUtil.CreateAsLabel(paramValue);
            IFCAnyHandle propSingleValue = IFCInstanceExporter.CreatePropertySingleValue(file, "SpatialZoneConditioningRequirement", null, paramVal, null);
            properties.Add(propSingleValue);
         }

         if (ParameterUtil.GetStringValueFromElement(element, "HVAC System Type", out paramValue) != null)
         {
            IFCData paramVal = Revit.IFC.Export.Toolkit.IFCDataUtil.CreateAsLabel(paramValue);
            IFCAnyHandle propSingleValue = IFCInstanceExporter.CreatePropertySingleValue(file, "HVACSystemType", null, paramVal, null);
            properties.Add(propSingleValue);
         }

         if (ParameterUtil.GetStringValueFromElement(element, "User Defined HVAC System Type", out paramValue) != null)
         {
            IFCData paramVal = Revit.IFC.Export.Toolkit.IFCDataUtil.CreateAsLabel(paramValue);
            IFCAnyHandle propSingleValue = IFCInstanceExporter.CreatePropertySingleValue(file, "UserDefinedHVACSystemType", null, paramVal, null);
            properties.Add(propSingleValue);
         }

         double infiltrationRate = 0.0;
         if (ParameterUtil.GetDoubleValueFromElement(element, null, "Infiltration Rate", out infiltrationRate) != null)
         {
            IFCData paramVal = Revit.IFC.Export.Toolkit.IFCDataUtil.CreateAsReal(infiltrationRate);
            IFCAnyHandle propSingleValue = IFCInstanceExporter.CreatePropertySingleValue(file, "InfiltrationRate", null, paramVal,
                ExporterCacheManager.UnitsCache["ACH"]);
            properties.Add(propSingleValue);
         }

         int isDaylitZone = 0;
         if (ParameterUtil.GetIntValueFromElement(element, "Is Daylit Zone", out isDaylitZone) != null)
         {
            IFCData paramVal = Revit.IFC.Export.Toolkit.IFCDataUtil.CreateAsBoolean(isDaylitZone != 0);
            IFCAnyHandle propSingleValue = IFCInstanceExporter.CreatePropertySingleValue(file, "IsDaylitZone", null, paramVal, null);
            properties.Add(propSingleValue);
         }

         int numberOfDaylightSensors = 0;
         if (ParameterUtil.GetIntValueFromElement(element, "Number of Daylight Sensors", out numberOfDaylightSensors) != null)
         {
            IFCData paramVal = Revit.IFC.Export.Toolkit.IFCDataUtil.CreateAsInteger(numberOfDaylightSensors);
            IFCAnyHandle propSingleValue = IFCInstanceExporter.CreatePropertySingleValue(file, "NumberOfDaylightSensors", null, paramVal, null);
            properties.Add(propSingleValue);
         }

         double designIlluminance = 0.0;
         if (ParameterUtil.GetDoubleValueFromElement(element, null, "Design Illuminance", out designIlluminance) != null)
         {
            double scaledValue = UnitUtil.ScaleIlluminance(designIlluminance);
            IFCData paramVal = Revit.IFC.Export.Toolkit.IFCDataUtil.CreateAsReal(designIlluminance);
            IFCAnyHandle propSingleValue = IFCInstanceExporter.CreatePropertySingleValue(file, "DesignIlluminance", null, paramVal,
                ExporterCacheManager.UnitsCache["LUX"]);
            properties.Add(propSingleValue);
         }

         if (ParameterUtil.GetStringValueFromElement(element, "Lighting Controls Type", out paramValue) != null)
         {
            IFCData paramVal = Revit.IFC.Export.Toolkit.IFCDataUtil.CreateAsLabel(paramValue);
            IFCAnyHandle propSingleValue = IFCInstanceExporter.CreatePropertySingleValue(file, "LightingControlsType", null, paramVal, null);
            properties.Add(propSingleValue);
         }

         if (properties.Count > 0)
         {
            return IFCInstanceExporter.CreatePropertySet(file,
                GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle, "ePset_SpatialZoneEnergyAnalysis",
                null, properties);
         }

         return null;
      }


      /// <summary>
      /// Get the name of the net planned area property, depending on the current schema, for levels and zones.
      /// </summary>
      /// <returns>The name of the net planned area property.</returns>
      /// <remarks>Note that PSet_SpaceCommon has had the property "NetPlannedArea" since IFC2x3.</remarks>
      static public string GetLevelAndZoneNetPlannedAreaName()
      {
         return ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 ? "NetAreaPlanned" : "NetPlannedArea";
      }

      /// <summary>
      /// Get the name of the gross planned area property, depending on the current schema, for levels and zones.
      /// </summary>
      /// <returns>The name of the net planned area property.</returns>
      /// <remarks>Note that PSet_SpaceCommon has had the property "GrossPlannedArea" since IFC2x3.</remarks>
      static public string GetLevelAndZoneGrossPlannedAreaName()
      {
         return ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4 ? "GrossAreaPlanned" : "GrossPlannedArea";
      }

      /// <summary>
      /// Creates zone common property set.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="file">The file.</param>
      /// <param name="element">The element.</param>
      /// <returns>The handle.</returns>
      static private IFCAnyHandle CreateZoneCommonPSet(ExporterIFC exporterIFC, IFCFile file, Element element)
      {
         // Property Sets.  We don't use the generic Property Set mechanism because Zones aren't "real" elements.
         HashSet<IFCAnyHandle> properties = new HashSet<IFCAnyHandle>();

         IFCAnyHandle propSingleValue = PropertyUtil.CreateLabelPropertyFromElement(file, element,
             "ZoneCategory", BuiltInParameter.INVALID, "Category", PropertyValueType.SingleValue, null);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
         {
            properties.Add(propSingleValue);
         }

         string grossPlannedAreaName = GetLevelAndZoneGrossPlannedAreaName();
         propSingleValue = PropertyUtil.CreateAreaMeasurePropertyFromElement(file, exporterIFC, element,
             "Pset_ZoneCommon." + grossPlannedAreaName, BuiltInParameter.INVALID, grossPlannedAreaName, PropertyValueType.SingleValue);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
         {
            // For backward compatibility
            propSingleValue = PropertyUtil.CreateAreaMeasurePropertyFromElement(file, exporterIFC, element,
                "Zone" + grossPlannedAreaName, BuiltInParameter.INVALID, grossPlannedAreaName, PropertyValueType.SingleValue);
         }
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
            properties.Add(propSingleValue);

         string netPlannedAreaName = GetLevelAndZoneNetPlannedAreaName();
         propSingleValue = PropertyUtil.CreateAreaMeasurePropertyFromElement(file, exporterIFC, element,
             "Pset_ZoneCommon." + netPlannedAreaName, BuiltInParameter.INVALID, netPlannedAreaName, PropertyValueType.SingleValue);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
         {
            // For backward compatibility
            propSingleValue = PropertyUtil.CreateAreaMeasurePropertyFromElement(file, exporterIFC, element,
                "Zone" + netPlannedAreaName, BuiltInParameter.INVALID, netPlannedAreaName, PropertyValueType.SingleValue);
         }
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
            properties.Add(propSingleValue);

         propSingleValue = PropertyUtil.CreateBooleanPropertyFromElement(file, element,
             "Pset_ZoneCommon.PubliclyAccessible", "PubliclyAccessible", PropertyValueType.SingleValue);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
         {
            propSingleValue = PropertyUtil.CreateBooleanPropertyFromElement(file, element,
               "ZonePubliclyAccessible", "PubliclyAccessible", PropertyValueType.SingleValue);
         }
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
         {
            properties.Add(propSingleValue);
         }

         propSingleValue = PropertyUtil.CreateBooleanPropertyFromElement(file, element,
             "Pset_ZoneCommon.HandicapAccessible", "HandicapAccessible", PropertyValueType.SingleValue);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
         {
            propSingleValue = PropertyUtil.CreateBooleanPropertyFromElement(file, element,
               "ZoneHandicapAccessible", "HandicapAccessible", PropertyValueType.SingleValue);

         }
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
         {
            properties.Add(propSingleValue);
         }

         propSingleValue = PropertyUtil.CreateBooleanPropertyFromElement(file, element,
            "Pset_ZoneCommon.IsExternal", "IsExternal", PropertyValueType.SingleValue);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
         {
            propSingleValue = PropertyUtil.CreateBooleanPropertyFromElement(file, element,
               "ZoneIsExternal", "IsExternal", PropertyValueType.SingleValue);

         }
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
         {
            properties.Add(propSingleValue);
         }

         propSingleValue = PropertyUtil.CreateIdentifierPropertyFromElement(file, element,
            "Pset_ZoneCommon.Reference", BuiltInParameter.INVALID, "Reference", PropertyValueType.SingleValue);
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
         {
            propSingleValue = PropertyUtil.CreateIdentifierPropertyFromElement(file, element,
               "ZoneReference", BuiltInParameter.INVALID, "Reference", PropertyValueType.SingleValue);
         }
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(propSingleValue))
         {
            properties.Add(propSingleValue);
         }

         if (properties.Count > 0)
         {
            return IFCInstanceExporter.CreatePropertySet(file,
                GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle, "Pset_ZoneCommon",
                null, properties);
         }

         return null;
      }

      /// <summary>
      /// Creates the ePset_SpaceOccupant.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="file">The file.</param>
      /// <param name="element">The element.</param>
      /// <returns>The handle.</returns>
      private static IFCAnyHandle CreatePSetSpaceOccupant(ExporterIFC exporterIFC, IFCFile file, Element element)
      {
         HashSet<IFCAnyHandle> properties = new HashSet<IFCAnyHandle>();

         string paramValue = "";
         if (ParameterUtil.GetStringValueFromElement(element, "Space Occupant Organization Abbreviation", out paramValue) != null)
         {
            IFCData paramVal = Revit.IFC.Export.Toolkit.IFCDataUtil.CreateAsLabel(paramValue);
            IFCAnyHandle propSingleValue = IFCInstanceExporter.CreatePropertySingleValue(file, "SpaceOccupantOrganizationAbbreviation", null, paramVal, null);
            properties.Add(propSingleValue);
         }

         if (ParameterUtil.GetStringValueFromElement(element, "Space Occupant Organization Name", out paramValue) != null)
         {
            IFCData paramVal = Revit.IFC.Export.Toolkit.IFCDataUtil.CreateAsLabel(paramValue);
            IFCAnyHandle propSingleValue = IFCInstanceExporter.CreatePropertySingleValue(file, "SpaceOccupantOrganizationName", null, paramVal, null);
            properties.Add(propSingleValue);
         }

         if (properties.Count > 0)
         {
            return IFCInstanceExporter.CreatePropertySet(file,
                GUIDUtil.CreateGUID(), ExporterCacheManager.OwnerHistoryHandle, "ePset_SpaceOccupant", null, properties);
         }

         return null;
      }

      /// <summary>
      /// Collect information to create space occupants and cache them to create when end export.
      /// </summary>
      /// <param name="exporterIFC">
      /// The exporterIFC object.
      /// </param>
      /// <param name="file">
      /// The IFCFile object.
      /// </param>
      /// <param name="element">
      /// The element.
      /// </param>
      /// <param name="productWrapper">
      /// The ProductWrapper.
      /// </param>
      static void CreateSpaceOccupantInfo(ExporterIFC exporterIFC, IFCFile file, Element element, ProductWrapper productWrapper)
      {
         IFCAnyHandle roomHandle = productWrapper.GetElementOfType(IFCEntityType.IfcSpace);

         bool exportToCOBIE = ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE;

         string name;
         if (ParameterUtil.GetStringValueFromElement(element, "Occupant", out name) != null)
         {
            Dictionary<string, IFCAnyHandle> classificationHandles = new Dictionary<string, IFCAnyHandle>();

            // Classifications.
            if (exportToCOBIE)
            {
               Document doc = element.Document;
               ProjectInfo projectInfo = doc.ProjectInformation;

               string location = null;
               if (projectInfo != null)
                  ParameterUtil.GetStringValueFromElement(projectInfo, "BIM Standards URL", out location);

               string itemReference;
               if (ParameterUtil.GetStringValueFromElementOrSymbol(element, "Space Occupant Organization ID Reference", out itemReference) != null)
               {
                  string itemName;
                  ParameterUtil.GetStringValueFromElementOrSymbol(element, "Space Occupant Organization ID Name", out itemName);

                  IFCAnyHandle classificationReference = IFCInstanceExporter.CreateClassificationReference(file,
                    location, itemReference, itemName, null);
                  classificationHandles["Space Occupant Organization ID"] = classificationReference;
               }

               if (ParameterUtil.GetStringValueFromElementOrSymbol(element, "Space Occupant Sub-Organization ID Reference", out itemReference) != null)
               {
                  string itemName;
                  ParameterUtil.GetStringValueFromElementOrSymbol(element, "Space Occupant Sub-Organization ID Name", out itemName);

                  IFCAnyHandle classificationReference = IFCInstanceExporter.CreateClassificationReference(file,
                    location, itemReference, itemName, null);
                  classificationHandles["Space Occupant Sub-Organization ID"] = classificationReference;
               }

               if (ParameterUtil.GetStringValueFromElementOrSymbol(element, "Space Occupant Sub-Organization ID Reference", out itemReference) != null)
               {
                  string itemName;
                  ParameterUtil.GetStringValueFromElementOrSymbol(element, "Space Occupant Sub-Organization ID Name", out itemName);

                  IFCAnyHandle classificationReference = IFCInstanceExporter.CreateClassificationReference(file,
                    location, itemReference, itemName, null);
                  classificationHandles["Space Occupant Sub-Organization ID"] = classificationReference;
               }

               if (ParameterUtil.GetStringValueFromElementOrSymbol(element, "Space Occupant Organization Billing ID Reference", out itemReference) != null)
               {
                  string itemName;
                  ParameterUtil.GetStringValueFromElementOrSymbol(element, "Space Occupant Organization Billing ID Name", out itemName);

                  IFCAnyHandle classificationReference = IFCInstanceExporter.CreateClassificationReference(file,
                    location, itemReference, itemName, null);
                  classificationHandles["Space Occupant Organization Billing ID"] = classificationReference;
               }
            }

            // Look for Parameter Set definition.  We don't use the general approach as Space Occupants are not "real" elements.
            IFCAnyHandle spaceOccupantPSetHnd = CreatePSetSpaceOccupant(exporterIFC, file, element);

            SpaceOccupantInfo spaceOccupantInfo = ExporterCacheManager.SpaceOccupantInfoCache.Find(name);
            if (spaceOccupantInfo == null)
            {
               spaceOccupantInfo = new SpaceOccupantInfo(roomHandle, classificationHandles, spaceOccupantPSetHnd);
               ExporterCacheManager.SpaceOccupantInfoCache.Register(name, spaceOccupantInfo);
            }
            else
            {
               spaceOccupantInfo.RoomHandles.Add(roomHandle);
               foreach (KeyValuePair<string, IFCAnyHandle> classificationReference in classificationHandles)
               {
                  if (!spaceOccupantInfo.ClassificationReferences[classificationReference.Key].HasValue)
                     spaceOccupantInfo.ClassificationReferences[classificationReference.Key] = classificationReference.Value;
                  else
                  {
                     // Delete redundant IfcClassificationReference from file.
                     IFCAnyHandleUtil.Delete(classificationReference.Value);
                  }
               }

               if (spaceOccupantInfo.SpaceOccupantProperySetHandle == null || !spaceOccupantInfo.SpaceOccupantProperySetHandle.HasValue)
                  spaceOccupantInfo.SpaceOccupantProperySetHandle = spaceOccupantPSetHnd;
               else if (spaceOccupantPSetHnd.HasValue)
                  IFCAnyHandleUtil.Delete(spaceOccupantPSetHnd);
            }
         }
      }

      static private bool CreateGSAInformation(ExporterIFC exporterIFC, Element element, string zoneObjectType,
          Dictionary<string, IFCAnyHandle> classificationHandles, IFCAnyHandle energyAnalysisPSetHnd)
      {
         IFCFile file = exporterIFC.GetFile();

         bool isSpatialZone = NamingUtil.IsEqualIgnoringCaseAndSpaces(zoneObjectType, "SpatialZone");
         if (isSpatialZone)
         {
            // Classifications.
            Document doc = element.Document;
            ProjectInfo projectInfo = doc.ProjectInformation;

            string location = null;
            if (projectInfo != null)
               ParameterUtil.GetStringValueFromElement(projectInfo, "BIM Standards URL", out location);

            string itemReference;
            string itemName;

            // Spatial Zone Type (Owner)
            if (ParameterUtil.GetStringValueFromElementOrSymbol(element, "Spatial Zone Type (Owner) Reference", out itemReference) != null)
            {
               ParameterUtil.GetStringValueFromElementOrSymbol(element, "Spatial Zone Type (Owner) Name", out itemName);

               IFCAnyHandle classificationReference = IFCInstanceExporter.CreateClassificationReference(file,
                 location, itemReference, itemName, null);
               classificationHandles["Spatial Zone Type (Owner)"] = classificationReference;
            }

            // Spatial Zone Security Level (Owner)
            if (ParameterUtil.GetStringValueFromElementOrSymbol(element, "Spatial Zone Security Level (Owner) Reference", out itemReference) != null)
            {
               itemName = "";
               ParameterUtil.GetStringValueFromElementOrSymbol(element, "Spatial Zone Security Level (Owner) Name", out itemName);

               IFCAnyHandle classificationReference = IFCInstanceExporter.CreateClassificationReference(file,
                 location, itemReference, itemName, null);
               classificationHandles["Spatial Zone Security Level (Owner)"] = classificationReference;
            }

            // Spatial Zone Type (Energy Analysis)
            if (ParameterUtil.GetStringValueFromElementOrSymbol(element, "ASHRAE Zone Type", out itemName) != null)
            {
               IFCAnyHandle classificationReference = IFCInstanceExporter.CreateClassificationReference(file,
                 "ASHRAE 90.1", "Common Space Type", itemName, null);
               classificationHandles["ASHRAE Zone Type"] = classificationReference;
            }
         }

         if (isSpatialZone || NamingUtil.IsEqualIgnoringCaseAndSpaces(zoneObjectType, "EnergyAnalysisZone"))
         {
            // Property Sets.  We don't use the generic Property Set mechanism because Zones aren't "real" elements.
            energyAnalysisPSetHnd = CreateSpatialZoneEnergyAnalysisPSet(exporterIFC, file, element);

            if (classificationHandles.Count > 0 || energyAnalysisPSetHnd != null)
               return true;
         }
         return false;
      }

      /// <summary>
      /// Collect information to create zones and cache them to create when end export.
      /// </summary>
      /// <param name="exporterIFC">The exporterIFC object.</param>
      /// <param name="file">The IFCFile object.</param>
      /// <param name="element">The element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      static void CreateZoneInfos(ExporterIFC exporterIFC, IFCFile file, Element element, ProductWrapper productWrapper)
      {
         bool exportToCOBIE = ExporterCacheManager.ExportOptionsCache.ExportAsCOBIE;

         // Extra zone information, since Revit doesn't have architectural zones.
         int val = 0;
         string basePropZoneName = "ZoneName";
         string basePropZoneObjectType = "ZoneObjectType";
         string basePropZoneDescription = "ZoneDescription";
         string basePropZoneLongName = "ZoneLongName";
         string basePropZoneClassificationCode = "ZoneClassificationCode";

         // While a room may contain multiple zones, only one can have the extra parameters.  We will allow the first zone encountered
         // to be defined by them. If we require defining multiple zones in one room, then the code below should be modified to modify the 
         // names of the shared parameters to include the index of the appropriate room.
         bool exportedExtraZoneInformation = false;

         while (++val < 1000)   // prevent infinite loop.
         {
            string propZoneName, propZoneObjectType, propZoneDescription, propZoneLongName, propZoneClassificationCode;
            if (val == 1)
            {
               propZoneName = basePropZoneName;
               propZoneObjectType = basePropZoneObjectType;
               propZoneDescription = basePropZoneDescription;
               propZoneLongName = basePropZoneLongName;
               propZoneClassificationCode = basePropZoneClassificationCode;
            }
            else
            {
               propZoneName = basePropZoneName + " " + val;
               propZoneObjectType = basePropZoneObjectType + " " + val;
               propZoneDescription = basePropZoneDescription + " " + val;
               propZoneLongName = basePropZoneLongName + " " + val;
               propZoneClassificationCode = basePropZoneClassificationCode + " " + val;
            }

            string zoneName;
            string zoneObjectType;
            string zoneDescription;
            string zoneLongName;
            string zoneClassificationCode;
            IFCAnyHandle zoneClassificationReference;

            if (ParameterUtil.GetOptionalStringValueFromElementOrSymbol(element, propZoneName, out zoneName) == null)
               break;

            // If we have an empty zone name, but the value exists, keep looking to make sure there aren't valid values later.
            if (!String.IsNullOrEmpty(zoneName))
            {
               Dictionary<string, IFCAnyHandle> classificationHandles = new Dictionary<string, IFCAnyHandle>();

               ParameterUtil.GetStringValueFromElementOrSymbol(element, propZoneObjectType, out zoneObjectType);

               ParameterUtil.GetStringValueFromElementOrSymbol(element, propZoneDescription, out zoneDescription);

               ParameterUtil.GetStringValueFromElementOrSymbol(element, propZoneLongName, out zoneLongName);

               ParameterUtil.GetStringValueFromElementOrSymbol(element, propZoneClassificationCode, out zoneClassificationCode);
               string classificationName, classificationCode, classificationDescription;

               if (!String.IsNullOrEmpty(zoneClassificationCode))
               {
                  ClassificationUtil.parseClassificationCode(zoneClassificationCode, propZoneClassificationCode, out classificationName, out classificationCode, out classificationDescription);
                  string location = null;
                  ExporterCacheManager.ClassificationLocationCache.TryGetValue(classificationName, out location);
                  zoneClassificationReference = ClassificationUtil.CreateClassificationReference(file, classificationName, classificationCode, classificationDescription, location);
                  classificationHandles.Add(classificationName, zoneClassificationReference);
               }

               IFCAnyHandle roomHandle = productWrapper.GetElementOfType(IFCEntityType.IfcSpace);

               IFCAnyHandle energyAnalysisPSetHnd = null;

               if (exportToCOBIE && !exportedExtraZoneInformation)
               {
                  exportedExtraZoneInformation = CreateGSAInformation(exporterIFC, element, zoneObjectType,
                      classificationHandles, energyAnalysisPSetHnd);
               }

               ZoneInfo zoneInfo = ExporterCacheManager.ZoneInfoCache.Find(zoneName);
               if (zoneInfo == null)
               {
                  IFCAnyHandle zoneCommonPropertySetHandle = CreateZoneCommonPSet(exporterIFC, file, element);
                  zoneInfo = new ZoneInfo(zoneObjectType, zoneDescription, zoneLongName, roomHandle, classificationHandles, energyAnalysisPSetHnd, zoneCommonPropertySetHandle);
                  ExporterCacheManager.ZoneInfoCache.Register(zoneName, zoneInfo);
               }
               else
               {
                  // if description, long name or object type were empty, overwrite.
                  if (!String.IsNullOrEmpty(zoneObjectType) && String.IsNullOrEmpty(zoneInfo.ObjectType))
                     zoneInfo.ObjectType = zoneObjectType;
                  if (!String.IsNullOrEmpty(zoneDescription) && String.IsNullOrEmpty(zoneInfo.Description))
                     zoneInfo.Description = zoneDescription;
                  if (!String.IsNullOrEmpty(zoneLongName) && String.IsNullOrEmpty(zoneInfo.LongName))
                     zoneInfo.LongName = zoneLongName;

                  zoneInfo.RoomHandles.Add(roomHandle);
                  foreach (KeyValuePair<string, IFCAnyHandle> classificationReference in classificationHandles)
                  {
                     if (!zoneInfo.ClassificationReferences[classificationReference.Key].HasValue)
                        zoneInfo.ClassificationReferences[classificationReference.Key] = classificationReference.Value;
                     else
                     {
                        // Delete redundant IfcClassificationReference from file.
                        IFCAnyHandleUtil.Delete(classificationReference.Value);
                     }
                  }

                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(zoneInfo.EnergyAnalysisProperySetHandle))
                     zoneInfo.EnergyAnalysisProperySetHandle = energyAnalysisPSetHnd;
                  else if (energyAnalysisPSetHnd.HasValue)
                     IFCAnyHandleUtil.Delete(energyAnalysisPSetHnd);

                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(zoneInfo.ZoneCommonProperySetHandle))
                     zoneInfo.ZoneCommonProperySetHandle = CreateZoneCommonPSet(exporterIFC, file, element);
               }
            }
         }
      }
   }
}