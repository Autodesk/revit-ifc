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
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Export.Utility;
using Revit.IFC.Export.Toolkit;
using Revit.IFC.Export.Exporter.PropertySet;

namespace Revit.IFC.Export.Exporter
{
   /// <summary>
   /// Provides methods to export a Revit element as IfcElementAssembly.
   /// </summary>
   class AssemblyInstanceExporter
   {
      private static IFCElementAssemblyType GetPredefinedTypeFromObjectType(string objectType)
      {
         if (String.IsNullOrEmpty(objectType))
            return IFCElementAssemblyType.NotDefined;

         foreach (IFCElementAssemblyType val in Enum.GetValues(typeof(IFCElementAssemblyType)))
         {
            if (NamingUtil.IsEqualIgnoringCaseSpacesAndUnderscores(objectType, val.ToString()))
               return val;
         }

         return IFCElementAssemblyType.UserDefined;
      }

      /// <summary>
      /// Exports an element as an IFC assembly.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="element">The element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      /// <returns>True if exported successfully, false otherwise.</returns>
      public static bool ExportAssemblyInstanceElement(ExporterIFC exporterIFC, AssemblyInstance element,
          ProductWrapper productWrapper)
      {
         if (element == null)
            return false;

         IFCFile file = exporterIFC.GetFile();

         using (IFCTransaction tr = new IFCTransaction(file))
         {
            IFCAnyHandle assemblyInstanceHnd = null;

            string guid = GUIDUtil.CreateGUID(element);
            IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
            IFCAnyHandle localPlacement = null;
            PlacementSetter placementSetter = null;
            IFCLevelInfo levelInfo = null;
            bool relateToLevel = true;

            string ifcEnumType;
            IFCExportInfoPair exportAs = ExporterUtil.GetObjectExportType(exporterIFC, element, out ifcEnumType);
            if (exportAs.ExportInstance == IFCEntityType.IfcSystem)
            {
               string name = NamingUtil.GetNameOverride(element, NamingUtil.GetIFCName(element));
               string description = NamingUtil.GetDescriptionOverride(element, null);
               string objectType = NamingUtil.GetObjectTypeOverride(element, NamingUtil.GetFamilyAndTypeName(element));
               assemblyInstanceHnd = IFCInstanceExporter.CreateSystem(file, guid, ownerHistory, name, description, objectType);

               // Create classification reference when System has classification filed name assigned to it
               ClassificationUtil.CreateClassification(exporterIFC, file, element, assemblyInstanceHnd);

               HashSet<IFCAnyHandle> relatedBuildings = new HashSet<IFCAnyHandle>();
               relatedBuildings.Add(ExporterCacheManager.BuildingHandle);

               IFCAnyHandle relServicesBuildings = IFCInstanceExporter.CreateRelServicesBuildings(file, GUIDUtil.CreateGUID(),
                   ExporterCacheManager.OwnerHistoryHandle, null, null, assemblyInstanceHnd, relatedBuildings);

               relateToLevel = false; // Already related to the building via IfcRelServicesBuildings.
            }
            else
            {
               // Check for containment override
               IFCAnyHandle overrideContainerHnd = null;
               ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, element, out overrideContainerHnd);

               using (placementSetter = PlacementSetter.Create(exporterIFC, element, null, null, overrideContainerId, overrideContainerHnd))
               {
                  IFCAnyHandle representation = null;

                  // We have limited support for exporting assemblies as other container types.
                  localPlacement = placementSetter.LocalPlacement;
                  levelInfo = placementSetter.LevelInfo;

                  switch (exportAs.ExportInstance)
                  {
                     case IFCEntityType.IfcCurtainWall:
                        assemblyInstanceHnd = IFCInstanceExporter.CreateCurtainWall(exporterIFC, element, guid,
                            ownerHistory, localPlacement, representation, ifcEnumType);
                        break;
                     case IFCEntityType.IfcRamp:
                        string rampPredefinedType = RampExporter.GetIFCRampType(ifcEnumType);
                        assemblyInstanceHnd = IFCInstanceExporter.CreateRamp(exporterIFC, element, guid,
                            ownerHistory, localPlacement, representation, rampPredefinedType);
                        break;
                     case IFCEntityType.IfcRoof:
                        assemblyInstanceHnd = IFCInstanceExporter.CreateRoof(exporterIFC, element, guid,
                            ownerHistory, localPlacement, representation, ifcEnumType);
                        break;
                     case IFCEntityType.IfcStair:
                        string stairPredefinedType = StairsExporter.GetIFCStairType(ifcEnumType);
                        assemblyInstanceHnd = IFCInstanceExporter.CreateStair(exporterIFC, element, guid,
                            ownerHistory, localPlacement, representation, stairPredefinedType);
                        break;
                     case IFCEntityType.IfcWall:
                        assemblyInstanceHnd = IFCInstanceExporter.CreateWall(exporterIFC, element, guid,
                            ownerHistory, localPlacement, representation, ifcEnumType);
                        break;
                     default:
                        string objectType = NamingUtil.GetObjectTypeOverride(element, NamingUtil.GetFamilyAndTypeName(element));
                        IFCElementAssemblyType assemblyPredefinedType = GetPredefinedTypeFromObjectType(objectType);
                        assemblyInstanceHnd = IFCInstanceExporter.CreateElementAssembly(exporterIFC, element, guid,
                            ownerHistory, localPlacement, representation, IFCAssemblyPlace.NotDefined, assemblyPredefinedType);
                        break;
                  }
               }
            }

            if (assemblyInstanceHnd == null)
               return false;

            // relateToLevel depends on how the AssemblyInstance is being mapped to IFC, above.
            productWrapper.AddElement(element, assemblyInstanceHnd, levelInfo, null, relateToLevel, exportAs);

            ExporterCacheManager.AssemblyInstanceCache.RegisterAssemblyInstance(element.Id, assemblyInstanceHnd);

            tr.Commit();
            return true;
         }
      }

