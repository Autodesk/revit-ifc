//
// Revit IFC Common library: this library works with Autodesk(R) Revit(R) to import IFC files.
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
using System.Runtime.InteropServices;

namespace Revit.IFC.Common.Utility
{
   /// <summary>
   /// Utilities for overwriting the Revit status bar.
   /// Code inspired by: http://thebuildingcoder.typepad.com/blog/2011/02/status-bar-text.html
   /// </summary>
   public class RevitStatusBar
   {

      [DllImport("user32.dll",
        SetLastError = true)]
      static extern IntPtr FindWindowEx(
        IntPtr hwndParent,
        IntPtr hwndChildAfter,
        string lpszClass,
        string lpszWindow);

      [DllImport("user32.dll",
         SetLastError = true,
         CharSet = CharSet.Auto)]
      static extern int SetWindowText(
        IntPtr hWnd,
        string lpString);

      private IntPtr m_StatusBar = IntPtr.Zero;

      protected RevitStatusBar()
      {
         // Find the status bar, so we can add messages.
         IntPtr revitHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
         if (revitHandle != IntPtr.Zero)
            m_StatusBar = FindWindowEx(revitHandle, IntPtr.Zero, "msctls_statusbar32", "");
      }

      /// <summary>
      /// Set the value of the status bar, if there is a valid handle to it.
      /// </summary>
      /// <param name="msg">The message.</param>
      public void Set(string msg)
      {
         if (m_StatusBar != IntPtr.Zero)
         {
            SetWindowText(m_StatusBar, msg);
         }
      }

      /// <summary>
      /// Create a new RevitStatusBar.
      /// </summary>
      /// <returns>The RevitStatusBar.</returns>
      public static RevitStatusBar Create()
      {
         return new RevitStatusBar();
      }
   }
}