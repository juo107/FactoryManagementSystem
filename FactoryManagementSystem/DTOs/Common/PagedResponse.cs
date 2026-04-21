using System.Collections.Generic;

namespace FactoryManagementSystem.DTOs.Common
{
    public class PagedResponse<T>
    {
        public int Total { get; set; }
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }

        public PagedResponse(IEnumerable<T> items, int total, int page, int pageSize)
        {
            Items = items;
            Total = total;
            Page = page;
            PageSize = pageSize;
        }
    }
}
