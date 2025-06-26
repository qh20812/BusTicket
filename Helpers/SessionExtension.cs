using BusTicketSystem.Models;

namespace BusTicketSystem.Helpers
{
    public static class SessionExtension
    {
        public static int? GetUserId(this ISession session)
        {
            return session.GetInt32("UserId");
        }
        public static void SetUserId(this ISession session, int UserId)
        {
            session.SetInt32("UserId", UserId);
        }
        public static string GetUserRole(this ISession session)
        {
            return session.GetString("UserRole") ?? string.Empty;
        }
        public static void SetUserRole(this ISession session, string Role)
        {
            session.SetString("UserRole", Role);
        }
        public static void ClearUser(this ISession session)
        {
            session.Remove("UserId");
            session.Remove("UserRole");
            session.Remove("Username");
        }
    }
}