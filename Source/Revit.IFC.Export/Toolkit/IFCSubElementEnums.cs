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

// The enums below specify sub-element values to be used in the CreateSubElementGUID function.
// This ensures that their GUIDs are consistent across exports.
// Note that sub-element GUIDs can not be stored on import, so they do not survive roundtrips.
namespace Revit.IFC.Export.Toolkit
{
   enum IFCAssemblyInstanceSubElements
   {
      RelContainedInSpatialStructure = 1,
      RelAggregates = 2
   }

   enum IFCBuildingStoreySubElements
   {
      RelContainedInSpatialStructure = 1,
      RelAggregates = 2
   }

   // Curtain Walls can be created from a variety of elements, including Walls and Roofs.
   // As such, start their subindexes high enough to not bother potential hosts.
   enum IFCCurtainWallSubElements
   {
      RelAggregates = 1024
   }

   enum IFCDoorSubElements
   {
      DoorLining = 1,
      DoorPanelStart = 2,
      DoorPanelEnd = 17, // 2 through 17 are reserved for panels.
      DoorOpening = 19,
      DoorOpeningRelVoid = 20,
      DoorStyle = 21,
      DoorType = 22
   }

   enum IFCGroupSubElements
   {
      RelAssignsToGroup = 1,
   }

   // Used for internal Revit property sets, split instances, and connectors.
   enum IFCGenericSubElements
   {
      PSetRevitInternalStart = 1536,
      PSetRevitInternalEnd = PSetRevitInternalStart + 255,
      PSetRevitInternalRelStart = PSetRevitInternalEnd + 1,
      PSetRevitInternalRelEnd = PSetRevitInternalRelStart + 255, // 2047
                                                                 // 2048 is IFCFamilyInstance.InstanceAsType
      SplitInstanceStart = 2049,
      SplitInstanceEnd = SplitInstanceStart + 255,
      SplitTypeStart = SplitInstanceEnd + 1,
      SplitTypeEnd = SplitTypeStart + 255, // 2560
   }

   // Family Instances can create a variety of elements.
   // As such, start their subindexes high enough to not bother potential hosts.
   enum IFCFamilyInstanceSubElements
   {
      InstanceAsType = 2048
   }

   enum IFCHostedSweepSubElements
   {
      PipeSegmentType = 1
   }

   enum IFCProjectSubElements
   {
      RelContainedInBuildingSpatialStructure = 1,
      RelContainedInSiteSpatialStructure = 2,
      RelAggregatesBuildingStories = 3
   }

   enum IFCRampSubElements
   {
      ContainedRamp = 2,
      ContainmentRelation = 3, // same as IFCStairSubElements.ContainmentRelation
      FlightIdOffset = 4,
      LandingIdOffset = 201,
      StringerIdOffset = 401
}

   enum IFCReinforcingBarSubElements
   {
      BarStart = 5,
      BarEnd = BarStart + 1023
   }

   enum IFCRoofSubElements
   {
      RoofSlabStart = 2,
      RoofSlabEnd = RoofSlabStart + 255
   }

   enum IFCSlabSubElements
   {
      SubSlabStart = 2,
      SubSlabEnd = SubSlabStart + 255
   }

   enum IFCStairSubElements
   {
      ContainedStair = 2,
      ContainmentRelation = 3,
      FlightIdOffset = 4,
      LandingIdOffset = 201,
      StringerIdOffset = 401,
}

   enum IFCWallSubElements
   {
      RelAggregatesReserved = IFCCurtainWallSubElements.RelAggregates
   }

   enum IFCWindowSubElements
   {
      WindowLining = IFCDoorSubElements.DoorLining,
      WindowPanelStart = IFCDoorSubElements.DoorPanelStart,
      WindowPanelEnd = IFCDoorSubElements.DoorPanelEnd,
      WindowOpening = IFCDoorSubElements.DoorOpening,
      WindowOpeningRelVoid = IFCDoorSubElements.DoorOpeningRelVoid,
      WindowStyle = IFCDoorSubElements.DoorStyle,
      WindowType = IFCDoorSubElements.DoorType
   }

   enum IFCZoneSubElements
   {
      RelAssignsToGroup = 1,
   }
}
