using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FabrikamFood.DataModels
{
    public class Food
    {
        [JsonProperty(PropertyName = "Id")]
        public string id { get; set; }

        [JsonProperty(PropertyName = "Price")]
        public double price { get; set; }

        [JsonProperty(PropertyName = "Quantity")]
        public double quantity { get; set; }

        [JsonProperty(PropertyName = "Image")]
        public string image { get; set; }

        [JsonProperty(PropertyName = "Description")]
        public string description { get; set; }
    }
}