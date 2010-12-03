﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Text;
using Signum.Utilities;
using System.Collections;

namespace Signum.Web
{
    public class HtmlTag
    {
        TagBuilder tagBuilder;
        public TagBuilder TagBuilder
        {
            get { return tagBuilder; }
        }

        public HtmlTag(string tagName)
        {
            tagBuilder = new TagBuilder(tagName);
        }

        public HtmlTag(string tagName, string id)
        {
            tagBuilder = new TagBuilder(tagName);

            if (!string.IsNullOrEmpty(id))
                tagBuilder.GenerateId(id);
        }

        public HtmlTag(TagBuilder tagBuilder)
        {
            this.tagBuilder = tagBuilder;
        }

        public HtmlTag Class(string value)
        {
            if (!string.IsNullOrEmpty(value))
                tagBuilder.AddCssClass(value);
            return this;
        }

        public HtmlTag Id(string id)
        {
            tagBuilder.GenerateId(id);
            return this;
        }

        public HtmlTag IdName(string id)
        {
            tagBuilder.GenerateId(id);
            tagBuilder.MergeAttribute("name", id); 
            return this;
        }

        public HtmlTag Attr(string key, string value)
        {
            tagBuilder.MergeAttribute(key, value);
            return this;
        }

        public HtmlTag Attr(string key, string value, bool replaceExisting)
        {
            tagBuilder.MergeAttribute(key, value, replaceExisting);
            return this;
        }

        public HtmlTag Attrs(object attributes)
        {
            tagBuilder.MergeAttributes(new RouteValueDictionary(attributes));
            return this;
        }

        public HtmlTag Attrs(object attributes, bool replaceExisting)
        {
            tagBuilder.MergeAttributes(new RouteValueDictionary(attributes), replaceExisting);
            return this;
        }

        public HtmlTag Attrs<TKey, TValue>(IDictionary<TKey, TValue> attributes)
        {
            tagBuilder.MergeAttributes(attributes);
            return this;
        }

        public HtmlTag Attrs<TKey, TValue>(IDictionary<TKey, TValue> attributes, bool replaceExisting)
        {
            tagBuilder.MergeAttributes(attributes, replaceExisting);
            return this;

        }

        public HtmlTag SetInnerText(string innerText)
        {
            tagBuilder.SetInnerText(innerText);
            return this;
        }

        public HtmlTag InnerHtml(MvcHtmlString html)
        {
            if (MvcHtmlString.IsNullOrEmpty(html))
                tagBuilder.InnerHtml = null;
            else
                tagBuilder.InnerHtml = html.ToHtmlString();

            return this;
        }

        public override string ToString()
        {
            throw new InvalidOperationException("Call ToHtml instead");
        }

        public MvcHtmlString ToHtml()
        {
            return this.ToHtml(TagRenderMode.Normal);
        }

        public MvcHtmlString ToHtmlSelf()
        {
            return this.ToHtml(TagRenderMode.SelfClosing);
        }

        public MvcHtmlString ToHtml(TagRenderMode renderMode)
        {
            return MvcHtmlString.Create(tagBuilder.ToString(renderMode));
        }
    }

    public static class HtmlStringExtensions
    {
        public static MvcHtmlString Concat(this MvcHtmlString one, MvcHtmlString other)
        {
            return MvcHtmlString.Create(one.ToHtmlString() + other.ToHtmlString());
        }

        public static MvcHtmlString Surround(this MvcHtmlString html, string tagName)
        {
            return html.Surround("<" + tagName + ">", "</" + tagName + ">"); 
        }

        public static MvcHtmlString Surround(this MvcHtmlString html, string startTag, string endTag)
        {
            if (MvcHtmlString.IsNullOrEmpty(html))
                return html; 

            return MvcHtmlString.Create(startTag + html.ToHtmlString() + endTag);
        }

        public static MvcHtmlString EncodeHtml(this string htmlToEncode)
        {
            return MvcHtmlString.Create(HttpUtility.HtmlEncode(htmlToEncode));
        }
    }

    public class HtmlStringBuilder: IEnumerable
    {
        StringBuilder sb = new StringBuilder();

        public void Add(MvcHtmlString html)
        {
            if (!MvcHtmlString.IsNullOrEmpty(html))
                sb.Append(html.ToHtmlString());
        }

        public void AddLine(MvcHtmlString html)
        {
            if (!MvcHtmlString.IsNullOrEmpty(html))
                sb.AppendLine(html.ToHtmlString());
        }

        public IDisposable Surround(string tagName)
        {
            return Surround(new HtmlTag(tagName));
        }

        public IDisposable Surround(HtmlTag div)
        {
            AddLine(div.ToHtml(TagRenderMode.StartTag));

            return new Disposable(() => AddLine(div.ToHtml(TagRenderMode.EndTag)));
        }

        public MvcHtmlString ToHtml()
        {
            return MvcHtmlString.Create(sb.ToString());
        }

        public override string ToString()
        {
            throw new InvalidOperationException("Call ToHtml instead");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException("just to use collection initializers");
        }
    }
}
