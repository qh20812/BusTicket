using System.Security.Claims;
using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusTicketSystem.Pages.ForPartner
{
    public abstract class PartnerBasePageModel : PageModel
    {
        protected readonly AppDbContext _context;
        protected readonly IHttpContextAccessor _httpContextAccessor;
        public int? PartnerCompanyId { get; private set; }
        public string? PartnerCompanyName { get; private set; }
        protected PartnerBasePageModel(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            InitializePartnerInfo();
        }
        private void InitializePartnerInfo()
        {
            var companyIdClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue("CompanyId");
            if (int.TryParse(companyIdClaim, out int companyId))
            {
                PartnerCompanyId = companyId;
            }
        }
        protected async Task<User?> GetCurrentUserAsync()
        {
            if (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false)
            {
                var userIdClaim = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdClaim, out int userId))
                {
                    return await _context.Users.FindAsync(userId);
                }
            }
            return null;
        }
    }
}