using System;
using Volo.Abp.Application.Dtos;

namespace PibesDelDestino.Users
{
    public class PublicUserDto : EntityDto<Guid>
    {
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }

       // "Photo" si en el futuro la implementamos
    }
}
