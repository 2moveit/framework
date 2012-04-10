﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using Signum.Entities;
using Signum.Utilities;
using System.Windows.Input;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using System.Windows.Media;
using Signum.Services;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Documents;
using Signum.Windows.DynamicQuery;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Windows.Automation.Peers;
using System.Windows.Automation;

namespace Signum.Windows
{
    public partial class SearchControl
    {
        public static readonly DependencyProperty QueryNameProperty =
            DependencyProperty.Register("QueryName", typeof(object), typeof(SearchControl), new UIPropertyMetadata((o,s)=>((SearchControl)o).QueryNameChanged(s)));
        public object QueryName
        {
            get { return (object)GetValue(QueryNameProperty); }
            set { SetValue(QueryNameProperty, value); }
        }

        public static readonly DependencyProperty OrderOptionsProperty =
          DependencyProperty.Register("OrderOptions", typeof(ObservableCollection<OrderOption>), typeof(SearchControl), new UIPropertyMetadata(null));
        public ObservableCollection<OrderOption> OrderOptions
        {
            get { return (ObservableCollection<OrderOption>)GetValue(OrderOptionsProperty); }
            set { SetValue(OrderOptionsProperty, value); }
        }

        public static readonly DependencyProperty FilterOptionsProperty =
           DependencyProperty.Register("FilterOptions", typeof(FreezableCollection<FilterOption>), typeof(SearchControl), new UIPropertyMetadata(null));
        public FreezableCollection<FilterOption> FilterOptions
        {
            get { return (FreezableCollection<FilterOption>)GetValue(FilterOptionsProperty); }
            set { SetValue(FilterOptionsProperty, value); }
        }


        public static readonly DependencyProperty SimpleFilterBuilderProperty =
          DependencyProperty.Register("SimpleFilterBuilder", typeof(ISimpleFilterBuilder), typeof(SearchControl), new UIPropertyMetadata(null, (d, e) => ((SearchControl)d).SimpleFilterBuilderChanged(e)));
        public ISimpleFilterBuilder SimpleFilterBuilder
        {
            get { return (ISimpleFilterBuilder)GetValue(SimpleFilterBuilderProperty); }
            set { SetValue(SimpleFilterBuilderProperty, value); }
        }


        public static readonly DependencyProperty ColumnOptionsModeProperty =
            DependencyProperty.Register("ColumnOptionsMode", typeof(ColumnOptionsMode), typeof(SearchControl), new UIPropertyMetadata(ColumnOptionsMode.Add));
        public ColumnOptionsMode ColumnOptionsMode
        {
            get { return (ColumnOptionsMode)GetValue(ColumnOptionsModeProperty); }
            set { SetValue(ColumnOptionsModeProperty, value); }
        }

        public static readonly DependencyProperty ColumnsOptionsProperty =
            DependencyProperty.Register("ColumnOptions", typeof(ObservableCollection<ColumnOption>), typeof(SearchControl), new UIPropertyMetadata(null));
        public ObservableCollection<ColumnOption> ColumnOptions
        {
            get { return (ObservableCollection<ColumnOption>)GetValue(ColumnsOptionsProperty); }
            set { SetValue(ColumnsOptionsProperty, value); }
        }

        public static readonly DependencyProperty AllowChangeColumnsProperty =
            DependencyProperty.Register("AllowChangeColumns", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(false));
        public bool AllowChangeColumns
        {
            get { return (bool)GetValue(AllowChangeColumnsProperty); }
            set { SetValue(AllowChangeColumnsProperty, value); }
        }

        public static readonly DependencyProperty ElementsPerPageProperty =
            DependencyProperty.Register("ElementsPerPage", typeof(int?), typeof(SearchControl), new UIPropertyMetadata(null, (s, e) => ((SearchControl)s).ElementsPerPage_Changed()));
        public int? ElementsPerPage
        {
            get { return (int?)GetValue(ElementsPerPageProperty); }
            set { SetValue(ElementsPerPageProperty, value); }
        }

