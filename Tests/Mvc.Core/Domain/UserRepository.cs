﻿using Mvc.Core.Domain.Entities;
using CodeArts.ORM;

namespace Mvc.Core.Domain
{
    /// <summary>
    /// 用户仓库
    /// </summary>
    [DbConfig("connectionStrings:default")]
    public class UserRepository : ReadWriteRepository<User>
    {

    }
}
