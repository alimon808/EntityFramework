// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Data.Entity.Query;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Relational.Internal
{
    public class AnnotatedDbSet<TEntity>
        : DbSet<TEntity>, IOrderedQueryable<TEntity>, IAsyncEnumerableAccessor<TEntity>
        where TEntity : class
    {
        private readonly EntityQueryable<TEntity> _queryableSource;

        public AnnotatedDbSet([NotNull] IQueryable<TEntity> source)
        {
            Check.NotNull(source, nameof(source));

            _queryableSource = (EntityQueryable<TEntity>)source;
        }

        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator() => _queryableSource.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _queryableSource.GetEnumerator();

        IAsyncEnumerable<TEntity> IAsyncEnumerableAccessor<TEntity>.AsyncEnumerable => _queryableSource;

        Type IQueryable.ElementType => _queryableSource.ElementType;

        Expression IQueryable.Expression => _queryableSource.Expression;

        IQueryProvider IQueryable.Provider => _queryableSource.Provider;
    }
}
