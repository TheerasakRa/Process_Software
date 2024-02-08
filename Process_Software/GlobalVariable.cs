namespace Process_Software
{
    public static class GlobalVariable
    {
        public static int UserID { get; private set; }
        public static string? UserEmail { get; private set; }

        public static void SetUserID(int id) => UserID = id;

        public static int GetUserID() => UserID;

        public static void SetUserEmail(string email) => UserEmail = email;

        public static string? GetUserEmail() => UserEmail;
        public static void ClearGlobalVariable()
        {
            UserID = default(int);
            UserEmail = null;
        }

    }
}