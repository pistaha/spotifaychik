using MediatR;
using System;

namespace MusicService.Application.Users.Commands
{
    public enum AddFriendStatus
    {
        Success,
        UserNotFound,
        FriendNotFound,
        AlreadyFriends,
        Error
    }

    public record AddFriendResult(bool Success, AddFriendStatus Status)
    {
        public static AddFriendResult Ok() => new(true, AddFriendStatus.Success);
        public static AddFriendResult AlreadyFriends() => new(false, AddFriendStatus.AlreadyFriends);
        public static AddFriendResult UserMissing() => new(false, AddFriendStatus.UserNotFound);
        public static AddFriendResult FriendMissing() => new(false, AddFriendStatus.FriendNotFound);
        public static AddFriendResult Failed() => new(false, AddFriendStatus.Error);
    }

    public record AddFriendCommand : IRequest<AddFriendResult>
    {
        public Guid UserId { get; init; }
        public Guid FriendId { get; init; }
    }
}
