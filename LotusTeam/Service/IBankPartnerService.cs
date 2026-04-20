using LotusTeam.DTOs;

public interface IBankPartnerService
{
    Task<List<BankPartnerDto>> GetAllAsync();

    Task<BankPartnerDto?> GetByIdAsync(int id);

    Task<BankPartnerDto> CreateAsync(CreateBankPartnerDto dto);
}