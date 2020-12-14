using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Explorer.Models.Parsing
{
    internal class XmlParser
    {
        public static string Serialize(object obj)
        {
            return Serialize(obj, 0, "");
        }

        public static T Deserialize<T>(string xml)
        {
            var objects = Parse(xml, true);
            return Deserialize<T>(objects);
        }

        private static T Deserialize<T>(List<ParsingObject> objects)
        {
            T result;
            var type = typeof(T);

            if (objects.First().ObjType == ParsingObject.ObjectType.Primitive
                && objects.Count == 1
                && objects.First().Name == "")
            {
                return (T) Convert.ChangeType(objects.First().ContentInstance.Trim('\"'), type);
            }
            if (ParserUtils.IsEnumerable(type) && objects.Count > 0)
            {
                objects.First().ObjType = ParsingObject.ObjectType.Collection;
                return (T) GetEnumerableInstance(objects.First(), type);
            }

            result = (T) Activator.CreateInstance(type);
            foreach (var obj in objects)
            {
                var key = obj.Name;
                var value = obj.ContentInstance.Trim('\"');
                var memberType = ParserUtils.GetMemberType(type, key);
                if (obj.ObjType == ParsingObject.ObjectType.Primitive)
                {
                    object converted;
                    if (memberType.IsEnum)
                        converted = Enum.Parse(memberType, value);
                    else
                        converted = Convert.ChangeType(value, memberType);
                    ParserUtils.SetMemberContent(result, key, converted);
                }
                else if (obj.ObjType == ParsingObject.ObjectType.ComplexObject)
                {
                    if (ParserUtils.IsEnumerable(ParserUtils.GetMemberType(type, key)))
                    {
                        if (ParserUtils.IsEnumerable(memberType))
                            ParserUtils.SetMemberContent(result, key, GetEnumerableInstance(obj, memberType));
                    }
                    else
                    {
                        var parsed = typeof(XmlParser)
                            .GetMethod("Deserialize", BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(memberType)
                            .Invoke(null, new object[] {Parse(value, false)});
                        ParserUtils.SetMemberContent(result, key, parsed);
                    }
                }
                else
                {
                    if (ParserUtils.IsEnumerable(memberType))
                        ParserUtils.SetMemberContent(result, key, GetEnumerableInstance(obj, memberType));
                    else
                        throw new Exception("Invalid deserialization");
                }
            }

            return result;
        }

        private static string Serialize(object obj, int depth, string key)
        {
            if (obj == null)
                return "";
            var type = obj.GetType();
            StringBuilder sb;
            if (type.GetCustomAttribute(typeof(NonSerializedAttribute)) != null)
            {
                return "";
            }
            if (type.IsPrimitive
                || type.IsEnum
                || type == typeof(string))
            {
                key = key == "" ? type.Name : key;
                return new StringBuilder($"{new string('\t', depth)}<{key}>{obj}</{key}>\n").ToString();
            }

            if (ParserUtils.IsEnumerable(type))
            {
                var genericArgument = type.GenericTypeArguments.Length == 0
                    ? type.GetElementType()
                    : type.GenericTypeArguments[0];
                if (key == "")
                {
                    key = $"{type.Name}_{genericArgument.Name}";
                }
                sb = new StringBuilder($"{new string('\t', depth)}<{key}>\n");
                foreach (var el in (IEnumerable) obj)
                {
                    sb.Append(Serialize(el, depth + 1, genericArgument.Name));
                }
                sb.AppendLine($"{new string('\t', depth)}</{key}>");
            }
            else
            {
                key = key == "" ? type.Name : key;
                sb = new StringBuilder($"{new string('\t', depth)}<{key}>\n");
                MemberInfo[] members = type.GetFields();
                members = members.Concat(type.GetProperties()).ToArray();
                var counter = 0;
                foreach (var member in members)
                {
                    if (member.GetCustomAttribute(typeof(NonSerializedAttribute)) != null ||
                        ParserUtils.GetMemberContent(obj, member.Name) == null)
                    {
                        continue;
                    }
                    var value = Serialize(ParserUtils.GetMemberContent(obj, member.Name), depth + 1, member.Name);
                    if (counter == members.Length - 1)
                    {
                        value = value.TrimEnd('\t', '\n', ' ');
                    }
                    sb.Append(value);
                    counter++;
                }

                sb.AppendLine($"\n{new string('\t', depth)}</{key}>");
            }

            return sb.ToString();
        }

        private static List<ParsingObject> Parse(string xml, bool trim)
        {
            xml = xml.Trim('\n', '\t', '\r', ' ');
            var objects = new List<ParsingObject>();
            var values = new List<string>();
            string tagName;
            Match match;
            try
            {
                tagName = GetNextTag(xml, 0);
                if (trim)
                {
                    var trimming = new Regex($"^<{tagName}>(.*)</{tagName}>$", RegexOptions.Singleline);
                    match = trimming.Match(xml);
                    if (match.Success)
                    {
                        xml = match.Groups[1].Value;
                    }
                }
            }
            catch
            {
                return new List<ParsingObject>
                    {new ParsingObject("", new List<string> {xml}, ParsingObject.ObjectType.Primitive)};
            }

            var Tag = new Regex(@"<(/?.*)>");

            var keyValues = new Dictionary<string, List<string>>();
            string mainTag = "", tag = "";
            var deep = 0;
            bool isMainTag = true, isValue = false;
            var value = "";
            var quotes = false;
            foreach (var c in xml)
                if (c != '\t' && c != '\r' && c != '\n' || quotes)
                {
                    if (c == '\"')
                    {
                        quotes = !quotes;
                    }
                    if (quotes)
                    {
                        value += c;
                        continue;
                    }

                    if (c == '<')
                    {
                        isValue = false;
                        if (!isMainTag)
                        {
                            tag += c;
                        }
                    }
                    else if (c == '>')
                    {
                        if (isMainTag)
                        {
                            isMainTag = false;
                            isValue = true;
                            deep++;
                        }
                        else
                        {
                            tag += c;
                            match = Tag.Match(tag);

                            if (match.Success)
                            {
                                tagName = match.Groups[1].Value;
                                if (tagName[0] == '/')
                                {
                                    if ('/' + mainTag == tagName && deep == 1)
                                    {
                                        if (keyValues.ContainsKey(mainTag))
                                        {
                                            keyValues[mainTag].Add(value);
                                        }
                                        else
                                        {
                                            keyValues.Add(mainTag, new List<string> {value});
                                        }
                                        mainTag = "";
                                        tag = "";
                                        isMainTag = true;
                                        isValue = false;
                                        value = "";
                                    }
                                    else
                                    {
                                        value += tag;
                                        tag = "";
                                        isValue = true;
                                    }

                                    deep--;
                                }
                                else
                                {
                                    deep++;
                                    isValue = true;
                                    value += tag;
                                    tag = "";
                                }
                            }
                            else
                            {
                                throw new Exception("XML file was damaged");
                            }
                        }
                    }
                    else
                    {
                        if (isValue)
                        {
                            value += c;
                        }
                        else if (isMainTag)
                        {
                            mainTag += c;
                        }
                        else
                        {
                            tag += c;
                        }
                    }
                }

            if (mainTag != "")
            {
                return new List<ParsingObject>
                {
                    new ParsingObject("", new List<string> {mainTag}, ParsingObject.ObjectType.Primitive)
                };

            }
            foreach (var pair in keyValues)
            {
                ParsingObject.ObjectType type;
                if (pair.Value.First().Length > 0 && pair.Value.First()[0] == '<')
                {
                    type = ParsingObject.ObjectType.ComplexObject;
                }
                else
                {
                    type = ParsingObject.ObjectType.Primitive;
                }
                objects.Add(new ParsingObject(pair.Key, pair.Value, type));
            }

            return objects;
        }

        private static string GetNextTag(string xml, int startIndex)
        {
            var tag = new StringBuilder("");
            var isTag = false;
            for (var i = startIndex; i < xml.Length; i++)
            {
                var ch = xml[i];
                if (ch == '<')
                {
                    isTag = true;
                }
                else if ((ch == '>' || ch == ' ') && isTag)
                {
                    return tag.ToString();
                }
                else if (isTag)
                {
                    tag.Append(ch);
                }
            }

            throw new Exception($"No tags are left from current position {startIndex}");
        }

        private static object GetEnumerableInstance(ParsingObject obj, Type type)
        {
            var genericArgument = type.GenericTypeArguments.Length == 0
                ? type.GetElementType()
                : type.GenericTypeArguments[0];
            var objects = new List<object>();
            var listType = typeof(List<>).MakeGenericType(genericArgument);
            var list = Activator.CreateInstance(listType) as IList;
            if (obj.ObjType == ParsingObject.ObjectType.ComplexObject)
            {
                obj = Parse(obj.ContentInstance, false).First();
            }
            foreach (var subVal in obj.content)
            {
                list.Add(typeof(XmlParser).GetMethod("Deserialize")
                    .MakeGenericMethod(genericArgument)
                    .Invoke(null, new object[] {subVal.Trim()}));

            }
            if (type.IsArray)
            {
                var array = Array.CreateInstance(genericArgument, list.Count);
                list.CopyTo(array, 0);
                return array;
            }

            var IEnumerableGenericType = typeof(IEnumerable<>).MakeGenericType(genericArgument);
            var info = type.GetConstructor(new[] {IEnumerableGenericType});
            if (info != null)
            {
                return Activator.CreateInstance(type, list);
            }
            throw new Exception("Type is not a collection");
        }
    }
} 