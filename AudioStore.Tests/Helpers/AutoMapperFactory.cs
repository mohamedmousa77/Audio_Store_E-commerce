using AutoMapper;

namespace AudioStore.Tests.Helpers;

public static class AutoMapperFactory
{
    private static IMapper? _mapper;

    public static IMapper CreateMapper()
    {
        if (_mapper == null)
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(typeof(AudioStore.Application.Mapping.MappingProfile).Assembly);
            });

            _mapper = configuration.CreateMapper();
        }

        return _mapper;
    }
}
