﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Collections;
using System.Runtime.CompilerServices;
using Signum.Utilities.ExpressionTrees;
using System.Reflection;
using Signum.Entities.Reflection;

namespace Signum.Entities
{
    public static class ObjectDumper
    {
        public static string Dump(this object o)
        {
            return o.Dump(false);
        }

        public static string Dump(this object o, bool showIgnoredFields)
        {
            var od = new DumpVisitor(showIgnoredFields);
            od.DumpObject(o);
            return od.Sb.TryToString();
        }

        static string Indent(this string t, int level)
        {
            return t.PadLeft(t.Length + (level * 3));
        }

        class DumpVisitor
        {
            HashSet<Object> objects = new HashSet<Object>();
            public StringBuilder Sb = new StringBuilder();
            int level = 0;
            bool showIgnoredFields = false;

            public DumpVisitor(bool showIgnoredFields)
            {
                this.showIgnoredFields = showIgnoredFields;
            }

            public void DumpObject(object o)
            {
                if (o == null)
                {
                    Sb.Append("null");
                    return;
                }

                Type t = o.GetType();

                if (IsDelegate(t))
                {
                    Sb.Append("[DELEGATE]");
                    return;
                }

                if (IsBasicType(t))
                {
                    Sb.Append(DumpValue(o));
                    return;
                }

                Sb.Append("new ");

                Sb.Append(CSharpRenderer.TypeName(t));

                if (objects.Contains(o))
                {
                    if (o is IdentifiableEntity || o is Lite)
                    {
                        var id = o is IdentifiableEntity ? (o as IdentifiableEntity).IdOrNull : (o as Lite).IdOrNull;
                        Sb.Append(id.HasValue ? "({0})".Formato(id.Value) : "");
                    }
                    Sb.Append(" /* [CICLE] {0} */".Formato(o.ToString()));
                    return;
                }

                objects.Add(o);

                if (o is IdentifiableEntity)
                {
                    var id = (o as IdentifiableEntity).IdOrNull;
                    Sb.Append(id.HasValue ? "({0})".Formato(id.Value) : "");
                    Sb.Append(" /* {0} */".Formato(o.ToString()));
                }

                if (o is Lite)
                {
                    var l = o as Lite;
                    Sb.Append("({0}, \"{1}\")".Formato((l.IdOrNull.HasValue ? l.Id.ToString() : "null"), l.ToString()));
                    if (((Lite)o).UntypedEntityOrNull == null)
                        return;
                }

                if (o is IEnumerable && !Any((o as IEnumerable)))
                {
                    Sb.Append("{}");
                    return;
                }

                Sb.AppendLine().AppendLine("{".Indent(level));
                level += 1;

                if (t.Namespace.HasText() && t.Namespace.StartsWith("System.Reflection"))
                    Sb.AppendLine("ToString = {0},".Formato(o.ToString()).Indent(level));
                else if (o is Exception)
                {
                    var ex = o as Exception;
                    DumpPropertyOrField(typeof(string), "Message", ex.Message);
                    DumpPropertyOrField(typeof(string), "StackTrace", ex.StackTrace);
                    DumpPropertyOrField(typeof(Exception), "InnerException", ex.InnerException);
                    DumpPropertyOrField(typeof(IDictionary), "Data", ex.Data);
                }
                else if (o is IEnumerable)
                {
                    if (o is IDictionary)
                    {
                        foreach (DictionaryEntry item in (o as IDictionary))
                        {
                            Sb.Append("{".Indent(level));
                            DumpObject(item.Key);
                            Sb.Append(", ");
                            DumpObject(item.Value);
                            Sb.AppendLine("},");
                        }
                    }
                    else
                        foreach (var item in (o as IEnumerable))
                        {
                            Sb.Append("".Indent(level));
                            DumpObject(item);
                            Sb.AppendLine(",");
                        }
                }
                else if (t.IsAnonymous())
                    foreach (var prop in t.GetProperties(BindingFlags.Instance |
                                                         BindingFlags.Public))
                    {
                        DumpPropertyOrField(prop.PropertyType, prop.Name, prop.GetValue(o, null));
                    }
                else
                    foreach (var field in Reflector.InstanceFieldsInOrder(t))
                    {
                        if (showIgnoredFields || !field.IsDefined(typeof(IgnoreAttribute), false))
                            DumpPropertyOrField(field.FieldType, field.Name, field.GetValue(o));
                    }

                level -= 1;
                Sb.Append("}".Indent(level));
                return;
            }

            private bool Any(IEnumerable ie)
            {
                if (ie is IList)
                    return (ie as IList).Count > 0;

                if (ie is Array)
                    return (ie as Array).Length > 0;

                foreach (var item in ie)
                {
                    return true;
                }

                return false;
            }

            private void DumpPropertyOrField(Type type, string name, object obj)
            {
                Sb.AppendFormat("{0} = ".Indent(level), name);
                DumpObject(obj);
                Sb.AppendLine(",");
            }

            private bool IsDelegate(Type type)
            {
                return typeof(Delegate).IsAssignableFrom(type);
            }

            bool IsBasicType(Type type)
            {
                var unType = type.UnNullify();
                return CSharpRenderer.IsBasicType(unType) || unType == typeof(DateTime);
            }

            string DumpValue(object item)
            {
                string value = item.TryToString() ?? "null";
                string startDelimiter = null;
                string endDelimiter = null;

                if (item != null)
                {
                    if (item is string)
                    {
                        startDelimiter = endDelimiter = "\"";
                        value = value.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
                    }

                    if (item is decimal || item is double || item is float)
                        value = value.Replace(',', '.');

                    if (item is decimal)
                        endDelimiter = "M";

                    if (item is float)
                        endDelimiter = "F";

                    if (item is Enum)
                        startDelimiter = item.GetType().Name + ".";

                    if (item is bool)
                        value = value.ToLower();

                    if (item is DateTime)
                    {
                        value = "DateTime.Parse(\"{0}\")".Formato(value);
                    }
                }

                return "{0}{1}{2}".Formato(startDelimiter, value, endDelimiter);
            }
        };

    }
}
