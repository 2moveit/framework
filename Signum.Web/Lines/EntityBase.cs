﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using Signum.Entities;
using Signum.Web.Properties;
using Signum.Entities.Reflection;

namespace Signum.Web
{
    public static class EntityBaseKeys
    { 
        public const string RuntimeInfo = "sfRuntimeInfo";
        public const string StaticInfo = "sfStaticInfo";
        public const string Entity = "sfEntity";
        public const string Template = "sfTemplate";
        public const string ToStr = "sfToStr";
        public const string ToStrLink = "sfLink";
        public const string IsNew = "sfIsNew";
        public const string Detail = "sfDetail";
    }

    public abstract class EntityBase : BaseLine
    {
        public EntityBase(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
            View = true;
            Create = true;
            Find = true;
            Remove = true;
        }

        public bool HasManyImplementations
        {
            get 
            {
                return Implementations != null && !Implementations.IsByAll && ((ImplementedByAttribute)Implementations).ImplementedTypes.Length > 1;
            }
        }

        public Implementations Implementations { get; set; }

        public ViewMode ViewMode { get; set; } 
        
        public virtual bool View { get; set; }
        public bool Create { get; set; }
        public bool Find { get; set; }
        public bool Remove { get; set; }
        public bool ReadOnlyEntity { get; set; }

        bool preserveViewData = false; 
        /// <summary>
        /// When rendering the line content, it will preserve the ViewData values except the Model
        /// </summary>
        public bool PreserveViewData
        {
            get { return preserveViewData; }
            set { preserveViewData = value; }
        }

        public string OnEntityChanged { get; set; }

        public string PartialViewName { get; set; }

        public abstract string ToJS();

        public string OptionsJS()
        {
            return OptionsJSInternal().ToJS();
        }

        protected virtual JsOptionsBuilder OptionsJSInternal()
        {
            return new JsOptionsBuilder(false)
            {
                {"prefix", ControlID.SingleQuote()},
                {"onEntityChanged", "function(){ " + OnEntityChanged + " }"}, 
            };
        }

        public virtual JsViewOptions DefaultJsViewOptions()
        {
            return new JsViewOptions { PartialViewName = this.PartialViewName };
        }

        public JsFindOptions DefaultJsfindOptions()
        {
            return new JsFindOptions();
        }

        public string Viewing { get; set; }
        protected abstract string DefaultView();
        internal string GetViewing()
        {
            if (!View)
                return "";
            return Viewing ?? DefaultView();
        }

        public string Creating { get; set; }
        protected abstract string DefaultCreate();
        internal string GetCreating()
        {
            if (!Create)
                return "";
            return Creating ?? DefaultCreate();
        }

        public string Finding { get; set; }
        protected abstract string DefaultFind();
        internal string GetFinding()
        { 
            if (!Find)
                return "";
            return Finding ?? DefaultFind();
        }

        public string Removing { get; set; }
        protected abstract string DefaultRemove();
        internal string GetRemoving()
        {
            if (!Remove)
                return "";
            return Removing ?? DefaultRemove();
        }

        internal Type CleanRuntimeType 
        { 
            get 
            {
                if (UntypedValue == null)
                    return null;

                return UntypedValue.GetType().IsLite() ? (UntypedValue as Lite).RuntimeType : UntypedValue.GetType();
            }
        }

        internal bool? IsNew
        {
            get 
            {
                return (UntypedValue as IIdentifiable).TryCS(i => i.IsNew) ??
                       (UntypedValue as Lite).TryCS(l => l.IdOrNull==null);
            }
        }

        internal int? IdOrNull
        {
            get
            {
                return (UntypedValue as IIdentifiable).TryCS(i => i.IdOrNull) ??
                       (UntypedValue as Lite).TryCS(l => l.IdOrNull);
            }
        }

        internal string ToStr
        {
            get 
            {
                return (UntypedValue as IIdentifiable).TryCC(i => i.ToStr) ??
                       (UntypedValue as Lite).TryCC(l => l.ToStr);
            }
        }
    }

    public enum ViewMode
    {
        Popup,
        Navigate
    }
}
