using Microsoft.AspNetCore.Mvc;
using LotusTeam.Data;
using Microsoft.EntityFrameworkCore;
using LotusTeam.DTOs;
using LotusTeam.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Text;
using LotusTeam.Service;

namespace LotusTeam.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER")]
    public class DepartmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(AppDbContext context, ILogger<DepartmentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,DIRECTOR")]
        public async Task<ActionResult<ApiResponse<IEnumerable<DepartmentDto>>>> GetDepartments(
            [FromQuery] string? search = null)
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

                return Ok(new ApiResponse<IEnumerable<DepartmentDto>>
                {
                    Success = true,
                    Data = departmentDtos,
                    Message = "Danh sách phòng ban"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving departments");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách phòng ban"
                });
            }
        }

        [HttpGet("tree")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,DIRECTOR")]
        public async Task<ActionResult<ApiResponse<IEnumerable<DepartmentTreeDto>>>> GetDepartmentTree()
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
                    // In a real tree structure, you might have parent-child relationships
                    Children = new List<DepartmentTreeDto>()
                }).ToList();

                return Ok(new ApiResponse<IEnumerable<DepartmentTreeDto>>
                {
                    Success = true,
                    Data = departmentTree,
                    Message = "Cây tổ chức phòng ban"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving department tree");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy cây tổ chức"
                });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,DIRECTOR")]
        public async Task<ActionResult<ApiResponse<DepartmentDetailDto>>> GetDepartment(int id)
        {
            try
            {
                var department = await _context.Departments
                    .Include(d => d.Employees)
                    .FirstOrDefaultAsync(d => d.DepartmentID == id);

                if (department == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Phòng ban với ID {id} không tồn tại"
                    });
                }

                var employees = department.Employees.Select(e => new EmployeeSimpleDto
                {
                    EmployeeId = e.EmployeeID,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    Email = e.Email,
                    Phone = e.Phone,
                    Status = e.Status
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

                return Ok(new ApiResponse<DepartmentDetailDto>
                {
                    Success = true,
                    Data = departmentDetail,
                    Message = "Thông tin phòng ban"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving department with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin phòng ban"
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> CreateDepartment(CreateDepartmentDto createDto)
        {
            try
            {
                // Check if department code exists
                var existingDepartment = await _context.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentCode == createDto.DepartmentCode);

                if (existingDepartment != null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Mã phòng ban '{createDto.DepartmentCode}' đã tồn tại"
                    });
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

                return CreatedAtAction(nameof(GetDepartment), new { id = department.DepartmentID },
                    new ApiResponse<DepartmentDto>
                    {
                        Success = true,
                        Data = departmentDto,
                        Message = "Tạo phòng ban thành công"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo phòng ban"
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> UpdateDepartment(int id, UpdateDepartmentDto updateDto)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);

                if (department == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Phòng ban với ID {id} không tồn tại"
                    });
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
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Mã phòng ban '{updateDto.DepartmentCode}' đã tồn tại"
                        });
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

                return Ok(new ApiResponse<DepartmentDto>
                {
                    Success = true,
                    Data = departmentDto,
                    Message = "Cập nhật phòng ban thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating department with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật phòng ban"
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteDepartment(int id)
        {
            try
            {
                var department = await _context.Departments
                    .Include(d => d.Employees)
                    .FirstOrDefaultAsync(d => d.DepartmentID == id);

                if (department == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Phòng ban với ID {id} không tồn tại"
                    });
                }

                // Check if department has employees
                if (department.Employees.Any())
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể xóa phòng ban đang có nhân viên"
                    });
                }

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa phòng ban thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting department with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa phòng ban"
                });
            }
        }

        [HttpGet("export")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,DIRECTOR")]
        public async Task<IActionResult> ExportDepartments()
        {
            try
            {
                var departments = await _context.Departments
                    .Include(d => d.Employees)
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync();

                // Create CSV or Excel file
                var csv = new StringBuilder();
                csv.AppendLine("Mã phòng ban,Tên phòng ban,Mô tả,Số nhân viên,Ngày tạo");

                foreach (var dept in departments)
                {
                    csv.AppendLine($"\"{dept.DepartmentCode}\",\"{dept.DepartmentName}\",\"{dept.Description ?? ""}\",{dept.Employees.Count},\"{dept.CreatedDate:dd/MM/yyyy}\"");
                }

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"departments_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting departments");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xuất danh sách phòng ban"
                });
            }
        }
    }

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
        public List<EmployeeSimpleDto> Employees { get; set; } = new();
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

    public class EmployeeSimpleDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public short Status { get; set; }
    }
}