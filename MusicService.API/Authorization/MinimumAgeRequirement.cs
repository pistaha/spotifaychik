using Microsoft.AspNetCore.Authorization;

namespace MusicService.API.Authorization
{
    public sealed class MinimumAgeRequirement : IAuthorizationRequirement
    {
        public MinimumAgeRequirement(int age)
        {
            Age = age;
        }

        public int Age { get; }
    }
}
