using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CInnovation.SignalProcessing.Filters.Utils
{
    [RemoveObj]
    [AttributeUsage(AttributeTargets.Method)]
    internal class CSCalliAttribute : Attribute
    {
        public CSCalliAttribute()
        {
        }
    }
}