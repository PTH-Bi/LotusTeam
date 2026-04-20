// Services/IAttendanceService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LotusTeam.Models;
using LotusTeam.Service;

namespace LotusTeam.Services
{
    public interface IAttendanceService
    {
        // Existing methods
        Task<List<Attendances>> GetMyAttendanceAsync(int employeeId);
        Task<List<Attendances>> GetDepartmentAttendanceAsync(int departmentId, DateTime from, DateTime to);
        Task<Attendances> ManualCheckAsync(Attendances attendance);
        Task<List<Attendances>> GetRawAttendanceLogAsync(int employeeId);
        Task<bool> AdjustAttendanceAsync(long attendanceId, TimeSpan? checkIn, TimeSpan? checkOut, string reason);
        Task<List<AttendanceOvertime>> GetOvertimeListAsync();
        Task<AttendanceOvertime> RegisterOvertimeAsync(AttendanceOvertime overtime);
        Task<bool> ApproveOvertimeAsync(long overtimeId);

        // Face attendance methods
        Task<(bool Success, string Message, double Confidence)> FaceCheckInAsync(int employeeId, string imageBase64);
        Task<(bool Success, string Message, double Confidence)> FaceCheckOutAsync(int employeeId, string imageBase64);
        Task<List<FaceAttendanceDto>> GetFaceAttendanceHistoryAsync(int employeeId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<object> GetTodayFaceAttendanceStatusAsync(int employeeId);
        Task<int> SyncFaceAttendanceToAttendanceAsync(DateTime? date = null);
    }
}