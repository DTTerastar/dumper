using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DTZ.Utilities
{
    public static class DumperExtensions
    {
        public static string DumpToString(this Object a)
        {
            return new Dumper(a).ToString();
        }

        public static IEnumerable<T> DumpToConsole<T>(this IEnumerable<T> enumerable)
        {
            foreach (T a in enumerable)
            {
                Console.Out.WriteLine(DumpToString(enumerable));
                yield return a;
            }
        }

        public static T DumpToConsole<T>(this T a)
        {
            Console.Out.WriteLine(DumpToString(a));
            return a;
        }
    }

    public class Dumper
    {
        private readonly object _a;
        private readonly IList<object> _l = new List<object>();
        private readonly IndentedStringBuilder _sb = new IndentedStringBuilder();

        public Dumper(Object a)
        {
            _a = a;
        }

        public static string ToGenericTypeString(Type t)
        {
            if (t.Name.Contains("__AnonymousType")) return t.IsArray ? "[]" : "";
            if (!t.IsGenericType)
                return t.Name;
            string genericTypeName = t.GetGenericTypeDefinition().Name;
            int indexOf = genericTypeName.IndexOf('`');
            if (indexOf > 0)
                genericTypeName = genericTypeName.Substring(0, indexOf);
            string genericArgs = string.Join(",", t.GetGenericArguments().Select(ToGenericTypeString).ToArray());
            return genericTypeName + "<" + genericArgs + ">";
        }

        private void Dump(Object o)
        {
            if (o == null) _sb.Append("null");
            else
            {
                Type type = o.GetType();
                Type genType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
                if (type.IsEnum) _sb.AppendFormat("{0}.{1}", type.Name, Enum.GetName(type, o));
                else if (type == typeof (string))
                {
                    string s = o.ToString();
                    if (!s.Any(b => b == '\r' || b == '\n' || b == '"' || b == '\\'))
                        _sb.AppendFormat("\"{0}\"", s);
                    else
                        _sb.AppendFormat(@"@""{0}""", s.Replace(@"""", @""""""));
                }
                else if (type == typeof (bool)) _sb.Append((bool) o ? "true" : "false");
                else if (type == typeof (Guid)) _sb.AppendFormat(@"new Guid(""{0}"")", o);
                else if (type == typeof (DateTime)) _sb.AppendFormat(@"DateTime.ParseExact(""{0:r}"", ""r"")", o);
                else if (genType == typeof (KeyValuePair<,>))
                {
                    _sb.Append("{");
                    Dump(type.GetProperty("Key").GetValue(o, null));
                    _sb.Append(", ");
                    Dump(type.GetProperty("Value").GetValue(o, null));
                    _sb.Append("}");
                }
                else if (type.IsValueType) _sb.Append(o.ToString());
                else if (type.IsSubclassOf(typeof (Type))) _sb.Append(o.ToString());
                else
                {
                    if (_l.Contains(o)) // This breaks circular references
                    {
                        _sb.Append("#Ref");
                        return;
                    }
                    _l.Add(o);

                    string genericTypeString = ToGenericTypeString(type);
                    _sb.AppendFormat(string.IsNullOrEmpty(genericTypeString) ? "new {0}{{" : "new {0} {{", genericTypeString);

                    using (_sb.IncreaseIndent())
                    {
                        var dictionary = o as IDictionary;
                        if (dictionary != null)
                        {
                            int i = 0;
                            foreach (DictionaryEntry b in dictionary)
                                using (_sb.IncreaseIndent())
                                {
                                    if (i == 0) _sb.AppendLine();
                                    if (i++ > 0) _sb.Append(",\r\n");
                                    _sb.Append("{");
                                    Dump(b.Key);
                                    _sb.Append(", ");
                                    Dump(b.Value);
                                    _sb.Append("}");
                                }
                            if (i != 0) _sb.AppendLine();
                        }
                        else
                        {
                            var enumerable = o as IEnumerable;
                            if (enumerable != null)
                            {
                                int i = 0;
                                foreach (object b in enumerable)
                                {
                                    if (i == 0) _sb.AppendLine();
                                    if (i++ > 0) _sb.Append(",\r\n");
                                    Dump(b);
                                }
                                if (i != 0) _sb.AppendLine();
                            }
                            else
                            {
                                int i = 0;
                                foreach (PropertyInfo info in type.GetProperties())
                                {
                                    if (info.GetIndexParameters().Length != 0) continue;
                                    if (info.Name != "SyncRoot" && info.Name != "ExtensionData")
                                    {
                                        if (i == 0) _sb.AppendLine();

                                        _sb.AppendFormat(i++ == 0 ? "{0} = " : ",\r\n{0} = ", info.Name);
                                        object value = GetValue(o, info);

                                        if (value == o)
                                            _sb.Append("this");
                                        else
                                            Dump(value);
                                    }
                                }
                                if (i != 0) _sb.AppendLine();
                            }
                        }
                    }
                    _sb.Append("}");
                }
            }
        }

        private static object GetValue(object a, PropertyInfo info)
        {
            try
            {
                return info.GetValue(a, null);
            }
            catch (Exception)
            {
                return "#Err";
            }
        }

        public override string ToString()
        {
            if (_sb.Length == 0) Dump(_a);
            return _sb.ToString();
        }
    }
}