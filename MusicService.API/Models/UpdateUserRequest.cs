using System;

namespace MusicService.API.Models
{
    public class UpdateUserRequest
    {
        /// <summary>имя</summary>
        public string? FirstName { get; set; }
        /// <summary>фамилия</summary>
        public string? LastName { get; set; }
        /// <summary>телефон</summary>
        public string? PhoneNumber { get; set; }
        /// <summary>статус активности</summary>
        public bool? IsActive { get; set; }
        /// <summary>email подтвержден</summary>
        public bool? IsEmailConfirmed { get; set; }
    }
}
