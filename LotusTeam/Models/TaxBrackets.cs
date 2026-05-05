namespace LotusTeam.Models
{
    public class TaxBracket
    {
        public int TaxBracketID { get; set; }
        public decimal FromAmount { get; set; }
        public decimal? ToAmount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal DeductionAmount { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; }
    }

}
