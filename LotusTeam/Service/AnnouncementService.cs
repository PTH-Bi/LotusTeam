using LotusTeam.Data;
using LotusTeam.DTOs;
using LotusTeam.Models;
using Microsoft.EntityFrameworkCore;

namespace LotusTeam.Service
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly AppDbContext _context;

        public AnnouncementService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<AnnouncementDto>> GetAllAsync()
        {
            return await _context.InternalAnnouncements
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.PublishDate)
                .Select(a => new AnnouncementDto
                {
                    AnnouncementId = a.AnnouncementId,
                    Title = a.Title,
                    Content = a.Content,
                    AuthorId = a.AuthorId,
                    Category = a.Category,
                    IsPinned = a.IsPinned,
                    PublishDate = a.PublishDate,
                    ExpireDate = a.ExpireDate,
                    IsActive = a.IsActive
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<AnnouncementDto?> GetByIdAsync(int id)
        {
            return await _context.InternalAnnouncements
                .Where(a => a.AnnouncementId == id)
                .Select(a => new AnnouncementDto
                {
                    AnnouncementId = a.AnnouncementId,
                    Title = a.Title,
                    Content = a.Content,
                    AuthorId = a.AuthorId,
                    Category = a.Category,
                    IsPinned = a.IsPinned,
                    PublishDate = a.PublishDate,
                    ExpireDate = a.ExpireDate,
                    IsActive = a.IsActive
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<AnnouncementDto> CreateAsync(AnnouncementCreateDto dto)
        {
            if (dto.AuthorId.HasValue)
            {
                var exists = await _context.Employees
                    .AnyAsync(e => e.EmployeeID == dto.AuthorId.Value);

                if (!exists)
                    throw new Exception("Author không tồn tại.");
            }

            var announcement = new InternalAnnouncement
            {
                Title = dto.Title,
                Content = dto.Content,
                AuthorId = dto.AuthorId,
                Category = dto.Category,
                IsPinned = dto.IsPinned,
                ExpireDate = dto.ExpireDate,
                IsActive = dto.IsActive,
                PublishDate = DateTime.Now
            };

            _context.InternalAnnouncements.Add(announcement);
            await _context.SaveChangesAsync();

            return new AnnouncementDto
            {
                AnnouncementId = announcement.AnnouncementId,
                Title = announcement.Title,
                Content = announcement.Content,
                AuthorId = announcement.AuthorId,
                Category = announcement.Category,
                IsPinned = announcement.IsPinned,
                PublishDate = announcement.PublishDate,
                ExpireDate = announcement.ExpireDate,
                IsActive = announcement.IsActive
            };
        }

        public async Task<AnnouncementDto?> UpdateAsync(int id, AnnouncementUpdateDto dto)
        {
            var announcement = await _context.InternalAnnouncements
                .FirstOrDefaultAsync(a => a.AnnouncementId == id);

            if (announcement == null)
                return null;

            // Update fields
            announcement.Title = dto.Title;
            announcement.Content = dto.Content;
            announcement.Category = dto.Category;
            announcement.IsPinned = dto.IsPinned;
            announcement.ExpireDate = dto.ExpireDate;
            announcement.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return new AnnouncementDto
            {
                AnnouncementId = announcement.AnnouncementId,
                Title = announcement.Title,
                Content = announcement.Content,
                AuthorId = announcement.AuthorId,
                Category = announcement.Category,
                IsPinned = announcement.IsPinned,
                PublishDate = announcement.PublishDate,
                ExpireDate = announcement.ExpireDate,
                IsActive = announcement.IsActive
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var announcement = await _context.InternalAnnouncements
                .FirstOrDefaultAsync(a => a.AnnouncementId == id);

            if (announcement == null)
                return false;

            // 🔥 Khuyến nghị: Soft delete (an toàn hơn)
            announcement.IsActive = false;

            // ❌ Nếu muốn xóa cứng thì dùng:
            // _context.InternalAnnouncements.Remove(announcement);

            await _context.SaveChangesAsync();
            return true;
        }
    }
}