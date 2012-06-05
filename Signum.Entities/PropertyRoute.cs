﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities.Properties;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities
{
    [Serializable]
    public class PropertyRoute : IEquatable<PropertyRoute>
    {
        Type type;
        public PropertyRouteType PropertyRouteType { get; private set; } 
        public FieldInfo FieldInfo { get; private set;}
        public PropertyInfo PropertyInfo { get; private set; }
        public PropertyRoute Parent { get; private set;}

        public MemberInfo[] Members
        {
            get { return this.FollowC(a => a.Parent).Select(a => a.FieldInfo ?? (MemberInfo)a.PropertyInfo).Reverse().Skip(1).ToArray(); }
        }

        public PropertyInfo[] Properties
        {
            get { return this.FollowC(a => a.Parent).Select(a => a.PropertyInfo).Reverse().Skip(1).ToArray(); }
        }

        public static PropertyRoute Construct<T>(Expression<Func<T, object>> expression)
            where T : IRootEntity
        {
            PropertyRoute result = Root(typeof(T));

            foreach (var mi in Reflector.GetMemberList(expression))
            {
                result = result.Add(mi); 
            }
            return result;
        }

        public PropertyRoute Add(string fieldOrProperty)
        {
            MemberInfo mi = (MemberInfo)Type.GetProperty(fieldOrProperty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
                            (MemberInfo)Type.GetField(fieldOrProperty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (mi == null)
                throw new InvalidOperationException("{0}.{1} does not exist".Formato(this, fieldOrProperty));

            return Add(mi);
        }

        public PropertyRoute Add(MemberInfo fieldOrProperty)
        {
            if (this.Type.IsIIdentifiable() && PropertyRouteType != PropertyRouteType.Root)
            {
                Implementations imp = GetImplementations();

                ImplementedByAttribute ib = imp as ImplementedByAttribute;
                if (ib != null && ib.ImplementedTypes.Length == 1)
                {
                    return new PropertyRoute(Root(ib.ImplementedTypes.SingleEx()), fieldOrProperty); 
                }

                if (imp != null)
                    throw new InvalidOperationException("Attempt to make a PropertyRoute on a {0}. Cast first".Formato(imp.GetType()));

                return new PropertyRoute(Root(this.Type), fieldOrProperty);
            }
            return new PropertyRoute(this, fieldOrProperty);
        }

        PropertyRoute(PropertyRoute parent, MemberInfo fieldOrProperty)
        {
            if (fieldOrProperty == null)
                throw new ArgumentNullException("fieldOrProperty");

            if (parent == null)
                throw new ArgumentNullException("parent");

            this.Parent = parent;


            if (parent.Type.IsIIdentifiable() && parent.PropertyRouteType != PropertyRouteType.Root)
                throw new ArgumentException("Parent can not be a non-root Identifiable");

            if (fieldOrProperty is PropertyInfo && Reflector.IsMList(parent.Type))
            {
                if (fieldOrProperty.Name != "Item")
                    throw new NotSupportedException("PropertyInfo {0} is not supported".Formato(fieldOrProperty.Name));

                PropertyInfo = (PropertyInfo)fieldOrProperty;
                PropertyRouteType = PropertyRouteType.MListItems;
            }
            else if (fieldOrProperty is PropertyInfo && Reflector.IsLite(parent.Type))
            {
                if (fieldOrProperty.Name != "Entity" && fieldOrProperty.Name != "EntityOrNull")
                    throw new NotSupportedException("PropertyInfo {0} is not supported".Formato(fieldOrProperty.Name));

                PropertyInfo = (PropertyInfo)fieldOrProperty;
                PropertyRouteType = PropertyRouteType.LiteEntity;
            }
            else if (typeof(ModifiableEntity).IsAssignableFrom(parent.Type) || typeof(IRootEntity).IsAssignableFrom(parent.Type))
            {
                PropertyRouteType = PropertyRouteType.FieldOrProperty;
                if (fieldOrProperty is PropertyInfo)
                {
                    if (!parent.Type.FollowC(a => a.BaseType).Contains(fieldOrProperty.DeclaringType))
                    {
                        var pi = (PropertyInfo)fieldOrProperty;

                        if (!parent.Type.GetInterfaces().Contains(fieldOrProperty.DeclaringType))
                            throw new ArgumentException("PropertyInfo {0} not found on {1}".Formato(pi.PropertyName(), parent.Type));

                        var otherProperty = parent.Type.FollowC(a => a.BaseType)
                            .Select(a => a.GetProperty(fieldOrProperty.Name, BindingFlags.Public | BindingFlags.Instance)).NotNull().FirstEx();

                        if (otherProperty == null)
                            throw new ArgumentException("PropertyInfo {0} not found on {1}".Formato(pi.PropertyName(), parent.Type));

                        fieldOrProperty = otherProperty;
                    }

                    PropertyInfo = (PropertyInfo)fieldOrProperty;
                    FieldInfo = Reflector.TryFindFieldInfo(Parent.Type, PropertyInfo);
                }
                else
                {
                    FieldInfo = (FieldInfo)fieldOrProperty;
                    PropertyInfo = Reflector.TryFindPropertyInfo(FieldInfo);
                }
            }
            else
                throw new NotSupportedException("Properties of {0} not supported".Formato(parent.Type));

         
            
        }

        public static PropertyRoute Root(Type rootEntity)
        {
            return new PropertyRoute(rootEntity);
        }

        PropertyRoute(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (!typeof(IRootEntity).IsAssignableFrom(type))
                throw new ArgumentException("Type must implement IPropertyRouteRoot");

            this.type = type;
            this.PropertyRouteType = PropertyRouteType.Root;
        }

        public Type Type
        {
            get
            {
                if (type != null)
                    return type;

                if (FieldInfo != null)
                    return FieldInfo.FieldType;

                if (PropertyInfo != null)
                    return PropertyInfo.PropertyType;

                throw new InvalidOperationException("No FieldInfo or PropertyInfo"); 
            }
        }

        public Type RootType { get { return type ?? Parent.RootType; } }

        public override string ToString()
        {
            switch (PropertyRouteType)
            {
                case PropertyRouteType.Root:
                    return "({0})".Formato(type.Name);
                case PropertyRouteType.FieldOrProperty:
                    return Parent.ToString() + (Parent.PropertyRouteType == PropertyRouteType.MListItems ? "" : ".") + (PropertyInfo != null ? PropertyInfo.Name : FieldInfo.Name);
                case PropertyRouteType.MListItems:
                    return Parent.ToString() + "/";
                case PropertyRouteType.LiteEntity:
                    return Parent.ToString() + ".Entity";
            }
            throw new InvalidOperationException();
        }

        public string PropertyString()
        {
            switch (PropertyRouteType)
            {
                case PropertyRouteType.Root:
                    throw new InvalidOperationException("Root has no PropertyString");
                case PropertyRouteType.FieldOrProperty:
                    switch (Parent.PropertyRouteType)
                    {
                        case PropertyRouteType.Root: return (PropertyInfo != null ? PropertyInfo.Name : FieldInfo.Name);
                        case PropertyRouteType.FieldOrProperty: return Parent.PropertyString() + "." + (PropertyInfo != null ? PropertyInfo.Name : FieldInfo.Name);
                        case PropertyRouteType.MListItems: return Parent.PropertyString() + PropertyInfo.Name;
                        default: throw new InvalidOperationException();
                    }
                case PropertyRouteType.MListItems:
                    return Parent.PropertyString() + "/";
            }
            throw new InvalidOperationException();
        }


        public static PropertyRoute Parse(Type type, string route)
        {
            PropertyRoute result = PropertyRoute.Root(type);

            foreach (var part in route.Replace("/", ".Item.").Split('.'))
            {
                result = result.Add(part);
            }

            return result;
        }

        public static void SetFindImplementationsCallback(Func<PropertyRoute, Implementations> findImplementations)
        {
            FindImplementations = findImplementations;
        }

        static Func<PropertyRoute, Implementations> FindImplementations;

        public Implementations GetImplementations()
        {
            if (FindImplementations == null)
                throw new InvalidOperationException("PropertyRoute.FindImplementations not set");

            return FindImplementations(this);
        }

        public static void SetIsAllowedCallback(Func<PropertyRoute, bool> isAllowed)
        {
            IsAllowedCallback = isAllowed;
        }

        static Func<PropertyRoute, bool> IsAllowedCallback;
        
        public bool IsAllowed()
        {
            if (IsAllowedCallback != null)
                return IsAllowedCallback(this);

            return true;
        }


        public static List<PropertyRoute> GenerateRoutes(Type type)
        {
            PropertyRoute root = PropertyRoute.Root(type);
            List<PropertyRoute> result = new List<PropertyRoute>();

            foreach (PropertyInfo pi in Reflector.PublicInstancePropertiesInOrder(type))
            {
                PropertyRoute route = root.Add(pi);
                result.Add(route);

                if (Reflector.IsEmbeddedEntity(pi.PropertyType))
                    result.AddRange(GenerateEmbeddedProperties(route));

                if (Reflector.IsMList(pi.PropertyType))
                {
                    Type colType = pi.PropertyType.ElementType();
                    if (Reflector.IsEmbeddedEntity(colType))
                        result.AddRange(GenerateEmbeddedProperties(route.Add("Item")));
                }
            }

            return result;
        }

        static List<PropertyRoute> GenerateEmbeddedProperties(PropertyRoute embeddedProperty)
        {
            List<PropertyRoute> result = new List<PropertyRoute>();
            foreach (var pi in Reflector.PublicInstancePropertiesInOrder(embeddedProperty.Type))
            {
                PropertyRoute property = embeddedProperty.Add(pi);
                result.AddRange(property);

                if (Reflector.IsEmbeddedEntity(pi.PropertyType))
                    result.AddRange(GenerateEmbeddedProperties(property));
            }

            return result;
        }

        public bool Equals(PropertyRoute other)
        {
            if (other.PropertyRouteType != this.PropertyRouteType)
                return false;

            if (Type != other.Type)
                return false;

            if (!ReflectionTools.FieldEquals(FieldInfo, other.FieldInfo))
                return false;

            return object.Equals(Parent, other.Parent); 
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            PropertyRoute other = obj as PropertyRoute;

            if (obj == null)
                return false;

            return Equals(other);
        }
    }

    public interface IImplementationsFinder
    {
        Implementations FindImplementations(PropertyRoute route);
    }

    public enum PropertyRouteType
    {
        Root,
        FieldOrProperty,
        LiteEntity, 
        MListItems,
    }

   
}
