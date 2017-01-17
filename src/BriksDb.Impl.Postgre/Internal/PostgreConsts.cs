namespace Qoollo.Impl.Postgre.Internal
{
    internal static class PostgreConsts
    {
        public static string OrderBy = "order by";
        public static string From = "from";
        public static string Where = "where";
        public static string Select = "select";
        public static string All = "*";
        public static string Asc = "asc";
        public static string Desc = "desc";
        public static string Declare = "declare";
        public static string With = "with";
        public static string As = "as";

        public static string Local = "Meta_Local";
        public static string IsDeleted = "Meta_IsDeleted";
        public static string DeleteTime = "Meta_DeleteTime";
        public static string Hash = "Meta_Hash";

        public static readonly string OrderByRegEx = @"(?<=\W)(ORDER\s+BY)\s+(.+?(?=\sASC|\sDESC|\sLIMIT|$))(?:\s+(ASC|DESC))?(?=$|\s)";
    }
}
