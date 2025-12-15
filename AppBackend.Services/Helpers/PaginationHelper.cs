using AppBackend.BusinessObjects.Dtos;
using AppBackend.Services.ApiModels.RoomModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.Services.Helpers
{
    public class PaginationHelper
    {
        public PagedResponseDto<dynamic> HandlePagination(List<dynamic>? entities, int pageIndex, int pageSize)
        {
            var pagedEntities = entities
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResponseDto<dynamic>
            {
                Items = pagedEntities,
                TotalCount = pagedEntities.Count,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)entities.Count / pageSize)
            };
        }
    }
}
