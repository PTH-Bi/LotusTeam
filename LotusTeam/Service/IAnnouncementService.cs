using LotusTeam.DTOs;

namespace LotusTeam.Service
{
    public interface IAnnouncementService
    {
        Task<List<AnnouncementDto>> GetAllAsync();
        Task<AnnouncementDto?> GetByIdAsync(int id);
        Task<AnnouncementDto> CreateAsync(AnnouncementCreateDto dto);
        Task<AnnouncementDto?> UpdateAsync(int id, AnnouncementUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}