using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace HoTroDuLich.Web.Services;

// Actor membership is explicit: ACT03 does not inherit ACT01/ACT02 permissions.
// A participating actor only gets its own operations; controller/action and object
// checks further restrict shared use cases (07.01, 08.01, 09.01, 10.02, 13.01).
public static class SucAccess
{
    public const string TravelSearch = "SUC01.01";
    public const string TripPlan = "SUC02.01";
    public const string CompanionSearch = "SUC03.01";
    public const string CompanionPost = "SUC03.02";
    public const string CompanionConnection = "SUC04.01";
    public const string CompanionAgreement = "SUC04.02";
    public const string Incident = "SUC05.01";
    public const string ServiceManagement = "SUC06.01";
    public const string ServiceReview = "SUC06.02";
    public const string Booking = "SUC07.01";
    public const string BookingChange = "SUC08.01";
    public const string ServiceResult = "SUC09.01";
    public const string Registration = "SUC10.01";
    public const string ProviderVerification = "SUC10.02";
    public const string Login = "SUC11.01";
    public const string Profile = "SUC12.01";
    public const string ProviderRights = "SUC13.01";
    public const string Deactivation = "SUC14.01";

    public sealed record Rule(string[] Actors, bool PublicRead = false, bool AnonymousOnly = false);
    private const string T = TravelService.Traveler, P = TravelService.ProviderRole, A = TravelService.AdminRole;
    public static readonly IReadOnlyDictionary<string, Rule> Rules = new Dictionary<string, Rule>
    {
        [TravelSearch] = new([T], PublicRead: true),
        [TripPlan] = new([T]),
        // Chapter 4: public reads include authenticated users; companion projection
        // treats provider/admin viewers as anonymous without traveler write rights.
        [CompanionSearch] = new([T], PublicRead: true),
        [CompanionPost] = new([T]),
        [CompanionConnection] = new([T]),
        [CompanionAgreement] = new([T]),
        [Incident] = new([T]),
        [ServiceManagement] = new([P]),
        [ServiceReview] = new([A]),
        [Booking] = new([T, P]),
        [BookingChange] = new([T, P]),
        [ServiceResult] = new([T, P]),
        [Registration] = new([T, P], AnonymousOnly: true),
        [ProviderVerification] = new([A, P]),
        [Login] = new([T, P, A], AnonymousOnly: true),
        [Profile] = new([T, P, A]),
        [ProviderRights] = new([A, P]),
        [Deactivation] = new([T, P])
    };

    public static bool Allows(ClaimsPrincipal user, string useCase)
    {
        if (!Rules.TryGetValue(useCase, out var rule)) return false;
        if (user.Identity?.IsAuthenticated != true) return rule.PublicRead || rule.AnonymousOnly;
        return rule.PublicRead || (!rule.AnonymousOnly && rule.Actors.Contains(user.FindFirstValue(ClaimTypes.Role)));
    }

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy("LaKhachDuLich",p=>p.RequireRole(T));
        options.AddPolicy("LaNhaCungCap",p=>p.RequireRole(P));
        options.AddPolicy("LaQuanTriVien",p=>p.RequireRole(A));
        options.AddPolicy("DaXacThuc",p=>p.RequireAuthenticatedUser());
        options.AddPolicy("LaKhachHoacNCC",p=>p.RequireRole(T,P));
        foreach (var useCase in Rules.Keys)
        {
            var key = useCase;
            options.AddPolicy(key, p => p.RequireAssertion(c => Allows(c.User, key)));
        }
    }

    public static string Home(string role) => role switch
    {
        P => "/nha-cung-cap/dich-vu",
        A => "/quan-tri/xet-duyet-dich-vu/mo-danh-sach-cho-duyet",
        T => "/ke-hoach",
        _ => "/"
    };
}

public sealed class SucAuthorizeAttribute(string useCase) : AuthorizeAttribute(useCase);
