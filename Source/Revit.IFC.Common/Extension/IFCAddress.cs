//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.ExtensibleStorage;


namespace Revit.IFC.Common.Extensions
{
   public class IFCAddress
   {
      private Schema m_schema = null;
      private static Guid s_schemaId = new Guid("79EE6AD8-8A45-49B1-AB13-DDDB128A9EFE");
      private const String s_addressMapField = "IFCAddressMapField";  // Don't change this name, it affects the schema.

      private const String s_addressPurpose = "Purpose";
      private const String s_addressDescription = "Description";
      private const String s_addressUserDefinedPurpose = "UserDefinedPurpose";
      private const String s_addressInternalLocation = "InternalLocation";
      private const String s_addressAddressLine1 = "AddressLine1";
      private const String s_addressAddressLine2 = "AddressLine2";
      private const String s_addressPOBox = "PostalBox";
      private const String s_addressTownOrCity = "Town";
      private const String s_addressRegionOrState = "Region";
      private const String s_addressPostalCode = "PostalCode";
      private const String s_addressCountry = "Country";
      private const String s_saveAddrToBldg = "SaveToIfcBuilding";
      private const String s_saveAddrToSite = "SaveToIfcSite";

      /// <summary>
      /// IFC address initialization
      /// </summary>
      public IFCAddress()
      {
         if (m_schema == null)
         {
            m_schema = Schema.Lookup(s_schemaId);
         }
         if (m_schema == null)
         {
            SchemaBuilder addressBuilder = new SchemaBuilder(s_schemaId);
            addressBuilder.SetSchemaName("IFCAddress");
            addressBuilder.AddMapField(s_addressMapField, typeof(String), typeof(String));
            m_schema = addressBuilder.Finish();
         }
      }

      /// <summary>
      /// Get saved addresses in Storage. Currently only one address is supported 
      /// </summary>
      /// <param name="document">The document storing the saved address.</param>
      /// <param name="schema">The schema for address.</param>
      /// <returns>List of stored addresses</returns>
      private IList<DataStorage> GetAddressInStorage(Document document, Schema schema)
      {
         FilteredElementCollector collector = new FilteredElementCollector(document);
         collector.OfClass(typeof(DataStorage));
         Func<DataStorage, bool> hasTargetData = ds => (ds.GetEntity(schema) != null && ds.GetEntity(schema).IsValid());

         return collector.Cast<DataStorage>().Where<DataStorage>(hasTargetData).ToList<DataStorage>();
      }

      /// <summary>
      /// Update the address saved into the extensible storage using values from the UI 
      /// </summary>
      /// <param name="document">The document storing the saved address.</param>
      /// <param name="addressItem">address information from the UI to be saved into extensible storage</param>
      public void UpdateAddress(Document document, IFCAddressItem addressItem)
      {
         if (m_schema == null)
         {
            m_schema = Schema.Lookup(s_schemaId);
         }

         if (m_schema != null)
         {
            Transaction transaction = new Transaction(document, "Update saved IFC Address");
            transaction.Start();

            IList<DataStorage> oldSavedAddress = GetAddressInStorage(document, m_schema);
            if (oldSavedAddress.Count > 0)
            {
               List<ElementId> dataStorageToDelete = new List<ElementId>();
               foreach (DataStorage dataStorage in oldSavedAddress)
               {
                  dataStorageToDelete.Add(dataStorage.Id);
               }
               document.Delete(dataStorageToDelete);
            }
         
            DataStorage addressStorage = DataStorage.Create(document);

            Entity mapEntity = new Entity(m_schema);
            IDictionary<string, string> mapData = new Dictionary<string, string>();
            if (addressItem.Purpose != null) mapData.Add(s_addressPurpose, addressItem.Purpose.ToString());
            if (addressItem.Description != null) mapData.Add(s_addressDescription, addressItem.Description.ToString());
            if (addressItem.UserDefinedPurpose != null) mapData.Add(s_addressUserDefinedPurpose, addressItem.UserDefinedPurpose.ToString());
            if (addressItem.InternalLocation != null) mapData.Add(s_addressInternalLocation, addressItem.InternalLocation.ToString());
            if (addressItem.AddressLine1 != null) mapData.Add(s_addressAddressLine1, addressItem.AddressLine1.ToString());
            if (addressItem.AddressLine2 != null) mapData.Add(s_addressAddressLine2, addressItem.AddressLine2.ToString());
            if (addressItem.POBox != null) mapData.Add(s_addressPOBox, addressItem.POBox.ToString());
            if (addressItem.TownOrCity != null) mapData.Add(s_addressTownOrCity, addressItem.TownOrCity.ToString());
            if (addressItem.RegionOrState != null) mapData.Add(s_addressRegionOrState, addressItem.RegionOrState.ToString());
            if (addressItem.PostalCode != null) mapData.Add(s_addressPostalCode, addressItem.PostalCode.ToString());
            if (addressItem.Country != null) mapData.Add(s_addressCountry, addressItem.Country.ToString());
            if (addressItem.AssignAddressToBuilding) mapData.Add(s_saveAddrToBldg, "Y");
            if (addressItem.AssignAddressToSite) mapData.Add(s_saveAddrToSite, "Y");

            mapEntity.Set<IDictionary<string, String>>(s_addressMapField, mapData);
            addressStorage.SetEntity(mapEntity);

            transaction.Commit();
         }
      }

