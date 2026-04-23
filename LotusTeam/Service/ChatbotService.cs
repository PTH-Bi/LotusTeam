using LotusTeam.Data;
using Microsoft.EntityFrameworkCore;
using LotusTeam.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace LotusTeam.Service
{
    /// <summary>
    /// Service xử lý logic chatbot - hỗ trợ nhân viên và quản lý
    /// </summary>
    public class ChatbotService
    {
        private readonly AIService _ai;
        private readonly HRQueryService _hr;
        private readonly AppDbContext _context;
        private readonly ILogger<ChatbotService> _logger;

        public ChatbotService(
            AIService ai,
            HRQueryService hr,
            AppDbContext context,
            ILogger<ChatbotService> logger)
        {
            _ai = ai;
            _hr = hr;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Xử lý tin nhắn từ người dùng và trả về phản hồi
        /// </summary>
        public async Task<ChatResponse> Handle(string message, int userId, string userRole)
        {
            var response = new ChatResponse
            {
                ConversationId = Guid.NewGuid().ToString(),
                MessageId = Guid.NewGuid().ToString(),
                NeedHumanSupport = false
            };

            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    response.Reply = "Vui lòng nhập câu hỏi để tôi có thể hỗ trợ bạn.";
                    response.Suggestions = GetDefaultSuggestions(userRole);
                    await SaveChatLog(userId, message, response.Reply);
                    return response;
                }

                var user = await _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Position)
                    .FirstOrDefaultAsync(e => e.EmployeeID == userId);

                var userName = user?.FullName ?? "Nhân viên";
                var departmentName = user?.Department?.DepartmentName ?? "";

                var aiRes = await _ai.CallFunction(message);

                if (aiRes.ToolName == null)
                {
                    response.Reply = aiRes.Content ?? await GenerateGenericResponse(message, userRole);
                    response.Suggestions = GetRelatedSuggestions(message);
                }
                else
                {
                    var result = await ExecuteTool(aiRes.ToolName, userId, userRole, message);
                    response.Reply = result.Reply;
                    response.Suggestions = result.Suggestions;
                    response.NeedHumanSupport = result.NeedHumanSupport;
                    response.ReferenceLink = result.ReferenceLink;
                }

                if (!string.IsNullOrEmpty(userName) && response.Reply.Contains("bạn"))
                {
                    response.Reply = response.Reply.Replace("bạn", $"anh/chị {userName}");
                }

                await SaveChatLog(userId, message, response.Reply);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling message from user {UserId}", userId);
                response.Reply = "Xin lỗi, hệ thống đang gặp sự cố. Vui lòng thử lại sau hoặc liên hệ HR để được hỗ trợ.";
                response.NeedHumanSupport = true;
            }

            return response;
        }

        /// <summary>
        /// Thực thi tool dựa trên intent từ AI
        /// </summary>
        private async Task<ChatResponse> ExecuteTool(string toolName, int userId, string userRole, string originalMessage)
        {
            var response = new ChatResponse();

            switch (toolName)
            {
                case "get_leave":
                    var leaveResult = await HandleLeave(userId, userRole);
                    response.Reply = leaveResult.Reply;
                    response.Suggestions = new List<string>
                    {
                        "Cách tính ngày phép?",
                        "Gửi đơn xin nghỉ phép",
                        "Lịch nghỉ lễ trong năm"
                    };
                    break;

                case "get_salary":
                    var salaryResult = await HandleSalary(userId, userRole);
                    response.Reply = salaryResult.Reply;
                    response.Suggestions = new List<string>
                    {
                        "Bảng lương tháng này",
                        "Cách tính thuế TNCN",
                        "Chính sách thưởng Tết"
                    };
                    break;

                case "get_attendance":
                    var attendanceResult = await HandleAttendance(userId, userRole);
                    response.Reply = attendanceResult.Reply;
                    response.Suggestions = new List<string>
                    {
                        "Chấm công hôm nay",
                        "Báo cáo công tháng này",
                        "Quy định đi muộn"
                    };
                    break;

                case "get_contract":
                    var contractResult = await HandleContract(userId, userRole);
                    response.Reply = contractResult.Reply;
                    response.Suggestions = new List<string>
                    {
                        "Thông tin hợp đồng",
                        "Gia hạn hợp đồng",
                        "Phụ lục hợp đồng"
                    };
                    break;

                case "get_benefit":
                    var benefitResult = await HandleBenefit(userId, userRole);
                    response.Reply = benefitResult.Reply;
                    response.Suggestions = new List<string>
                    {
                        "Chính sách bảo hiểm",
                        "Phúc lợi nhân viên",
                        "Đăng ký khám sức khỏe"
                    };
                    break;

                case "get_employee_info":
                    var employeeResult = await HandleEmployeeInfo(userId, userRole, originalMessage);
                    response.Reply = employeeResult.Reply;
                    response.Suggestions = new List<string>
                    {
                        "Thông tin phòng ban",
                        "Danh sách nhân viên",
                        "Cơ cấu tổ chức"
                    };
                    break;

                case "request_leave":
                    response.Reply = await HandleLeaveRequest(userId, originalMessage);
                    response.Suggestions = new List<string>
                    {
                        "Kiểm tra trạng thái đơn",
                        "Hủy đơn xin nghỉ",
                        "Lịch nghỉ của team"
                    };
                    break;

                default:
                    response.Reply = "Tôi chưa được huấn luyện để xử lý yêu cầu này. Vui lòng liên hệ HR để được hỗ trợ.";
                    response.NeedHumanSupport = true;
                    break;
            }

            return response;
        }

        #region Handle Functions

        private async Task<ChatResponse> HandleLeave(int userId, string userRole)
        {
            var response = new ChatResponse();

            try
            {
                var leave = await _hr.GetLeave(userId);

                if (userRole == "MANAGER" || userRole == "TEAM_LEADER" || userRole == "HR_MANAGER")
                {
                    var teamLeave = await _hr.GetTeamLeaveSummary(userId);
                    response.Reply = $"Bạn còn {leave} ngày phép năm nay.\n\n" +
                                     $"📊 *Thống kê phép của team:*\n" +
                                     $"- Đã nghỉ: {teamLeave.Used} ngày\n" +
                                     $"- Chờ duyệt: {teamLeave.Pending} ngày\n" +
                                     $"- Sắp nghỉ: {teamLeave.Upcoming} ngày";
                }
                else
                {
                    response.Reply = $"✅ Bạn còn {leave} ngày phép năm nay.\n\n" +
                                     $"💡 *Gợi ý:* Bạn có thể gửi đơn xin nghỉ phép qua menu 'Nghỉ phép' để được duyệt.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave for user {UserId}", userId);
                response.Reply = "Xin lỗi, tôi không thể lấy thông tin ngày phép của bạn lúc này. Vui lòng liên hệ HR.";
            }

            return response;
        }

        private async Task<ChatResponse> HandleSalary(int userId, string userRole)
        {
            var response = new ChatResponse();

            try
            {
                var salary = await _hr.GetSalary(userId);

                if (userRole == "ACCOUNTANT" || userRole == "FINANCE_MANAGER")
                {
                    response.Reply = $"💰 Lương tháng gần nhất: {salary:N0} VNĐ\n\n" +
                                     $"*Lưu ý:* Đây là lương thực nhận sau thuế và bảo hiểm.";
                    response.ReferenceLink = "/reports/salary-detail";
                }
                else
                {
                    response.Reply = $"💰 Lương tháng gần nhất của bạn là {salary:N0} VNĐ.\n\n" +
                                     $"Để xem chi tiết bảng lương, vui lòng vào mục 'Lương' trên hệ thống.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting salary for user {UserId}", userId);
                response.Reply = "Xin lỗi, tôi không thể lấy thông tin lương lúc này. Vui lòng liên hệ phòng Kế toán.";
            }

            return response;
        }

        private async Task<ChatResponse> HandleAttendance(int userId, string userRole)
        {
            var response = new ChatResponse();

            try
            {
                var attendance = await _hr.GetAttendance(userId);

                response.Reply = $"📊 Bạn đã đi làm {attendance} ngày trong tháng này.\n\n" +
                                 $"🎯 *Mục tiêu tháng:* {attendance}/22 ngày công.\n\n" +
                                 $"⚠️ *Lưu ý:* Nếu có sai sót về chấm công, vui lòng báo HR trước ngày 25 hàng tháng.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance for user {UserId}", userId);
                response.Reply = "Xin lỗi, tôi không thể lấy thông tin chấm công lúc này.";
            }

            return response;
        }

        private async Task<ChatResponse> HandleContract(int userId, string userRole)
        {
            var response = new ChatResponse();

            try
            {
                var contract = await _context.Contracts
                    .Include(c => c.ContractType)
                    .Where(c => c.EmployeeID == userId)
                    .OrderByDescending(c => c.StartDate)
                    .FirstOrDefaultAsync();

                if (contract == null)
                {
                    response.Reply = "Hiện tại bạn chưa có hợp đồng nào trong hệ thống. Vui lòng liên hệ HR.";
                    return response;
                }

                var status = GetContractStatus(contract);
                var endDateText = contract.EndDate.HasValue
                    ? $"đến ngày {contract.EndDate:dd/MM/yyyy}"
                    : "không thời hạn";

                response.Reply = $"📄 *Thông tin hợp đồng hiện tại:*\n" +
                                 $"- Loại hợp đồng: {contract.ContractType?.ContractTypeName}\n" +
                                 $"- Mã hợp đồng: {contract.ContractCode}\n" +
                                 $"- Thời hạn: Từ {contract.StartDate:dd/MM/yyyy} {endDateText}\n" +
                                 $"- Trạng thái: {status}\n" +
                                 $"- Lương cơ bản: {contract.Salary:N0} VNĐ";

                if (status == "Sắp hết hạn")
                {
                    response.Reply += $"\n\n⚠️ *Cảnh báo:* Hợp đồng của bạn sắp hết hạn. Vui lòng liên hệ HR để gia hạn.";
                    response.NeedHumanSupport = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contract for user {UserId}", userId);
                response.Reply = "Xin lỗi, tôi không thể lấy thông tin hợp đồng lúc này.";
            }

            return response;
        }

        private async Task<ChatResponse> HandleBenefit(int userId, string userRole)
        {
            var response = new ChatResponse();

            try
            {
                var benefits = await _context.Benefits
                    .Where(b => b.EmployeeID == userId)
                    .ToListAsync();

                if (!benefits.Any())
                {
                    response.Reply = "Hiện tại bạn chưa đăng ký phúc lợi nào. Hãy liên hệ HR để được tư vấn các gói phúc lợi.";
                    return response;
                }

                var benefitText = string.Join("\n", benefits.Select(b =>
                    $"- {b.InsuranceType}: {b.InsuranceNumber ?? "Đã đăng ký"}"));

                response.Reply = $"🎁 *Phúc lợi của bạn:*\n{benefitText}\n\n" +
                                 $"📞 Cần hỗ trợ thêm về phúc lợi? Liên hệ HR qua số nội bộ 1234.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting benefits for user {UserId}", userId);
                response.Reply = "Xin lỗi, tôi không thể lấy thông tin phúc lợi lúc này.";
            }

            return response;
        }

        private async Task<ChatResponse> HandleEmployeeInfo(int userId, string userRole, string message)
        {
            var response = new ChatResponse();

            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };
            if (!allowedRoles.Contains(userRole))
            {
                response.Reply = "Bạn không có quyền xem thông tin của nhân viên khác. Vui lòng liên hệ quản lý nếu cần hỗ trợ.";
                return response;
            }

            try
            {
                var employeeInfo = await _hr.SearchEmployee(message);

                if (employeeInfo == null)
                {
                    response.Reply = "Không tìm thấy thông tin nhân viên phù hợp. Vui lòng cung cấp mã nhân viên hoặc họ tên chính xác.";
                    return response;
                }

                response.Reply = $"👤 *Thông tin nhân viên:*\n" +
                                 $"- Họ tên: {employeeInfo.FullName}\n" +
                                 $"- Mã NV: {employeeInfo.EmployeeCode}\n" +
                                 $"- Phòng ban: {employeeInfo.DepartmentName}\n" +
                                 $"- Chức vụ: {employeeInfo.PositionName}\n" +
                                 $"- Ngày vào làm: {employeeInfo.JoinDate:dd/MM/yyyy}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching employee info");
                response.Reply = "Xin lỗi, tôi không thể tìm kiếm thông tin nhân viên lúc này.";
            }

            return response;
        }

        private async Task<string> HandleLeaveRequest(int userId, string message)
        {
            try
            {
                var dates = ExtractDates(message);

                if (dates.StartDate == default)
                {
                    return "Vui lòng cung cấp ngày bắt đầu nghỉ. Ví dụ: 'Tôi muốn nghỉ từ ngày 20/12/2024'";
                }

                var leaveRequest = new LeaveRequest
                {
                    EmployeeID = userId,
                    StartDate = dates.StartDate,
                    EndDate = dates.EndDate ?? dates.StartDate,
                    Reason = "Nghỉ phép (tạo từ chatbot)",
                    StatusID = 1,  // Pending
                    NumberOfDays = (decimal)((dates.EndDate ?? dates.StartDate) - dates.StartDate).TotalDays + 1
                };

                _context.LeaveRequests.Add(leaveRequest);
                await _context.SaveChangesAsync();

                return $"✅ Đã tạo đơn xin nghỉ phép từ ngày {dates.StartDate:dd/MM/yyyy} " +
                       $"{(dates.EndDate.HasValue ? $"đến {dates.EndDate.Value:dd/MM/yyyy}" : "")}.\n" +
                       $"Đơn đang chờ quản lý duyệt. Bạn có thể theo dõi trạng thái trong mục 'Nghỉ phép'.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating leave request");
                return "Xin lỗi, tôi không thể tạo đơn xin nghỉ phép lúc này. Vui lòng thử lại hoặc tạo đơn trực tiếp trên hệ thống.";
            }
        }

        #endregion

        #region Helper Methods

        private async Task<string> GenerateGenericResponse(string message, string userRole)
        {
            var greetings = new[] { "chào", "hello", "hi", "xin chào", "chào bạn" };
            if (greetings.Any(g => message.ToLower().Contains(g)))
            {
                return $"Xin chào! Tôi là trợ lý HR Bot. Tôi có thể giúp bạn tra cứu:\n" +
                       $"📅 Ngày phép còn lại\n" +
                       $"💰 Thông tin lương\n" +
                       $"📊 Chấm công\n" +
                       $"📄 Hợp đồng & phúc lợi\n\n" +
                       $"Bạn cần tôi hỗ trợ gì hôm nay?";
            }

            if (message.ToLower().Contains("help") || message.ToLower().Contains("giúp"))
            {
                return GetHelpText(userRole);
            }

            return "Tôi chưa hiểu rõ câu hỏi của bạn. Bạn có thể thử:\n" +
                   "- 'Tôi còn bao nhiêu ngày phép?'\n" +
                   "- 'Lương tháng này của tôi là bao nhiêu?'\n" +
                   "- 'Tôi đã đi làm bao nhiêu ngày?'\n" +
                   "- 'Thông tin hợp đồng của tôi'";
        }

        private List<string> GetDefaultSuggestions(string userRole)
        {
            var suggestions = new List<string>
            {
                "Số ngày phép còn lại?",
                "Lương tháng này?",
                "Chấm công tháng này?",
                "Thông tin hợp đồng"
            };

            if (userRole == "MANAGER" || userRole == "TEAM_LEADER" || userRole == "HR_MANAGER")
            {
                suggestions.AddRange(new[]
                {
                    "Thống kê phép của team",
                    "Tìm nhân viên"
                });
            }

            return suggestions;
        }

        private List<string> GetRelatedSuggestions(string message)
        {
            var suggestions = new List<string>();
            var lowerMsg = message.ToLower();

            if (lowerMsg.Contains("phép") || lowerMsg.Contains("nghỉ"))
            {
                suggestions.Add("Quy định nghỉ phép");
                suggestions.Add("Nghỉ không lương");
                suggestions.Add("Nghỉ ốm đau");
            }
            else if (lowerMsg.Contains("lương") || lowerMsg.Contains("thu nhập"))
            {
                suggestions.Add("Cách tính lương");
                suggestions.Add("Bảng lương chi tiết");
                suggestions.Add("Thuế TNCN");
            }
            else if (lowerMsg.Contains("bảo hiểm"))
            {
                suggestions.Add("Mức đóng bảo hiểm");
                suggestions.Add("Quyền lợi bảo hiểm");
                suggestions.Add("Thủ tục thanh toán bảo hiểm");
            }

            return suggestions;
        }

        private string GetHelpText(string userRole)
        {
            var help = "📖 *Hướng dẫn sử dụng HR Bot*\n\n" +
                       "*Các câu hỏi có thể hỏi:*\n" +
                       "1️⃣ Hỏi về ngày phép\n" +
                       "   - 'Tôi còn bao nhiêu ngày phép?'\n" +
                       "   - 'Năm nay tôi được bao nhiêu ngày phép?'\n\n" +
                       "2️⃣ Hỏi về lương\n" +
                       "   - 'Lương tháng 12 của tôi là bao nhiêu?'\n" +
                       "   - 'Bảng lương tháng này'\n\n" +
                       "3️⃣ Hỏi về chấm công\n" +
                       "   - 'Tháng này tôi đi làm bao nhiêu ngày?'\n" +
                       "   - 'Hôm nay tôi có đi muộn không?'\n\n" +
                       "4️⃣ Hỏi về hợp đồng\n" +
                       "   - 'Xem thông tin hợp đồng của tôi'\n" +
                       "   - 'Hợp đồng của tôi đến khi nào?'";

            if (userRole == "MANAGER" || userRole == "TEAM_LEADER")
            {
                help += "\n\n*Quản lý có thể hỏi thêm:*\n" +
                        "   - 'Team tôi còn bao nhiêu ngày phép?'\n" +
                        "   - 'Tìm nhân viên Nguyễn Văn A'\n" +
                        "   - 'Danh sách nhân viên nghỉ hôm nay'";
            }

            return help;
        }

        private (DateTime StartDate, DateTime? EndDate) ExtractDates(string message)
        {
            var today = DateTime.Today;
            var startDate = today;
            DateTime? endDate = null;

            var dateRegex = new Regex(@"(\d{1,2})[/\-](\d{1,2})[/\-](\d{4})");
            var matches = dateRegex.Matches(message);

            if (matches.Count >= 1)
            {
                if (DateTime.TryParse(matches[0].Value, out var parsedStart))
                    startDate = parsedStart;
            }

            if (matches.Count >= 2)
            {
                if (DateTime.TryParse(matches[1].Value, out var parsedEnd))
                    endDate = parsedEnd;
            }

            return (startDate, endDate);
        }

        private string GetContractStatus(Contract contract)
        {
            var today = DateTime.Today;

            if (contract.StartDate > today)
                return "Sắp hiệu lực";

            if (contract.EndDate.HasValue)
            {
                if (contract.EndDate.Value < today)
                    return "Đã hết hạn";

                if (contract.EndDate.Value.AddMonths(-1) <= today)
                    return "Sắp hết hạn";
            }

            return "Đang hiệu lực";
        }

        private async Task SaveChatLog(int userId, string message, string response)
        {
            try
            {
                await _context.ChatLogs.AddAsync(new ChatLogs
                {
                    EmployeeId = userId,
                    Message = message,
                    Response = response,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save chat log for user {UserId}", userId);
            }
        }

        public async Task<List<FaqItem>> GetFaqsAsync()
        {
            return await _context.Set<Faq>()
                .Where(f => f.IsActive)
                .OrderBy(f => f.Category)
                .ThenBy(f => f.Order)
                .Select(f => new FaqItem
                {
                    Id = f.Id,
                    Question = f.Question,
                    Answer = f.Answer,
                    Category = f.Category
                })
                .ToListAsync();
        }

        public async Task SaveFeedbackAsync(string messageId, int userId, bool isHelpful, string? comment)
        {
            var feedback = new ChatFeedback
            {
                MessageId = messageId,
                UserId = userId,
                IsHelpful = isHelpful,
                Comment = comment,
                CreatedAt = DateTime.Now
            };

            await _context.Set<ChatFeedback>().AddAsync(feedback);
            await _context.SaveChangesAsync();
        }

        #endregion
    }

    #region Supporting Models

    public class ChatResponse
    {
        public string ConversationId { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public string Reply { get; set; } = string.Empty;
        public List<string> Suggestions { get; set; } = new();
        public bool NeedHumanSupport { get; set; }
        public string? ReferenceLink { get; set; }
    }

    public class FaqItem
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    #endregion
}