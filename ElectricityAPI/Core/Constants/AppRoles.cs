namespace Core.Entities
{
	public static class AppRoles
	{
		public const string User = "User";
		public const string AuthorizedUser = "AuthorizedUser";
		public const string Admin = "Admin";

		public const string UserAndAbove = User + "," + AuthorizedUser + "," + Admin;
		public const string AuthorizedUserAndAbove = AuthorizedUser + "," + Admin;
	}
}
