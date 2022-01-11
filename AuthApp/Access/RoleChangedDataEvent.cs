﻿using Data.Common.Contracts;

namespace Access
{
    public class RoleChangedDataEvent : DataEvent
    {
        public Guid User { get; }
        public Guid Role { get; }

        public RoleChangedDataEvent(Guid user, Guid role)
        {
            User = user;
            Role = role;
        }
    }
}