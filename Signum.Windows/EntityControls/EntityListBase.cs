﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections;
using Signum.Utilities.Reflection;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Windows
{
    public class EntityListBase : EntityBase
    {
        public static readonly DependencyProperty EntitiesProperty =
          DependencyProperty.Register("Entities", typeof(IList), typeof(EntityListBase), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((EntityListBase)d).EntitiesChanged(e)));
        public IList Entities
        {
            get { return (IList)GetValue(EntitiesProperty); }
            set { SetValue(EntitiesProperty, value); }
        }

        public static readonly DependencyProperty EntitiesTypeProperty =
          DependencyProperty.Register("EntitiesType", typeof(Type), typeof(EntityListBase), new UIPropertyMetadata(null, (d, e) => ((EntityListBase)d).EntitiesTypeChanged((Type)e.NewValue)));

        public Type EntitiesType
        {
            get { return (Type)GetValue(EntitiesTypeProperty); }
            set { SetValue(EntitiesTypeProperty, value); }
        }

        private void EntitiesTypeChanged(Type type)
        {
 	        Type = ReflectionTools.CollectionType(type).ThrowIfNullC("EntitiesType must be a collection type");
        }

        public new event Func<object> Finding;
        
        protected internal override DependencyProperty CommonRouteValue()
        {
            return EntitiesProperty;
        }

        protected internal override DependencyProperty CommonRouteType()
        {
            return EntitiesTypeProperty;
        }

        protected override bool CanFind()
        {
            return Find && !Common.GetIsReadOnly(this);
        }

        protected override bool CanCreate()
        {
            return Create && !Common.GetIsReadOnly(this);
        }

        protected new object OnFinding()
        {
            if (!CanFind())
                return null;

            object value;
            if (Finding == null)
            {
                Type type = SelectType();
                if (type == null)
                    return null;

                value = Navigator.FindMany(new FindManyOptions { QueryName = type });
            }
            else
                value = Finding();

            if (value == null)
                return null;

            if (value is IEnumerable)
                return ((IEnumerable)value).Cast<object>().Select(o => Server.Convert(o, Type)).ToArray();
            else
                return Server.Convert(value, Type);
        }

        public override PropertyRoute GetEntityTypeContext()
        {
            PropertyRoute tc = base.GetEntityTypeContext();
            if (tc == null)
                return null;

            return tc.Add("Item");
        }

        public IList EnsureEntities()
        {
            if (Entities == null)
                Entities = (IList)Activator.CreateInstance(EntitiesType);
            return Entities;
        }

        public virtual void EntitiesChanged(DependencyPropertyChangedEventArgs e)
        {

        }
    }
}
