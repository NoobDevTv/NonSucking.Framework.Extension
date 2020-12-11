using System;
using System.Collections.Generic;
using System.Text;

namespace NonSucking.Framework.Extension.SumTypes
{
    public class TypeMismatchException : Exception
    {
        public TypeMismatchException() : base($"Unexpected Type")
        {


        }

    }
}
