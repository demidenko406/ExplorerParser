using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Explorer.Models.Parsing;

namespace Explorer.Models
{
    internal class CollectionHistory
    {
        public string Collection { get; set; }

        public void UpdateCollection()
        {
            using (var file = File.Open(Config.CollectionTarget, FileMode.Create))
            {
                var writer = new StreamWriter(file);
                var collection = XmlParser.Serialize(this);
                writer.Write(collection);
                writer.Close();
            }
        }
    }
}
