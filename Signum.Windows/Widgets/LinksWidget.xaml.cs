﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Entities;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for LinksWidget.xaml
    /// </summary>
    public partial class LinksWidget : UserControl, IWidget
    {
        public Control Control { get; set; }
        public event Action ForceShow;

        public LinksWidget()
        {
            InitializeComponent();

            this.AddHandler(Button.ClickEvent, new RoutedEventHandler(QuickLink_MouseDown));
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(LinksWidget_DataContextChanged);
        }

        void LinksWidget_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            IdentifiableEntity ident = e.NewValue as IdentifiableEntity;
            
            List<QuickLink> links = ident != null ? Links.GetForEntity(ident, Control) : new List<QuickLink>();

            lvQuickLinks.ItemsSource = links;

            if (links.Count == 0)
                Visibility = Visibility.Collapsed;
            else
            {
                Visibility = Visibility.Visible;
                if (ForceShow != null)
                    ForceShow(); 
            }
        }

        private void QuickLink_MouseDown(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button) //Not to capture the mouseDown of the scrollbar buttons
            {
                Button b = (Button)e.OriginalSource;
                ((QuickLink)b.Tag).Execute();
            }
        }

        public static void Start()
        {
            WidgetPanel.GetWidgets += (obj, mainControl) => new LinksWidget() { Control = mainControl } ;
        }
    }

    public static class Links
    {
        static Dictionary<Type, List<Delegate>> entityLinks = new Dictionary<Type, List<Delegate>>();
        static List<Func<IdentifiableEntity, Control, QuickLink[]>> globalLinks = new List<Func<IdentifiableEntity, Control, QuickLink[]>>();

        public static void RegisterEntityLinks<T>(Func<T, Control, QuickLink[]> getQuickLinks)
            where T : IdentifiableEntity
        {
            entityLinks.GetOrCreate(typeof(T)).Add(getQuickLinks);
        }

        public static void RegisterGlobalLinks(Func<IdentifiableEntity, Control, QuickLink[]> getQuickLinks)
        {
            globalLinks.Add(getQuickLinks);
        }

        public static List<QuickLink> GetForEntity(IdentifiableEntity ident, Control control)
        {
            List<QuickLink> links = new List<QuickLink>();

            links.AddRange(globalLinks.SelectMany(a => a(ident, control).NotNull()));

            List<Delegate> list = entityLinks.TryGetC(ident.GetType());
            if (list != null)
                links.AddRange(list.SelectMany(a => (QuickLink[])a.DynamicInvoke(ident, control)));

            links = links.Where(l => l.IsVisible).ToList();

            return links;
        }

    }

    ///// <summary>
    ///// Controls must implement this interface to have the left navigation panel
    ///// </summary>
    //public interface IHaveQuickLinks
    //{
    //    List<QuickLink> QuickLinks();
    //}

    /// <summary>
    /// Represents an item of the left navigation panel
    /// </summary>
    public abstract class QuickLink
    {
        protected QuickLink() { }

        public string Label { get; set; }

        public bool IsVisible { get; set;}

        public string ToolTip { get; set; }

        public ImageSource Icon { get; set; }

        public abstract void Execute();
    }

    public class QuickLinkAction : QuickLink
    {
        Action action; 
        public QuickLinkAction(string label, Action action)
        {
            this.Label = label;
            this.action = action;
            this.IsVisible = true;
        }

        public override void Execute()
        {
            action();
        }
    }

    public class QuickLinkExplore : QuickLink
    {
        public ExploreOptions Options {get; set;} 

        public QuickLinkExplore(object queryName, string columnName, object value):
            this(new ExploreOptions(queryName)
            { 
                ShowFilters = false,
                SearchOnLoad = true,
                FilterOptions = new List<FilterOption>
                {
                    new FilterOption(columnName, value),
                }
            })
        {  
        }

        public QuickLinkExplore(ExploreOptions options)
        {
            Options = options;
            Label = QueryUtils.GetNiceQueryName(Options.QueryName);
            Icon = Navigator.Manager.GetFindIcon(Options.QueryName, false);
            IsVisible = Navigator.IsFindable(Options.QueryName);
        }

        public override void Execute()
        {
 	       Navigator.Explore(Options); 
        }
    }

    public class QuickLinkNavigate<T> : QuickLink
        where T:IdentifiableEntity
    {
        public NavigateOptions NavigateOptions {get; set;}

        public FindUniqueOptions FindUniqueOptions { get; set; }

        public QuickLinkNavigate(string columnName, object value)
            : this(typeof(T), columnName, value, UniqueType.Single)
        {
        }

        public QuickLinkNavigate(object queryName, string columnName, object value, UniqueType unique): 
            this(new FindUniqueOptions(queryName)
            {
                 UniqueType = unique,
                 FilterOptions = new List<FilterOption>()
                 {
                     new FilterOption(columnName, value)
                 }
            })
        {
        }

        public QuickLinkNavigate(FindUniqueOptions options)
        {
            FindUniqueOptions = options;
            Label = typeof(T).NiceName();
            Icon = Navigator.Manager.GetEntityIcon(typeof(T), false);
            IsVisible = Navigator.IsFindable(FindUniqueOptions.QueryName) && Navigator.IsViewable(typeof(T), false);
        }

        public override void Execute()
        {
            Lite<T> lite = Navigator.FindUnique<T>(FindUniqueOptions);

            if (lite == null)
                return;

            if (NavigateOptions != null)
                Navigator.Navigate(lite, NavigateOptions);
            else
                Navigator.Navigate(lite);
        }
    }
}
