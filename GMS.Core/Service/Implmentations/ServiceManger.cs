using AutoMapper;
using Domin.Contract;
using Services.Abstraction.Contract;

namespace Services.Implmentations {
    public class ServiceManger(IUnitOfWork unitOfWork, IMapper mapper) : IServiceManger {
        private readonly Lazy<ISessionService> _sessionService = new(() => new SessionService(unitOfWork, mapper));
        private readonly Lazy<IMemberService> _memberService = new(() => new MemberService(unitOfWork, mapper));
        private readonly Lazy<ITrainerService> _trainerService = new(() => new TrainerService(unitOfWork, mapper));
        private readonly Lazy<IAnalyticsService> _analyticsService = new(() => new AnalyticsService(unitOfWork, mapper));
        private readonly Lazy<IPlanService> _planService = new(() => new PlanService(unitOfWork, mapper));
        private readonly Lazy<IMembershipService> _membershipService = new(() => new MembershipService(unitOfWork, mapper));
        private readonly Lazy<IBookingService> _bookingService = new(() => new BookingService(unitOfWork, mapper));

        public ISessionService SessionService => _sessionService.Value;
        public IMemberService MemberService => _memberService.Value;
        public ITrainerService TrainerService => _trainerService.Value;
        public IAnalyticsService AnalyticsService => _analyticsService.Value;
        public IPlanService PlanService => _planService.Value;
        public IMembershipService MembershipService => _membershipService.Value;
        public IBookingService BookingService => _bookingService.Value;
    }
}