      /// <summary>
      /// Get saved IFC address from the extensible storage.
      /// </summary>
      /// <param name="document">The document storing the saved address.</param>
      /// <param name="address">saved address information from the extensible storage.</param>
      /// <returns>returns status whether there is any saved address in the extensible storage.</returns>
      public bool GetSavedAddress(Document document, out IFCAddressItem address)
      {
         IFCAddressItem addressItemSaved = new IFCAddressItem();

         if (m_schema == null)
         {
            m_schema = Schema.Lookup(s_schemaId);
         }
         if (m_schema != null)
         {
            IList<DataStorage> addressStorage = GetAddressInStorage(document, m_schema);

            if (addressStorage.Count > 0)
            {
               // Expected only one address in the storage
               Entity savedAddress = addressStorage[0].GetEntity(m_schema);
               IDictionary<string, string> savedAddressMap = savedAddress.Get<IDictionary<string, string>>(s_addressMapField);
               if (savedAddressMap.ContainsKey(s_addressPurpose))
                  addressItemSaved.Purpose = savedAddressMap[s_addressPurpose];
               if (savedAddressMap.ContainsKey(s_addressDescription))
                  addressItemSaved.Description = savedAddressMap[s_addressDescription];
               if (savedAddressMap.ContainsKey(s_addressUserDefinedPurpose))
                  addressItemSaved.UserDefinedPurpose = savedAddressMap[s_addressUserDefinedPurpose];
               if (savedAddressMap.ContainsKey(s_addressInternalLocation))
                  addressItemSaved.InternalLocation = savedAddressMap[s_addressInternalLocation];
               if (savedAddressMap.ContainsKey(s_addressAddressLine1))
                  addressItemSaved.AddressLine1 = savedAddressMap[s_addressAddressLine1];
               if (savedAddressMap.ContainsKey(s_addressAddressLine2))
                  addressItemSaved.AddressLine2 = savedAddressMap[s_addressAddressLine2];
               if (savedAddressMap.ContainsKey(s_addressPOBox))
                  addressItemSaved.POBox = savedAddressMap[s_addressPOBox];
               if (savedAddressMap.ContainsKey(s_addressTownOrCity))
                  addressItemSaved.TownOrCity = savedAddressMap[s_addressTownOrCity];
               if (savedAddressMap.ContainsKey(s_addressRegionOrState))
                  addressItemSaved.RegionOrState = savedAddressMap[s_addressRegionOrState];
               if (savedAddressMap.ContainsKey(s_addressPostalCode))
                  addressItemSaved.PostalCode = savedAddressMap[s_addressPostalCode];
               if (savedAddressMap.ContainsKey(s_addressCountry))
                  addressItemSaved.Country = savedAddressMap[s_addressCountry];
               if (savedAddressMap.ContainsKey(s_saveAddrToBldg))
                  addressItemSaved.AssignAddressToBuilding = savedAddressMap[s_saveAddrToBldg].Equals("Y");
               if (savedAddressMap.ContainsKey(s_saveAddrToSite))
                  addressItemSaved.AssignAddressToSite = savedAddressMap[s_saveAddrToSite].Equals("Y");
               address = addressItemSaved;
               return true;
            }
         }

         address = addressItemSaved;
         return false;
      }
   }
}