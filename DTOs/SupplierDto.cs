namespace backend.DTOs
{
    public class SupplierDto
    {
        public int Supplier_ID { get; set; }
        public string Supplier_Name { get; set; } = null!;
        public string Contact_Name { get; set; } = null!;
        public string Contact_Phone { get; set; } = null!;
        public string Contact_Email { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string City { get; set; } = null!;
        public string State { get; set; } = null!;
        public string Postal_Code { get; set; } = null!;
        public string Country { get; set; } = null!;
        public string Tax_Identification { get; set; } = null!;
    }
}