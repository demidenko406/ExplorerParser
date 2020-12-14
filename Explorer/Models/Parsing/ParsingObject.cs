using System.Collections.Generic;
using System.Linq;

namespace Explorer.Models.Parsing
{
    public class ParsingObject
    {
        
        public enum ObjectType { Primitive, ComplexObject, Collection }
        public ObjectType ObjType { get; set; }
        public string Name { get; }
        public string ContentInstance => content.First();
        public List<string> content { get; set; }

        public ParsingObject(string passedName, List<string> passedContent, ObjectType passedObjectType)
        {
            Name = passedName;
            content = passedContent;
            ObjType = passedObjectType;
        }

        // Constructor that defines type of object.
        public ParsingObject(string passedName, List<string> passedContent)
        {
            Name = passedName;
            content = passedContent;
            if (content.Count > 1)
                ObjType = ObjectType.Collection;
            else if (ContentInstance.Contains('{'))
                ObjType = ObjectType.ComplexObject;
            else
                ObjType = ObjectType.Primitive;
        }
    }
}