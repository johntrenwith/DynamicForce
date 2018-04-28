using System.Collections.Generic;

namespace DynamicForce
{
    public class Field
    {
        public string Label { get; set; }
        public string Name { get; set; }
        public IList<PicklistValue> PickListValues { get; set; }
    }
}