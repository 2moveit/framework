﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using System.Reflection;
using System.IO;
using System.Web;
namespace Signum.Web
{
    public enum ScriptType
    {
        Css,
        Javascript
    }

    public class BasicScriptHtmlManager
    {
        public Assembly MainAssembly;

        DateTime? lastModified;
        public DateTime LastModified
        {
            get
            {
                if (lastModified == null)
                    lastModified = new DateTime(new FileInfo(MainAssembly.Location).LastWriteTime.Ticks).TrimToSeconds();

                return lastModified.Value; 
            }
        }

        string version;
        public string Version
        {
            get
            {
                if (version == null)
                    version = new FileInfo(MainAssembly.Location).LastWriteTime.Ticks.ToString();

                return version;
            }
        }

        public Func<string, string> Subdomain = VirtualPathUtility.ToAbsolute;

        protected static string cssElement = "<link href=\"{0}\" rel=\"stylesheet\" type=\"text/css\" />";
        protected static string jsElement = "<script type=\"text/javascript\" src=\"{0}\"></script>";

        public virtual string CombinedScript(HtmlHelper html, string[] files, ScriptType scriptType)
        {
            string scriptElement = scriptType == ScriptType.Css ? cssElement : jsElement;

            StringBuilder sb = new StringBuilder();
            foreach (var f in files)
                sb.AppendLine(scriptElement.Formato(Subdomain(f + "?v=" + Version)));

            return sb.ToString();
        }

        public virtual string[] GerUrlsFor(string[] files, ScriptType scriptType)
        {
            return files.Select(f => Subdomain(f + "?v=" + Version)).ToArray();
        }
    }

    public static class ScriptHtmlHelper
    {
        public static BasicScriptHtmlManager Manager = new BasicScriptHtmlManager();

        //useful for loading static resources such as JS and CSS files
        //from a different subdomain (real or virtual)
        public static MvcHtmlString ScriptCss(this HtmlHelper html, params string[] files)
        {
            return MvcHtmlString.Create(Manager.CombinedScript(html, FilterAndInclude(files), ScriptType.Css));
        }

        public static MvcHtmlString ScriptsJs(this HtmlHelper html, params string[] files)
        {
            return MvcHtmlString.Create(Manager.CombinedScript(html, FilterAndInclude(files), ScriptType.Javascript));
        }

        public static string[] UrlCss(params string[] files)
        {
            return Manager.GerUrlsFor(files, ScriptType.Css);
        }

        public static string[] UrlJs(params string[] files)
        {
            return Manager.GerUrlsFor(files, ScriptType.Javascript);
        }

        public static MvcHtmlString DynamicCss(this HtmlHelper html, params string[] files)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<script type=\"text/javascript\">");
            sb.AppendLine("SF.loadCss({0});".Formato(
                    UrlCss(files).ToString(f => '"' + f + '"', ",")
            ));
            sb.AppendLine("</script>");

            return MvcHtmlString.Create(sb.ToString());
        }

        public static HtmlCallbackString DynamicJs(this HtmlHelper html, params string[] files)
        {
            return new HtmlCallbackString(callback =>
            {
                string[] filteredUrls = UrlJs(files);

                if (filteredUrls.Length == 0 && !callback.HasText())
                    return "";

                StringBuilder sb = new StringBuilder();

                string urls = filteredUrls.ToString(f => '"' + f + '"', ",");

                sb.AppendLine("<script type=\"text/javascript\">");

                if (callback != null)
                    sb.AppendLine("SF.loadJs([{0}], {1});".Formato(urls, callback));
                else
                    sb.AppendLine("SF.loadJs([{0}]);".Formato(urls));

                sb.AppendLine("</script>");

                return sb.ToString();
            });
        }

        const string resourceKey = "__resources";

        public static List<string> LoadedResources
        {
            get
            {
                var resources = (List<string>)HttpContext.Current.Items[resourceKey];
                if (resources != null)
                    return resources;

                resources = new List<string>();
                HttpContext.Current.Items[resourceKey] = resources;
                return resources;
            }

        }

        internal static string[] FilterAndInclude(string[] urls)
        {
            var loaded = LoadedResources;
            var toInclude = urls.Except(loaded).ToArray();
            loaded.AddRange(toInclude);
            return toInclude;
        }
    }

    public class HtmlCallbackString : IHtmlString
    {
        string callback; 
        Func<string, string> render; 

        public HtmlCallbackString(Func<string, string> render)
        {
            this.render = render; 
        }

        public string ToHtmlString()
        {
 	        return render(callback);
        }

        public HtmlCallbackString Callback(string callback)
        {
            this.callback = callback;
            return this; 
        }
    }
}

