using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Domain.Dtos
{
    public sealed class PagedResult<T>
    {
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int Total { get; init; }
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    }
}
