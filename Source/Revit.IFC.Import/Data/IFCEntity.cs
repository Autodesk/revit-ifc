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
   /// Base level class for all objects created from an IFC entity.
   /// </summary>
   public abstract class IFCEntity
   {
      private int m_StepId;

      private IFCEntityType m_EntityType;

      /// <summary>
      /// The id of the entity, corresponding to the STEP id of the IFC entity.
      /// </summary>
      public int Id
      {
         get { return m_StepId; }
         protected set { m_StepId = value; }
      }

      /// <summary>
      /// The entity type of the corresponding IFC entity.
      /// </summary>
      public IFCEntityType EntityType
      {
         get { return m_EntityType; }
         protected set { m_EntityType = value; }
      }

      bool m_IsValidForCreation = true;

      /// <summary>
      /// Returns if the entity can be successfully converted into a Revit element.
      /// This prevents repeated attempts to create an element from an invalid entity.
      /// </summary>
      public bool IsValidForCreation
      {
         get { return m_IsValidForCreation; }
         protected set { m_IsValidForCreation = value; }
      }

      protected IFCEntity()
      {
      }

      virtual protected void Process(IFCAnyHandle item)
      {
         Id = item.StepId;
         EntityType = IFCAnyHandleUtil.GetEntityType(item);
         IFCImportFile.TheFile.EntityMap.Add(Id, this);
      
         // Note that we will log processing entities even if they are later deleted.
         Importer.TheLog.AddProcessedEntity(EntityType);
      }

      static public void HandleError(string message, IFCAnyHandle item, bool warn)
      {
         if (message != "Don't Import")
         {
            if (warn)
            {
               Importer.TheLog.LogError(item.StepId, message, false);
            }
         }
         else
         {
            IFCImportFile.TheFile.EntityMap[item.Id] = null;
         }
      }

      /// <summary>
      /// Post-process IFCEntity attributes.
      /// </summary>
      virtual public void PostProcess()
      {
      }

      /// <summary>
      /// Check if two IFCEntity lists are equal.
      /// </summary>
      /// <param name="list1">The first list.</param>
      /// <param name="list2">The second list.</param>
      /// <returns>True if they are equal, false otherwise.</returns>
      /// <remarks>The is not intended to be an exhaustive check.</remarks>
      static public bool AreIFCEntityListsEquivalent<T>(IList<T> list1, IList<T> list2) where T : IFCEntity
      {
         int numItems = list1.Count;
         if (numItems != list2.Count)
            return false;

         for (int ii = 0; ii < numItems; ii++)
         {
            if (!IFCRoot.Equals(list1[ii], list2[ii]))
               return false;
         }

         return true;
      }

      /// <summary>
      /// Does a top-level check to see if this entity may be equivalent to otherEntity.
      /// </summary>
      /// <param name="otherEntity">The other IFCEntity.</param>
      /// <returns>True if they are equivalent, false if they aren't, null if not enough information.</returns>
      /// <remarks>This isn't intended to be an exhaustive check, and isn't implemented for all types.  This is intended
      /// to be used by derived classes.</remarks>
      virtual public bool? MaybeEquivalentTo(IFCEntity otherEntity)
      {
         if (otherEntity == null)
            return false;

         // If the entities have the same Id, they are definitely the same object.  If they don't, they could
         // still be considered equivalent, so we won't disqualify them.
         if (Id == otherEntity.Id)
            return true;

         if (EntityType != otherEntity.EntityType)
            return false;

         return null;
      }

      /// <summary>
      /// Does a top-level check to see if this entity is equivalent to otherEntity.
      /// </summary>
      /// <param name="otherEntity">The other IFCEntity.</param>
      /// <returns>True if they are equivalent, false if they aren't.</returns>
      /// <remarks>This isn't intended to be an exhaustive check, and isn't implemented for all types.  This is intended
      /// to make a final decision, and will err on the side of deciding that entities aren't equivalent.</remarks>
      virtual public bool IsEquivalentTo(IFCEntity otherEntity)
      {
         bool? maybeEquivalentTo = MaybeEquivalentTo(otherEntity);
         if (maybeEquivalentTo.HasValue)
            return maybeEquivalentTo.Value;

         // If we couldn't determine that they were the same, assume that they aren't.
         return false;
      }
   }
}