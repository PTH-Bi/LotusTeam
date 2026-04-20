using LotusTeam.Data;
using Microsoft.EntityFrameworkCore;
using LotusTeam.Models;

namespace LotusTeam.Service
{
    public class ChatbotService
    {
        private readonly AIService _ai;
        private readonly HRQueryService _hr;
        private readonly AppDbContext _context;

        public ChatbotService(
            AIService ai,
            HRQueryService hr,
            AppDbContext context)
        {
            _ai = ai;
            _hr = hr;
            _context = context;
        }

        public async Task<string> Handle(string message, int userId)
        {
            string result = "";

            try
            {
                var aiRes = await _ai.CallFunction(message);

                if (aiRes.ToolName == null)
                {
                    result = aiRes.Content ?? "Không hiểu câu hỏi.";
                }
                else
                {
                    result = aiRes.ToolName switch
                    {
                        "get_leave" => await HandleLeave(userId),
                        "get_salary" => await HandleSalary(userId),
                        "get_attendance" => await HandleAttendance(userId),
                        _ => "Không xử lý được yêu cầu."
                    };
                }
            }
            catch (Exception ex)
            {
                result = "Lỗi hệ thống: " + ex.Message;
            }

            // ===== LƯU CHAT LOG =====
            try
            {
                await _context.ChatLogs.AddAsync(new ChatLogs
                {
                    EmployeeId = userId,
                    Message = message,
                    Response = result,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }
            catch
            {
                // không làm crash nếu log lỗi
            }

            return result;
        }

        // ================= HANDLE FUNCTIONS =================

        private async Task<string> HandleLeave(int userId)
        {
            var leave = await _hr.GetLeave(userId);
            return $"Bạn còn {leave} ngày phép.";
        }

        private async Task<string> HandleSalary(int userId)
        {
            var salary = await _hr.GetSalary(userId);
            return $"Lương gần nhất của bạn là {salary:N0} VNĐ.";
        }

        private async Task<string> HandleAttendance(int userId)
        {
            var attendance = await _hr.GetAttendance(userId);
            return $"Bạn đã đi làm {attendance} ngày.";
        }
    }
}