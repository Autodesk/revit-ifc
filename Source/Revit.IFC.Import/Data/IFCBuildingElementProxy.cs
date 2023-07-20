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
   /// Represents an IfcBuildingElementProxy.
   /// </summary>
   public class IFCBuildingElementProxy : IFCBuildingElement
   {
      private HashSet<IFCObjectDefinition> m_IFCEmbeddedObjects = null;

      /// <summary>
      /// The objects embedded in the complex IFCBuildingElementProxy.
      /// </summary>
      public HashSet<IFCObjectDefinition> EmbeddedObjects
      {
         get
         {
            if (m_IFCEmbeddedObjects == null)
               m_IFCEmbeddedObjects = new HashSet<IFCObjectDefinition>();
            return m_IFCEmbeddedObjects;
         }
         protected set { m_IFCEmbeddedObjects = value; }
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCBuildingElementProxy()
      {

      }

      /// <summary>
      /// Constructs an IFCBuildingElementProxy from the IfcBuildingElementProxy handle.
      /// </summary>
      /// <param name="ifcBuildingElementProxy">The IfcBuildingElementProxy handle.</param>
      protected IFCBuildingElementProxy(IFCAnyHandle ifcBuildingElementProxy)
      {
         Process(ifcBuildingElementProxy);
      }

      /// <summary>
      /// Processes IfcBuildingElementProxy attributes.
      /// </summary>
      /// <param name="ifcBuildingElementProxy">The IfcBuildingElementProxy handle.</param>
      protected override void Process(IFCAnyHandle ifcBuildingElementProxy)
      {
         base.Process(ifcBuildingElementProxy);
      }

      /// <summary>
      /// Processes an IFCBuildingElementProxy object.
      /// </summary>
      /// <param name="ifcBuildingElementProxy">The IfcBuildingElementProxy handle.</param>
      /// <returns>The IFCBuildingElementProxy object.</returns>
      public static IFCBuildingElementProxy ProcessIFCBuildingElementProxy(IFCAnyHandle ifcBuildingElementProxy)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcBuildingElementProxy))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcBuildingElementProxy);
            return null;
         }

         IFCImportFile.TheFile.EntityMap.TryGetValue(ifcBuildingElementProxy.StepId, out IFCEntity buildingElementProxy);
         if (buildingElementProxy != null)
            return (buildingElementProxy as IFCBuildingElementProxy);

         IFCBuildingElementProxy newIFCBuildingElementProxy = new IFCBuildingElementProxy(ifcBuildingElementProxy);

         // Collecting embedded objects for the complex IfcBuildingElementProxy.
         if (IFCEnums.GetSafeEnumerationAttribute<IFCElementComposition>(ifcBuildingElementProxy, "CompositionType") == IFCElementComposition.Complex)
         {
            HashSet<IFCAnyHandle> ifcRelDecomposes = IFCAnyHandleUtil.GetAggregateInstanceAttribute
                   <HashSet<IFCAnyHandle>>(ifcBuildingElementProxy, "IsDecomposedBy");

            if (ifcRelDecomposes != null)
            {
               foreach (IFCAnyHandle elem in ifcRelDecomposes)
               {
                  ICollection<IFCObjectDefinition> relatedObjects = ProcessIFCRelation.ProcessRelatedObjects(newIFCBuildingElementProxy, elem);
                  if (relatedObjects != null)
                     newIFCBuildingElementProxy.EmbeddedObjects.UnionWith(relatedObjects);
               }
            }
         }

         return newIFCBuildingElementProxy;
      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      protected override void Create(Document doc)
      {
         List<GeometryObject> geomObjs = new List<GeometryObject>();
         foreach (IFCObjectDefinition objDef in EmbeddedObjects)
         {
            CreateElement(doc, objDef);
            if (objDef.CreatedElementId == ElementId.InvalidElementId)
               continue;

            geomObjs.AddRange(objDef.CreatedGeometry);
         }

         // If embedded objects contain no geometry, element geometry will be created in IFCProduct.Create method.
         if (geomObjs.Count > 0)
         {
            // Adding main element geometry to include it in direct shape. 
            IList<IFCSolidInfo> clonedGeometry = CloneElementGeometry(doc, this, this, false);
            foreach (IFCSolidInfo solid in clonedGeometry)
            {
               if (CutSolidByVoids(solid))
                  geomObjs.Add(solid.GeometryObject);
            }

            DirectShape buildingElementProxyShape = IFCElementUtil.CreateElement(doc, GetCategoryId(doc), GlobalId, geomObjs, Id, EntityType);
            if (buildingElementProxyShape != null)
            {
               CreatedElementId = buildingElementProxyShape.Id;
               CreatedGeometry = geomObjs;
            }
         }

         base.Create(doc);
      }
   }
}
