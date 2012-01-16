﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Properties;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class BagPropertyToken : QueryToken
    {
        public PropertyInfo PropertyInfo { get; private set; }

        internal BagPropertyToken(QueryToken parent, PropertyInfo pi)
            : base(parent)
        {
            if (pi == null)
                throw new ArgumentNullException("pi");

            this.PropertyInfo = pi;
        }

        public override Type Type
        {
            get { return BuildLite(PropertyInfo.PropertyType).Nullify(); }
        }

        public override string ToString()
        {
            return PropertyInfo.NiceName();
        }

        public override string Key
        {
            get { return PropertyInfo.Name; }
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var baseExpression = Parent.BuildExpression(context);

            Expression result = Expression.Property(baseExpression, PropertyInfo);
            
            return BuildLite(result).Nullify();
        }

        protected override List<QueryToken> SubTokensInternal()
        {
            return SubTokensBase(PropertyInfo.PropertyType, GetImplementations());
        }

        public override Implementations? GetImplementations()
        {
            return null;
        }

        public override string Format
        {
            get
            {
                FormatAttribute format = PropertyInfo.SingleAttribute<FormatAttribute>();
                if (format != null)
                    return format.Format;

                return Reflector.FormatString(Type);
            }
        }

        public override string Unit
        {
            get { return PropertyInfo.SingleAttribute<UnitAttribute>().TryCC(u => u.UnitName); }
        }

        public override bool IsAllowed()
        { 
            return Parent.IsAllowed();
        }
    
        public override string NiceName()
        {
            return PropertyInfo.NiceName() + Resources.Of + Parent.ToString();
        }

        public override QueryToken Clone()
        {
            return new BagPropertyToken(Parent.Clone(), PropertyInfo);
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return null;
        }
    }

    public interface IQueryTokenBag { }
}
