using Microsoft.EntityFrameworkCore;
using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LotusTeam.Services
{
    public class DepartmentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DepartmentService> _logger;

        public DepartmentService(AppDbContext context, ILogger<DepartmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<DepartmentDto>>> GetDepartmentsAsync(string? search = null)
        {
            try
            {
                var query = _context.Departments.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(d =>
                        d.DepartmentCode.Contains(search) ||
                        d.DepartmentName.Contains(search) ||
                        (d.Description != null && d.Description.Contains(search)));
                }

                var departments = await query
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync();

                var departmentDtos = departments.Select(d => new DepartmentDto
                {
                    DepartmentId = d.DepartmentID,
                    DepartmentCode = d.DepartmentCode,
                    DepartmentName = d.DepartmentName,
                    Description = d.Description,
                    CreatedDate = d.CreatedDate
                }).ToList();

                return new ApiResponse<IEnumerable<DepartmentDto>>
                {
                    Success = true,
                    Data = departmentDtos,
                    Message = "Danh sách phòng ban",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving departments");
                return new ApiResponse<IEnumerable<DepartmentDto>>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách phòng ban",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<IEnumerable<DepartmentTreeDto>>> GetDepartmentTreeAsync()
        {
            try
            {
                var departments = await _context.Departments
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync();

                var departmentTree = departments.Select(d => new DepartmentTreeDto
                {
                    DepartmentId = d.DepartmentID,
                    DepartmentCode = d.DepartmentCode,
                    DepartmentName = d.DepartmentName,
                    Description = d.Description,
                    CreatedDate = d.CreatedDate,
                    Children = new List<DepartmentTreeDto>()
                }).ToList();

                return new ApiResponse<IEnumerable<DepartmentTreeDto>>
                {
                    Success = true,
                    Data = departmentTree,
                    Message = "Cây tổ chức phòng ban",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving department tree");
                return new ApiResponse<IEnumerable<DepartmentTreeDto>>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy cây tổ chức",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<DepartmentDetailDto>> GetDepartmentAsync(int id)
        {
            try
            {
                var department = await _context.Departments
                    .Include(d => d.Employees)
                    .FirstOrDefaultAsync(d => d.DepartmentID == id);

                if (department == null)
                {
                    return new ApiResponse<DepartmentDetailDto>
                    {
                        Success = false,
                        Message = $"Phòng ban với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                // Sử dụng DTO riêng cho Service thay vì EmployeeSimpleDto từ namespace LotusTeam.DTOs
                var employees = department.Employees.Select(e => new DepartmentEmployeeDto
                {
                    EmployeeId = e.EmployeeID,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    Email = e.Email,
                    Phone = e.Phone,
                    Status = e.Status, // Kiểu short
                    StatusText = GetStatusText(e.Status) // Thêm trường text để hiển thị
                }).ToList();

                var departmentDetail = new DepartmentDetailDto
                {
                    DepartmentId = department.DepartmentID,
                    DepartmentCode = department.DepartmentCode,
                    DepartmentName = department.DepartmentName,
                    Description = department.Description,
                    CreatedDate = department.CreatedDate,
                    Employees = employees,
                    EmployeeCount = employees.Count
                };

                return new ApiResponse<DepartmentDetailDto>
                {
                    Success = true,
                    Data = departmentDetail,
                    Message = "Thông tin phòng ban",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving department with ID {Id}", id);
                return new ApiResponse<DepartmentDetailDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin phòng ban",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<DepartmentDto>> CreateDepartmentAsync(CreateDepartmentDto createDto)
        {
            try
            {
                // Check if department code exists
                var existingDepartment = await _context.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentCode == createDto.DepartmentCode);

                if (existingDepartment != null)
                {
                    return new ApiResponse<DepartmentDto>
                    {
                        Success = false,
                        Message = $"Mã phòng ban '{createDto.DepartmentCode}' đã tồn tại",
                        StatusCode = 400
                    };
                }

                var department = new Department
                {
                    DepartmentCode = createDto.DepartmentCode,
                    DepartmentName = createDto.DepartmentName,
                    Description = createDto.Description,
                    CreatedDate = DateTime.Now
                };

                _context.Departments.Add(department);
                await _context.SaveChangesAsync();

                var departmentDto = new DepartmentDto
                {
                    DepartmentId = department.DepartmentID,
                    DepartmentCode = department.DepartmentCode,
                    DepartmentName = department.DepartmentName,
                    Description = department.Description,
                    CreatedDate = department.CreatedDate
                };

                return new ApiResponse<DepartmentDto>
                {
                    Success = true,
                    Data = departmentDto,
                    Message = "Tạo phòng ban thành công",
                    StatusCode = 201
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department");
                return new ApiResponse<DepartmentDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo phòng ban",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<DepartmentDto>> UpdateDepartmentAsync(int id, UpdateDepartmentDto updateDto)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);

                if (department == null)
                {
                    return new ApiResponse<DepartmentDto>
                    {
                        Success = false,
                        Message = $"Phòng ban với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                // Check if new department code conflicts
                if (!string.IsNullOrEmpty(updateDto.DepartmentCode) &&
                    updateDto.DepartmentCode != department.DepartmentCode)
                {
                    var existingCode = await _context.Departments
                        .FirstOrDefaultAsync(d => d.DepartmentCode == updateDto.DepartmentCode &&
                                                 d.DepartmentID != id);

                    if (existingCode != null)
                    {
                        return new ApiResponse<DepartmentDto>
                        {
                            Success = false,
                            Message = $"Mã phòng ban '{updateDto.DepartmentCode}' đã tồn tại",
                            StatusCode = 400
                        };
                    }
                }

                // Update properties
                if (!string.IsNullOrEmpty(updateDto.DepartmentCode))
                    department.DepartmentCode = updateDto.DepartmentCode;

                if (!string.IsNullOrEmpty(updateDto.DepartmentName))
                    department.DepartmentName = updateDto.DepartmentName;

                if (updateDto.Description != null)
                    department.Description = updateDto.Description;

                await _context.SaveChangesAsync();

                var departmentDto = new DepartmentDto
                {
                    DepartmentId = department.DepartmentID,
                    DepartmentCode = department.DepartmentCode,
                    DepartmentName = department.DepartmentName,
                    Description = department.Description,
                    CreatedDate = department.CreatedDate
                };

                return new ApiResponse<DepartmentDto>
                {
                    Success = true,
                    Data = departmentDto,
                    Message = "Cập nhật phòng ban thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating department with ID {Id}", id);
                return new ApiResponse<DepartmentDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật phòng ban",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<object>> DeleteDepartmentAsync(int id)
        {
            try
            {
                var department = await _context.Departments
                    .Include(d => d.Employees)
                    .FirstOrDefaultAsync(d => d.DepartmentID == id);

                if (department == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Phòng ban với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                // Check if department has employees
                if (department.Employees.Any())
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể xóa phòng ban đang có nhân viên",
                        StatusCode = 400
                    };
                }

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa phòng ban thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting department with ID {Id}", id);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa phòng ban",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<object>> ExportDepartmentsAsync()
        {
            try
            {
                var departments = await _context.Departments
                    .Include(d => d.Employees)
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync();

                // Create CSV content
                var csv = new StringBuilder();
                csv.AppendLine("Mã phòng ban,Tên phòng ban,Mô tả,Số nhân viên,Ngày tạo");

                foreach (var dept in departments)
                {
                    // Sửa: Gọi phương thức Count() thay vì chỉ sử dụng property Count
                    csv.AppendLine($"\"{dept.DepartmentCode}\",\"{dept.DepartmentName}\",\"{dept.Description ?? ""}\",{dept.Employees.Count()},\"{dept.CreatedDate:dd/MM/yyyy}\"");
                }

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());

                return new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        FileName = $"departments_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                        Content = csv.ToString(),
                        ContentType = "text/csv",
                        Bytes = bytes
                    },
                    Message = "Xuất danh sách phòng ban thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting departments");
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xuất danh sách phòng ban",
                    StatusCode = 500
                };
            }
        }

        // Helper method to convert status code to text
        private string GetStatusText(short status)
        {
            return status switch
            {
                0 => "Đã nghỉ việc",
                1 => "Đang làm việc",
                2 => "Nghỉ phép",
                3 => "Tạm ngừng",
                _ => "Không xác định"
            };
        }

        // ========================= DTO Definitions =========================
        public class DepartmentDto
        {
            public int DepartmentId { get; set; }
            public string DepartmentCode { get; set; } = string.Empty;
            public string DepartmentName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        public class DepartmentTreeDto : DepartmentDto
        {
            public List<DepartmentTreeDto> Children { get; set; } = new();
        }

        public class DepartmentDetailDto : DepartmentDto
        {
            public int EmployeeCount { get; set; }
            public List<DepartmentEmployeeDto> Employees { get; set; } = new();
        }

        // Tạo DTO riêng cho DepartmentService
        public class DepartmentEmployeeDto
        {
            public int EmployeeId { get; set; }
            public string EmployeeCode { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public short Status { get; set; } // Kiểu short
            public string StatusText { get; set; } = string.Empty; // Thêm để hiển thị text
        }

        public class CreateDepartmentDto
        {
            [Required]
            [StringLength(10)]
            public string DepartmentCode { get; set; } = string.Empty;

            [Required]
            [StringLength(100)]
            public string DepartmentName { get; set; } = string.Empty;

            [StringLength(255)]
            public string? Description { get; set; }
        }

        public class UpdateDepartmentDto
        {
            [StringLength(10)]
            public string? DepartmentCode { get; set; }

            [StringLength(100)]
            public string? DepartmentName { get; set; }

            [StringLength(255)]
            public string? Description { get; set; }
        }
    }
}