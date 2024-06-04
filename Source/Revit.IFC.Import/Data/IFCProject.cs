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
   /// <summary>
   /// Represents an IfcProject.
   /// </summary>
   /// <remarks>In IFC4, IfcProject inherits from IfcContext, which in turn inherits from IfcObjectDefinition.
   /// In IFC2x3, IfcProject inherits from IfcObject, which in turn inherits from IfcObjectDefintion.
   /// In addition, in IFC4, all of the IfcProject specific attributes are at the IfcContext level.
   /// For now, we will:
   /// 1. Keep IfcProject inheriting from IfcObject, and make IfcObject aware that some attributes are not appropriate for IfcProject.
   /// 2. Keep IfcContext attributes at the IfcProject level.
   /// Note also that "LongName" and "Phase" are not yet supported, and that the content of "RepresentationContexts" is stored directly
   /// in IfcProject.</remarks>
   public class IFCProject : IFCObject
   {
      private UV m_TrueNorthDirection = null;

      private Transform m_WorldCoordinateSystem = null;

      private ISet<IFCUnit> m_UnitsInContext = null;

      private IDictionary<string, IFCGridAxis> m_GridAxes = null;

      /// <summary>
      /// The true north direction of the project.
      /// </summary>
      public UV TrueNorthDirection
      {
         get { return m_TrueNorthDirection; }
         protected set { m_TrueNorthDirection = value; }
      }

      /// <summary>
      /// The true north direction of the project.
      /// </summary>
      /// <remarks>Strictly speaking, the WCS is associated with a GeometricRepresentationContext,
      /// and each individual geometry may point to a different one.  In practice, however, we only have
      /// one, a Model one, and it is generally the identity transform, with or without an offset.
      /// The reason we only want to apply this once, at a top level, is that we can get into trouble
      /// with mapped representations - if the representation containing the mapped item and the
      /// representation map both apply the WCS, the object may be transformed twice.</remarks>
      public Transform WorldCoordinateSystem
      {
         get { return m_WorldCoordinateSystem; }
         protected set { m_WorldCoordinateSystem = value; }
      }

      /// <summary>
      /// The units in the project.
      /// </summary>
      public ISet<IFCUnit> UnitsInContext
      {
         get { return m_UnitsInContext; }
      }

      /// <summary>
      /// Constructs an IFCProject from the IfcProject handle.
      /// </summary>
      /// <param name="ifcProject">The IfcProject handle.</param>
      protected IFCProject(IFCAnyHandle ifcProject)
      {
         IFCImportFile.TheFile.IFCProject = this;
         Process(ifcProject);
      }

      /// <summary>
      /// Returns true if sub-elements should be grouped; false otherwise.
      /// </summary>
      public override bool GroupSubElements()
      {
         return false;
      }

      private void UpdateProjectLocation(ProjectLocation projectLocation, XYZ geoRef, double trueNorth)
      {
         ProjectPosition originalPosition = projectLocation?.GetProjectPosition(XYZ.Zero);
         if (originalPosition == null)
         {
            return;
         }

         // If we are using legacy import, we might be doing a re-link where the position changed.
         // If not, then we always defer to the ATF portion.
         XYZ originalRef = Importer.TheOptions.IsHybridImport ?
            new XYZ(originalPosition.EastWest, originalPosition.NorthSouth, originalPosition.Elevation) :
            XYZ.Zero;
         XYZ refToUse = originalRef.IsZeroLength() ? geoRef : originalRef;

         ProjectPosition projectPosition = new ProjectPosition(refToUse.X, refToUse.Y, refToUse.Z, trueNorth);
         projectLocation.SetProjectPosition(XYZ.Zero, projectPosition);
      }

      /// <summary>
      /// Processes IfcProject attributes.
      /// </summary>
      /// <param name="ifcProjectHandle">The IfcProject handle.</param>
      protected override void Process(IFCAnyHandle ifcProjectHandle)
      {
         IFCAnyHandle unitsInContext = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcProjectHandle, "UnitsInContext", false);

         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(unitsInContext))
         {
            IList<IFCAnyHandle> units = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(unitsInContext, "Units");

            if (units != null)
            {
               m_UnitsInContext = new HashSet<IFCUnit>();

               foreach (IFCAnyHandle unit in units)
               {
                  IFCUnit ifcUnit = IFCImportFile.TheFile.IFCUnits.ProcessIFCProjectUnit(unit);
                  if (!IFCUnit.IsNullOrInvalid(ifcUnit))
                     m_UnitsInContext.Add(ifcUnit);
               }
            }
            else
            {
               Importer.TheLog.LogMissingRequiredAttributeError(unitsInContext, "Units", false);
            }
         }

         var application = IFCImportFile.TheFile.Document.Application;
         var projectUnits = IFCImportFile.TheFile.IFCUnits.GetIFCProjectUnit(SpecTypeId.Length);

         IFCImportFile.TheFile.VertexTolerance = application.VertexTolerance;
         IFCImportFile.TheFile.ShortCurveTolerance = application.ShortCurveTolerance;
         Importer.TheProcessor.PostProcessProject(projectUnits?.ScaleFactor, projectUnits?.Unit);

         // We need to process the units before we process the rest of the file, since we will scale values as we go along.
         base.Process(ifcProjectHandle);

         // process true north - take the first valid representation context that has a true north value.
         HashSet<IFCAnyHandle> repContexts = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(ifcProjectHandle, "RepresentationContexts");

         bool hasMapConv = false;
         XYZ geoRef = XYZ.Zero;
         string geoRefName = null;
         double trueNorth = 0.0;
         if (repContexts != null)
         {
            IFCAnyHandle mapConv = null;

            foreach (IFCAnyHandle geomRepContextHandle in repContexts)
            {
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(geomRepContextHandle) &&
                   IFCAnyHandleUtil.IsSubTypeOf(geomRepContextHandle, IFCEntityType.IfcGeometricRepresentationContext))
               {
                  IFCRepresentationContext context = IFCRepresentationContext.ProcessIFCRepresentationContext(geomRepContextHandle);
                  if (TrueNorthDirection == null && context.TrueNorth != null)
                  {
                     // TODO: Verify that we don't have inconsistent true norths.  If we do, warn.
                     TrueNorthDirection = new UV(context.TrueNorth.X, context.TrueNorth.Y);
                  }

                  if (WorldCoordinateSystem == null && context.WorldCoordinateSystem != null && !context.WorldCoordinateSystem.IsIdentity)
                  {
                     WorldCoordinateSystem = context.WorldCoordinateSystem;
                  }

                  // Process Map Conversion if any
                  HashSet<IFCAnyHandle> coordOperation = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(geomRepContextHandle, "HasCoordinateOperation");
                  if (coordOperation != null)
                  {
                     if (coordOperation.Count > 0)
                     {
                        if (IFCAnyHandleUtil.IsSubTypeOf(coordOperation.FirstOrDefault(), IFCEntityType.IfcMapConversion))
                        {
                           hasMapConv = true;
                           mapConv = coordOperation.FirstOrDefault();
                           bool found = false;
                           double eastings = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(mapConv, "Eastings", out found);
                           if (!found)
                              eastings = 0.0;
                           double northings = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(mapConv, "Northings", out found);
                           if (!found)
                              northings = 0.0;
                           double orthogonalHeight = IFCImportHandleUtil.GetRequiredScaledLengthAttribute(mapConv, "OrthogonalHeight", out found);
                           if (!found)
                              orthogonalHeight = 0.0;
                           double xAxisAbs = IFCImportHandleUtil.GetOptionalRealAttribute(mapConv, "XAxisAbscissa", 1.0);
                           double xAxisOrd = IFCImportHandleUtil.GetOptionalRealAttribute(mapConv, "XAxisOrdinate", 0.0);
                           trueNorth = Math.Atan2(xAxisOrd, xAxisAbs);
                           //angleToNorth = -((xAxisAngle > -Math.PI / 2.0) ? xAxisAngle - Math.PI / 2.0 : xAxisAngle + Math.PI * 1.5);
                           double scale = IFCImportHandleUtil.GetOptionalRealAttribute(mapConv, "Scale", 1.0);
                           geoRef = new XYZ(scale * eastings, scale * northings, scale * orthogonalHeight);

                           // Process the IfcProjectedCRS
                           IFCAnyHandle projCRS = IFCAnyHandleUtil.GetInstanceAttribute(mapConv, "TargetCRS");
                           if (projCRS != null && IFCAnyHandleUtil.IsSubTypeOf(projCRS, IFCEntityType.IfcProjectedCRS))
                           {
                              geoRefName = IFCImportHandleUtil.GetRequiredStringAttribute(projCRS, "Name", false);
                              string desc = IFCImportHandleUtil.GetOptionalStringAttribute(projCRS, "Description", null);
                              string geodeticDatum = IFCImportHandleUtil.GetOptionalStringAttribute(projCRS, "GeodeticDatum", null);
                              string verticalDatum = IFCImportHandleUtil.GetOptionalStringAttribute(projCRS, "VerticalDatum", null);
                              string mapProj = IFCImportHandleUtil.GetOptionalStringAttribute(projCRS, "MapProjection", null);
                              string mapZone = IFCImportHandleUtil.GetOptionalStringAttribute(projCRS, "MapZone", null);
                              IFCAnyHandle mapUnit = IFCImportHandleUtil.GetOptionalInstanceAttribute(projCRS, "MapUnit");

                              Document doc = IFCImportFile.TheFile.Document;
                              ProjectInfo projectInfo = doc.ProjectInformation;

                              // We add this here because we want to make sure that external processors (e.g., Navisworks)
                              // get a chance to add a container for the parameters that get added below.  In general,
                              // we should probably augment Processor.AddParameter to ensure that CreateOrUpdateElement
                              // is called before anything is attempted to be added.  This is a special case, though,
                              // as in Revit we don't actually create an element for the IfcProject.
                              if (!Importer.IsDefaultProcessor())
                              {
                                 Importer.TheProcessor.CreateOrUpdateElement(Id, GlobalId, EntityType.ToString(), GetCategoryId(doc).Value, null);
                              }

                              Category category = IFCPropertySet.GetCategoryForParameterIfValid(projectInfo, Id);

                              using (ParameterSetter setter = new ParameterSetter())
                              {
                                 ParametersToSet parametersToSet = setter.ParametersToSet;
                                 parametersToSet.AddStringParameter(doc, projectInfo, category, this, "IfcProjectedCRS.Name", geoRefName, Id);
                                 if (!string.IsNullOrEmpty(desc))
                                    parametersToSet.AddStringParameter(doc, projectInfo, category, this, "IfcProjectedCRS.Description", desc, Id);
                                 if (!string.IsNullOrEmpty(geodeticDatum))
                                    parametersToSet.AddStringParameter(doc, projectInfo, category, this, "IfcProjectedCRS.GeodeticDatum", geodeticDatum, Id);
                                 if (!string.IsNullOrEmpty(verticalDatum))
                                    parametersToSet.AddStringParameter(doc, projectInfo, category, this, "IfcProjectedCRS.VerticalDatum", verticalDatum, Id);
                                 if (!string.IsNullOrEmpty(mapProj))
                                    parametersToSet.AddStringParameter(doc, projectInfo, category, this, "IfcProjectedCRS.MapProjection", mapProj, Id);
                                 if (!string.IsNullOrEmpty(mapZone))
                                    parametersToSet.AddStringParameter(doc, projectInfo, category, this, "IfcProjectedCRS.MapZone", mapZone, Id);

                                 if (!IFCAnyHandleUtil.IsNullOrHasNoValue(mapUnit))
                                 {
                                    IFCUnit mapUnitIfc = IFCUnit.ProcessIFCUnit(mapUnit);
                                    string unitStr = UnitUtils.GetTypeCatalogStringForUnit(mapUnitIfc.Unit);
                                    parametersToSet.AddStringParameter(doc, projectInfo, category, this, "IfcProjectedCRS.MapUnit", unitStr, Id);
                                    double convFactor = UnitUtils.Convert(1.0, mapUnitIfc.Unit, IFCImportFile.TheFile.IFCUnits.GetIFCProjectUnit(SpecTypeId.Length).Unit);
                                    eastings = convFactor * eastings;
                                    northings = convFactor * northings;
                                    orthogonalHeight = convFactor * orthogonalHeight;
                                    geoRef = new XYZ(eastings, northings, orthogonalHeight);
                                 }
                              }
                           }
                        }
                     }
                  }
               }
            }

            ProjectLocation projectLocation = IFCImportFile.TheFile.Document.ActiveProjectLocation;
            if (projectLocation != null)
            {
               if (hasMapConv)
               {
                  UpdateProjectLocation(projectLocation, geoRef, trueNorth);

                  if (!string.IsNullOrEmpty(geoRefName))
                  {
                     try
                     {
                        IFCImportFile.TheFile.Document.SiteLocation.SetGeoCoordinateSystem(geoRefName);
                     }
                     catch
                     {
                        Importer.TheLog.LogError(mapConv?.Id ?? -1, geoRefName + " is not a recognized coordinate system.", false);
                     }
                  }
               }
               else
               {
                  // Set initial project location based on the information above.
                  // This may be further modified by the site.
                  trueNorth = 0.0;
                  if (TrueNorthDirection != null)
                  {
                     trueNorth = -Math.Atan2(-TrueNorthDirection.U, TrueNorthDirection.V);
                  }

                  // TODO: Extend this to work properly if the world coordinate system
                  // isn't a simple translation.
                  XYZ origin = XYZ.Zero;
                  if (WorldCoordinateSystem != null)
                  {
                     geoRef = WorldCoordinateSystem.Origin;
                     double angleRot = Math.Atan2(WorldCoordinateSystem.BasisX.Y, WorldCoordinateSystem.BasisX.X);

                     // If it is translation only, or if the WCS rotation is equal to trueNorth, we assume they are the same
                     if (WorldCoordinateSystem.IsTranslation
                        || MathUtil.IsAlmostEqual(angleRot, trueNorth))
                     {
                        WorldCoordinateSystem = null;
                     }
                     else
                     {
                        // If the trueNorth is not set (=0), set the trueNorth by the rotation of the WCS, otherwise add the angle                       
                        if (MathUtil.IsAlmostZero(trueNorth))
                           trueNorth = angleRot;
                        else
                           trueNorth += angleRot;

                        WorldCoordinateSystem = null;
                     }
                  }

                  UpdateProjectLocation(projectLocation, geoRef, trueNorth);
               }
            }
         }
      }

      /// <summary>
      /// The list of grid axes in this IFCProject, sorted by Revit name.
      /// </summary>
      public IDictionary<string, IFCGridAxis> GridAxes
      {
         get
         {
            if (m_GridAxes == null)
               m_GridAxes = new Dictionary<string, IFCGridAxis>();
            return m_GridAxes;
         }
      }

      /// <summary>
      /// Allow for override of IfcObjectDefinition shared parameter names.
      /// </summary>
      /// <param name="name">The enum corresponding of the shared parameter.</param>
      /// <param name="isType">True if the shared parameter is a type parameter.</param>
      /// <returns>The name appropriate for this IfcObjectDefinition.</returns>
      public override string GetSharedParameterName(IFCSharedParameters name, bool isType)
      {
         if (!isType)
         {
            switch (name)
            {
               case IFCSharedParameters.IfcName:
                  return "IfcProject Name";
               case IFCSharedParameters.IfcDescription:
                  return "IfcProject Description";
            }
         }

         return base.GetSharedParameterName(name, isType);
      }

      /// <summary>
      /// Get the element ids created for this entity, for summary logging.
      /// </summary>
      /// <param name="createdElementIds">The creation list.</param>
      /// <remarks>May contain InvalidElementId; the caller is expected to remove it.</remarks>
      public override void GetCreatedElementIds(ISet<ElementId> createdElementIds)
      {
         // If we used ProjectInformation, don't report that.
         if (CreatedElementId != ElementId.InvalidElementId && CreatedElementId != Importer.TheCache.ProjectInformationId)
         {
            createdElementIds.Add(CreatedElementId);
         }
      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      protected override void Create(Document doc)
      {
         if (UnitsInContext != null)
         {
            Units documentUnits = new Units(doc.DisplayUnitSystem == DisplayUnit.METRIC ?
                UnitSystem.Metric : UnitSystem.Imperial);

            foreach (IFCUnit unit in UnitsInContext)
            {
               if (!IFCUnit.IsNullOrInvalid(unit))
               {
                  try
                  {
                     FormatOptions formatOptions = new FormatOptions(unit.Unit);
                     formatOptions.SetSymbolTypeId(unit.Symbol);
                     documentUnits.SetFormatOptions(unit.Spec, formatOptions);
                  }
                  catch (Exception ex)
                  {
                     Importer.TheLog.LogError(unit.Id, ex.Message, false);
                  }
               }
            }
            doc.SetUnits(documentUnits);
         }

         // We will randomize unused grid names so that they don't conflict with new entries with the same name.
         // This is only for relink.
         foreach (ElementId gridId in Importer.TheCache.GridNameToElementMap.Values)
         {
            Grid grid = doc.GetElement(gridId) as Grid;
            if (grid == null)
               continue;

            // Note that new Guid() is useless - it creates a GUID of all 0s.
            grid.Name = Guid.NewGuid().ToString();
         }

         // Pre-process sites to orient them properly.
         IList<IFCSite> sites = new List<IFCSite>();
         foreach (IFCObjectDefinition objectDefinition in ComposedObjectDefinitions)
         {
            if (objectDefinition is IFCSite)
            {
               sites.Add(objectDefinition as IFCSite);
            }
         }
         IFCSite.ProcessSiteLocations(doc, sites);
         IFCSite.FindDefaultSite(sites);
               
         base.Create(doc);

         // IfcProject usually won't create an element, as it contains no geometry.
         // If it doesn't, use the ProjectInfo element in the document to store its parameters.
         if (CreatedElementId == ElementId.InvalidElementId)
            CreatedElementId = Importer.TheCache.ProjectInformationId;
      }

      /// <summary>
      /// Processes an IfcProject object.
      /// </summary>
      /// <param name="ifcProject">The IfcProject handle.</param>
      /// <returns>The IFCProject object.</returns>
      public static IFCProject ProcessIFCProject(IFCAnyHandle ifcProject)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcProject))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcProject);
            return null;
         }

         IFCEntity project;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcProject.StepId, out project))
            return (project as IFCProject);

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcProject, IFCEntityType.IfcProject))
         {
            return new IFCProject(ifcProject);
         }

         //LOG: ERROR: Not processed project.
         return null;
      }
   }
}