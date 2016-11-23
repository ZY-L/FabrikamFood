using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FabrikamFood.Models
{
    public class Item
    {
        public string id { get; set; }
        public string description { get; set; }
        public string image { get; set; }
        public double price { get; set; }

        public Item(string id, string description, string image, double price)
        {
            this.id = id;
            this.description = description;
            this.image = image;
            this.price = price;
        }
    }

}