using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.Service;
using Microsoft.EntityFrameworkCore;

public class FaceAttendanceService : IFaceAttendanceService
{
    private readonly AppDbContext _context;

    public FaceAttendanceService(AppDbContext context)
    {
        _context = context;
    }

    // =============================
    // CHECK IN
    // =============================
    public async Task<(bool Success, string Message, double Confidence)> CheckIn(int employeeId, string imageBase64)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(x => x.EmployeeID == employeeId);

        if (employee == null)
            return (false, "Nhân viên không tồn tại", 0);

        double confidence = FakeFaceMatch(imageBase64);

        if (confidence < 0.7)
            return (false, "Khuôn mặt không khớp", confidence);

        var today = DateTime.Today;

        var existing = await _context.FaceAttendances
            .FirstOrDefaultAsync(x =>
                x.EmployeeId == employeeId &&
                x.CheckInTime.HasValue &&
                x.CheckInTime.Value.Date == today);

        if (existing != null)
            return (false, "Đã check-in hôm nay", confidence);

        var attendance = new FaceAttendances
        {
            EmployeeId = employeeId,
            CheckInTime = DateTime.Now,
            CapturedImage = imageBase64,
            Confidence = confidence,
            CreatedDate = DateTime.Now
        };

        _context.FaceAttendances.Add(attendance);
        await _context.SaveChangesAsync();

        return (true, "Check-in thành công", confidence);
    }

    // =============================
    // CHECK OUT
    // =============================
    public async Task<(bool Success, string Message, double Confidence)> CheckOut(int employeeId, string imageBase64)
    {
        var today = DateTime.Today;

        var attendance = await _context.FaceAttendances
            .FirstOrDefaultAsync(x =>
                x.EmployeeId == employeeId &&
                x.CheckInTime.HasValue &&
                x.CheckInTime.Value.Date == today);

        if (attendance == null)
            return (false, "Chưa check-in", 0);

        if (attendance.CheckOutTime != null)
            return (false, "Đã check-out rồi", 0);

        double confidence = FakeFaceMatch(imageBase64);

        if (confidence < 0.7)
            return (false, "Khuôn mặt không khớp", confidence);

        attendance.CheckOutTime = DateTime.Now;
        attendance.Confidence = confidence;

        await _context.SaveChangesAsync();

        return (true, "Check-out thành công", confidence);
    }

    // =============================
    // HISTORY
    // =============================
    public async Task<object> GetHistory(int employeeId)
    {
        return await _context.FaceAttendances
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.CreatedDate)
            .Select(x => new
            {
                x.Id,
                x.CheckInTime,
                x.CheckOutTime,
                x.Confidence,
                x.CreatedDate
            })
            .ToListAsync();
    }

    // =============================
    // MOCK AI
    // =============================
    private double FakeFaceMatch(string base64)
    {
        return new Random().NextDouble() * (0.95 - 0.7) + 0.7;
    }
}