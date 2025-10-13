
public class CsvImportResultDto { public string Message { get; set; } = ""; public int Rows { get; set; } }
public class SupplierSaveResultDto { public int ReceivedId { get; set; } public string Message { get; set; } = ""; }
public class SupplierDataDto
{
    public int Sup_id { get; set; }
    public string Sup_Name { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public int Postal_code { get; set; }
    public string Country { get; set; } = "";
    public string Tax_id { get; set; } = "";
    public string Error_msg { get; set; } = "";
}
public class MessageDto { public string Message { get; set; } = ""; }
public class ErrorDto { public string Error { get; set; } = ""; }