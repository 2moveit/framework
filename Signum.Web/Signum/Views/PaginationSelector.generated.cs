﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34011
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Signum.Web.Views
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    
    #line 3 "..\..\Signum\Views\PaginationSelector.cshtml"
    using Signum.Engine;
    
    #line default
    #line hidden
    using Signum.Entities;
    
    #line 1 "..\..\Signum\Views\PaginationSelector.cshtml"
    using Signum.Entities.DynamicQuery;
    
    #line default
    #line hidden
    
    #line 2 "..\..\Signum\Views\PaginationSelector.cshtml"
    using Signum.Entities.Reflection;
    
    #line default
    #line hidden
    using Signum.Utilities;
    
    #line 4 "..\..\Signum\Views\PaginationSelector.cshtml"
    using Signum.Utilities.DataStructures;
    
    #line default
    #line hidden
    using Signum.Web;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Signum/Views/PaginationSelector.cshtml")]
    public partial class PaginationSelector : System.Web.Mvc.WebViewPage<Context>
    {
        public PaginationSelector()
        {
        }
        public override void Execute()
        {
            
            #line 6 "..\..\Signum\Views\PaginationSelector.cshtml"
   
    Pagination pagination = (Pagination)ViewData[ViewDataKeys.Pagination];
    var paginate = pagination as Pagination.Paginate;

    FilterMode filterMode = (FilterMode)ViewData[ViewDataKeys.FilterMode];

    ResultTable resultTable = (ResultTable)ViewData[ViewDataKeys.Results];

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n<div");

WriteLiteral(" class=\"sf-search-footer\"");

WriteAttribute("style", Tuple.Create(" style=\"", 476), Tuple.Create("\"", 547)
            
            #line 15 "..\..\Signum\Views\PaginationSelector.cshtml"
, Tuple.Create(Tuple.Create("", 484), Tuple.Create<System.Object, System.Int32>(filterMode == FilterMode.OnlyResults ? "display:none" : null
            
            #line default
            #line hidden
, 484), false)
);

WriteLiteral(">\r\n    <div");

WriteLiteral(" class=\"sf-pagination-left\"");

WriteLiteral(">\r\n");

            
            #line 17 "..\..\Signum\Views\PaginationSelector.cshtml"
        
            
            #line default
            #line hidden
            
            #line 17 "..\..\Signum\Views\PaginationSelector.cshtml"
         if (resultTable != null)
        {
            if (pagination is Pagination.All)
            {

            
            #line default
            #line hidden
WriteLiteral("            <span>");

            
            #line 21 "..\..\Signum\Views\PaginationSelector.cshtml"
             Write(SearchMessage._0Results.NiceToString().ForGenderAndNumber(number: resultTable.TotalElements).FormatHtml(
                       new HtmlTag("span").Class("sf-pagination-strong").SetInnerText(resultTable.TotalElements.ToString())));

            
            #line default
            #line hidden
WriteLiteral("\r\n            </span>\r\n");

            
            #line 24 "..\..\Signum\Views\PaginationSelector.cshtml"
                    
            }
            else if (pagination is Pagination.Firsts)
            {
                var first = (Pagination.Firsts)pagination;
                    

            
            #line default
            #line hidden
WriteLiteral("            <span>");

            
            #line 30 "..\..\Signum\Views\PaginationSelector.cshtml"
             Write(SearchMessage.First0Results.NiceToString().ForGenderAndNumber(number: resultTable.Rows.Length).FormatHtml(
                    new HtmlTag("span").Class("sf-pagination-strong").Class(resultTable.Rows.Length == first.TopElements ? "sf-pagination-overflow" : null).SetInnerText(resultTable.Rows.Length.ToString())));

            
            #line default
            #line hidden
WriteLiteral("\r\n            </span>\r\n");

            
            #line 33 "..\..\Signum\Views\PaginationSelector.cshtml"
            }
            else if (pagination is Pagination.Paginate)
            {

            
            #line default
            #line hidden
WriteLiteral("            <span>\r\n");

WriteLiteral("                ");

            
            #line 37 "..\..\Signum\Views\PaginationSelector.cshtml"
           Write(SearchMessage._01of2Results.NiceToString().ForGenderAndNumber(number: resultTable.TotalElements).FormatHtml(
                        new HtmlTag("span").Class("sf-pagination-strong").SetInnerText(resultTable.StartElementIndex.ToString()),
                        new HtmlTag("span").Class("sf-pagination-strong").SetInnerText(resultTable.EndElementIndex.ToString()),
                        new HtmlTag("span").Class("sf-pagination-strong").SetInnerText(resultTable.TotalElements.ToString())
                        ));

            
            #line default
            #line hidden
WriteLiteral("\r\n            </span>\r\n");

            
            #line 43 "..\..\Signum\Views\PaginationSelector.cshtml"
            }
        }

            
            #line default
            #line hidden
WriteLiteral("    </div>\r\n    <div");

WriteLiteral(" class=\"sf-pagination-center\"");

WriteLiteral(">\r\n");

            
            #line 47 "..\..\Signum\Views\PaginationSelector.cshtml"
        
            
            #line default
            #line hidden
            
            #line 47 "..\..\Signum\Views\PaginationSelector.cshtml"
          
            var currentMode = pagination.GetMode();
            var modes = EnumExtensions.GetValues<PaginationMode>().Select(pm => new SelectListItem
            {
                Text = pm.NiceToString(),
                Value = pm.ToString(),
                Selected = currentMode == pm
            }).ToList();   
        
            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("        ");

            
            #line 56 "..\..\Signum\Views\PaginationSelector.cshtml"
   Write(Html.DropDownList(Model.Compose("sfPaginationMode"), modes, new { @class = "sf-pagination-size form-control" }));

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n");

            
            #line 58 "..\..\Signum\Views\PaginationSelector.cshtml"
        
            
            #line default
            #line hidden
            
            #line 58 "..\..\Signum\Views\PaginationSelector.cshtml"
         if (!(pagination is Pagination.All))
        {
            var currentElements = pagination.GetElementsPerPage();
            var elements = new List<int> { 5, 10, 20, 50, 100, 200 }.Select(i => new SelectListItem { Text = i.ToString(), Value = i.ToString(), Selected = i == currentElements }).ToList();
            
            
            
            #line default
            #line hidden
            
            #line 63 "..\..\Signum\Views\PaginationSelector.cshtml"
       Write(Html.DropDownList(Model.Compose("sfElems"), elements, new { @class = "sf-pagination-size form-control" }));

            
            #line default
            #line hidden
            
            #line 63 "..\..\Signum\Views\PaginationSelector.cshtml"
                                                                                                                      
        }

            
            #line default
            #line hidden
WriteLiteral("    </div>\r\n\r\n    <div");

WriteLiteral(" class=\"sf-pagination-right\"");

WriteLiteral(">\r\n\r\n");

            
            #line 69 "..\..\Signum\Views\PaginationSelector.cshtml"
        
            
            #line default
            #line hidden
            
            #line 69 "..\..\Signum\Views\PaginationSelector.cshtml"
         if (resultTable != null && paginate != null)
        {
            MinMax<int> interval = new MinMax<int>(
             Math.Max(1, paginate.CurrentPage - 2),
             Math.Min(paginate.CurrentPage + 2, resultTable.TotalPages.Value));
                

            
            #line default
            #line hidden
WriteLiteral("            <input");

WriteLiteral(" type=\"hidden\"");

WriteAttribute("id", Tuple.Create(" id=\"", 3552), Tuple.Create("\"", 3583)
            
            #line 75 "..\..\Signum\Views\PaginationSelector.cshtml"
, Tuple.Create(Tuple.Create("", 3557), Tuple.Create<System.Object, System.Int32>(Model.Compose("sfPage")
            
            #line default
            #line hidden
, 3557), false)
);

WriteAttribute("value", Tuple.Create(" value=\"", 3584), Tuple.Create("\"", 3613)
            
            #line 75 "..\..\Signum\Views\PaginationSelector.cshtml"
, Tuple.Create(Tuple.Create("", 3592), Tuple.Create<System.Object, System.Int32>(paginate.CurrentPage
            
            #line default
            #line hidden
, 3592), false)
);

WriteLiteral(" />\r\n");

WriteLiteral("            <ul");

WriteLiteral(" class=\"pagination\"");

WriteLiteral(">\r\n                <li");

WriteAttribute("class", Tuple.Create(" class=\"", 3675), Tuple.Create("\"", 3733)
            
            #line 77 "..\..\Signum\Views\PaginationSelector.cshtml"
, Tuple.Create(Tuple.Create("", 3683), Tuple.Create<System.Object, System.Int32>((paginate.CurrentPage <= 1) ? "disabled" : null
            
            #line default
            #line hidden
, 3683), false)
);

WriteLiteral(" ><a");

WriteLiteral(" data-page=\"");

            
            #line 77 "..\..\Signum\Views\PaginationSelector.cshtml"
                                                                                          Write(paginate.CurrentPage - 1);

            
            #line default
            #line hidden
WriteLiteral("\"");

WriteLiteral(" href=\"#\"");

WriteLiteral(">&laquo;</a></li>\r\n\r\n\r\n\r\n");

            
            #line 81 "..\..\Signum\Views\PaginationSelector.cshtml"
                
            
            #line default
            #line hidden
            
            #line 81 "..\..\Signum\Views\PaginationSelector.cshtml"
                 if (interval.Min != 1)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <li><a");

WriteLiteral(" data-page=\"1\"");

WriteLiteral(" href=\"#\"");

WriteLiteral(">1</a></li>\r\n");

            
            #line 84 "..\..\Signum\Views\PaginationSelector.cshtml"
                    if (interval.Min - 1 != 1)
                    {

            
            #line default
            #line hidden
WriteLiteral("                    <li");

WriteLiteral(" class=\"disabled\"");

WriteLiteral("><span>...</span></li>\r\n");

            
            #line 87 "..\..\Signum\Views\PaginationSelector.cshtml"
                    }
                }

            
            #line default
            #line hidden
WriteLiteral("\r\n");

            
            #line 90 "..\..\Signum\Views\PaginationSelector.cshtml"
                
            
            #line default
            #line hidden
            
            #line 90 "..\..\Signum\Views\PaginationSelector.cshtml"
                 for (int i = interval.Min; i < paginate.CurrentPage; i++)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <li><a");

WriteLiteral(" data-page=\"");

            
            #line 92 "..\..\Signum\Views\PaginationSelector.cshtml"
                                 Write(i);

            
            #line default
            #line hidden
WriteLiteral("\"");

WriteLiteral(" href=\"#\"");

WriteLiteral(">");

            
            #line 92 "..\..\Signum\Views\PaginationSelector.cshtml"
                                              Write(i);

            
            #line default
            #line hidden
WriteLiteral("</a></li>  \r\n");

            
            #line 93 "..\..\Signum\Views\PaginationSelector.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("\r\n                <li");

WriteLiteral(" class=\"active\"");

WriteLiteral("><span>");

            
            #line 95 "..\..\Signum\Views\PaginationSelector.cshtml"
                                    Write(paginate.CurrentPage.ToString());

            
            #line default
            #line hidden
WriteLiteral("</span></li>\r\n\r\n");

            
            #line 97 "..\..\Signum\Views\PaginationSelector.cshtml"
                
            
            #line default
            #line hidden
            
            #line 97 "..\..\Signum\Views\PaginationSelector.cshtml"
                 for (int i = paginate.CurrentPage + 1; i <= interval.Max; i++)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <li><a");

WriteLiteral(" data-page=\"");

            
            #line 99 "..\..\Signum\Views\PaginationSelector.cshtml"
                                 Write(i);

            
            #line default
            #line hidden
WriteLiteral("\"");

WriteLiteral(" href=\"#\"");

WriteLiteral(">");

            
            #line 99 "..\..\Signum\Views\PaginationSelector.cshtml"
                                              Write(i);

            
            #line default
            #line hidden
WriteLiteral("</a></li> \r\n");

            
            #line 100 "..\..\Signum\Views\PaginationSelector.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("\r\n");

            
            #line 102 "..\..\Signum\Views\PaginationSelector.cshtml"
                
            
            #line default
            #line hidden
            
            #line 102 "..\..\Signum\Views\PaginationSelector.cshtml"
                 if (interval.Max != resultTable.TotalPages)
                {
                    if (interval.Max + 1 != resultTable.TotalPages)
                    {

            
            #line default
            #line hidden
WriteLiteral("                    <li");

WriteLiteral(" class=\"disabled\"");

WriteLiteral("><span>...</span></li> \r\n");

            
            #line 107 "..\..\Signum\Views\PaginationSelector.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("                    <li><a");

WriteLiteral(" data-page=\"");

            
            #line 108 "..\..\Signum\Views\PaginationSelector.cshtml"
                                 Write(resultTable.TotalPages);

            
            #line default
            #line hidden
WriteLiteral("\"");

WriteLiteral(" href=\"#\"");

WriteLiteral(">");

            
            #line 108 "..\..\Signum\Views\PaginationSelector.cshtml"
                                                                   Write(resultTable.TotalPages);

            
            #line default
            #line hidden
WriteLiteral("</a></li> \r\n");

            
            #line 109 "..\..\Signum\Views\PaginationSelector.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("\r\n                <li");

WriteAttribute("class", Tuple.Create(" class=\"", 4978), Tuple.Create("\"", 5055)
            
            #line 111 "..\..\Signum\Views\PaginationSelector.cshtml"
, Tuple.Create(Tuple.Create("", 4986), Tuple.Create<System.Object, System.Int32>(resultTable.TotalPages <= paginate.CurrentPage ? "disabled" : null
            
            #line default
            #line hidden
, 4986), false)
);

WriteLiteral("><a");

WriteLiteral(" class=\"sf-pagination-button\"");

WriteLiteral(" data-page=\"");

            
            #line 111 "..\..\Signum\Views\PaginationSelector.cshtml"
                                                                                                                                         Write(paginate.CurrentPage + 1);

            
            #line default
            #line hidden
WriteLiteral("\"");

WriteLiteral(" href=\"#\"");

WriteLiteral(">&raquo;</a></li>\r\n            </ul>\r\n");

            
            #line 113 "..\..\Signum\Views\PaginationSelector.cshtml"
        }

            
            #line default
            #line hidden
WriteLiteral("    </div>\r\n</div>\r\n");

        }
    }
}
#pragma warning restore 1591
