using System;
using System.Collections.Generic;

namespace CBRE.Common.Extensions;
   public static class ListExtensions {
       public static void Remove<T>(this List<T> list, Range range) {
           if (!range.Start.IsFromEnd && range.Start.Value > list.Count) {
               return;
           }
           var (offset, length) = range.GetOffsetAndLength(list.Count);
           list.RemoveRange(offset, length);
       }
   }
