using System.IO;
using Explorer.Models.Parsing;

namespace Explorer.Models
{
    internal class Configuration
    {
        public string CollectionConnectionString { get; set; }
        public string DeletedConnectionString { get; set; }

        public static Configuration GetConfiguration(string src)
        {
            using (var file = File.Open(src, FileMode.Open))
            {
                var reader = new StreamReader(file);
                var xml = reader.ReadToEnd();
                reader.Close();
                var configuration = JsonParser.Deserialize<Configuration>(xml);
                return configuration;
            }
        }

        public static void SetConfiguration(string src, Configuration configuration)
        {
            using (var file = File.Open(src, FileMode.Create))
            {
                var writer = new StreamWriter(file);
                var xml = JsonParser.Serialize(configuration);
                writer.Write(xml);
                writer.Close();
            }
        }
    }
}