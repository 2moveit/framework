﻿#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Web.Mvc.Html;
using Signum.Entities;
using System.Reflection;
using Signum.Entities.Reflection;
using System.Configuration;
using Signum.Web.Properties;
#endregion

namespace Signum.Web
{
    public static class EntityLineDetailHelper
    {
        internal static MvcHtmlString InternalEntityLineDetail(this HtmlHelper helper, EntityLineDetail entityDetail)
        {
            if (!entityDetail.Visible || entityDetail.HideIfNull && entityDetail.UntypedValue == null)
                return MvcHtmlString.Empty;

            HtmlStringBuilder sb = new HtmlStringBuilder();

            using (sb.Surround(new HtmlTag("div").Class("sf-entity-line-detail")))
            {
                sb.AddLine(EntityBaseHelper.BaseLineLabel(helper, entityDetail));

                sb.AddLine(helper.HiddenEntityInfo(entityDetail));

                if (entityDetail.Type.IsEmbeddedEntity())
                {
                    TypeContext templateTC = ((TypeContext)entityDetail.Parent).Clone((object)Constructor.Construct(entityDetail.Type.CleanType()));
                    sb.AddLine(EntityBaseHelper.EmbeddedTemplate(entityDetail, EntityBaseHelper.RenderTypeContext(helper, templateTC, RenderMode.Content, entityDetail)));
                }

                sb.AddLine(EntityBaseHelper.CreateButton(helper, entityDetail));
                sb.AddLine(EntityBaseHelper.FindButton(helper, entityDetail));
                sb.AddLine(EntityBaseHelper.RemoveButton(helper, entityDetail));

                MvcHtmlString controlHtml = null;
                if (entityDetail.UntypedValue != null)
                    controlHtml = EntityBaseHelper.RenderTypeContext(helper, (TypeContext)entityDetail.Parent, RenderMode.Content, entityDetail);

                if (entityDetail.DetailDiv == entityDetail.DefaultDetailDiv)
                    sb.AddLine(helper.Div(entityDetail.DetailDiv, controlHtml, ""));
                else if (controlHtml != null)
                    sb.AddLine(MvcHtmlString.Create("<script type=\"text/javascript\">\n" +
                            "$(document).ready(function() {\n" +
                            "$('#" + entityDetail.DetailDiv + "').html(" + controlHtml + ");\n" +
                            "});\n" +
                            "</script>"));

            }

            return sb.ToHtml();
        }

        public static MvcHtmlString EntityLineDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property) 
        {
            return helper.EntityLineDetail<T, S>(tc, property, null);
        }

        public static MvcHtmlString EntityLineDetail<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, S>> property, Action<EntityLineDetail> settingsModifier)
        {
            TypeContext<S> context = Common.WalkExpression(tc, property);

            EntityLineDetail edl = new EntityLineDetail(context.Type, context.Value, context, null, context.PropertyRoute); 
           
            EntityBaseHelper.ConfigureEntityBase(edl, edl.CleanRuntimeType ?? edl.Type.CleanType());

            Common.FireCommonTasks(edl);

            if (settingsModifier != null)
                settingsModifier(edl);

            return helper.InternalEntityLineDetail(edl);
        }
    }
}
