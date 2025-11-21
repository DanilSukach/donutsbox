using AutoMapper;
using Donutsbox.Api.Dto;
using Donutsbox.Domain.Entities;
using System.Globalization;

namespace Donutsbox.Api.Mapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>().ReverseMap();
        CreateMap<UserType, UserTypeDto>().ReverseMap();
        CreateMap<UserData, UserDataDto>().ReverseMap();
        CreateMap<UserAuth, UserAuthDto>().ReverseMap();
        CreateMap<Subscription, SubscriptionDto>()
            .ForMember(dest => dest.SubscriptionPeriodMonths, opt => opt.MapFrom(src => src.SubscriptionPeriod.Months))
            .ForMember(dest => dest.SubscriptionPeriodId, opt => opt.MapFrom(src => src.SubscriptionPeriodId))
            .ForMember(dest => dest.MonthlyPrice, opt => opt.MapFrom(src => CalculateMonthlyPrice(src.Price, src.SubscriptionPeriod.Months)))
            .ReverseMap()
            .ForMember(dest => dest.SubscriptionPeriod, opt => opt.Ignore())
            .ForMember(dest => dest.CreatorPageData, opt => opt.Ignore())
            .ForMember(dest => dest.UserSubscriptions, opt => opt.Ignore())
            .ForMember(dest => dest.ContentPosts, opt => opt.Ignore());
        CreateMap<CreatorPageData, CreatorPageDataDto>().ReverseMap();
        CreateMap<ContentPost, ContentPostDto>().ReverseMap();
        CreateMap<UserSubscription, UserSubscriptionDto>().ReverseMap();
    }

    private static string CalculateMonthlyPrice(string price, int months)
    {
        if (months <= 0)
        {
            return price;
        }

        if (decimal.TryParse(price, NumberStyles.Any, CultureInfo.InvariantCulture, out var totalPrice))
        {
            var monthly = totalPrice / months;
            return monthly.ToString("F2", CultureInfo.InvariantCulture);
        }

        return price;
    }
}