      /// <summary>
      /// Update the local placements of the members of an assembly relative to the assembly.
      /// </summary>
      /// <param name="exporterIFC">The ExporerIFC.</param>
      /// <param name="assemblyPlacement">The assembly local placement handle.</param>
      /// <param name="elementPlacements">The member local placement handles.</param>
      public static void SetLocalPlacementsRelativeToAssembly(ExporterIFC exporterIFC, IFCAnyHandle assemblyPlacement, ICollection<IFCAnyHandle> elementPlacements)
      {
         foreach (IFCAnyHandle elementHandle in elementPlacements)
         {
            IFCAnyHandle elementPlacement = null;
            try
            {
               // The assembly may contain nested groups, that don't have an object placement.  In this case, continue.
               elementPlacement = IFCAnyHandleUtil.GetObjectPlacement(elementHandle);
            }
            catch
            {
               continue;
            }

            Transform relTrf = ExporterIFCUtils.GetRelativeLocalPlacementOffsetTransform(assemblyPlacement, elementPlacement);
            Transform inverseTrf = relTrf.Inverse;

            IFCFile file = exporterIFC.GetFile();
            IFCAnyHandle relLocalPlacement = ExporterUtil.CreateAxis2Placement3D(file, inverseTrf.Origin, inverseTrf.BasisZ, inverseTrf.BasisX);

            // NOTE: caution that old IFCAXIS2PLACEMENT3D may be unused as the new one replace it. 
            // But we cannot delete it safely yet because we don't know if any handle is referencing it.
            GeometryUtil.SetRelativePlacement(elementPlacement, relLocalPlacement);

            GeometryUtil.SetPlacementRelTo(elementPlacement, assemblyPlacement);
         }
      }

