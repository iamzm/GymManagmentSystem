namespace GMS.MVC.Services {
    /// <summary>Named Authorization Policies, So Controllers Never Repeat A Role List Inline.</summary>
    public static class AppPolicies {
        /// <summary>Full Back-Office Control: Create, Edit And Delete Across Every Module.</summary>
        public const string AdminOnly = "AdminOnly";

        /// <summary>Admins And Trainers: Read Access To The Gym Records And The Timetable.</summary>
        public const string StaffOnly = "StaffOnly";
    }
}
