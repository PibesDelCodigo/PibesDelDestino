using System;

namespace PibesDelDestino.Users
{
    public interface IUserOwned
    {
        Guid UserId { get; set; }
    }
}