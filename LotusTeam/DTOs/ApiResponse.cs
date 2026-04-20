// DTOs/ApiResponse.cs
namespace LotusTeam.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int? StatusCode { get; set; }
        public PaginationInfo? Pagination { get; set; } // Thêm dòng này

        public object? Errors { get; set; }
    }
}