namespace BLL.Services
{
    public static class KyivTimeHelper
    {
        private const string KyivTimeZoneId = "Europe/Kyiv";

        public static DateTime Now => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, KyivTimeZoneId);

        public static DateTime Today => Now.Date;
    }
}
