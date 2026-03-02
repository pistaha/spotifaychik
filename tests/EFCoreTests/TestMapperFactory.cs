using AutoMapper;
using MusicService.Application.Common.Mapping;
using MusicService.Application.Search.Mapping;

namespace Tests.EFCoreTests
{
    public static class TestMapperFactory
    {
        public static IMapper Create()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
                cfg.AddProfile<SearchMappingProfile>();
            });
            return config.CreateMapper();
        }
    }
}
