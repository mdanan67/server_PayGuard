using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using server.Dto;

namespace server.repository
{
    public interface IUser
    {
        public IEnumerable<ParentRegistrationDto> UserRegistration();
    }
}