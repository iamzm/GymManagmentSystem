namespace Shared.DTOs.MembershipDTOs {
    /// <summary>Everything The Member's Own Plan Screen Shows In One Round Trip.</summary>
    public class MyMembershipDTO {
        public int MemberId { get; set; }
        public string MemberName { get; set; } = null!;
        public string? MemberPhoto { get; set; }

        /// <summary>The Term Running Right Now, Or Null When The Member Is Not Subscribed.</summary>
        public MembershipDetailsDTO? Current { get; set; }

        /// <summary>The Plan Already Booked To Take Over When The Current Term Ends.</summary>
        public MembershipDTO? Scheduled { get; set; }

        public List<PlanOptionDTO> Options { get; set; } = [];

        public bool HasActivePlan => Current is not null;
        public bool HasScheduledChange => Scheduled is not null;

        /// <summary>
        /// The Day A Change Chosen Now Would Take Effect: The End Of The Paid-For Term, Or Today
        /// When There Is Nothing Running.
        /// </summary>
        public DateOnly NextEffectiveDate { get; set; }
    }
}
