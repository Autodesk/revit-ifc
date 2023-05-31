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
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export beams.
   /// </summary>
   public class BeamExporter
   {
      /// <summary>
      /// A structure to contain information about the defining axis of a beam.
      /// </summary>
      public class BeamAxisInfo
      {
         /// <summary>
         /// The default constructor.
         /// </summary>
         public BeamAxisInfo()
         {
            Axis = null;
            LCSAsTransform = null;
            AxisDirection = null;
            AxisNormal = null;
         }

         /// <summary>
         /// The curve that represents the beam axis.
         /// </summary>
         public Curve Axis { get; set; }

         /// <summary>
         /// The local coordinate system of the beam used for IFC export as a transform.
         /// </summary>
         public Transform LCSAsTransform { get; set; }

         /// <summary>
         /// The tangent to the axis at the start parameter of the axis curve.
         /// </summary>
         public XYZ AxisDirection { get; set; }

         /// <summary>
         /// The normal to the axis at the start parameter of the axis curve.
         /// </summary>
         public XYZ AxisNormal { get; set; }
      }

      /// <summary>
      /// A structure to contain the body representation of the beam, if it can be expressed as an extrusion, potentially with clippings and openings.
      /// </summary>
      private class BeamBodyAsExtrusionInfo
      {
         /// <summary>
         /// The default constructor.
         /// </summary>
         public BeamBodyAsExtrusionInfo()
         {
            RepresentationHandle = null;
            Materials = null;
            Slope = 0.0;
            DontExport = false;
         }

         /// <summary>
         /// The IFC handle representing the created extrusion, potentially with clippings.
         /// </summary>
         public IFCAnyHandle RepresentationHandle { get; set; }

         /// <summary>
         /// The set of material ids for the beam.  This should usually only contain one material id. (Probably will be deprecated, replaced with MaterialProfile)
         /// </summary>
         public ICollection<ElementId> Materials { get; set; }

         /// <summary>
         /// The material profile set for the extruded Beam
         /// </summary>
         public MaterialAndProfile MaterialAndProfile { get; set; }

         /// <summary>
         /// The calculated slope of the beam along its axis, relative to the XY plane.
         /// </summary>
         public double Slope { get; set; }

         /// <summary>
         /// True if the beam has no geometry to export, and as such attempts to export should stop.
         /// </summary>
         public bool DontExport { get; set; }
      }

      /// <summary>
      /// Get information about the beam axis, if possible.
      /// </summary>
      /// <param name="element">The beam element.</param>
      /// <returns>The BeamAxisInfo structure, or null if the beam has no axis, or it is not a Line or Arc.</returns>
      public static BeamAxisInfo GetBeamAxisTransform(Element element)
      {
         BeamAxisInfo axisInfo = null;

         Transform orientTrf = Transform.Identity;
         XYZ beamDirection = null;
         XYZ projDir = null;
         Curve curve = null;

         LocationCurve locCurve = element.Location as LocationCurve;
         bool canExportAxis = (locCurve != null);

         if (canExportAxis)
         {
            curve = locCurve.Curve;
            if (curve is Line)
            {
               Line line = curve as Line;
               XYZ planeY, planeOrig;
               planeOrig = line.GetEndPoint(0);
               beamDirection = line.Direction;
               beamDirection = beamDirection.Normalize();
               if (Math.Abs(beamDirection.Z) < 0.707)  // approx 1.0/sqrt(2.0)
               {
                  planeY = XYZ.BasisZ.CrossProduct(beamDirection);
               }
               else
               {
                  planeY = XYZ.BasisX.CrossProduct(beamDirection);
               }
               planeY = planeY.Normalize();
               projDir = beamDirection.CrossProduct(planeY);
               orientTrf.BasisX = beamDirection; orientTrf.BasisY = planeY; orientTrf.BasisZ = projDir; orientTrf.Origin = planeOrig;
            }
            else if (curve is Arc)
            {
               XYZ yDir, center;
               Arc arc = curve as Arc;
               beamDirection = arc.XDirection; yDir = arc.YDirection; projDir = arc.Normal; center = arc.Center;
               beamDirection = beamDirection.Normalize();
               yDir = yDir.Normalize();
               if (!MathUtil.IsAlmostZero(beamDirection.DotProduct(yDir)))
               {
                  // ensure that beamDirection and yDir are orthogonal
                  yDir = projDir.CrossProduct(beamDirection);
                  yDir = yDir.Normalize();
               }
               orientTrf.BasisX = beamDirection; orientTrf.BasisY = yDir; orientTrf.BasisZ = projDir; orientTrf.Origin = center;
            }
            else
               canExportAxis = false;
         }

         if (canExportAxis)
         {
            axisInfo = new BeamAxisInfo();
            axisInfo.Axis = curve;
            axisInfo.AxisDirection = beamDirection;
            axisInfo.AxisNormal = projDir;
            axisInfo.LCSAsTransform = orientTrf;
         }

         return axisInfo;
      }

      /// <summary>
      /// Create the handle corresponding to the "Axis" IfcRepresentation for a beam, if possible.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC class.</param>
      /// <param name="element">The beam element.</param>
      /// <param name="catId">The beam category id.</param>
      /// <param name="axisInfo">The optional beam axis information.</param>
      /// <param name="offsetTransform">The optional offset transform applied to the "Body" representation.</param>
      /// <param name="elevation">The optional level elevation.</param>
      /// <returns>The handle, or null if not created.</returns>
      private static IFCAnyHandle CreateBeamAxis(ExporterIFC exporterIFC, Element element, ElementId catId, BeamAxisInfo axisInfo, Transform offsetTransform, double elevation)
      {
         if (axisInfo == null)
            return null;

         Curve curve = axisInfo.Axis;
         XYZ projDir = axisInfo.AxisNormal;
         Transform lcs = axisInfo.LCSAsTransform;

         string representationTypeOpt = "Curve2D";  // This is by IFC2x2+ convention.

         XYZ curveOffset = XYZ.Zero;
         if (offsetTransform != null)
         {
            curveOffset = -(offsetTransform.Origin);
            curveOffset -= new XYZ(0, 0, elevation);
         }
         else
         {
            // Note that we do not have to have any scaling adjustment here, since the curve origin is in the 
            // same internal coordinate system as the curve.
            curveOffset = -lcs.Origin;
         }

         Transform offsetLCS = new Transform(lcs);
         offsetLCS.Origin = XYZ.Zero;
         IList<IFCAnyHandle> axis_items = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
         {
            IFCAnyHandle axisHnd = GeometryUtil.CreatePolyCurveFromCurve(exporterIFC, curve);
            axis_items = new List<IFCAnyHandle>();
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(axisHnd))
            {
               axis_items.Add(axisHnd);
               representationTypeOpt = "Curve3D";        // We use Curve3D for IFC4RV Axis
            }
         }
         else
         {
            IFCGeometryInfo info = IFCGeometryInfo.CreateCurveGeometryInfo(exporterIFC, offsetLCS, projDir, false);
            ExporterIFCUtils.CollectGeometryInfo(exporterIFC, info, curve, curveOffset, true);

            axis_items = info.GetCurves();
         }

         if (axis_items.Count > 0)
         {
            IFCRepresentationIdentifier identifier = IFCRepresentationIdentifier.Axis;
            string identifierOpt = identifier.ToString();   // This is by IFC2x2+ convention.
            IFCAnyHandle contextHandle3d = ExporterCacheManager.Get3DContextHandle(identifier);
            IFCAnyHandle axisRep = RepresentationUtil.CreateShapeRepresentation(exporterIFC, 
               element, catId, contextHandle3d, identifierOpt, representationTypeOpt, 
               axis_items);
            return axisRep;
         }

         return null;
      }

      /// <summary>
      /// Create the "Body" IfcRepresentation for a beam if it is representable by an extrusion, possibly with clippings and openings.
      /// </summary>
      /// <param name="exporterIFC">The exporterIFC class.</param>
      /// <param name="element">The beam element.T</param>
      /// <param name="catId">The category id.</param>
      /// <param name="geomObjects">The list of solids and meshes representing the beam's geometry.
      /// <param name="axisInfo">The beam axis information.</param>
      /// <returns>The BeamBodyAsExtrusionInfo class which contains the created handle (if any) and other information, or null.</returns>
      private static BeamBodyAsExtrusionInfo CreateBeamGeometryAsExtrusion(ExporterIFC exporterIFC, Element element, ElementId catId,
            IList<GeometryObject> geomObjects, BeamAxisInfo axisInfo, out IFCExportBodyParams extrusionData)
      {
         extrusionData = null;
         // If we have a beam with a Linear location line that only has one solid geometry,
         // we will try to use the ExtrusionAnalyzer to generate an extrusion with 0 or more clippings.
         // This code is currently limited in that it will not process beams with openings, so we
         // use other methods below if this one fails.
         if (geomObjects == null || geomObjects.Count != 1 || (!(geomObjects[0] is Solid)) || axisInfo == null || !(axisInfo.Axis is Line))
            return null;

         BeamBodyAsExtrusionInfo info = new BeamBodyAsExtrusionInfo();
         info.DontExport = false;
         info.Materials = new HashSet<ElementId>();
         info.Slope = 0.0;

         Transform orientTrf = axisInfo.LCSAsTransform;

         Solid solid = geomObjects[0] as Solid;

         XYZ beamDirection = orientTrf.BasisX;
         XYZ planeXVec = orientTrf.BasisY.Normalize();
         XYZ planeYVec = orientTrf.BasisZ.Normalize();

         MaterialAndProfile materialAndProfile = null;
         FootPrintInfo footPrintInfo = null;

         string profileName = NamingUtil.GetProfileName(element);

         Plane beamExtrusionBasePlane = GeometryUtil.CreatePlaneByXYVectorsAtOrigin(planeXVec, planeYVec);
         GenerateAdditionalInfo addInfo = GenerateAdditionalInfo.GenerateBody | GenerateAdditionalInfo.GenerateProfileDef;
         ExtrusionExporter.ExtraClippingData extraClippingData = null;
         info.RepresentationHandle = ExtrusionExporter.CreateExtrusionWithClipping(exporterIFC, element, false,
             catId, solid, beamExtrusionBasePlane, orientTrf.Origin, beamDirection, null, 
             out extraClippingData,
             out footPrintInfo, out materialAndProfile, out extrusionData, addInfo: addInfo, profileName: profileName);
         if (extraClippingData.CompletelyClipped)
         {
            info.DontExport = true;
            return null;
         }

         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(info.RepresentationHandle))
         {
            // This is used by the BeamSlopeCalculator.  This should probably be generated automatically by
            // CreateExtrusionWithClipping.
            IFCExtrusionBasis bestAxis = (Math.Abs(beamDirection[0]) > Math.Abs(beamDirection[1])) ?
                IFCExtrusionBasis.BasisX : IFCExtrusionBasis.BasisY;
            info.Slope = GeometryUtil.GetSimpleExtrusionSlope(beamDirection, bestAxis);
            ElementId materialId = BodyExporter.GetBestMaterialIdFromGeometryOrParameter(solid, element);
            if (materialId != ElementId.InvalidElementId)
               info.Materials.Add(materialId);
         }

         if (materialAndProfile != null)
            info.MaterialAndProfile = materialAndProfile;

         return info;
      }

      /// <summary>
      /// Determines the beam geometry to export after removing invisible geometry.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The beam element to be exported.</param>
      /// <param name="geometryElement">The geometry element that contains the beam geometry.</param>
      /// <param name="dontExport">An output value that says that the element shouldn't be exported at all.</param>
      private static IList<GeometryObject> BeamGeometryToExport(ExporterIFC exporterIFC, Element element,
         GeometryElement geometryElement, out bool dontExport)
      {
         dontExport = true;
         if (element == null || geometryElement == null)
            return null;

         IList<GeometryObject> visibleGeomObjects = new List<GeometryObject>();
         {
            SolidMeshGeometryInfo solidMeshInfo = GeometryUtil.GetSplitSolidMeshGeometry(geometryElement);

            IList<Solid> solids = solidMeshInfo.GetSolids();
            IList<Mesh> meshes = solidMeshInfo.GetMeshes();

            visibleGeomObjects = FamilyExporterUtil.RemoveInvisibleSolidsAndMeshes(element.Document, exporterIFC, ref solids, ref meshes);

            // If we found solids and meshes, and they are all invisible, don't export the beam.
            // If we didn't find solids and meshes, we won't export the beam with ExportBeamAsStandardElement, but will allow the generic
            // family export routine to work.
            if ((visibleGeomObjects == null || visibleGeomObjects.Count == 0) && (solids.Count > 0 || meshes.Count > 0))
               return null;
         }

         dontExport = false;
         return visibleGeomObjects;
      }

      /// <summary>
      /// Creates a new IfcBeamType and relates it to the current element.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="wrapper">The ProductWrapper class.</param>
      /// <param name="elementHandle">The element handle.</param>
      /// <param name="element">The element.</param>
      /// <param name="overrideMaterialId">The material id used for the element type.</param>
      public static void ExportBeamType(ExporterIFC exporterIFC, ProductWrapper wrapper, IFCAnyHandle elementHandle, Element element, string predefinedType)
      {
         if (elementHandle == null || element == null)
            return;

         Document doc = element.Document;
         ElementId typeElemId = element.GetTypeId();
         ElementType elementType = doc.GetElement(typeElemId) as ElementType;
         if (elementType == null)
            return;

         string preDefinedTypeSearch = predefinedType;
         if (string.IsNullOrEmpty(preDefinedTypeSearch))
            preDefinedTypeSearch = "NULL";
         IFCExportInfoPair exportType = new IFCExportInfoPair(IFCEntityType.IfcBeamType, preDefinedTypeSearch);
         IFCAnyHandle beamType = ExporterCacheManager.ElementTypeToHandleCache.Find(elementType, exportType);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(beamType))
         {
            ExporterCacheManager.TypeRelationsCache.Add(beamType, elementHandle);
            return;
         }

         // Property sets will be set later.
         beamType = IFCInstanceExporter.CreateBeamType(exporterIFC.GetFile(), elementType, null,
            null, null, predefinedType);

         wrapper.RegisterHandleWithElementType(elementType, exportType, beamType, null);

         ExporterCacheManager.TypeRelationsCache.Add(beamType, elementHandle);
      }

      /// <summary>
      /// Exports a beam to IFC beam if it has an axis representation and only one Solid as its geometry, ideally as an extrusion, potentially with clippings and openings.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element to be exported.</param>
      /// <param name="geometryElement">The geometry element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <param name="dontExport">An output value that says that the element shouldn't be exported at all.</param>
      /// <returns>The created handle.</returns>
      /// <remarks>In the original implementation, the ExportBeam function would export each beam as its own individual geometry (that is, not use representation maps).
      /// For non-standard beams, this could result in massive IFC files.  Now, we use the ExportBeamAsStandardElement function and limit its scope, and instead
      /// resort to the standard FamilyInstanceExporter.ExportFamilyInstanceAsMappedItem for more complicated objects categorized as beams.  This has the following pros and cons:
      /// Pro: possiblity for massively reduced file sizes for files containing repeated complex beam families
      /// Con: some beams that may have had an "Axis" representation before will no longer have them, although this possibility is minimized.
      /// Con: some beams that have 1 Solid and an axis, but that Solid will be heavily faceted, won't be helped by this improvement.
      /// It is intended that we phase out this routine entirely and instead teach ExportFamilyInstanceAsMappedItem how to sometimes export the Axis representation for beams.</remarks>
      public static IFCAnyHandle ExportBeamAsStandardElement(ExporterIFC exporterIFC,
         Element element, IFCExportInfoPair exportType, GeometryElement geometryElement, ProductWrapper productWrapper, out bool dontExport)
      {
         dontExport = true;
         IList<GeometryObject> geomObjects = BeamGeometryToExport(exporterIFC, element, geometryElement, out dontExport);
         if (dontExport)
            return null;

         IFCAnyHandle beam = null;
         IFCFile file = exporterIFC.GetFile();
         MaterialAndProfile materialAndProfile = null;
         IFCAnyHandle materialProfileSet = null;

         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            BeamAxisInfo axisInfo = GetBeamAxisTransform(element);
            bool canExportAxis = (axisInfo != null);

            Curve curve = canExportAxis ? axisInfo.Axis : null;
            XYZ beamDirection = canExportAxis ? axisInfo.AxisDirection : null;
            Transform orientTrf = canExportAxis ? axisInfo.LCSAsTransform : null;

            // Check for containment override
            IFCAnyHandle overrideContainerHnd = null;
            ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainerHnd);
            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, element, null, orientTrf, overrideContainerId, overrideContainerHnd))
            {
               IFCAnyHandle localPlacement = setter.LocalPlacement;
               using (IFCExportBodyParams extrusionCreationData = new IFCExportBodyParams())
               {
                  extrusionCreationData.SetLocalPlacement(localPlacement);
                  if (canExportAxis && (orientTrf.BasisX != null))
                  {
                     extrusionCreationData.CustomAxis = beamDirection;
                     extrusionCreationData.PossibleExtrusionAxes = IFCExtrusionAxes.TryCustom;
                  }
                  else
                     extrusionCreationData.PossibleExtrusionAxes = IFCExtrusionAxes.TryXY;

                  ElementId catId = CategoryUtil.GetSafeCategoryId(element);

                  // There may be an offset to make the local coordinate system
                  // be near the origin.  This offset will be used to move the axis to the new LCS.
                  Transform offsetTransform = null;

                  // The list of materials in the solids or meshes.
                  ICollection<ElementId> materialIds = null;

                  // If the beam is a FamilyInstance, and it uses transformed FamilySymbol geometry only, then
                  // let's only try to CreateBeamGeometryAsExtrusion unsuccesfully once.  Otherwise, we can spend a lot of time trying
                  // unsuccessfully to do so.
                  bool tryToCreateBeamGeometryAsExtrusion = true;

                  //bool useFamilySymbolGeometry = (element is FamilyInstance) ? !ExporterIFCUtils.UsesInstanceGeometry(element as FamilyInstance) : false;
                  bool useFamilySymbolGeometry = (element is FamilyInstance) && !GeometryUtil.UsesInstanceGeometry(element as FamilyInstance);
                  ElementId beamTypeId = element.GetTypeId();
                  if (useFamilySymbolGeometry)
                  {
                     tryToCreateBeamGeometryAsExtrusion = !ExporterCacheManager.CanExportBeamGeometryAsExtrusionCache.ContainsKey(beamTypeId) ||
                        ExporterCacheManager.CanExportBeamGeometryAsExtrusionCache[beamTypeId];
                  }

                  // The representation handle generated from one of the methods below.
                  BeamBodyAsExtrusionInfo extrusionInfo = null;
                  IFCExportBodyParams extrusionData = null;
                  if (tryToCreateBeamGeometryAsExtrusion)
                  {
                     extrusionInfo = CreateBeamGeometryAsExtrusion(exporterIFC, element, catId, geomObjects, axisInfo, out extrusionData);
                     if (useFamilySymbolGeometry)
                        ExporterCacheManager.CanExportBeamGeometryAsExtrusionCache[beamTypeId] = (extrusionInfo != null);
                  }

                  if (extrusionInfo != null && extrusionInfo.DontExport)
                  {
                     dontExport = true;
                     return null;
                  }

                  if (extrusionData != null)
                  {
                     extrusionCreationData.ScaledLength = extrusionData.ScaledLength;
                     extrusionCreationData.ScaledOuterPerimeter = extrusionData.ScaledOuterPerimeter;
                  }

                  IFCAnyHandle repHnd = extrusionInfo?.RepresentationHandle;

                  if (!IFCAnyHandleUtil.IsNullOrHasNoValue(repHnd))
                  {
                     materialIds = extrusionInfo.Materials;
                     extrusionCreationData.Slope = extrusionInfo.Slope;
                     if (extrusionInfo?.MaterialAndProfile?.CrossSectionArea != null)
                        extrusionCreationData.ScaledArea = extrusionInfo.MaterialAndProfile.CrossSectionArea.Value;
                  }
                  else
                  {
                     // Here is where we limit the scope of how complex a case we will still try to export as a standard element.
                     // This is explicitly added so that many curved beams that can be represented by a reasonable facetation because of the
                     // SweptSolidExporter can still have an Axis representation.
                     BodyData bodyData = null;

                     BodyExporterOptions bodyExporterOptions = new BodyExporterOptions(true, ExportOptionsCache.ExportTessellationLevel.ExtraLow);
                     if (ExporterCacheManager.ExportOptionsCache.ExportAs4ReferenceView)
                        bodyExporterOptions.CollectMaterialAndProfile = false;
                     else
                     bodyExporterOptions.CollectMaterialAndProfile = true;

                     if (geomObjects != null && geomObjects.Count == 1 && geomObjects[0] is Solid)
                     {
                        bodyData = BodyExporter.ExportBody(exporterIFC, element, catId, ElementId.InvalidElementId,
                            geomObjects[0], bodyExporterOptions, extrusionCreationData);

                        repHnd = bodyData.RepresentationHnd;
                        materialIds = bodyData.MaterialIds;
                        if (!bodyData.OffsetTransform.IsIdentity)
                           offsetTransform = bodyData.OffsetTransform;
                        materialAndProfile = bodyData.MaterialAndProfile;
                     }
                  }

                  if (IFCAnyHandleUtil.IsNullOrHasNoValue(repHnd))
                  {
                     extrusionCreationData.ClearOpenings();
                     return null;
                  }

                  IList<IFCAnyHandle> representations = new List<IFCAnyHandle>();
                  double elevation = (setter.LevelInfo != null) ? setter.LevelInfo.Elevation : 0.0;
                  IFCAnyHandle axisRep = CreateBeamAxis(exporterIFC, element, catId, axisInfo, offsetTransform, elevation);
                     if (!IFCAnyHandleUtil.IsNullOrHasNoValue(axisRep))
                        representations.Add(axisRep);
                  representations.Add(repHnd);

                  Transform boundingBoxTrf = offsetTransform?.Inverse ?? Transform.Identity;
                  IFCAnyHandle boundingBoxRep = BoundingBoxExporter.ExportBoundingBox(exporterIFC, geometryElement, boundingBoxTrf);
                  if (boundingBoxRep != null)
                     representations.Add(boundingBoxRep);

                  IFCAnyHandle prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, representations);

                  string instanceGUID = GUIDUtil.CreateGUID(element);
                  beam = IFCInstanceExporter.CreateBeam(exporterIFC, element, instanceGUID, ExporterCacheManager.OwnerHistoryHandle, extrusionCreationData.GetLocalPlacement(), prodRep, exportType.ValidatedPredefinedType);


                  IFCAnyHandle mpSetUsage;
                  if (materialProfileSet != null)
                     mpSetUsage = IFCInstanceExporter.CreateMaterialProfileSetUsage(file, materialProfileSet, null, null);

                  productWrapper.AddElement(element, beam, setter, extrusionCreationData, true, exportType);

                  ExportBeamType(exporterIFC, productWrapper, beam, element, exportType.ValidatedPredefinedType);

                  OpeningUtil.CreateOpeningsIfNecessary(beam, element, extrusionCreationData, offsetTransform, exporterIFC,
                      extrusionCreationData.GetLocalPlacement(), setter, productWrapper);

                  FamilyTypeInfo typeInfo = new FamilyTypeInfo();
                  typeInfo.extraParams = extrusionCreationData;
                  PropertyUtil.CreateBeamColumnBaseQuantities(exporterIFC, beam, element, typeInfo, null);

                  if (materialIds.Count != 0)
                     CategoryUtil.CreateMaterialAssociation(exporterIFC, beam, materialIds);

                  // Register the beam's IFC handle for later use by truss and beam system export.
                  ExporterCacheManager.ElementToHandleCache.Register(element.Id, beam, exportType);
               }
            }

            transaction.Commit();
            return beam;
         }
      }
   }
}