﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.ObjectModel;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;

namespace Signum.Engine.Linq
{
    internal enum MetaExpressionType
    {
        MetaProjector = 2000,
        MetaExpression,
        MetaConstant
    }

    internal class MetaProjectorExpression : Expression
    {
        public readonly Expression Projector;

        readonly Type type;
        public override Type Type
        {
            get { return type; }
        }

        public override ExpressionType NodeType
        {
            get { return (ExpressionType)MetaExpressionType.MetaProjector; }
        }

        public MetaProjectorExpression(Type type, Expression projector)
        {
            this.type = type;
            this.Projector = projector;
        }
    }

    internal class MetaExpression : Expression
    {
        public bool IsEntity
        {
            get { return typeof(ModifiableEntity).IsAssignableFrom(Type); }
        }

        readonly Type type;
        public override Type Type
        {
            get { return type; }
        }

        public override ExpressionType NodeType
        {
            get { return (ExpressionType)MetaExpressionType.MetaExpression; }
        }

        public readonly Meta Meta;

        public MetaExpression(Type type, Meta meta)
        {
            this.type = type;
            this.Meta = meta;
        }
    }

    internal class MetaConstant : Expression
    {
        readonly Type type;
        public override Type Type
        {
            get { return type; }
        }

        public override ExpressionType NodeType
        {
            get { return (ExpressionType)MetaExpressionType.MetaConstant; }
        }

        public MetaConstant(Type type)
        {
            this.type = type;
        }
    }   
}
