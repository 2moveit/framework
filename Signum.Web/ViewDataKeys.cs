﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;

namespace Signum.Web
{
    public static class ViewDataKeys
    {
        public const string WriteSFInfo = "sfWriteSFInfo";
        public const string GlobalErrors = "sfGlobalErrors"; //Key for Global Errors in ModelStateDictionary
        public const string Title = "Title";
        public const string CustomHtml = "sfCustomHtml";
        public const string OnOk = "sfOnOk";
        public const string FindOptions = "sfFindOptions";
        public const string QueryDescription = "sfQueryDescription";
        public const string QueryName = "sfQueryName";
        public const string Results = "sfResults";
        public const string MultipliedMessage = "sfMultipliedMessage";
        public const string Formatters = "sfFormatters";
        public const string TabId = "sfTabId";
        public const string PartialViewName = "sfPartialViewName";
        
        public static string WindowPrefix(this HtmlHelper helper)
        {
            TypeContext tc = helper.ViewData.Model as TypeContext;
            if (tc == null)
                return null;
            else
                return tc.ControlID;
        }
    }
}
