namespace PharmacyAPI.DTOs
{
    public class SalesReportDTO
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<DailySalesDTO> DailySales { get; set; } = new();
    }

    public class DailySalesDTO
    {
        public DateTime Date { get; set; }
        public decimal TotalSales { get; set; }
        public int Count { get; set; }
    }
}