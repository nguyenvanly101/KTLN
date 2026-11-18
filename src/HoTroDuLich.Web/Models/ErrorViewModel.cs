namespace HoTroDuLich.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public int Status { get; set; } = 500;
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
