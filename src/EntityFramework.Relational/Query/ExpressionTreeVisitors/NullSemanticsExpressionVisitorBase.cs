﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq.Expressions;
using Remotion.Linq.Parsing;

namespace Microsoft.Data.Entity.Relational.Query.ExpressionTreeVisitors
{
    public abstract class NullSemanticsExpressionVisitorBase : ExpressionTreeVisitor
    {
        protected Expression BuildIsNullExpression(Expression expression)
        {
            var isNullExpressionBuilder = new IsNullExpressionBuildingVisitor();
            isNullExpressionBuilder.VisitExpression(expression);

            return isNullExpressionBuilder.ResultExpression;
        }
    }
}
