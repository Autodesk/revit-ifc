using System.Collections.Generic;

using Autodesk.Revit.DB;
using Revit.IFC.Import.Core;

namespace Revit.IFC.Import
{
   public class IFCDefaultProcessor : IIFCFileProcessor
   {
      public bool CreateOrUpdateElement(int ifcId, string ifcGuid, string ifcEntityType, long categoryId,
         IList<GeometryObject> geomObjs)
      {
         return false;
      }

      public void CreateElementType(int ifcId, string ifcGuid, string ifcEntityType, long categoryId)
      {
      }

      public void PostProcessMappedItem(int creatorId,
         string globalId,
         string entityTypeAsString,
         ElementId categoryId,
         int geometrySourceId,
         Transform newLcs)
      { 
      }

      public bool PostProcessProduct(int ifcId,
         int? typeObjectId,
         Transform lcs,
         IList<GeometryObject> directShapeGeometries)
      {
         return false;
      }

      public bool? ProcessParameter(int objDefId, ForgeTypeId specTypeId, ForgeTypeId unitsTypeId,
         int parameterSetId, string parameterName, double parameterValue)
      {
         return null;
      }

      public bool? ProcessParameter(int objDefId, int parameterSetId, string parameterName, string parameterValue)
      {
         return null;
      }

      public bool? ProcessParameter(int objDefId, int parameterSetId, string parameterName, int parameterValue)
      {
         return null;
      }

      public bool? ProcessParameter(int objDefId, int parameterSetId, string parameterName, ElementId parameterValue)
      {
         return null;
      }

      public bool? ProcessParameter(int objDefId, int parameterSetId, string parameterName, bool parameterValue)
      {
         return null;
      }

      public void SetStringParameter(Element element, int objDefId, BuiltInParameter parameterId, string value, bool allowUnfound)
      {
         Parameter parameter = element?.get_Parameter(parameterId);
         if (parameter == null)
         {
            if (!allowUnfound)
               Importer.TheLog.LogError(objDefId, "Parameter \"" + parameterId.ToString() + "\" not found, ignoring.", false);
            return;
         }

         if (parameter.StorageType != StorageType.String)
         {
            Importer.TheLog.LogError(objDefId, "Parameter \"" + parameterId.ToString() + "\" not String type, ignoring.", false);
            return;
         }

         if (parameter.IsReadOnly)
         {
            Importer.TheLog.LogError(objDefId, "Parameter \"" + parameterId.ToString() + "\" is read-only, ignoring.", false);
            return;
         }

         parameter.Set(value);
      }

      public bool PostProcessRepresentationMap(int typeId, 
         IList<Curve> mappedCurves, IList<GeometryObject> mappedSolids)
      {
         return false;
      }

      public void PostProcessSite(int siteId, double? refLatitude, double? refLongitude,
         double refElevation, string landTitleNumber, ForgeTypeId baseLengthUnits,
         Transform lcs)
      {
      }

      public void PostProcessProject(double? lengthScaleFactor, ForgeTypeId lengthUnit)
      {
      }

      public bool FindAlternateGeometrySource { get => false; }

      public bool ScaleValues { get => true; }

      public bool ApplyTransforms { get => true; }

      public bool TryToFixFarawayOrigin { get => true; }
      
      public double ScaleLength(double length) { return length; }
   }
}
