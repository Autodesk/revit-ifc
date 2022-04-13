using System.Collections.Generic;

using Autodesk.Revit.DB;

namespace Revit.IFC.Import.Core
{
   /// <summary>
   /// The consumer object.
   /// 
   /// All communications from the Revit-IFC code go through here.
   /// 
   /// The implementation of the consumer, then creates IElement objects to work as required and 
   /// the Revit-IFC code provides the data to them through the interface below.
   /// </summary>
   public interface IIFCFileProcessor
   {
      /// <summary>
      /// Create or update an element (instance) object.
      /// </summary>
      /// <param name="ifcId">The STEP Id of the IFC entity</param>
      /// <param name="ifcGuid">The GUID of the IFC entity</param>
      /// <param name="ifcEntityType">The entity type name of the IFC entity</param>
      /// <param name="categoryId">The Revit category Id - not sure if this will be used in the end</param>
      bool CreateOrUpdateElement(int ifcId, string ifcGuid, string ifcEntityType, long categoryId,
         IList<GeometryObject> geomObjs);

      /// <summary>
      /// Create or update an element object for an IFC type object.
      /// </summary>
      /// <param name="ifcId">The STEP Id of the IFC type element</param>
      /// <param name="ifcGuid">The GUID of the IFC type element</param>
      /// <param name="ifcEntityType">The entity type name of the IFC type element</param>
      /// <param name="categoryId">The Revit category Id - not sure if this will be used in the end</param>
      void CreateElementType(int ifcId, string ifcGuid, string ifcEntityType, long categoryId);

      /// <summary>
      /// Allow a processor to add a parameter with a double value.
      /// </summary>
      /// <param name="objDefId">The STEP id of the object definition.</param>
      /// <param name="specTypeId">The Forge id for the parameter specification.</param>
      /// <param name="unitsTypeId">The Forge id for the parameter units.</param>
      /// <param name="parameterSetId">The STEP id of the parameter set.</param>
      /// <param name="parameterName">The parameter name.</param>
      /// <param name="parameterValue">The parameter value.</param>
      /// <returns>True if processed correctly, false if not, and null if not implemented.</returns>
      bool? ProcessParameter(int objDefId, ForgeTypeId specTypeId, ForgeTypeId unitsTypeId,
         int parameterSetId, string parameterName, double parameterValue);

      bool? ProcessParameter(int objDefId, int parameterSetId, string parameterName, string parameterValue);

      bool? ProcessParameter(int objDefId, int parameterSetId, string parameterName, int parameterValue);

      bool? ProcessParameter(int objDefId, int parameterSetId, string parameterName, bool parameterValue);

      bool? ProcessParameter(int objDefId, int parameterSetId, string parameterName, ElementId parameterValue);

      /// <summary>
      /// Create or update an element object containing shared geometry.
      /// </summary>
      /// <param name="shapeEditScope">The shape edit scope of the object.</param>
      /// <param name="mappedItem">The mapped item container.</param>
      /// <param name="newLcs">The local transform for the mapped item.</param>
      void PostProcessMappedItem(int creatorId,
         string globalId,
         string entityTypeAsString,
         ElementId categoryId,
         int geometrySourceId,
         Transform newLcs);

      /// <summary>
      /// NAVIS_TODO
      /// </summary>
      /// <param name="ifcId">The STEP Id of the IFC entity</param>
      /// <param name="typeObjectId">The id of the IfcTypeObject related to the entity, if any</param>
      /// <param name="shapeEditScope">The shape edit scope</param>
      /// <param name="shape">The DirectShape containing the IFC entity data</param>
      /// <param name="lcs">The transform of the associated geometry</param>
      /// <param name="directShapeGeometries">The associated geometry</param>
      bool PostProcessProduct(int ifcId,
         int? typeObjectId,
         Transform lcs,
         IList<GeometryObject> directShapeGeometries);

      /// <summary>
      /// NAVIS_TODO
      /// </summary>
      void PostProcessProject(double? lengthScaleFactor, ForgeTypeId lengthUnit);

      bool PostProcessRepresentationMap(int typeId, 
         IList<Curve> mappedCurves, IList<GeometryObject> mappedSolids);

      void PostProcessSite(int siteId, double? refLatitude, double? refLongitude,
         double refElevation, string landTitleNumber, ForgeTypeId baseLengthUnits,
         Transform lcs);

      /// <summary>
      /// Set a built-in parameter string value in an element.
      /// </summary>
      /// <param name="element">The element.</param>
      /// <param name="parameterId">The built-in parameter id.</param>
      /// <param name="value">The string value.</param>
      /// <returns>True if the parameter was set, false otherwise.</returns>
      bool SetParameter(Element element, BuiltInParameter parameterId, string value);

      /// <summary>
      /// Determine if we should apply transforms to geometries.
      /// </summary>
      bool ApplyTransforms { get; }

      /// <summary>
      /// Determine if we should try to fix building and building story locations 
      /// far from the origin.
      /// </summary>
      bool TryToFixFarawayOrigin { get; }
      
      /// <summary>
      /// If true, then scale all values into internal units, otherwise leave them as is.
      /// </summary>
      /// <remarks>
      /// This applies to all values, whether parameters, geometry or locations.
      /// The only exception is that angles are always converted, so that we always deal 
      /// with them in Radians.
      /// </remarks>
      bool ScaleValues { get; }

      bool FindAlternateGeometrySource { get; }
      
      /// <summary>
      /// Performs a processor-specific extra length scaling if necessary.
      /// </summary>
      /// <param name="length">The original length.</param>
      /// <returns>The scaled length.</returns>
      double ScaleLength(double length);
   }
}
