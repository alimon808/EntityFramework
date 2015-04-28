// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Remotion.Linq.Parsing;
using Microsoft.Data.Entity.Query;
using System.Collections.Generic;
using Microsoft.Data.Entity.Query.Annotations;
using Microsoft.Data.Entity.Relational.Query.Expressions;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Relational.Query.ExpressionTreeVisitors
{
    public class CompositePredicateExpressionTreeVisitor : ExpressionTreeVisitor
    {
        private ICollection<QueryAnnotation> _queryAnnotations;

        public CompositePredicateExpressionTreeVisitor([NotNull] ICollection<QueryAnnotation> queryAnnotations)
        {
            Check.NotNull(queryAnnotations, nameof(queryAnnotations));

            _queryAnnotations = queryAnnotations;
        }

        public override Expression VisitExpression(
            [NotNull]Expression expression)
        {
            var currentExpression = expression;
            var inExpressionOptimized = 
                new EqualityPredicateInExpressionOptimizer().VisitExpression(currentExpression);

            currentExpression = inExpressionOptimized;

            var negationOptimized1 =
                new PredicateNegationExpressionOptimizer()
                .VisitExpression(currentExpression);

            currentExpression = negationOptimized1;

            var equalityExpanded =
                new EqualityPredicateExpandingVisitor().VisitExpression(currentExpression);

            currentExpression = equalityExpanded;

            var negationOptimized2 =
                new PredicateNegationExpressionOptimizer()
                .VisitExpression(currentExpression);

            currentExpression = negationOptimized2;

            var parameterDectector = new ParameterExpressionDetectingVisitor();
            parameterDectector.VisitExpression(currentExpression);

            var useRelationalNullComparisons = _queryAnnotations.OfType<UseRelationalNullComparisonsQueryAnnotation>().Any();
            if (!parameterDectector.ContainsParameters && !useRelationalNullComparisons)
            {
                var optimizedNullExpansionVisitor = new NullSemanticsOptimizedExpandingVisitor();
                var nullSemanticsExpandedOptimized = optimizedNullExpansionVisitor.VisitExpression(currentExpression);
                if (optimizedNullExpansionVisitor.OptimizedExpansionPossible)
                {
                    currentExpression = nullSemanticsExpandedOptimized;
                }
                else
                {
                    currentExpression = new NullSemanticsExpandingVisitor()
                        .VisitExpression(currentExpression);
                }
            }

            if (useRelationalNullComparisons)
            {
                currentExpression = new NotNullableExpression(currentExpression);
            }

            var negationOptimized3 =
                new PredicateNegationExpressionOptimizer()
                .VisitExpression(currentExpression);

            currentExpression = negationOptimized3;

            return currentExpression;
        }

        private class ParameterExpressionDetectingVisitor : ExpressionTreeVisitor
        {
            public bool ContainsParameters { get; set; }

            protected override Expression VisitParameterExpression(ParameterExpression expression)
            {
                ContainsParameters = true;

                return base.VisitParameterExpression(expression);
            }
        }
    }
}
