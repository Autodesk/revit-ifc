﻿//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2017  Autodesk, Inc.
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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit.IFC.Export.Utility
{
   class ParamExprLogger
   {
      private static MemoryStream m_mStream = null;

      /// <summary>
      /// Logger stream
      /// </summary>
      public static MemoryStream loggerStream
      {
         get
         {
            if (m_mStream == null) m_mStream = new MemoryStream();
            return m_mStream;
         }
      }

      /// <summary>
      /// reset the logger stream
      /// </summary>
      public static void resetStream()
      {
         if (m_mStream != null)
         {
            m_mStream.Dispose();
            m_mStream = null;
         }
      }

      /// <summary>
      /// Write log
      /// </summary>
      /// <param name="msgText">the message</param>
      public static void writeLog(string msgText)
      {
         if (m_mStream == null) m_mStream = new MemoryStream();
         UnicodeEncoding uniEncoding = new UnicodeEncoding();

         byte[] msgString = uniEncoding.GetBytes(msgText);
         m_mStream.Write(msgString, 0, msgString.Length);
         m_mStream.Flush();
      }

      /// <summary>
      /// Get MemoryStream content
      /// </summary>
      /// <returns>character array</returns>
      public static char[] getmStreamContent()
      {
         char[] charArray;
         UnicodeEncoding uniEncoding = new UnicodeEncoding();

         byte[] byteArray = new byte[m_mStream.Length];
         int countC = uniEncoding.GetCharCount(byteArray);
         int countB = (int)m_mStream.Length;
         m_mStream.Seek(0, SeekOrigin.Begin);
         m_mStream.Read(byteArray, 0, countB);
         charArray = new char[countC];
         uniEncoding.GetDecoder().GetChars(byteArray, 0, countB, charArray, 0);

         return charArray;
      }
   }
}
