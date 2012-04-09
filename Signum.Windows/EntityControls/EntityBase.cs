﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.ComponentModel;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Windows.Input;
using System.Windows.Automation;

namespace Signum.Windows
{
    public class EntityBase : LineBase
    {
        public static RoutedCommand CreateCommand =
          new RoutedCommand("Create", typeof(EntityLine), new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.N, ModifierKeys.Control, "Create") }));
        public static RoutedCommand ViewCommand =
            new RoutedCommand("View", typeof(EntityLine), new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.G, ModifierKeys.Control, "View") }));
        public static RoutedCommand RemoveCommand =
            new RoutedCommand("Remove", typeof(EntityLine), new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.R, ModifierKeys.Control, "Remove") }));
        public static RoutedCommand FindCommand =
            new RoutedCommand("Find", typeof(EntityLine), new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.F, ModifierKeys.Control, "Find") }));

        public static readonly DependencyProperty EntityProperty =
            DependencyProperty.Register("Entity", typeof(object), typeof(EntityBase), new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((EntityBase)d).OnEntityChanged(e.OldValue, e.NewValue)));
        public object Entity
        {
            get { return (object)GetValue(EntityProperty); }
            set
            {
                SetValue(EntityProperty, null);  //entities have equals overriden
                SetValue(EntityProperty, value);
            }
        }

        protected Implementations safeImplementations;
        public static readonly DependencyProperty ImplementationsProperty =
            DependencyProperty.Register("Implementations", typeof(Implementations), typeof(EntityBase), new UIPropertyMetadata((d, e) => ((EntityBase)d).safeImplementations = (Implementations)e.NewValue));
        public Implementations Implementations
        {
            get { return (Implementations)GetValue(ImplementationsProperty); }
            set { SetValue(ImplementationsProperty, value); }
        }

        public static readonly DependencyProperty EntityTemplateProperty =
           DependencyProperty.Register("EntityTemplate", typeof(DataTemplate), typeof(EntityBase), new UIPropertyMetadata(null));
        public DataTemplate EntityTemplate
        {
            get { return (DataTemplate)GetValue(EntityTemplateProperty); }
            set { SetValue(EntityTemplateProperty, value); }
        }

        public static readonly DependencyProperty CreateProperty =
            DependencyProperty.Register("Create", typeof(bool), typeof(EntityBase), new FrameworkPropertyMetadata(true, (d, e) => ((EntityBase)d).UpdateVisibility()));
        public bool Create
        {
            get { return (bool)GetValue(CreateProperty); }
            set { SetValue(CreateProperty, value); }
        }

        public static readonly DependencyProperty ViewProperty =
            DependencyProperty.Register("View", typeof(bool), typeof(EntityBase), new FrameworkPropertyMetadata(true, (d, e) => ((EntityBase)d).UpdateVisibility()));
        public bool View
        {
            get { return (bool)GetValue(ViewProperty); }
            set { SetValue(ViewProperty, value); }
        }

        public static readonly DependencyProperty ViewButtonsProperty =
            DependencyProperty.Register("ViewButtons", typeof(ViewButtons), typeof(EntityBase), new UIPropertyMetadata(ViewButtons.Ok));
        public ViewButtons ViewButtons
        {
            get { return (ViewButtons)GetValue(ViewButtonsProperty); }
            set { SetValue(ViewButtonsProperty, value); }
        }

        public static readonly DependencyProperty FindProperty =
            DependencyProperty.Register("Find", typeof(bool), typeof(EntityBase), new FrameworkPropertyMetadata(true, (d, e) => ((EntityBase)d).UpdateVisibility()));
        public bool Find
        {
            get { return (bool)GetValue(FindProperty); }
            set { SetValue(FindProperty, value); }
        }

        public static readonly DependencyProperty RemoveProperty =
            DependencyProperty.Register("Remove", typeof(bool), typeof(EntityBase), new FrameworkPropertyMetadata(true, (d, e) => ((EntityBase)d).UpdateVisibility()));
        public bool Remove
        {
            get { return (bool)GetValue(RemoveProperty); }
            set { SetValue(RemoveProperty, value); }
        }

        public static readonly DependencyProperty ViewOnCreateProperty =
            DependencyProperty.Register("ViewOnCreate", typeof(bool), typeof(EntityBase), new UIPropertyMetadata(true));
        public bool ViewOnCreate
        {
            get { return (bool)GetValue(ViewOnCreateProperty); }
            set { SetValue(ViewOnCreateProperty, value); }
        }

        public event Func<object> Creating;
        public event Func<object> Finding;
        public event Func<object, object> Viewing;
        public event Func<object, bool> Removing;

        public event EntityChangedEventHandler EntityChanged;

        static EntityBase()
        {
            LineBase.TypeProperty.OverrideMetadata(typeof(EntityBase), 
                new UIPropertyMetadata((d, e) => ((EntityBase)d).SetType((Type)e.NewValue)));
        }

        public EntityBase()
        {
            this.CommandBindings.Add(new CommandBinding(CreateCommand, btCreate_Click));
            this.CommandBindings.Add(new CommandBinding(FindCommand, btFind_Click));
            this.CommandBindings.Add(new CommandBinding(RemoveCommand, btRemove_Click));
            this.CommandBindings.Add(new CommandBinding(ViewCommand, btView_Click));

          
        }

        public bool dynamicReadOnly;
        public bool DynamicReadOnly
        {
            get { return dynamicReadOnly; }
            set
            {
                if (value != dynamicReadOnly)
                {
                    DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(Common.IsReadOnlyProperty, typeof(EntityBase));

                    if (value)
                        dpd.AddValueChanged(this, IsReadOnlyChanged);
                    else
                        dpd.RemoveValueChanged(this, IsReadOnlyChanged);

                    dynamicReadOnly = true;
                }
            }
        }

        void IsReadOnlyChanged(object sender, EventArgs e)
        {
            UpdateVisibility();
        }

        protected internal override DependencyProperty CommonRouteValue()
        {
            return EntityProperty;
        }

        private void SetType(Type type)
        {
            if (type.IsLite())
            {
                CleanLite = true;
                CleanType = Reflector.ExtractLite(type);
            }
            else
            {
                CleanLite = false;
                CleanType = type;
            }
        }

        protected internal Type CleanType { get; private set; }
        protected internal bool CleanLite { get; private set; }

        protected bool isUserInteraction = false;

        protected void SetEntityUserInteraction(object entity)
        {
            try
            {
                isUserInteraction = true;
                Entity = entity;
            }
            finally
            {
                isUserInteraction = false;
            }
        }
        

        public override void OnLoad(object sender, RoutedEventArgs e)
        {
            base.OnLoad(sender, e);

            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            if (this.NotSet(EntityBase.EntityTemplateProperty))
            {
                var type = Type;

                if (this is EntityCombo && !type.IsLite()) //Allways going to be lite
                    type = Reflector.GenerateLite(type);

                EntityTemplate = Navigator.FindDataTemplate(this, type);
            }

            if (this.NotSet(EntityBase.CreateProperty) && Create && Implementations == null)
                Create = Navigator.IsCreable(CleanType, false);

            if (this.NotSet(EntityBase.ViewProperty) && View && Implementations == null)
                View = Navigator.IsViewable(CleanType, false);

            if (this.NotSet(EntityBase.FindProperty) && Find)
            {
                if (Implementations == null)
                    Find = Navigator.IsFindable(CleanType);
                if (Implementations is ImplementedByAllAttribute)
                    Find = false;
            }

            if (this.NotSet(EntityBase.ViewOnCreateProperty) && ViewOnCreate && !View)
                ViewOnCreate = false;

            if (this.NotSet(EntityBase.ViewButtonsProperty) && CleanLite)
                ViewButtons = ViewButtons.Save;

            UpdateVisibility();
        }


        protected virtual void UpdateVisibility()
        {
        }


        protected virtual bool CanRemove()
        {
            return Entity != null && Remove && !Common.GetIsReadOnly(this);
        }

        protected bool CanView()
        {
            return CanView(Entity);
        }

        protected virtual bool CanView(object entity)
        {
            if (entity == null)
                return false;

            if (View && this.NotSet(ViewProperty) && Implementations != null)
            {
                Type runtimeType = CleanLite ? ((Lite)entity).RuntimeType : entity.GetType();

                return Navigator.IsViewable(runtimeType, false);
            }
            else
                return View;
        }

        protected virtual bool CanFind()
        {
            return Entity == null && Find && !Common.GetIsReadOnly(this);
        }

        protected virtual bool CanCreate()
        {
            return Entity == null && Create && !Common.GetIsReadOnly(this);
        }

        protected virtual void btCreate_Click(object sender, RoutedEventArgs e)
        {
            object entity = OnCreate();

            if (entity != null)
                SetEntityUserInteraction(entity);
        }

        protected virtual void btFind_Click(object sender, RoutedEventArgs e)
        {
            object entity = OnFinding();

            if (entity != null)
                SetEntityUserInteraction(entity);
        }

        protected virtual void btView_Click(object sender, RoutedEventArgs e)
        {
            object entity = OnViewing(Entity, false);

            if (entity != null)
                SetEntityUserInteraction(entity);
        }

        protected virtual void btRemove_Click(object sender, RoutedEventArgs e)
        {
            if (OnRemoving(Entity))
                SetEntityUserInteraction(null);
        }

        protected virtual void OnEntityChanged(object oldValue, object newValue)
        {
            if (EntityChanged != null)
                EntityChanged(this, isUserInteraction, oldValue, newValue);

            AutomationProperties.SetHelpText(this, GetEntityString(newValue));

            UpdateVisibility();
        }

        private string GetEntityString(object newValue)
        {
            if (newValue == null)
                return "";

            if (newValue is EmbeddedEntity)
                return newValue.GetType().Name;

            var ident = newValue as IdentifiableEntity;
            if (ident != null)
            {
                if (ident.IsNew)
                    return "{0};New".Formato(Server.ServerTypes[ident.GetType()].CleanName);

                return ident.ToLite().Key(t => Server.ServerTypes[t].CleanName);
            }

            var lite = newValue as Lite;
            if (lite != null)
            {
                if (lite.UntypedEntityOrNull != null && lite.UntypedEntityOrNull.IsNew)
                    return "{0};New".Formato(Server.ServerTypes[lite.RuntimeType].CleanName);

                return lite.Key(t => Server.ServerTypes[t].CleanName);
            }

            throw new InvalidOperationException("Unexpected entity of type {0}".Formato(newValue.GetType()));
        }

        public Type SelectType()
        {
            if (Implementations == null)
                return CleanType;
            else if (Implementations.IsByAll)
                throw new InvalidOperationException("ImplementedByAll is not supported for this operation, override the event");
            else
                return Navigator.SelectType(Window.GetWindow(this), ((ImplementedByAttribute)Implementations).ImplementedTypes);
        }

        protected object OnCreate()
        {
            if (!CanCreate())
                return null;

            object value;
            if (Creating == null)
            {
                Type type = SelectType();
                if (type == null)
                    return null;

                object entity = Constructor.Construct(type, Window.GetWindow(this));

                value = Server.Convert(entity, Type);
            }
            else
                value = Creating();

            if (value == null)
                return null;

            if (ViewOnCreate)
            {
                value = OnViewing(value, true);
            }

            return value;
        }

        protected object OnFinding()
        {
            if (!CanFind())
                return null;

            object value;
            if (Finding == null)
            {
                Type type = SelectType();
                if (type == null)
                    return null;

                value = Navigator.Find(new FindOptions { QueryName = type });
            }
            else
                value = Finding();

            if (value == null)
                return null;

            return Server.Convert(value, Type);
        }

        public virtual PropertyRoute GetEntityTypeContext()
        {
            return Common.GetTypeContext(this);
        }

        protected object OnViewing(object entity, bool creating)
        {
            if (!CanView(entity))
                return null;

            if (Viewing != null)
                return Viewing(entity);

            bool isReadOnly = Common.GetIsReadOnly(this) && !creating;

            if (ViewButtons == ViewButtons.Ok)
            {
                var options = new ViewOptions
                {
                    TypeContext = CleanType.IsEmbeddedEntity() ? GetEntityTypeContext() : null, 
                };

                if (isReadOnly)
                    options.ReadOnly = isReadOnly;

                return Navigator.ViewUntyped(entity, options);
            }
            else
            {
                var options = new NavigateOptions();

                if (isReadOnly)
                    options.ReadOnly = isReadOnly;

                Navigator.NavigateUntyped(entity, options);

                return null;
            }
        }

        protected bool OnRemoving(object entity)
        {
            if (!CanRemove())
                return false;

            return Removing == null ? true : Removing(entity);
        }
    }

    public delegate void EntityChangedEventHandler(object sender, bool userInteraction, object oldValue, object newValue);
}
