namespace BomEtlAddin.Models
{
    public class BomRow
    {
        public string ItemNumber { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Configuration { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