      /// <summary>
      /// Exports a truss as an IFC assembly.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="truss">The truss element.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportTrussElement(ExporterIFC exporterIFC, Truss truss,
          ProductWrapper productWrapper)
      {
         if (truss == null)
            return;

         ICollection<ElementId> trussMemberIds = truss.Members;
         ExportAssemblyInstanceWithMembers(exporterIFC, truss, trussMemberIds, IFCElementAssemblyType.Truss, productWrapper);
      }

      /// <summary>
      /// Exports a beam system as an IFC assembly.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="beamSystem">The beam system.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      public static void ExportBeamSystem(ExporterIFC exporterIFC, BeamSystem beamSystem,
          ProductWrapper productWrapper)
      {
         if (beamSystem == null)
            return;

         ICollection<ElementId> beamMemberIds = beamSystem.GetBeamIds();
         ExportAssemblyInstanceWithMembers(exporterIFC, beamSystem, beamMemberIds, IFCElementAssemblyType.Beam_Grid, productWrapper);
      }

      /// <summary>
      /// Exports an element as an IFC assembly with its members.
      /// </summary>
      /// <param name="exporterIFC">The exporter.</param>
      /// <param name="assemblyElem">The element to be exported as IFC assembly.</param>
      /// <param name="memberIds">The member element ids.</param>
      /// <param name="assemblyType">The IFC assembly type.</param>
      /// <param name="productWrapper">The ProductWrapper.</param>
      static void ExportAssemblyInstanceWithMembers(ExporterIFC exporterIFC, Element assemblyElem,
          ICollection<ElementId> memberIds, IFCElementAssemblyType assemblyType, ProductWrapper productWrapper)
      {
         HashSet<IFCAnyHandle> memberHnds = new HashSet<IFCAnyHandle>();

         foreach (ElementId memberId in memberIds)
         {
            IFCAnyHandle memberHnd = ExporterCacheManager.ElementToHandleCache.Find(memberId);
            if (!IFCAnyHandleUtil.IsNullOrHasNoValue(memberHnd))
               memberHnds.Add(memberHnd);
         }

         if (memberHnds.Count == 0)
            return;

         // Check the intended IFC entity or type name is in the exclude list specified in the UI
         Common.Enums.IFCEntityType elementClassTypeEnum = Common.Enums.IFCEntityType.IfcElementAssembly;
         if (ExporterCacheManager.ExportOptionsCache.IsElementInExcludeList(elementClassTypeEnum))
            return;

         IFCFile file = exporterIFC.GetFile();
         using (IFCTransaction tr = new IFCTransaction(file))
         {
            // Check for containment override
            IFCAnyHandle overrideContainerHnd = null;
            ElementId overrideContainerId = ParameterUtil.OverrideContainmentParameter(exporterIFC, assemblyElem, out overrideContainerHnd);

            using (PlacementSetter placementSetter = PlacementSetter.Create(exporterIFC, assemblyElem, null, null, overrideContainerId, overrideContainerHnd))
            {
               IFCAnyHandle ownerHistory = ExporterCacheManager.OwnerHistoryHandle;
               IFCAnyHandle localPlacement = placementSetter.LocalPlacement;

               string guid = GUIDUtil.CreateGUID(assemblyElem);


               IFCAnyHandle assemblyInstanceHnd = IFCInstanceExporter.CreateElementAssembly(exporterIFC, assemblyElem, guid,
                   ownerHistory, localPlacement, null, IFCAssemblyPlace.NotDefined, assemblyType);
               IFCExportInfoPair exportInfo = new IFCExportInfoPair(elementClassTypeEnum, assemblyType.ToString());

               productWrapper.AddElement(assemblyElem, assemblyInstanceHnd, placementSetter.LevelInfo, null, true, exportInfo);

               string aggregateGuid = GUIDUtil.CreateSubElementGUID(assemblyElem, (int)IFCAssemblyInstanceSubElements.RelAggregates);
               IFCInstanceExporter.CreateRelAggregates(file, aggregateGuid, ownerHistory, null, null, assemblyInstanceHnd, memberHnds);

               ExporterCacheManager.ElementsInAssembliesCache.UnionWith(memberHnds);

               // Update member local placements to be relative to the assembly.
               SetLocalPlacementsRelativeToAssembly(exporterIFC, localPlacement, memberHnds);
            }
            tr.Commit();
         }
      }
   }
}
