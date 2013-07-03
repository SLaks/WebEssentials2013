using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.CSS.Editor.Schemas;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    internal static class ParseItemExtensions
    {
        public static T FindAncestor<T>(this ParseItem item) where T : ComplexItem
        {
            T retVal;
            do
            {
                retVal = item as T;
            }
            while (retVal == null && null != (item = item.Parent));
            return retVal;
        }
    }
}
