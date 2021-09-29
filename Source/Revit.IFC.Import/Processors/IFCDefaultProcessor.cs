using System.Collections.Generic;

using Autodesk.Revit.DB;

namespace Revit.IFC.Import
{
   public class IFCDefaultProcessor : IIFCFileProcessor
   {
      public bool CreateOrUpdateElement(int ifcId, string ifcGuid, string ifcEntityType, int categoryId,
         IList<GeometryObject> geomObjs)
      {
         return false;
      }

      public void CreateElementType(int ifcId, string ifcGuid, string ifcEntityType, int categoryId)
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

      public bool SetParameter(Element element, BuiltInParameter parameterId, string value)
      {
         Parameter parameter = element?.get_Parameter(parameterId);
         if (parameter == null || parameter.StorageType != StorageType.String || parameter.IsReadOnly)
            return false;

         parameter.Set(value);
         return true;
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
