
using System.Collections.Generic;
using System.IO;
using Explorer.Models.Parsing;

namespace Explorer.Models
{
    class DeleteHisotry
    {
        public List <string> DeletedHistory { get; set; }
        
        public void UpdateBin()
        {
            using (var file = File.Open(Config.DeletedTarget, FileMode.Create))
            {
                var writer = new StreamWriter(file);
                var log = JsonParser.Serialize(this);
                writer.Write(log);
                writer.Close();
            }
        }
    }
}


