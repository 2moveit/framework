﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;

namespace Signum.Web
{
    public class JsFindOptions : JsRenderer
    {
        public JsValue<string> Prefix { get; set; }
        public FindOptions FindOptions { get; set; }
        public JsValue<int?> ElementsPerPage { get; set; }
        public JsValue<string> OpenFinderUrl { get; set; }
        /// <summary>
        /// To be called when closing the popup (if exists) with the Ok button
        /// </summary>
        public JsFunction OnOk { get; set; }
        public JsFunction OnCancelled { get; set; }  

        public JsFindOptions()
        {
            Renderer = () =>
            {
                JsOptionsBuilder options = new JsOptionsBuilder(true)
                {
                    { "prefix", Prefix.TryCC(t => t.ToJS()) },
                    { "elems", ElementsPerPage.TryCC(t => t.ToJS()) },
                    { "openFinderUrl", OpenFinderUrl.TryCC(t => t.ToJS()) },
                    { "onOk", OnOk.TryCC(t => t.ToJS()) },
                    { "onCancelled", OnCancelled.TryCC(t => t.ToJS()) }
                };

                if (FindOptions != null)
                    FindOptions.Fill(options);

                return options.ToJS();
            };
        }
    }

    public class JsFindNavigator : JsInstruction
    {
        private JsFindNavigator(string prefix)
        {
            Renderer = () => "SF.FindNavigator.getFor('{0}')".Formato(prefix);
        }

        public static JsFindNavigator GetFor(string prefix)
        {
            return new JsFindNavigator(prefix);
        }

        public static JsInstruction openFinder(JsFindOptions options)
        { 
            return new JsInstruction(() => "SF.FindNavigator.openFinder({0})".Formato(options.ToJS()));
        }

        public JsInstruction search()
        {
            return new JsInstruction(() => "{0}.search()".Formato(this.ToJS()));
        }

        public JsInstruction selectedItems(JsFindOptions options)
        {
            return new JsInstruction(() => "{0}.selectedItems()".Formato(this.ToJS()));
        }

        public JsInstruction hasSelectedItems(JsFunction onSuccess)
        {
            return new JsInstruction(() => "{0}.hasSelectedItems({1})".Formato(this.ToJS(), onSuccess.ToJS()));
        }

        public JsInstruction splitSelectedIds()
        {
            return new JsInstruction(() => "{0}.splitSelectedIds()".Formato(this.ToJS()));
        }

        public JsInstruction requestData()
        {
            return new JsInstruction(() => "{0}.requestDataForSearch()".Formato(this.ToJS()));
        }
    }
}
