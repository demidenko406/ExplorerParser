using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Explorer.Models.Parsing
{
    internal class JsonParser
    {
        public static string Serialize(object obj)
        {
            return Serialize(obj, 0);
        }

        public static T Deserialize<T>(string json)
        {
            return Deserialize<T>(Parse(json));
        }

        private static string Serialize(object obj, int depth)
        {
            if (obj == null)
                return "";
            var type = obj.GetType();
            StringBuilder sb;
            if (type.GetCustomAttribute(typeof(NonSerializedAttribute)) != null)
            {
                return "";
            }

            if (type.IsPrimitive || type.IsEnum)
            {
                return $"{obj}";
            }
            if (type == typeof(string))
            {return $"\"{obj}\"";}
            if (ParserUtils.IsEnumerable(type))
            {
                var isComplex = false;
                sb = new StringBuilder("");
                var counter = 0;
                var collectionLength = ParserUtils.CollectionLength((IEnumerable) obj);
                foreach (var subObj in (IEnumerable) obj)
                {
                    if (subObj == null)
                        continue;
                    var contetntInstance = $"{Serialize(subObj, depth + 1)}";
                    if (contetntInstance.Trim('\t', '\n', ' ').First() == '{')
                        isComplex = true;
                    if (counter < collectionLength - 1)
                    {
                        contetntInstance = contetntInstance.TrimEnd('\n');
                        contetntInstance += ',';
                    }

                    sb.Append(contetntInstance);
                    counter++;
                }

                if (isComplex)
                {
                    sb.Insert(0, $"\n{new string('\t', depth)}[");
                    sb.AppendLine();
                    sb.Append($"{new string('\t', depth)}]");
                }
                else
                {
                    sb.Insert(0, "[");
                    sb.Append("]");
                }
            }
            else
            {
                sb = new StringBuilder($"\n{new string('\t', depth)}{{\n");
                MemberInfo[] members = type.GetProperties();
                members = members.Concat(type.GetFields()).ToArray();
                var counter = 0;
                var length = members.Length;
                foreach (var member in members)
                {
                    if (member.GetCustomAttribute(typeof(NonSerializedAttribute)) != null ||
                        ParserUtils.GetMemberContent(obj, member.Name) == null)
                    {
                        continue;
                    }
                    var content = $"{Serialize(ParserUtils.GetMemberContent(obj, member.Name), depth + 1)}";
                    sb.Append($"{new string('\t', depth + 1)}\"{member.Name}\" : {content}"
                        .TrimEnd('\n')); //           "Name" : { field }
                    if (counter != length - 1)
                        sb.Append(',');
                    sb.AppendLine();
                    ++counter;
                }

                sb.Append($"{new string('\t', depth)}}}\n");
            }

            return sb.ToString();
        }

        private static List<ParsingObject> Parse(string json)
        {
            var objects = new List<ParsingObject>();
            var content = new List<string>();
            string passedName = "", passedContent = "";
            int braces = 0, squares = 0;
            var inQuotes = false;
            var inName = true;
            var trimming = new Regex("^\\s*{(?<object>.*)}\\s*$", RegexOptions.Singleline);
            var match = trimming.Match(json);
            if (match.Success) json = match.Groups["object"].Value;
            foreach (var ch in json)
                if (char.IsPunctuation(ch) || char.IsLetterOrDigit(ch) || inQuotes)
                {
                    if (ch == '\"')
                    {
                        inQuotes = !inQuotes;
                    }
                    if (inQuotes)
                    {
                        if (inName)
                            passedName += ch;
                        else
                            passedContent += ch;
                        continue;
                    }

                    if (ch == '{')
                    {
                        braces++;
                    }
                    else if (ch == '}')
                    {
                        braces--;
                    }
                    else if (ch == '[' && braces == 0)
                    {
                        squares++;
                        if (squares == 1) continue;
                    }
                    else if (ch == ']' && braces == 0)
                    {
                        squares--;
                        if (squares == 0) continue;
                    }
                    else if (ch == ':' && braces == 0 && squares == 0)
                    {
                        inName = false;
                        continue;
                    }
                    else if (ch == ',' && braces == 0)
                    {
                        if (inName)
                        {
                            passedContent = passedName;
                            passedName = "";
                        }

                        content.Add(passedContent);
                        passedContent = "";
                        if (squares == 0)
                        {
                            objects.Add(new ParsingObject(passedName, content));
                            content = new List<string>();
                            passedName = "";
                            passedContent = "";
                            inName = true;
                        }

                        continue;
                    }

                    if (inName)
                    {
                        passedName += ch;
                    }
                    else
                    {
                        passedContent += ch;
                    }
                }

            if (braces == 0 && squares == 0)
            {
                if (!(passedName == "" && passedContent == ""))
                {
                    if (inName)
                    {
                        content.Add(passedName);
                        passedName = "";
                    }
                    else
                    {
                        content.Add(passedContent);
                    }

                    objects.Add(new ParsingObject(passedName, content));
                }
            }
            else
            {
                throw new Exception("Json read failure");
            }

            return objects;
        }

        private static T Deserialize<T>(List<ParsingObject> objects)
        {
            T result;
            var type = typeof(T);

            if (objects.Count == 1 && objects.First().Name == "")
            {
                if (objects.First().ObjType == ParsingObject.ObjectType.Primitive)
                {
                    return (T) Convert.ChangeType(objects.First().ContentInstance.Trim('\"'), type);
                }
                if (objects.First().ObjType == ParsingObject.ObjectType.Collection)
                {
                    if (ParserUtils.IsEnumerable(type))
                    {
                        return (T) GetEnumerableInstance(objects.First(), type);
                    }
                }
            }

            result = (T) Activator.CreateInstance(type);
            foreach (var obj in objects)
            {
                var name = obj.Name.Trim('\"');
                var contentInstance = obj.ContentInstance.Trim('\"');
                var memberType = ParserUtils.GetMemberType(type, name);
                if (obj.ObjType == ParsingObject.ObjectType.Primitive)
                {
                    object converted;
                    if (memberType.IsEnum)
                    {
                        converted = Enum.Parse(memberType, contentInstance);
                    }
                    else
                    {
                        converted = Convert.ChangeType(contentInstance, memberType);
                    }
                    ParserUtils.SetMemberContent(result, name, converted);
                }
                else if (obj.ObjType == ParsingObject.ObjectType.ComplexObject)
                {
                    var parsed = typeof(JsonParser)
                        .GetMethod("Deserialize")
                        .MakeGenericMethod(memberType)
                        .Invoke(null, new object[] {Parse(contentInstance)});
                    ParserUtils.SetMemberContent(result, name, parsed);
                }
                else
                {
                    if (ParserUtils.IsEnumerable(memberType))
                    {
                        ParserUtils.SetMemberContent(result, name, GetEnumerableInstance(obj, memberType));
                    }
                    else
                    {
                        throw new Exception("Invalid deserialization");
                    }
                }
            }

            return result;
        }

        private static object GetEnumerableInstance(ParsingObject obj, Type type)
        {
            var genericArgument = type.GenericTypeArguments.Length == 0
                ? type.GetElementType()
                : type.GenericTypeArguments[0];
            var objects = new List<object>();
            var listType = typeof(List<>).MakeGenericType(genericArgument);
            var list = Activator.CreateInstance(listType) as IList;
            foreach (var subValue in obj.content)
                list.Add(typeof(JsonParser)
                    .GetMethod("Deserialize")
                    .MakeGenericMethod(genericArgument)
                    .Invoke(null, new object[] {subValue.Trim()}));
            if (type.IsArray)
            {
                var array = Array.CreateInstance(genericArgument, list.Count);
                list.CopyTo(array, 0);
                return array;
            }

            var enumerableGenericType = typeof(IEnumerable<>).MakeGenericType(genericArgument);
            var info = type.GetConstructor(new[] {enumerableGenericType});
            if (info != null)
            {
                return Activator.CreateInstance(type, list);
            }
            throw new Exception("Type is not a collection");
        }
    }
}