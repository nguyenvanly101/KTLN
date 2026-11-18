namespace HoTroDuLich.Web.Models;

/// <summary>Mot lua chon trong form quyet dinh cua quan tri.</summary>
public sealed record DecisionOption(string Value, string Title, string Hint, string TextLabel, bool TextRequired, string Placeholder, bool Danger = false);

/// <summary>Form quyet dinh dung chung: chon quyet dinh truoc, mot o noi dung doi nhan / bat buoc theo quyet dinh.</summary>
public sealed record DecisionFormModel(string Action, IReadOnlyDictionary<string, string> Hidden, IReadOnlyList<DecisionOption> Options, string TextName = "Reason", string Help = "Bên còn lại sẽ thấy nội dung này. Tối đa 500 ký tự.")
{
    public static DecisionFormModel Review(string action, IReadOnlyDictionary<string, string> hidden, string approve, string approveTitle, string approveHint, string changes, string changesTitle, string changesHint, string reject, string rejectHint) =>
        new(action, hidden, [
            new(approve, approveTitle, approveHint, "Ghi chú (không bắt buộc)", false, "Ví dụ: Thông tin đầy đủ, khớp với giấy tờ."),
            new(changes, changesTitle, changesHint, "Cần sửa hoặc bổ sung gì", true, "Ví dụ: Ảnh mờ, ghi lại giờ mở cửa cho đúng."),
            new(reject, "Từ chối", rejectHint, "Lý do từ chối", true, "Ví dụ: Thông tin không khớp với thực tế.", true)
        ]);
}
