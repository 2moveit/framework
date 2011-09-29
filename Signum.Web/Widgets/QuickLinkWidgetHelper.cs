﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Web.Properties;
using Signum.Engine;

namespace Signum.Web
{
    public delegate QuickLink[] GetQuickLinkItemDelegate<T>(T entity, string partialViewName, string prefix);  

    public static class QuickLinkWidgetHelper
    {
        static Dictionary<Type, List<Delegate>> entityLinks = new Dictionary<Type, List<Delegate>>();
        static List<GetQuickLinkItemDelegate<IdentifiableEntity>> globalLinks = new List<GetQuickLinkItemDelegate<IdentifiableEntity>>();

        public static void RegisterEntityLinks<T>(GetQuickLinkItemDelegate<T> getQuickLinks)
            where T : IdentifiableEntity
        {
            entityLinks.GetOrCreate(typeof(T)).Add(getQuickLinks);
        }

        public static void RegisterGlobalLinks(GetQuickLinkItemDelegate<IdentifiableEntity> getQuickLinks)
        {
            globalLinks.Add(getQuickLinks);
        }

        public static List<QuickLink> GetForEntity(IdentifiableEntity ident, string partialViewName, string prefix)
        {
            List<QuickLink> links = new List<QuickLink>();

            links.AddRange(globalLinks.SelectMany(a => (a(ident, partialViewName, prefix) ?? Empty)).NotNull());

            List<Delegate> list = entityLinks.TryGetC(ident.GetType());
            if (list != null)
                links.AddRange(list.SelectMany(a => (QuickLink[])a.DynamicInvoke(ident, partialViewName, prefix) ?? Empty).NotNull());

            links = links.Where(l => l.IsVisible).ToList();

            return links;
        }

        static QuickLink[] Empty = new QuickLink[0];

        public static void Start()
        {
            WidgetsHelper.GetWidgetsForView += (entity, partialViewName, prefix) => entity is IdentifiableEntity ? CreateWidget((IdentifiableEntity)entity, partialViewName, prefix) : null;

            ContextualItemsHelper.GetContextualItemsForLite += new GetContextualItemDelegate(ContextualItemsHelper_GetContextualItemsForLite);
        }

        public static WidgetItem CreateWidget(IdentifiableEntity identifiable, string partialViewName, string prefix)
        {
            List<QuickLink> quicklinks = GetForEntity(identifiable, partialViewName, prefix);
            if (quicklinks == null || quicklinks.Count == 0) 
                return null;

            HtmlStringBuilder content = new HtmlStringBuilder();
            using (content.Surround(new HtmlTag("ul").Class("sf-menu-button sf-widget-content sf-quicklinks")))
            {
                foreach (var q in quicklinks)
                {
                    using (content.Surround(new HtmlTag("li").Class("sf-quicklink")))
                    {
                        content.Add(q.Execute());
                    }
                }
            }

            HtmlStringBuilder label = new HtmlStringBuilder();
            using (label.Surround(new HtmlTag("a").Class("sf-widget-toggler sf-quicklink-toggler").Attr("title", Resources.Quicklinks)))
            {
                label.Add(new HtmlTag("span")
                    .Class("ui-icon ui-icon-star")
                    .InnerHtml(Resources.Quicklinks.EncodeHtml())
                    .ToHtml());

                label.Add(new HtmlTag("span")
                    .Class("sf-widget-count")
                    .SetInnerText(quicklinks.Count.ToString())
                    .ToHtml());
            }
            
            return new WidgetItem
            {
                Id = TypeContextUtilities.Compose(prefix, "quicklinksWidget"),
                Label = label.ToHtml(),
                Content = content.ToHtml()
            };
        }

        static ContextualItem ContextualItemsHelper_GetContextualItemsForLite(ControllerContext controllerContext, Lite lite, object queryName, string prefix)
        {
            IdentifiableEntity ie = Database.Retrieve(lite);
            List<QuickLink> quicklinks = GetForEntity(ie, Navigator.EntitySettings(ie.GetType()).OnPartialViewName(ie), prefix);
            if (quicklinks == null || quicklinks.Count == 0)
                return null;

            HtmlStringBuilder content = new HtmlStringBuilder();
            using (content.Surround(new HtmlTag("ul").Class("sf-search-ctxmenu-quicklinks")))
            {
                string ctxItemClass = "sf-search-ctxitem";

                content.AddLine(new HtmlTag("li")
                    .Class(ctxItemClass + " sf-search-ctxitem-header")
                    .InnerHtml(
                        new HtmlTag("span").InnerHtml(Resources.Quicklinks.EncodeHtml()))
                    );

                foreach (var q in quicklinks)
                {
                    using (content.Surround(new HtmlTag("li").Class(ctxItemClass)))
                    {
                        content.Add(q.Execute());
                    }
                }
            }

            return new ContextualItem
            {
                Id = TypeContextUtilities.Compose(prefix, "ctxItemQuickLinks"),
                Content = content.ToHtml().ToString()
            };
        }
    }

    public abstract class QuickLink : ToolBarButton
    {
        public QuickLink()
        {
            DivCssClass = "sf-quicklink";
        }

        public string Prefix { get; set; }

        public bool IsVisible { get; set; }

        public abstract MvcHtmlString Execute();
    }

    public class QuickLinkAction : QuickLink
    {
        public string Url { get; set; }
        
        public QuickLinkAction(string text, string url)
        {
            Text = text;
            Url = url;
            IsVisible = true;
        }

        public override MvcHtmlString Execute()
        {
            return new HtmlTag("a").Attr("href", Url).SetInnerText(Text);
        }
    }

    public class QuickLinkFind : QuickLink
    {
        public FindOptions FindOptions { get; set; }
        
        public QuickLinkFind(FindOptions findOptions)
        {
            FindOptions = findOptions;
            IsVisible = Navigator.IsFindable(findOptions.QueryName);
            Text = QueryUtils.GetNiceName(findOptions.QueryName);
        }

        public QuickLinkFind(object queryName, string columnName, object value, bool hideColumn) :
            this(new FindOptions
            {
                QueryName = queryName,
                SearchOnLoad = true,
                ColumnOptionsMode = hideColumn ? ColumnOptionsMode.Remove: ColumnOptionsMode.Add,
                ColumnOptions = hideColumn ? new List<ColumnOption>{new ColumnOption(columnName)}: new List<ColumnOption>(),
                FilterMode = FilterMode.Hidden,
                FilterOptions = new List<FilterOption>
                {
                    new FilterOption(columnName, value),
                },
                Create = false
            })
        {
        }

        public override MvcHtmlString Execute()
        {
            string onclick = new JsFindNavigator(new JsFindOptions
            {
                FindOptions = FindOptions,
                Prefix = Js.NewPrefix(Prefix ?? "")
            }).openFinder().ToJS();

            return new HtmlTag("a").Attr("onclick", onclick).SetInnerText(Text);
        }
    }

    public class QuickLinkView : QuickLink
    {
        public Lite lite;

        public QuickLinkView(Lite liteEntity)
        {
            lite = liteEntity;
            IsVisible = Navigator.IsViewable(lite.RuntimeType, EntitySettingsContext.Default);
            Text = lite.RuntimeType.NiceName();
        }

        public override MvcHtmlString Execute()
        {
            return new HtmlTag("a").Attr("href", Navigator.ViewRoute(lite)).SetInnerText(Text);
        }
    }
}