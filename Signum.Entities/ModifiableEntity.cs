﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.ComponentModel;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Specialized;
using Signum.Utilities.Reflection;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using Signum.Entities.Reflection;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities
{
    [Serializable, DebuggerTypeProxy(typeof(FlattenHierarchyProxy))]
    public abstract class ModifiableEntity : Modifiable, INotifyPropertyChanged, IDataErrorInfo, ICloneable
    {
        [Ignore]
        bool selfModified = true;

        [HiddenProperty]
        public override bool SelfModified
        {
            get { return selfModified; }
        }

        protected internal virtual void SetSelfModified()
        {
            selfModified = true; 
        }

        protected override void CleanSelfModified()
        {
            selfModified = false;
        }

        protected virtual bool Set<T>(ref T field, T value, Expression<Func<T>> property)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            PropertyInfo pi = ReflectionTools.BasePropertyInfo(property);

            INotifyCollectionChanged col = field as INotifyCollectionChanged;
            if (col != null)
            {
                if (AttributeManager<NotifyCollectionChangedAttribute>.FieldContainsAttribute(GetType(), pi))
                    col.CollectionChanged -= ChildCollectionChanged;

                if (AttributeManager<NotifyChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    foreach (INotifyPropertyChanged item in (IEnumerable)col)
                        item.PropertyChanged -= ChildPropertyChanged;

                if (AttributeManager<ValidateChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    foreach (ModifiableEntity item in (IEnumerable)col)
                        item.ExternalPropertyValidation -= ChildPropertyValidation;
            }

            ModifiableEntity mod = field as ModifiableEntity;
            if (mod != null)
            {
                if (AttributeManager<NotifyChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    mod.PropertyChanged -= ChildPropertyChanged;

                if (AttributeManager<ValidateChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    mod.ExternalPropertyValidation -= ChildPropertyValidation;
            }

            SetSelfModified();
            field = value;

            col = field as INotifyCollectionChanged;
            if (col != null)
            {
                if (AttributeManager<NotifyCollectionChangedAttribute>.FieldContainsAttribute(GetType(), pi))
                    col.CollectionChanged += ChildCollectionChanged;

                if (AttributeManager<NotifyChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    foreach (INotifyPropertyChanged item in (IEnumerable)col)
                        item.PropertyChanged += ChildPropertyChanged;

                if (AttributeManager<ValidateChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    foreach (ModifiableEntity item in (IEnumerable)col)
                        item.ExternalPropertyValidation += ChildPropertyValidation;
            }

            mod = field as ModifiableEntity;
            if (mod != null)
            {
                if (AttributeManager<NotifyChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    mod.PropertyChanged += ChildPropertyChanged;

                if (AttributeManager<ValidateChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    mod.ExternalPropertyValidation += ChildPropertyValidation;
            }

            NotifyPrivate(pi.Name);
            NotifyPrivate("Error");

            return true;
        }

        static readonly Expression<Func<ModifiableEntity, string>> ToStringPropertyExpression = m => m.ToString();
        [HiddenProperty]
        public string ToStringProperty
        {
            get
            {
                string str = ToString();
                return str.HasText() ? str : this.GetType().NiceName();
            }
        }

        public bool SetToStr<T>(ref T field, T value, Expression<Func<T>> property)
        {
            if (this.Set(ref field, value, property))
            {
                NotifyToString();
                return true;
            }
            return false;
        }

        #region Collection Events

        protected internal override void PostRetrieving()
        {
            RebindEvents();
        }

        protected virtual void RebindEvents()
        {
            foreach (INotifyCollectionChanged notify in AttributeManager<NotifyCollectionChangedAttribute>.FieldsWithAttribute(this))
            {
                if (notify == null)
                    continue;
             
                notify.CollectionChanged += ChildCollectionChanged;
            }

            foreach (object field in AttributeManager<NotifyChildPropertyAttribute>.FieldsWithAttribute(this))
            {
                if (field == null)
                    continue;

                var entity = field as ModifiableEntity;
                if (entity != null)
                    entity.PropertyChanged += ChildPropertyChanged;
                else
                {
                    foreach (INotifyPropertyChanged item in (IEnumerable)field)
                        item.PropertyChanged += ChildPropertyChanged;
                }
            }

            foreach (object field in AttributeManager<ValidateChildPropertyAttribute>.FieldsWithAttribute(this))
            {
                if (field == null)
                    continue;

                var entity = field as ModifiableEntity;
                if (entity != null)
                    entity.ExternalPropertyValidation += ChildPropertyValidation;
                else
                {
                    foreach (ModifiableEntity item in (IEnumerable)field)
                        item.ExternalPropertyValidation += ChildPropertyValidation;
                }
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            RebindEvents();
        }

        protected virtual void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            string propertyName = AttributeManager<NotifyCollectionChangedAttribute>.FindPropertyName(this, sender);
            if (propertyName != null)
                NotifyPrivate(propertyName); 

            if (AttributeManager<NotifyChildPropertyAttribute>.FieldsWithAttribute(this).Contains(sender))
            {
                if (args.NewItems != null)
                    foreach (var p in args.NewItems.Cast<INotifyPropertyChanged>()) p.PropertyChanged += ChildPropertyChanged;
                if (args.OldItems != null)
                    foreach (var p in args.OldItems.Cast<INotifyPropertyChanged>()) p.PropertyChanged -= ChildPropertyChanged;
            }

            if (AttributeManager<ValidateChildPropertyAttribute>.FieldsWithAttribute(this).Contains(sender))
            {
                if (args.NewItems != null)
                    foreach (var p in args.NewItems.Cast<ModifiableEntity>()) p.ExternalPropertyValidation += ChildPropertyValidation;
                if (args.OldItems != null)
                    foreach (var p in args.OldItems.Cast<ModifiableEntity>()) p.ExternalPropertyValidation -= ChildPropertyValidation;
            }
        }

        protected virtual void ChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        protected virtual string ChildPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
        {
            return null;
        }
        #endregion

        [field: NonSerialized, Ignore]
        public event PropertyChangedEventHandler PropertyChanged;

        [field: NonSerialized, Ignore]
        public event PropertyValidationEventHandler ExternalPropertyValidation;

        protected void Notify<T>(Expression<Func<T>> property)
        {
            NotifyPrivate(ReflectionTools.BasePropertyInfo(property).Name);
            NotifyError();
        }

        public void NotifyError()
        {
            NotifyPrivate("Error");
        }

        public void NotifyToString()
        {
            NotifyPrivate("ToStringProperty");
        }

        void NotifyPrivate(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        static long temporalIdCounter = 0;

        #region Temporal ID
        [Ignore]
        internal int temporalId;

        internal ModifiableEntity()
        {
            temporalId = unchecked((int)Interlocked.Increment(ref temporalIdCounter));
        }

        public override int GetHashCode()
        {
            return GetType().FullName.GetHashCode() ^ temporalId;
        }
        #endregion

        #region IDataErrorInfo Members
        [HiddenProperty]
        public string Error
        {
            get { return IntegrityCheck(); }
        }

        public string IntegrityCheck()
        {
            return Validator.GetPropertyPacks(GetType()).Select(k => PropertyCheck(k.Value)).NotNull().ToString("\r\n");
        }

        //override for per-property checks
        [HiddenProperty]
        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                if (columnName == null)
                    return ((IDataErrorInfo)this).Error;
                else
                {
                    PropertyPack pp = Validator.GetOrCreatePropertyPack(GetType(), columnName);
                    if (pp == null)
                        return null; //Hidden properties

                    return PropertyCheck(pp);
                }
            }
        }

        public string PropertyCheck<T>(Expression<Func<T, object>> property) where T : ModifiableEntity
        {
            return PropertyCheck(Validator.GetOrCreatePropertyPack(property));
        }

        public string PropertyCheck(string propertyName) 
        {
            return PropertyCheck(Validator.GetOrCreatePropertyPack(GetType(), propertyName));
        }

        public string PropertyCheck(PropertyPack pp)
        {
            if (pp.DoNotValidate)
                return null;

            if (pp.Validators.Count > 0)
            {
                object propertyValue = pp.GetValue(this);

                //ValidatorAttributes
                foreach (var validator in pp.Validators)
                {
                    string result = validator.Error(this, pp.PropertyInfo, propertyValue);
                    if (result != null)
                        return result;
                }
            }

            //Internal Validation
            if (!pp.SkipPropertyValidation)
            {
                string result = PropertyValidation(pp.PropertyInfo);
                if (result != null)
                    return result;
            }

            //External Validation
            if (!pp.SkipExternalPropertyValidation && ExternalPropertyValidation != null)
            {
                string result = ExternalPropertyValidation(this, pp.PropertyInfo);
                if (result != null)
                    return result;
            }

            //Static validation
            if (pp.HasStaticPropertyValidation)
            {
                string result = pp.OnStaticPropertyValidation(this, pp.PropertyInfo);
                if (result != null)
                    return result;
            }
            return null;
        }

        protected virtual string PropertyValidation(PropertyInfo pi)
        {
            return null;
        }

        public string FullIntegrityCheck()
        {
            var graph = GraphExplorer.FromRoot(this);
            return GraphExplorer.Integrity(graph);
        }

        public Dictionary<ModifiableEntity, string> FullIntegrityCheckDictionary()
        {
            var graph = GraphExplorer.FromRoot(this);
            return GraphExplorer.IntegrityDictionary(graph);
        }

        #endregion

        #region ICloneable Members
        object ICloneable.Clone()
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                bf.Serialize(stream, this);
                stream.Seek(0, SeekOrigin.Begin);
                return bf.Deserialize(stream);
            }
        }

        #endregion
    }

    //Based on: http://blogs.msdn.com/b/jaredpar/archive/2010/02/19/flattening-class-hierarchies-when-debugging-c.aspx
    internal sealed class FlattenHierarchyProxy
    {
        [DebuggerDisplay("{Value}", Name = "{Name,nq}", Type = "{TypeName,nq}")]
        internal struct Member
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            internal string Name;
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            internal object Value;
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            internal Type Type;
            internal Member(string name, object value, Type type)
            {
                Name = name;
                Value = value;
                Type = type;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            internal string TypeName
            {
                get { return Type.TypeName(); }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object target;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Member[] memberList;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        internal Member[] Items
        {
            get
            {
                if (memberList == null)
                {
                    memberList = BuildMemberList().ToArray();
                }
                return memberList;
            }
        }

        public FlattenHierarchyProxy(object target)
        {
            this.target = target;
        }

        private List<Member> BuildMemberList()
        {
            var list = new List<Member>();
            if (target == null)
                return list;

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var type = target.GetType();
            list.Add(new Member("Type", type, typeof(Type)));

            foreach (var t in type.FollowC(t => t.BaseType).TakeWhile(t=> t!= typeof(ModifiableEntity) && t!= typeof(Modifiable)).Reverse())
            {
                foreach (var fi in t.GetFields(flags).OrderBy(f => f.MetadataToken))
                {
                    object value = fi.GetValue(target);
                    list.Add(new Member(fi.Name, value, fi.FieldType));
                }
            }

            return list;
        }
    }
}
