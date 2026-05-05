using LotusTeam.Models;

namespace LotusTeam.Service
{
    public interface IRecruitmentService
    {
        // ================= ỨNG VIÊN =================
        Task<List<Candidates>> GetAllCandidatesAsync();
        Task<Candidates> CreateCandidateAsync(Candidates candidate);

        // ================= WORKFLOW TUYỂN DỤNG (TEMPLATE) =================
        Task<Workflows?> GetRecruitmentWorkflowTemplateAsync();

        // ================= CẬP NHẬT TRẠNG THÁI =================
        Task AdvanceCandidateStageAsync(int candidateId, short nextStatusId);

        // ================= ĐÁNH GIÁ =================
        Task<PerformanceReview> ReviewCandidateAsync(PerformanceReview review);

        // ================= TUYỂN THÀNH NHÂN VIÊN =================
        Task<Employees> ConvertToEmployeeAsync(int candidateId);
    }
}
