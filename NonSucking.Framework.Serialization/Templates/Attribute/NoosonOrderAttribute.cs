﻿using System;

namespace NonSucking.Framework.Serialization
{
    [AttributeUsage(AttributeTargets.Property | AttributeTarget.Fields, Inherited = false, AllowMultiple = false)]
    public class NoosonOrderAttribute : Attribute
    {
        public int Order { get;  }

        public NoosonOrderAttribute(int order)
        {
            Order = order;
        }
    }
}