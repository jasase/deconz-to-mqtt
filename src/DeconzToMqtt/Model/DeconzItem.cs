using System;
using System.Collections.Generic;
using System.Text;

namespace DeconzToMqtt.Model
{
    public class DeconzItem
    {
        public int Id { get; set; }
        public string ETag { get; set; }
        public string ManufacturerName { get; set; }
        public string ModelId { get; set; }
        public string Name { get; set; }
        public string UniqueId { get; set; }
    }
}
