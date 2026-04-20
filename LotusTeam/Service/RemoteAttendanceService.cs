using System;
using System.Threading.Tasks;
using System.Linq;
using LotusTeam.Data;
using LotusTeam.Models;
using Microsoft.EntityFrameworkCore;
using LotusTeam.Service;

namespace LotusTeam.Services
{
    public class RemoteAttendanceService : IRemoteAttendanceService
    {
        private readonly AppDbContext _context;

        public RemoteAttendanceService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Kiểm tra xem nhân viên có được phép chấm công từ xa trong ngày hôm nay không
        /// </summary>
        public async Task<bool> CanFaceScanAsync(int employeeId)
        {
            var today = DateTime.Today;

            return await _context.RemoteAttendances
                .AnyAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.WorkDate == today &&
                    x.Status == "Approved");
        }

        /// <summary>
        /// Gửi yêu cầu chấm công từ xa
        /// </summary>
        public async Task<bool> RequestAsync(RemoteAttendanceRequestDto dto)
        {
            var exist = await _context.RemoteAttendances
                .AnyAsync(x =>
                    x.EmployeeId == dto.EmployeeId &&
                    x.WorkDate == dto.WorkDate.Date);

            if (exist) return false;

            var entity = new RemoteAttendances
            {
                EmployeeId = dto.EmployeeId,
                WorkDate = dto.WorkDate.Date,
                Reason = dto.Reason,
                Status = "Pending",
                CreatedDate = DateTime.Now
            };

            _context.RemoteAttendances.Add(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Duyệt yêu cầu chấm công từ xa
        /// </summary>
        public async Task<bool> ApproveAsync(int id, int approverId)
        {
            var request = await _context.RemoteAttendances.FindAsync(id);

            if (request == null) return false;

            request.Status = "Approved";
            request.ApprovedBy = approverId;
            request.ApprovedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Từ chối yêu cầu chấm công từ xa
        /// </summary>
        public async Task<bool> RejectAsync(int id)
        {
            var request = await _context.RemoteAttendances.FindAsync(id);

            if (request == null) return false;

            request.Status = "Rejected";

            await _context.SaveChangesAsync();
            return true;
        }
    }

    public class RemoteAttendanceRequestDto
    {
        public int EmployeeId { get; set; }
        public DateTime WorkDate { get; set; }
        public string Reason { get; set; } = null!;
    }
}