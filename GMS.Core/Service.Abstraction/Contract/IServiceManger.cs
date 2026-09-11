namespace Services.Abstraction.Contract {
    public interface IServiceManger {
        public IMemberService MemberService { get; }
        public ITrainerService TrainerService { get; }
        public IAnalyticsService AnalyticsService { get; }
        public IPlanService PlanService { get; }
        public ISessionService SessionService { get; }
        public IMembershipService MembershipService { get; }
        public IBookingService BookingService { get; }

        // Operations — Staff, Stock And Spending.
        public IEmployeeService EmployeeService { get; }
        public IInventoryService InventoryService { get; }
        public IExpenseService ExpenseService { get; }
    }
}
