namespace GPC.Boost.SpendX.Dto.ResponseDto
{
    public class PaginationInfo
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public int TotalPages => PageSize > 0 ? (int)System.Math.Ceiling((double)TotalCount / PageSize) : 0;
    }
}
