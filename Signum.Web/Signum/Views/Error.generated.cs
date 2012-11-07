﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18010
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
    using Signum.Entities;
    using Signum.Utilities;
    using Signum.Web;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "1.5.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Signum/Views/Error.cshtml")]
    public class Error : System.Web.Mvc.WebViewPage<HandleErrorInfo >
    {
        public Error()
        {
        }
        public override void Execute()
        {

            
            #line 1 "..\..\Signum\Views\Error.cshtml"
  
    this.ViewBag.Title = "Error"; 


            
            #line default
            #line hidden

WriteLiteral("<div id=\"main-home\">\r\n");


            
            #line 6 "..\..\Signum\Views\Error.cshtml"
     if (Model.Exception is ApplicationException)
    {

            
            #line default
            #line hidden
WriteLiteral("        <h1>");


            
            #line 8 "..\..\Signum\Views\Error.cshtml"
       Write(Model.Exception.Message);

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n");


            
            #line 9 "..\..\Signum\Views\Error.cshtml"
    }
    else
    {

            
            #line default
            #line hidden
WriteLiteral("        <h1>");


            
            #line 12 "..\..\Signum\Views\Error.cshtml"
        Write("Error " + this.ViewContext.HttpContext.Response.StatusCode);

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n");



WriteLiteral("        <h2>");


            
            #line 13 "..\..\Signum\Views\Error.cshtml"
        Write("Error thrown");

            
            #line default
            #line hidden
WriteLiteral("</h2>\r\n");


            
            #line 14 "..\..\Signum\Views\Error.cshtml"
    }

            
            #line default
            #line hidden
WriteLiteral("    <div class=\"error-region\">\r\n        <p>\r\n            <span>Controller: </span" +
">\r\n            <code>\r\n                ");


            
            #line 19 "..\..\Signum\Views\Error.cshtml"
           Write(Model.ControllerName);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </code>\r\n        </p>\r\n        <p>\r\n            <span>Action: </spa" +
"n>\r\n            <code>\r\n                ");


            
            #line 25 "..\..\Signum\Views\Error.cshtml"
           Write(Model.ActionName);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </code>\r\n        </p>\r\n    </div>\r\n    <div class=\"error-region\">\r\n" +
"        <span>Message: </span>\r\n        <pre>\r\n            <code>\r\n             " +
"   ");


            
            #line 33 "..\..\Signum\Views\Error.cshtml"
           Write(Model.Exception.Message);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </code>\r\n        </pre>\r\n        <span>StackTrace: </span>\r\n       " +
" <pre>\r\n            <code>\r\n                ");


            
            #line 39 "..\..\Signum\Views\Error.cshtml"
           Write(Model.Exception.StackTrace);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </code>\r\n        </pre>\r\n    </div>\r\n</div>\r\n");


        }
    }
}
#pragma warning restore 1591