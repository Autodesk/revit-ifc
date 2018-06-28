//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
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
using Autodesk.Revit.DB.Structure;
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
   class RebarExporter
   {
      /// <summary>
      /// A class to allow for a delayed add to the product wrapper.  Used by Rebars to determine if a level association is necessary or not.
      /// </summary>
      class DelayedProductWrapper
      {
         /// <summary>
         /// The constructor for the class.
         /// </summary>
         /// <param name="rebarElement">The element that created the rebar.</param>
         /// <param name="elementHandle">The associated IFC element handle.</param>
         /// <param name="levelInfo">The information for the associated level.</param>
         public DelayedProductWrapper(Element rebarElement, IFCAnyHandle elementHandle, IFCLevelInfo levelInfo)
         {
            RebarElement = rebarElement;
            ElementHandle = elementHandle;
            LevelInfo = levelInfo;
         }

         public Element RebarElement { get; protected set; }

         public IFCAnyHandle ElementHandle { get; protected set; }

         public IFCLevelInfo LevelInfo { get; protected set; }
      }

      private static bool ElementIsContainedInAssembly(Element element)
      {
         if (element == null)
            return false;

         return (element.AssemblyInstanceId != ElementId.InvalidElementId);
      }

      /// <summary>
      /// Exports a Rebar, AreaReinforcement or PathReinforcement to IFC ReinforcingBar.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="element">The element.</param>
      /// <param name="productWrapper">The product wrapper.</param>
      public static void Export(ExporterIFC exporterIFC, Element element, ProductWrapper productWrapper)
      {
         ISet<DelayedProductWrapper> createdRebars = new HashSet<DelayedProductWrapper>();

         if (element is Rebar)
         {
            createdRebars = ExportRebar(exporterIFC, element, productWrapper);
         }
         else if (element is AreaReinforcement)
         {
            AreaReinforcement areaReinforcement = element as AreaReinforcement;
            IList<ElementId> rebarIds = areaReinforcement.GetRebarInSystemIds();

            Document doc = areaReinforcement.Document;
            foreach (ElementId id in rebarIds)
            {
               Element rebarInSystem = doc.GetElement(id);
               ISet<DelayedProductWrapper> newlyCreatedRebars = ExportRebar(exporterIFC, rebarInSystem, productWrapper);
               if (newlyCreatedRebars != null)
                  createdRebars.UnionWith(newlyCreatedRebars);
            }
         }
         else if (element is PathReinforcement)
         {
            PathReinforcement pathReinforcement = element as PathReinforcement;
            IList<ElementId> rebarIds = pathReinforcement.GetRebarInSystemIds();

            Document doc = pathReinforcement.Document;
            foreach (ElementId id in rebarIds)
            {
               Element rebarInSystem = doc.GetElement(id);
               ISet<DelayedProductWrapper> newlyCreatedRebars = ExportRebar(exporterIFC, rebarInSystem, productWrapper);
               if (newlyCreatedRebars != null)
                  createdRebars.UnionWith(newlyCreatedRebars);
            }
         }
         else if (element is RebarContainer)
         {
            int itemIndex = 1;
            RebarContainer rebarContainer = element as RebarContainer;
            foreach (RebarContainerItem rebarContainerItem in rebarContainer)
            {
               ISet<DelayedProductWrapper> newlyCreatedRebars = ExportRebar(exporterIFC, rebarContainerItem, rebarContainer, itemIndex, productWrapper);
               if (newlyCreatedRebars != null)
               {
                  itemIndex += createdRebars.Count;
                  createdRebars.UnionWith(newlyCreatedRebars);
               }
            }
         }

         if (createdRebars != null && createdRebars.Count != 0)
         {
            string guid = GUIDUtil.CreateGUID(element);

            // Create a group to hold all of the created IFC entities, if the rebars aren't already in an assembly or a group.  
            // We want to avoid nested groups of groups of rebars.
            bool relateToLevel = true;
            bool groupRebarHandles = (createdRebars.Count != 1);
            foreach (DelayedProductWrapper delayedProductWrapper in createdRebars)
            {
               if (ElementIsContainedInAssembly(delayedProductWrapper.RebarElement))
               {
                  groupRebarHandles = false;
                  relateToLevel = false;
                  break;
               }
            }

            ISet<IFCAnyHandle> createdRebarHandles = new HashSet<IFCAnyHandle>();
            foreach (DelayedProductWrapper delayedProductWrapper in createdRebars)
            {
               IFCAnyHandle currentRebarHandle = delayedProductWrapper.ElementHandle;
               productWrapper.AddElement(delayedProductWrapper.RebarElement, currentRebarHandle, delayedProductWrapper.LevelInfo, null, relateToLevel);
               createdRebarHandles.Add(currentRebarHandle);
            }

            if (createdRebars.Count > 1)
            {
               if (groupRebarHandles)
               {
                  // Check the intended IFC entity or type name is in the exclude list specified in the UI
                  Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcGroup;
                  if (!ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
                  {
                     IFCFile file = exporterIFC.GetFile();
                     using (IFCTransaction tr = new IFCTransaction(file))
                     {
                        IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
                        string revitObjectType = NamingUtil.GetFamilyAndTypeName(element);
                        string name = NamingUtil.GetNameOverride(element, revitObjectType);
                        string description = NamingUtil.GetDescriptionOverride(element, null);
                        string objectType = NamingUtil.GetObjectTypeOverride(element, revitObjectType);

                        IFCAnyHandle rebarGroup = IFCInstanceExporter.CreateGroup(file, guid,
                            ownerHistory, name, description, objectType);

                        productWrapper.AddElement(element, rebarGroup);

                        IFCInstanceExporter.CreateRelAssignsToGroup(file, GUIDUtil.CreateGUID(), ownerHistory,
                            null, null, createdRebarHandles, null, rebarGroup);

                        tr.Commit();
                     }
                  }
               }
            }
            else
            {
               // We will update the GUID of the one created IfcReinforcingElement to be the element GUID.
               // This will allow the IfcGUID parameter to be use/set if appropriate.
               ExporterUtil.SetGlobalId(createdRebarHandles.ElementAt(0), guid);
            }
         }
      }

      private static IFCReinforcingBarRole GetReinforcingBarRole(string role)
      {
         if (String.IsNullOrWhiteSpace(role))
            return IFCReinforcingBarRole.NotDefined;

         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(role, "Main"))
            return IFCReinforcingBarRole.Main;
         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(role, "Shear"))
            return IFCReinforcingBarRole.Shear;
         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(role, "Ligature"))
            return IFCReinforcingBarRole.Ligature;
         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(role, "Stud"))
            return IFCReinforcingBarRole.Stud;
         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(role, "Punching"))
            return IFCReinforcingBarRole.Punching;
         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(role, "Edge"))
            return IFCReinforcingBarRole.Edge;
         if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(role, "Ring"))
            return IFCReinforcingBarRole.Ring;
         return IFCReinforcingBarRole.UserDefined;
      }

      /// <summary>
      /// A special case function that can export a Rebar element as an IfcBuildingElementProxy for view-specific exports where the exact geometry of the rebar matters.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="rebarElement">The rebar element to be exported.</param>
      /// <param name="productWrapper">The ProductWrapper object.</param>
      /// <param name="cannotExportRebar">True if we tried to create an IFC entity but failed.</param>
      /// <returns>True if the rebar was exported here, false otherwise.</returns>
      /// <remarks>This functionality may be obsoleted in the future.</remarks>
      private static IFCAnyHandle ExportRebarAsProxyElementInView(ExporterIFC exporterIFC, Element rebarElement, ProductWrapper productWrapper, out bool cannotExportRebar)
      {
         IFCAnyHandle rebarEntity = null;
         cannotExportRebar = false;

         if (rebarElement is Rebar && ExporterCacheManager.ExportOptionsCache.FilterViewForExport != null)
         {
            // The only options handled here is IfcBuildingElementProxy.
            // Not Exported is handled previously, and ReinforcingBar vs Mesh will be handled later.
            string ifcEnumType;
            IFCExportInfoPair exportType = ExporterUtil.GetExportType(exporterIFC, rebarElement, out ifcEnumType);

            if (exportType.ExportInstance == IFCEntityType.IfcBuildingElementProxy ||
                exportType.ExportType == IFCEntityType.IfcBuildingElementProxyType)
            {
               Rebar rebar = rebarElement as Rebar;
               GeometryElement rebarGeometry = rebar.GetFullGeometryForView(ExporterCacheManager.ExportOptionsCache.FilterViewForExport);
               if (rebarGeometry != null)
                  rebarEntity = ProxyElementExporter.ExportBuildingElementProxy(exporterIFC, rebarElement, rebarGeometry, productWrapper);

               cannotExportRebar = IFCAnyHandleUtil.IsNullOrHasNoValue(rebarEntity);
            }
         }

         return rebarEntity;
      }

      /// <summary>
      /// Exports a Rebar to IFC ReinforcingBar.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="rebarItem">The rebar to be exported.  This might be an element or a sub-element.</param>
      /// <param name="rebarElement">The element that contains the rebar to be exported.  This may be the same as rebarItem.</param>
      /// <param name="itemIndex">If greater than 0, the index of the first rebar in the rebarItem in the rebarElement, used for naming and GUID creation.</param>
      /// <param name="productWrapper">The ProductWrapper object.</param>
      /// <returns>The set of handles created, to add to the ProductWrapper in the calling function.</returns>
      private static ISet<DelayedProductWrapper> ExportRebar(ExporterIFC exporterIFC, object rebarItem, Element rebarElement, int itemIndex, ProductWrapper productWrapper)
      {
         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcReinforcingBar;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return null;

         IFCFile file = exporterIFC.GetFile();
         HashSet<DelayedProductWrapper> createdRebars = new HashSet<DelayedProductWrapper>();

         int rebarQuantity = GetRebarQuantity(rebarItem);
         if (rebarQuantity == 0)
            return null;

         using (IFCTransaction transaction = new IFCTransaction(file))
         {
            using (PlacementSetter setter = PlacementSetter.Create(exporterIFC, rebarElement))
            {
               bool cannotExportRebar = false;
               IFCAnyHandle rebarHandle = ExportRebarAsProxyElementInView(exporterIFC, rebarElement, productWrapper, out cannotExportRebar);
               if (!IFCAnyHandleUtil.IsNullOrHasNoValue(rebarHandle) || cannotExportRebar)
               {
                  if (!cannotExportRebar)
                     transaction.Commit();
                  return null;   // Rebar doesn't create a group.
               }

               IFCAnyHandle prodRep = null;

               double totalBarLengthUnscale = GetRebarTotalLength(rebarItem);
               double volumeUnscale = GetRebarVolume(rebarItem);
               double totalBarLength = UnitUtil.ScaleLength(totalBarLengthUnscale);

               if (MathUtil.IsAlmostZero(totalBarLength))
                  return null;

               ElementId materialId = ElementId.InvalidElementId;
               ParameterUtil.GetElementIdValueFromElementOrSymbol(rebarElement, BuiltInParameter.MATERIAL_ID_PARAM, out materialId);

               double diameter = GetBarDiameter(rebarItem);
               double radius = diameter / 2.0;
               double longitudinalBarNominalDiameter = diameter;
               double longitudinalBarCrossSectionArea = UnitUtil.ScaleArea(volumeUnscale / totalBarLengthUnscale);

               int numberOfBarPositions = GetNumberOfBarPositions(rebarItem);

               string steelGrade = NamingUtil.GetOverrideStringValue(rebarElement, "SteelGrade", null);

               // Allow use of IFC2x3 or IFC4 naming.
               string predefinedType = NamingUtil.GetOverrideStringValue(rebarElement, "BarRole", null);
               if (string.IsNullOrWhiteSpace(predefinedType))
                  predefinedType = NamingUtil.GetOverrideStringValue(rebarElement, "PredefinedType", null);
               IFCReinforcingBarRole role = GetReinforcingBarRole(predefinedType);

               string origRebarName = NamingUtil.GetNameOverride(rebarElement, NamingUtil.GetIFCName(rebarElement));

               const int maxBarGUIDS = IFCReinforcingBarSubElements.BarEnd - IFCReinforcingBarSubElements.BarStart + 1;
               ElementId categoryId = CategoryUtil.GetSafeCategoryId(rebarElement);

               IFCAnyHandle originalPlacement = setter.LocalPlacement;

               // Potential issue : totalBarLength has a rounded value but individual lengths (from centerlines) do not have rounded values.
               // Also dividing a rounded totalBarLength does not result in barLength rounded by the same round value.
               double barLength = totalBarLength / rebarQuantity;
               IList<Curve> baseCurves = GetRebarCenterlineCurves(rebarItem, true, false, false);

               ElementId barLengthParamId = new ElementId(BuiltInParameter.REBAR_ELEM_LENGTH);
               ParameterSet rebarElementParams = rebarElement.Parameters;
               for (int ii = 0; ii < numberOfBarPositions; ii++)
               {
                  if (!DoesBarExistAtPosition(rebarItem, ii))
                     continue;

                  Rebar rebar = rebarElement as Rebar;
                  if ((rebar != null) && (rebar.DistributionType == DistributionType.VaryingLength || rebar.IsRebarFreeForm()))
                  {
                     baseCurves = GetRebarCenterlineCurves(rebar, true, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, ii);
                     DoubleParameterValue barLengthParamVal = rebar.GetParameterValueAtIndex(barLengthParamId, ii) as DoubleParameterValue;
                     if (barLengthParamVal != null)
                        barLength = barLengthParamVal.Value;
                  }


                  string rebarNameFormated = origRebarName;

                  int indexForNamingAndGUID = (itemIndex > 0) ? ii + itemIndex : ii + 1;

                  string rebarName = NamingUtil.GetNameOverride(rebarElement, rebarNameFormated + ": " + indexForNamingAndGUID);

                  Transform barTrf = GetBarPositionTransform(rebarItem, ii);

                  IList<Curve> curves = new List<Curve>();
                  double endParam = 0.0;
                  foreach (Curve baseCurve in baseCurves)
                  {
                     if (baseCurve is Arc || baseCurve is Ellipse)
                     {
                        if (baseCurve.IsBound)
                           endParam += UnitUtil.ScaleAngle(baseCurve.GetEndParameter(1) - baseCurve.GetEndParameter(0));
                        else
                           endParam += UnitUtil.ScaleAngle(2 * Math.PI);
                     }
                     else
                        endParam += 1.0;
                     curves.Add(baseCurve.CreateTransformed(barTrf));
                  }

                  IFCAnyHandle compositeCurve = GeometryUtil.CreateCompositeCurve(exporterIFC, curves);
                  IFCAnyHandle sweptDiskSolid = IFCInstanceExporter.CreateSweptDiskSolid(file, compositeCurve, radius, null, 0, endParam);
                  HashSet<IFCAnyHandle> bodyItems = new HashSet<IFCAnyHandle>();
                  bodyItems.Add(sweptDiskSolid);

                  IFCAnyHandle shapeRep = RepresentationUtil.CreateAdvancedSweptSolidRep(exporterIFC, rebarElement, categoryId, exporterIFC.Get3DContextHandle("Body"), bodyItems, null);
                  IList<IFCAnyHandle> shapeReps = new List<IFCAnyHandle>();
                  shapeReps.Add(shapeRep);
                  prodRep = IFCInstanceExporter.CreateProductDefinitionShape(file, null, null, shapeReps);

                  IFCAnyHandle copyLevelPlacement = (ii == 0) ? originalPlacement : ExporterUtil.CopyLocalPlacement(file, originalPlacement);

                  string rebarGUID = (indexForNamingAndGUID < maxBarGUIDS) ?
                      GUIDUtil.CreateSubElementGUID(rebarElement, indexForNamingAndGUID + (int)IFCReinforcingBarSubElements.BarStart - 1) :
                      GUIDUtil.CreateGUID();
                  IFCAnyHandle elemHnd = IFCInstanceExporter.CreateReinforcingBar(exporterIFC, rebarElement, rebarGUID, ExporterCacheManager.OwnerHistoryHandle,
                     copyLevelPlacement, prodRep, steelGrade, longitudinalBarNominalDiameter, longitudinalBarCrossSectionArea, barLength, role, null);
                  IFCAnyHandleUtil.OverrideNameAttribute(elemHnd, rebarName);

                  // We will not add the element ot the productWrapper here, but instead in the function that calls
                  // ExportRebar.  The reason for this is that we don't currently know if the handles such be associated
                  // to the level or not, depending on whether they will or won't be grouped.
                  createdRebars.Add(new DelayedProductWrapper(rebarElement, elemHnd, setter.LevelInfo));

                  CacheSubelementParameterValues(rebarElement, rebarElementParams, ii, elemHnd);

                  ExporterCacheManager.HandleToElementCache.Register(elemHnd, rebarElement.Id);
                  CategoryUtil.CreateMaterialAssociation(exporterIFC, elemHnd, materialId);
               }
            }
            transaction.Commit();
         }
         return createdRebars;
      }

      /// <summary>
      /// Exports a Rebar to IFC ReinforcingBar.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="rebarElement">The rebar to be exported.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <returns>The list of IfcReinforcingBar handles created.</returns>
      private static ISet<DelayedProductWrapper> ExportRebar(ExporterIFC exporterIFC, Element rebarElement, ProductWrapper productWrapper)
      {
         return ExportRebar(exporterIFC, rebarElement, rebarElement, 0, productWrapper);
      }

      /// <summary>
      /// Gets total length of a rebar.
      /// </summary>
      /// <param name="element">The rebar element.</param>
      /// <returns>The length.</returns>
      static double GetRebarTotalLength(object element)
      {
         if (element is Rebar)
         {
            return (element as Rebar).TotalLength;
         }
         else if (element is RebarInSystem)
         {
            return (element as RebarInSystem).TotalLength;
         }
         else if (element is RebarContainerItem)
         {
            return (element as RebarContainerItem).TotalLength;
         }
         else
            throw new ArgumentException("Not a rebar.");
      }


      /// <summary>
      /// Gets volume of a rebar.
      /// </summary>
      /// <param name="element">The rebar element.</param>
      /// <returns>The volume.</returns>
      static double GetRebarVolume(object element)
      {
         if (element is Rebar)
         {
            return (element as Rebar).Volume;
         }
         else if (element is RebarInSystem)
         {
            return (element as RebarInSystem).Volume;
         }
         else if (element is RebarContainerItem)
         {
            return (element as RebarContainerItem).Volume;
         }
         else
            throw new ArgumentException("Not a rebar.");
      }

      /// <summary>
      /// Gets quantity of a rebar.
      /// </summary>
      /// <param name="element">The rebar element.</param>
      /// <returns>The number.</returns>
      static int GetRebarQuantity(object element)
      {
         if (element is Rebar)
         {
            return (element as Rebar).Quantity;
         }
         else if (element is RebarInSystem)
         {
            return (element as RebarInSystem).Quantity;
         }
         else if (element is RebarContainerItem)
         {
            return (element as RebarContainerItem).Quantity;
         }
         else
            throw new ArgumentException("Not a rebar.");
      }

      /// <summary>
      /// Gets center line curves of a rebar.
      /// </summary>
      /// <param name="element">The rebar element.</param>
      /// <param name="adjustForSelfIntersection">Identifies if curves should be adjusted to avoid intersection.</param>
      /// <param name="suppressHooks">Identifies if the chain will include hooks curves.</param>
      /// <param name="suppressBendRadius">Identifies if the connected chain will include unfilled curves.</param>
      /// <returns></returns>
      static IList<Curve> GetRebarCenterlineCurves(object element, bool adjustForSelfIntersection, bool suppressHooks, bool suppressBendRadius,
        MultiplanarOption multiplanarOption = MultiplanarOption.IncludeOnlyPlanarCurves, int barPositionIndex = 0)
      {
         if (element is Rebar)
         {
            return (element as Rebar).GetCenterlineCurves(adjustForSelfIntersection, suppressHooks, suppressBendRadius, multiplanarOption, barPositionIndex);
         }
         else if (element is RebarInSystem)
         {
            return (element as RebarInSystem).GetCenterlineCurves(adjustForSelfIntersection, suppressHooks, suppressBendRadius);
         }
         else if (element is RebarContainerItem)
         {
            return (element as RebarContainerItem).GetCenterlineCurves(adjustForSelfIntersection, suppressHooks, suppressBendRadius);
         }
         else
            throw new ArgumentException("Not a rebar.");
      }

      /// <summary>
      /// Gets number of rebar positions.
      /// </summary>
      /// <param name="element">The rebar element.</param>
      /// <returns>The number.</returns>
      static int GetNumberOfBarPositions(object element)
      {
         if (element is Rebar)
         {
            return (element as Rebar).NumberOfBarPositions;
         }
         else if (element is RebarInSystem)
         {
            return (element as RebarInSystem).NumberOfBarPositions;
         }
         else if (element is RebarContainerItem)
         {
            return (element as RebarContainerItem).NumberOfBarPositions;
         }
         else
            throw new ArgumentException("Not a rebar.");
      }

      /// <summary>
      /// Identifies if rebar exists at a certain position.
      /// </summary>
      /// <param name="element">The rebar element.</param>
      /// <param name="barPosition">The bar position.</param>
      /// <returns>True if it exists.</returns>
      static bool DoesBarExistAtPosition(object element, int barPosition)
      {
         if (element is Rebar)
         {
            return (element as Rebar).DoesBarExistAtPosition(barPosition);
         }
         else if (element is RebarInSystem)
         {
            return (element as RebarInSystem).DoesBarExistAtPosition(barPosition);
         }
         else if (element is RebarContainerItem)
         {
            return (element as RebarContainerItem).DoesBarExistAtPosition(barPosition);
         }
         else
            throw new ArgumentException("Not a rebar.");
      }

      /// <summary>
      /// Gets bar position transform.
      /// </summary>
      /// <param name="element">The rebar element.</param>
      /// <param name="barPositionIndex">The bar position.</param>
      /// <returns>The transform.</returns>
      static Transform GetBarPositionTransform(object element, int barPositionIndex)
      {
         if (element is Rebar)
         {
            Rebar rebar = element as Rebar;
            if (rebar.IsRebarFreeForm())
               return Transform.Identity; // free form rebar don't have a transformation

            return (element as Rebar).GetShapeDrivenAccessor().GetBarPositionTransform(barPositionIndex);
         }
         else if (element is RebarInSystem)
         {
            return (element as RebarInSystem).GetBarPositionTransform(barPositionIndex);
         }
         else if (element is RebarContainerItem)
         {
            return (element as RebarContainerItem).GetBarPositionTransform(barPositionIndex);
         }
         else
            throw new ArgumentException("Not a rebar.");
      }

      /// <summary>
      /// Get the bar diameter from the rebar object.
      /// </summary>
      /// <param name="element">The rebar object.</param>
      /// <returns>The returned bar diameter from the rebar, or a default value.</returns>
      static double GetBarDiameter(object element)
      {
         double bendDiameter = 0.0;

         if (element is RebarContainerItem)
         {
            RebarBendData bendData = (element as RebarContainerItem).GetBendData();
            if (bendData != null)
               bendDiameter = UnitUtil.ScaleLength(bendData.BarDiameter);
         }
         else if (element is Element)
         {
            Element rebarElement = element as Element;
            Document doc = rebarElement.Document;
            ElementId typeId = rebarElement.GetTypeId();
            RebarBarType elementType = doc.GetElement(rebarElement.GetTypeId()) as RebarBarType;
            if (elementType != null)
               bendDiameter = UnitUtil.ScaleLength(elementType.BarDiameter);
         }

         if (bendDiameter < MathUtil.Eps())
            return UnitUtil.ScaleLength(1.0 / 12.0);

         return bendDiameter;
      }

      /// <summary>
      /// Caches all parameter value overrides of the subelement.
      /// </summary>
      /// <param name="element">The rebar element.</param>
      /// <param name="parameters">The paramters of the rebar element.</param>
      /// <param name="barIndex">The index of the subelement.</param>
      /// <param name="handleSubelement">The handle of the subelement.</param>
      static void CacheSubelementParameterValues(Element element, ParameterSet parameters, int barIndex, IFCAnyHandle handleSubelement)
      {
         if (element == null)
            return;

         if (element is Rebar)
         {
            Rebar rebar = element as Rebar;
            if (rebar.DistributionType != DistributionType.VaryingLength && !rebar.IsRebarFreeForm())
               return;

            foreach (Parameter param in parameters)
               ParameterUtil.CacheParameterValuesForSubelementHandle(element.Id, handleSubelement, param, rebar.GetParameterValueAtIndex(param.Id, barIndex));
         }
      }

      static public string getShapeNameAtIndex(Rebar rebar, int barPositionIndex)
      {
         string shapeName = "";
         if (rebar == null)
            return shapeName;
         RebarFreeFormAccessor accessor = rebar.GetFreeFormAccessor();
         if (accessor == null)
            return shapeName;
         if (rebar.Document == null)
            return shapeName;

         return shapeName;
      }
   }
}