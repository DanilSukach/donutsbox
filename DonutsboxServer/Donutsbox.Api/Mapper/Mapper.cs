using AutoMapper;
using Donutsbox.Api.Dto;
using Donutsbox.Domain.Entities;

namespace Donutsbox.Api.Mapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>().ReverseMap();
        CreateMap<UserType, UserTypeDto>().ReverseMap();
        CreateMap<UserData, UserDataDto>().ReverseMap();
        CreateMap<UserAuth, UserAuthDto>().ReverseMap();
        CreateMap<Subscription, SubscriptionDto>().ReverseMap();
        CreateMap<CreatorPageData, CreatorPageDataDto>().ReverseMap();
        CreateMap<ContentPost, ContentPostDto>().ReverseMap();
        CreateMap<UserSubscription, UserSubscriptionDto>().ReverseMap();
    }
}