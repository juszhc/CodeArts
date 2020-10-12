﻿using CodeArts.ORM;
using UnitTest.Domain.Entities;
using UnitTest.Serialize;

namespace UnitTest.Domain
{
    [DefaultDbConfig]
    public class AuthShipRepository : ReadWriteRepository<AuthShip>
    {
    }
}