        public static readonly DependencyProperty CurrentPageProperty =
           DependencyProperty.Register("CurrentPage", typeof(int), typeof(SearchControl), new UIPropertyMetadata(1));
        public int CurrentPage
        {
            get { return (int)GetValue(CurrentPageProperty); }
            set { SetValue(CurrentPageProperty, value); }
        }

        public static readonly DependencyProperty ItemsCountProperty =
            DependencyProperty.Register("ItemsCount", typeof(int), typeof(SearchControl), new UIPropertyMetadata(0));
        public int ItemsCount
        {
            get { return (int)GetValue(ItemsCountProperty); }
            set { SetValue(ItemsCountProperty, value); }
        }

        public static readonly DependencyProperty ShowFiltersProperty =
            DependencyProperty.Register("ShowFilters", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(false, (s, e) => ((SearchControl)s).ShowFiltersChanged(e)));
        public bool ShowFilters
        {
            get { return (bool)GetValue(ShowFiltersProperty); }
            set { SetValue(ShowFiltersProperty, value); }
        }

        public static readonly DependencyProperty ShowFilterButtonProperty =
            DependencyProperty.Register("ShowFilterButton", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool ShowFilterButton
        {
            get { return (bool)GetValue(ShowFilterButtonProperty); }
            set { SetValue(ShowFilterButtonProperty, value); }
        }

        public static readonly DependencyProperty ShowHeaderProperty =
            DependencyProperty.Register("ShowHeader", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool ShowHeader
        {
            get { return (bool)GetValue(ShowHeaderProperty); }
            set { SetValue(ShowHeaderProperty, value); }
        }

        public static readonly DependencyProperty ShowFooterProperty =
            DependencyProperty.Register("ShowFooter", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(false));
        public bool ShowFooter
        {
            get { return (bool)GetValue(ShowFooterProperty); }
            set { SetValue(ShowFooterProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
          DependencyProperty.Register("SelectedItem", typeof(Lite), typeof(SearchControl), new UIPropertyMetadata(null));
        public Lite SelectedItem
        {
            get { return (Lite)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemsProperty =
          DependencyProperty.Register("SelectedItems", typeof(Lite[]), typeof(SearchControl), new UIPropertyMetadata(null));
        public Lite[] SelectedItems
        {
            get { return (Lite[])GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public static readonly DependencyProperty MultiSelectionProperty =
            DependencyProperty.Register("MultiSelection", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool MultiSelection
        {
            get { return (bool)GetValue(MultiSelectionProperty); }
            set { SetValue(MultiSelectionProperty, value); }
        }

        public static readonly DependencyProperty SearchOnLoadProperty =
          DependencyProperty.Register("SearchOnLoad", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(false));
        public bool SearchOnLoad
        {
            get { return (bool)GetValue(SearchOnLoadProperty); }
            set { SetValue(SearchOnLoadProperty, value); }
        }

        public static readonly DependencyProperty IsAdminProperty =
            DependencyProperty.Register("IsAdmin", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool IsAdmin
        {
            get { return (bool)GetValue(IsAdminProperty); }
            set { SetValue(IsAdminProperty, value); }
        }

        public static readonly DependencyProperty ViewProperty =
           DependencyProperty.Register("View", typeof(bool), typeof(SearchControl), new FrameworkPropertyMetadata(true, (d, e) => ((SearchControl)d).UpdateVisibility()));
        public bool View
        {
            get { return (bool)GetValue(ViewProperty); }
            set { SetValue(ViewProperty, value); }
        }

        public static readonly DependencyProperty CreateProperty =
            DependencyProperty.Register("Create", typeof(bool), typeof(SearchControl), new FrameworkPropertyMetadata(true, (d, e) => ((SearchControl)d).UpdateVisibility()));
        public bool Create
        {
            get { return (bool)GetValue(CreateProperty); }
            set { SetValue(CreateProperty, value); }
        }

        public static readonly DependencyProperty RemoveProperty =
            DependencyProperty.Register("Remove", typeof(bool), typeof(SearchControl), new FrameworkPropertyMetadata(false, (d, e) => ((SearchControl)d).UpdateVisibility()));
        public bool Remove
        {
            get { return (bool)GetValue(RemoveProperty); }
            set { SetValue(RemoveProperty, value); }
        }

        public static readonly DependencyProperty ViewOnCreateProperty =
          DependencyProperty.Register("ViewOnCreate", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(true));
        public bool ViewOnCreate
        {
            get { return (bool)GetValue(ViewOnCreateProperty); }
            set { SetValue(ViewOnCreateProperty, value); }
        }

        public static readonly DependencyProperty FilterColumnProperty =
        DependencyProperty.Register("FilterColumn", typeof(string), typeof(SearchControl), new UIPropertyMetadata(null, (d, e) => ((SearchControl)d).AssetNotLoaded(e)));
        public string FilterColumn
        {
            get { return (string)GetValue(FilterColumnProperty); }
            set { SetValue(FilterColumnProperty, value); }
        }

        public static readonly DependencyProperty FilterRouteProperty =
            DependencyProperty.Register("FilterRoute", typeof(string), typeof(SearchControl), new UIPropertyMetadata(null, (d, e) => ((SearchControl)d).AssetNotLoaded(e)));
        public string FilterRoute
        {
            get { return (string)GetValue(FilterRouteProperty); }
            set { SetValue(FilterRouteProperty, value); }
        }
        
        private void AssetNotLoaded(DependencyPropertyChangedEventArgs e)
        {
            if(IsLoaded)
                throw new InvalidProgramException("You can not change {0} property once loaded".Formato(e.Property));
        }


        private void SimpleFilterBuilderChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                ShowFilters = false;
            }
        }

        private void ShowFiltersChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true && SimpleFilterBuilder != null)
            {
                RefreshSimpleFilters();

                SimpleFilterBuilder = null;
            }
        }


        public Type EntityType
        {
            get { return entityColumn == null ? null : Reflector.ExtractLite(entityColumn.Type); }
        }

        public Implementations Implementations
        {
            get { return entityColumn.Implementations; }
        }

        public static readonly DependencyProperty CollapseOnNoResultsProperty =
            DependencyProperty.Register("CollapseOnNoResults", typeof(bool), typeof(SearchControl), new UIPropertyMetadata(false));
        public bool CollapseOnNoResults
        {
            get { return (bool)GetValue(CollapseOnNoResultsProperty); }
            set { SetValue(CollapseOnNoResultsProperty, value); }
        }

        private void UpdateVisibility()
        {
            btCreate.Visibility = Create && EntityType != null ? Visibility.Visible : Visibility.Collapsed;
            UpdateViewSelection();
        }

        public event Func<IdentifiableEntity> Creating;
        public event Action<IdentifiableEntity> Viewing;
        public event Action<List<Lite>> Removing;
        public event Action DoubleClick;

        public SearchControl()
        {
            //ColumnDragController = new DragController(col => CreateFilter((GridViewColumnHeader)col), DragDropEffects.Copy);

            this.InitializeComponent();

            FilterOptions = new FreezableCollection<FilterOption>();
            OrderOptions = new ObservableCollection<OrderOption>();
            ColumnOptions = new ObservableCollection<ColumnOption>();
            this.Loaded += new RoutedEventHandler(SearchControl_Loaded);
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += new EventHandler(timer_Tick);
        }

        private void QueryNameChanged(DependencyPropertyChangedEventArgs s)
        {
            if (DesignerProperties.GetIsInDesignMode(this) || s.NewValue == null)
            {
                return;
            }

            Settings = Navigator.GetQuerySettings(s.NewValue);


            Description = Navigator.Manager.GetQueryDescription(s.NewValue);

            if (Settings.SimpleFilterBuilder != null)
            {
                SimpleFilterBuilder = Settings.SimpleFilterBuilder(Description);
            }

            tokenBuilder.Token = null;
            tokenBuilder.SubTokensEvent += tokenBuilder_SubTokensEvent;

            entityColumn = Description.Columns.SingleOrDefaultEx(a => a.IsEntity);
            if (entityColumn == null)
                throw new InvalidOperationException("Entity Column not found");
        }

        ColumnDescription entityColumn;

        ResultTable resultTable;
        public ResultTable ResultTable { get { return resultTable; } }
        public QuerySettings Settings { get; private set; }
        public QueryDescription Description { get; private set; }

        public static readonly RoutedEvent QueryResultChangedEvent = EventManager.RegisterRoutedEvent(
            "QueryResultChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SearchControl));
        public event RoutedEventHandler QueryResultChanged
        {
            add { AddHandler(QueryResultChangedEvent, value); }
            remove { RemoveHandler(QueryResultChangedEvent, value); }
        }

        void SearchControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= SearchControl_Loaded;

            if (DesignerProperties.GetIsInDesignMode(this) || QueryName == null)
            {
                tokenBuilder.Token = null;
                tokenBuilder.SubTokensEvent += q => new List<QueryToken>();
                return;
            }

          
            if (FilterColumn.HasText())
            {
                FilterOptions.Add(new FilterOption
                {
                    Path = FilterColumn,
                    Operation = FilterOperation.EqualTo,
                    Frozen = true,
                }.Bind(FilterOption.ValueProperty, new Binding("DataContext" + (FilterRoute.HasText() ? "." + FilterRoute : null)) { Source = this }));
                ColumnOptions.Add(new ColumnOption(FilterColumn));
                ColumnOptionsMode = ColumnOptionsMode.Remove;
                if (ControlExtensions.NotSet(this, SearchOnLoadProperty))
                    SearchOnLoad = true;
            }


            if (this.NotSet(ViewProperty) && View && entityColumn.Implementations == null)
                View = Navigator.IsViewable(EntityType, IsAdmin);

            if (this.NotSet(CreateProperty) && Create && entityColumn.Implementations == null)
                Create = Navigator.IsCreable(EntityType, IsAdmin);

            GenerateListViewColumns();

            Navigator.Manager.SetFilterTokens(QueryName, FilterOptions);

            foreach (var fo in FilterOptions)
            {
                fo.ValueChanged += new EventHandler(fo_ValueChanged);
            }

            filterBuilder.Filters = FilterOptions;
            ((INotifyCollectionChanged)FilterOptions).CollectionChanged += FilterOptions_CollectionChanged;

            Navigator.Manager.SetOrderTokens(QueryName, OrderOptions);

            SortGridViewColumnHeader.SetColumnAdorners(gvResults, OrderOptions);

            if (IsVisible)
            {
                FillMenuItems();

                if (SearchOnLoad)
                    Search();
            }
            else
                IsVisibleChanged += SearchControl_IsVisibleChanged;

            UpdateVisibility();

            AutomationProperties.SetItemStatus(this, QueryUtils.GetQueryUniqueKey(QueryName));
        }


        void FilterOptions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateMultiplyMessage(false);                       
        }

        List<QueryToken> tokenBuilder_SubTokensEvent(QueryToken arg)
        {
            string canColumn = QueryUtils.CanColumn(arg);
            btCreateColumn.IsEnabled = string.IsNullOrEmpty(canColumn);
            btCreateColumn.ToolTip = canColumn;
         

            string canFilter = QueryUtils.CanFilter(arg);
            btCreateFilter.IsEnabled = string.IsNullOrEmpty(canFilter);
            btCreateFilter.ToolTip = canFilter;

            return QueryUtils.SubTokens(arg, Description.Columns);
        }

        private void btCreateFilter_Click(object sender, RoutedEventArgs e)
        {
            filterBuilder.AddFilter(tokenBuilder.Token);
        }

        DispatcherTimer timer;
        void fo_ValueChanged(object sender, EventArgs e)
        {
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (resultTable != null)
            {
                Search();
            }

            timer.Stop();
        }

        void SearchControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (((bool)e.NewValue) == true)
            {
                IsVisibleChanged -= SearchControl_IsVisibleChanged;

                FillMenuItems();

                if (SearchOnLoad)
                    Search();
            }
        }

        private void FillMenuItems()
        {
            if (GetCustomMenuItems != null)
            {
                MenuItem[] menus = GetCustomMenuItems.GetInvocationList().Cast<MenuItemForQueryName>().Select(d => d(QueryName, EntityType)).NotNull().ToArray();
                menu.Items.Clear();
                foreach (MenuItem mi in menus)
                {
                    menu.Items.Add(mi);
                }
            }
        }

        void UpdateViewSelection()
        {
            btView.Visibility = View && lvResult.SelectedItem != null ? Visibility.Visible : Visibility.Collapsed;
            btRemove.Visibility = Remove && lvResult.SelectedItem != null ? Visibility.Visible : Visibility.Collapsed;

            SelectedItem = ((ResultRow)lvResult.SelectedItem).TryCC(r => r.Entity);
            if (MultiSelection)
                SelectedItems = lvResult.SelectedItems.Cast<ResultRow>().Select(r => r.Entity).ToArray();
            else
                SelectedItems = null;
        }

        void GenerateListViewColumns()
        {
            List<Column> columns = MergeColumns();

            gvResults.Columns.Clear();

            foreach (var co in columns)
            {
                AddListViewColumn(co);
            }
        }

        private List<Column> MergeColumns()
        {
            switch (ColumnOptionsMode)
            {
                case ColumnOptionsMode.Add:
                    return Description.Columns.Where(cd => !cd.IsEntity).Select(cd => new Column(cd)).Concat(
                        ColumnOptions.Select(co => co.CreateColumn(Description))).ToList();
                case ColumnOptionsMode.Remove:
                    return Description.Columns.Where(cd => !cd.IsEntity && !ColumnOptions.Any(co => co.Path == cd.Name)).Select(cd => new Column(cd)).ToList();
                case ColumnOptionsMode.Replace:
                    return ColumnOptions.Select(co => co.CreateColumn(Description)).ToList();
                default:
                    throw new InvalidOperationException("{0} is not a valid ColumnOptionMode".Formato(ColumnOptionsMode));
            }
        }

        void AddListViewColumn(Column col)
        {
            GridViewColumn gvc = new GridViewColumn
            {
                Header = new SortGridViewColumnHeader
                {
                    Content = col.DisplayName,
                    ContextMenu = (ContextMenu)FindResource("contextMenu"),
                    RequestColumn = col,
                },
            };
            gvResults.Columns.Add(gvc);
        }

        DataTemplate CreateDataTemplate(ResultColumn c)
        {
            Binding b = new Binding("[{0}]".Formato(c.Index)) { Mode = BindingMode.OneTime };
            DataTemplate dt = Settings.GetFormatter(c.Column)(b);
            return dt;
        }

        void FilterBuilder_SearchClicked(object sender, RoutedEventArgs e)
        {
            Search();
        }

        public void Search()
        {
            ClearResults();

            btFind.IsEnabled = false;

            var request = UpdateMultiplyMessage(true);

            DynamicQueryBachRequest.Enqueue(request,          
                obj =>
                {
                    resultTable = (ResultTable)obj;

                    if (resultTable != null)
                    {
                        SetResults();
                    }
                },
                () => { btFind.IsEnabled = true; });
        }

        public QueryRequest UpdateMultiplyMessage(bool updateSimpleFilters)
        {
            var result = GetQueryRequest(updateSimpleFilters);

            string message = CollectionElementToken.MultipliedMessage(result.Multiplications, EntityType);

            tbMultiplications.Text = message;
            brMultiplications.Visibility = message.HasText() ? Visibility.Visible : Visibility.Collapsed;

            return result;
        }

        public QueryRequest GetQueryRequest(bool updateSimpleFilters)
        {
            if (updateSimpleFilters)
                RefreshSimpleFilters();

            var request = new QueryRequest
            {
                QueryName = QueryName,
                Filters = FilterOptions.Select(f => f.ToFilter()).ToList(),
                Orders = OrderOptions.Select(o => o.ToOrder()).ToList(),
                Columns = gvResults.Columns.Select(gvc => ((SortGridViewColumnHeader)gvc.Header).RequestColumn).ToList(),
                ElementsPerPage = ElementsPerPage,
                CurrentPage = CurrentPage,
            };

            return request;
        }

        private void RefreshSimpleFilters()
        {
            if (SimpleFilterBuilder != null)
            {
                FilterOptions.Clear();
                var newFilters = SimpleFilterBuilder.GenerateFilterOptions();

                Navigator.Manager.SetFilterTokens(QueryName, newFilters);
                FilterOptions.AddRange(newFilters);
            }
        }

        private void SetResults()
        {
            gvResults.Columns.ZipForeach(resultTable.Columns, (gvc, rc) =>
            {
                var header = (SortGridViewColumnHeader)gvc.Header;

                Debug.Assert(rc.Column.Token.Equals(header.RequestColumn.Token));

                if (header.ResultColumn == null || header.ResultColumn.Index != rc.Index)
                    gvc.CellTemplate = CreateDataTemplate(rc);

                header.ResultColumn = rc; 
            });             

            lvResult.ItemsSource = resultTable.Rows;

            if (resultTable.Rows.Length > 0)
            {
                lvResult.SelectedIndex = 0;
                lvResult.ScrollIntoView(resultTable.Rows.FirstEx());
            }            
            ItemsCount = lvResult.Items.Count;
            lvResult.Background = Brushes.White;
            lvResult.Focus();
            elementsInPageLabel.Visibility = Visibility.Visible;
            elementsInPageLabel.TotalPages = resultTable.TotalPages;
            elementsInPageLabel.StartElementIndex = resultTable.StartElementIndex;
            elementsInPageLabel.EndElementIndex = resultTable.EndElementIndex;
            elementsInPageLabel.TotalElements = resultTable.TotalElements;

            pageSizeSelector.PageSize = resultTable.ElementsPerPage;

            pageSelector.Visibility = System.Windows.Visibility.Visible;
            pageSelector.CurrentPage = resultTable.CurrentPage;
            pageSelector.TotalPages = resultTable.TotalPages;
            
            //tbResultados.Visibility = Visibility.Visible;
            //tbResultados.Foreground = resultTable.Rows.Length == ElementsPerPage ? Brushes.Red : Brushes.Black;
            OnQueryResultChanged(false);
        }

        public void ClearResults()
        {
            OnQueryResultChanged(true);
            resultTable = null;
            elementsInPageLabel.Visibility = Visibility.Hidden;
            pageSelector.Visibility = Visibility.Hidden;
            lvResult.ItemsSource = null;
            lvResult.Background = Brushes.WhiteSmoke;
        }

        void pageSelector_Changed(object sender, RoutedEventArgs e)
        {
            if (FixSize != null)
                FixSize(this, new EventArgs()); 
            Search(); 
        }

        public event EventHandler FixSize; 
        public event EventHandler ClearSize; 

        void ElementsPerPage_Changed()
        {
            if (IsLoaded)
            {
                CurrentPage = 1;
                if (ClearSize != null)
                    ClearSize(this, new EventArgs()); 
                Search();
            }
        }

        void OnQueryResultChanged(bool cleaning)
        {
            if (!cleaning && CollapseOnNoResults)
                Visibility = resultTable.Rows.Length == 0 ? Visibility.Collapsed : Visibility.Visible;

            RaiseEvent(new RoutedEventArgs(QueryResultChangedEvent));
        }

        void btView_Click(object sender, RoutedEventArgs e)
        {
            OnViewClicked();
        }

        void OnViewClicked()
        {
            ResultRow row = (ResultRow)lvResult.SelectedItem;

            if (row == null)
                return;

            IdentifiableEntity entity = (IdentifiableEntity)Server.Convert(row.Entity, EntityType);

            OnViewing(entity);
        }

        void btCreate_Click(object sender, RoutedEventArgs e)
        {
            OnCreate();
        }

        public Type SelectType()
        {
            if (Implementations == null)
                return EntityType;
            else if (Implementations.IsByAll)
                throw new InvalidOperationException("ImplementedByAll is not supported for this operation, override the event");
            else
                return Navigator.SelectType(Window.GetWindow(this), ((ImplementedByAttribute)Implementations).ImplementedTypes);
        }


        protected void OnCreate()
        {
            if (!Create)
                return;

            IdentifiableEntity result = Creating == null ? (IdentifiableEntity)Constructor.Construct(SelectType(), Window.GetWindow(this)) : Creating();

            if (result == null)
                return;

            if (ViewOnCreate)
            {
                OnViewing(result);
            }
        }

        protected void OnViewing(IdentifiableEntity entity)
        {
            if (!View)
                return;

            if (this.Viewing == null)
                Navigator.NavigateUntyped(entity, new NavigateOptions { Admin = IsAdmin });
            else
                this.Viewing(entity);
        }

        void btRemove_Click(object sender, RoutedEventArgs e)
        {
            if (lvResult.SelectedItems.Count == 0)
                return;

            var lites = lvResult.SelectedItems.Cast<ResultRow>().Select(r => r.Entity).ToList();

            if (this.Removing == null)
                throw new InvalidOperationException("Remove event not set");

            this.Removing(lites);
        }

        void lvResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateViewSelection();
        }

        void lvResult_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DoubleClick != null)
                DoubleClick();
            else
                OnViewClicked();
            e.Handled = true;
        }

        void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            SortGridViewColumnHeader header = sender as SortGridViewColumnHeader;

            if (header == null)
                return;

            string canOrder = QueryUtils.CanOrder(header.RequestColumn.Token);
            if (canOrder.HasText())
            {
                MessageBox.Show(canOrder);
                return; 
            }

            header.ChangeOrders(OrderOptions);

            Search();
        }

        public static event MenuItemForQueryName GetCustomMenuItems;

        FilterOption CreateFilter(SortGridViewColumnHeader header)
        {
            if (resultTable != null)
            {
                ResultRow row = (ResultRow)lvResult.SelectedItem;
                if (row != null)
                {
                    object value = row[header.ResultColumn];

                    return new FilterOption
                    {
                        Token = header.RequestColumn.Token,
                        Operation = FilterOperation.EqualTo,
                        Value = value is EmbeddedEntity ? null : value
                    };
                }
            }

            return new FilterOption
            {
                Token = header.RequestColumn.Token,
                Operation = FilterOperation.EqualTo,
                Value = FilterOption.DefaultValue(header.RequestColumn.Type),
            };
        }

        private void btCreateColumn_Click(object sender, RoutedEventArgs e)
        {
            QueryToken token = tokenBuilder.Token;

            AddColumn(token);

            UpdateMultiplyMessage(true); 
        }

        private void AddColumn(QueryToken token)
        {
            if (!AllowChangeColumns)
                return;

            string result = token.NiceName();
            if (ValueLineBox.Show<string>(ref result, Properties.Resources.NewColumnSName, Properties.Resources.ChooseTheDisplayNameOfTheNewColumn, Properties.Resources.Name, null, null, Window.GetWindow(this)))
            {
                ClearResults();

                AddListViewColumn(new Column(token, result));
            }
        }

        private void lvResult_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(FilterOption)) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void lvResult_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FilterOption)))
            {
                FilterOption filter = (FilterOption)e.Data.GetData(typeof(FilterOption));

                QueryToken token = filter.Token;

                AddColumn(filter.Token);
            }
        }

        private void renameMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!AllowChangeColumns)
                return;

            SortGridViewColumnHeader gvch = GetMenuItemHeader(sender);

            string result = gvch.RequestColumn.DisplayName;
            if (ValueLineBox.Show<string>(ref result, Properties.Resources.NewColumnSName, Properties.Resources.ChooseTheDisplayNameOfTheNewColumn, Properties.Resources.Name, null, null, Window.GetWindow(this)))
            {
                gvch.RequestColumn.DisplayName = result;
                gvch.Content = result;
            }
        }

        private void removeMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!AllowChangeColumns)
                return;

            SortGridViewColumnHeader gvch = GetMenuItemHeader(sender);

            gvResults.Columns.Remove(gvch.Column);

            UpdateMultiplyMessage(true); 
        }

        private void filter_Click(object sender, RoutedEventArgs e)
        {
            SortGridViewColumnHeader gvch = GetMenuItemHeader(sender);

            FilterOptions.Add(CreateFilter(gvch)); 
        }

        private static SortGridViewColumnHeader GetMenuItemHeader(object sender)
        {
            return (SortGridViewColumnHeader)((ContextMenu)(((MenuItem)sender).Parent)).PlacementTarget;
        }

        public void Reinitialize(List<FilterOption> filters, List<ColumnOption> columns, ColumnOptionsMode columnOptionsMode, List<OrderOption> orders)
        {
            ColumnOptions.Clear();
            ColumnOptions.AddRange(columns);
            ColumnOptionsMode = columnOptionsMode; 
            GenerateListViewColumns();

            if (SimpleFilterBuilder != null)
                SimpleFilterBuilder = null;

            FilterOptions.Clear();
            FilterOptions.AddRange(filters);
            Navigator.Manager.SetFilterTokens(QueryName, FilterOptions);

            OrderOptions.Clear();
            OrderOptions.AddRange(orders);
            Navigator.Manager.SetOrderTokens(QueryName, OrderOptions);
            SortGridViewColumnHeader.SetColumnAdorners(gvResults, OrderOptions);


            UpdateMultiplyMessage(true); 
        }

        private void btFilters_Unchecked(object sender, RoutedEventArgs e)
        {
            rowFilters.Height = new GridLength(); //Auto
        }
    }

    public delegate SearchControlMenuItem MenuItemForQueryName(object queryName, Type entityType);

    public class SearchControlMenuItem : MenuItem
    {
        public SearchControl SearchControl { get; set; }

        public SearchControlMenuItem() { }
        public SearchControlMenuItem(RoutedEventHandler onClick)
        {
            this.Click += onClick;
        }

        protected override void OnInitialized(EventArgs e)
        {
            this.Loaded += new RoutedEventHandler(SearchControlMenuItem_Loaded);
            base.OnInitialized(e);
        }

        void SearchControlMenuItem_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= SearchControlMenuItem_Loaded;
            if (this.Parent != null)
            {
                SearchControl result = this.LogicalParents().OfType<SearchControl>().FirstEx();
             
                if (result is SearchControl)
                {
                    SearchControl = (SearchControl)result;

                    SearchControl.QueryResultChanged += new RoutedEventHandler(searchControl_QueryResultChanged);

                    Initialize();
                }
            }
        }

        void searchControl_QueryResultChanged(object sender, RoutedEventArgs e)
        {
            QueryResultChanged();
        }

        public virtual void Initialize()
        {
            foreach (var item in Items.OfType<SearchControlMenuItem>())
            {
                item.SearchControl = this.SearchControl;
                item.Initialize();
            }
        }

        public virtual void QueryResultChanged()
        {
            foreach (var item in Items.OfType<SearchControlMenuItem>())
            {
                item.QueryResultChanged();
            }
        }


    }

    

    public interface ISimpleFilterBuilder
    {
        List<FilterOption> GenerateFilterOptions();
    }
}
