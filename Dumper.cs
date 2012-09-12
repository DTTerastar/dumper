using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DTZ.Utilities
{
    public class Dumper
    {
        private readonly object _a;
        private readonly IndentedStringBuilder _sb = new IndentedStringBuilder();
        private readonly IList<object> _l = new List<object>();

        private Dumper(Object a)
        {
            _a = a;
        }

        public static string DumpToString(Object a)
        {
            return new Dumper(a).ToString();
        }

        private void Dump(Object o)
        {
            if (o == null) _sb.Append("null");
            else
            {
                var type = o.GetType();
                if (type.IsEnum) _sb.AppendFormat("{0}.{1}", type.Name, o);
                else if (type == typeof(string))
                {
                    var s = o.ToString();
                    if (!SpecialChars(s))
                        _sb.AppendFormat("\"{0}\"", s);
                    else
                        _sb.AppendFormat(@"@""{0}""", s.Replace(@"""", @""""""));
                }
                else if (type == typeof(Guid)) _sb.AppendFormat(@"new Guid(""{0}"")", o);
                else if (type.IsValueType) _sb.Append(o.ToString());
                else if (type.IsSubclassOf(typeof(Type))) _sb.Append(o.ToString());
                else
                {
                    if (_l.Contains(o)) // This breaks circular references
                    {
                        _sb.Append("#Ref");
                        return;
                    }
                    _l.Add(o);

                    _sb.AppendFormat("new {0} {{\r\n", type.Name);
                    using (_sb.IncreaseIndent())
                    {
                        int i = 0;

                        var enumerable = o as IEnumerable;
                        if (enumerable != null)
                            foreach (object b in enumerable)
                            {
                                if (i++ > 0) _sb.Append(",\r\n");
                                Dump(b);
                            }
                        if (!type.IsArray)
                        {
                            int j = 0;
                            foreach (PropertyInfo info in type.GetProperties())
                            {
                                if (info.GetIndexParameters().Length != 0) continue;
                                if (info.Name != "SyncRoot" && info.Name != "ExtensionData")
                                {
                                    _sb.AppendFormat(j++ == 0 ? "{0} = " : ",\r\n{0} = ", info.Name);
                                    var value = GetValue(o, info);

                                    if (value == o)
                                        _sb.Append("this");
                                    else
                                        Dump(value);
                                }
                            }
                        }
                    }
                    _sb.Append("\r\n}");
                }
            }
        }

        private static bool SpecialChars(string s)
        {
            foreach (var a in s)
                if (a == '\r' || a == '\n' || a == '"')
                    return false;
            return true;
        }

        public static void DumpToConsole(Object a)
        {
            Console.Out.WriteLine(DumpToString(a));
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