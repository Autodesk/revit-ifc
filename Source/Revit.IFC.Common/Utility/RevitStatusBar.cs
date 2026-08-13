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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

      private IntPtr StatusBar { get; set; } = IntPtr.Zero;

      private Stopwatch Stopwatch { get; set; } = new();

      private long LastMessageTime { get; set; } = -1;

      protected RevitStatusBar()
      {
         // Find the status bar, so we can add messages.
         IntPtr revitHandle = Process.GetCurrentProcess().MainWindowHandle;
         if (revitHandle == IntPtr.Zero)
            return;

         StatusBar = FindWindowEx(revitHandle, IntPtr.Zero, "msctls_statusbar32", "");
         Stopwatch.Start();
      }

      private void SetInternal(string msg, long currTime)
      {
         SetWindowText(StatusBar, msg);
         LastMessageTime = currTime;
      }

      static readonly long UpdateTimeInMilliseconds = 750;

      private long CanDisplayMessage(bool forceSet)
      {
         if (StatusBar == IntPtr.Zero)
            return -1;

         long currTime = Stopwatch.ElapsedMilliseconds;
         if (forceSet)
            return currTime;

         return (currTime < LastMessageTime + UpdateTimeInMilliseconds) ? -1 : currTime;
      }

      /// <summary>
      /// Set the value of the status bar, if there is a valid handle to it.
      /// </summary>
      /// <param name="format">The message format.</param>
      public void ForceSet([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format)
      {
         long currTime = CanDisplayMessage(true);
         if (currTime < 0)
            return;

         SetInternal(format, currTime);
      }

      /// <summary>
      /// Set the value of the status bar, if there is a valid handle to it and enough time has elapsed.
      /// </summary>
      /// <param name="format">The message format.</param>
      /// <param name="argument">The argument for the format string.</param>
      public void Set([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object argument)
      {
         long currTime = CanDisplayMessage(false);
         if (currTime < 0)
            return;
         
         string msg = string.Format(format, argument);
         SetInternal(msg, currTime);
      }

      /// <summary>
      /// Set the value of the status bar, if there is a valid handle to it and enough time has elapsed.
      /// </summary>
      /// <param name="format">The message format.</param>
      /// <param name="firstArgument">The first argument for the format string.</param>
      /// <param name="secondArgument">The second argument for the format string.</param>
      public void Set([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format,
         object firstArgument, object secondArgument)
      {
         long currTime = CanDisplayMessage(false);
         if (currTime < 0)
            return;

         string msg = string.Format(format, firstArgument, secondArgument);
         SetInternal(msg, currTime);
      }
      
      /// <summary>
      /// Set the value of the status bar, if there is a valid handle to it and enough time has elapsed.
      /// </summary>
      /// <param name="format">The message format.</param>
      /// <param name="firstArgument">The first argument for the format string.</param>
      /// <param name="secondArgument">The second argument for the format string.</param>
      /// <param name="thirdArgument">The third argument for the format string.</param>
      public void Set([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, 
         object firstArgument, object secondArgument, object thirdArgument)
      {
         long currTime = CanDisplayMessage(false);
         if (currTime < 0)
            return;

         string msg = string.Format(format, firstArgument, secondArgument, thirdArgument);
         SetInternal(msg, currTime);
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