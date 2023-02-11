using System;

namespace NonSucking.Framework.Serialization
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    internal class NoosonVersioningAttribute : Attribute
    {
        /// <summary>
        /// The method to call for checking the version
        /// </summary>
        public string MethodName { get; set; }
        /// <summary>
        /// The c# expression to assign as a default. When empty <see langword="default"/> will be used.
        /// </summary>
        public string DefaultExpression { get; set; }
        /// <summary>
        /// The parameter names for the <see cref="MethodName"/>, has to be in the correct order
        /// </summary>
        public string[] ParameterNames { get; set; }

        public NoosonVersioningAttribute(string methodName, string defaultExpression, params string[] parameterNames)
        {
            MethodName = methodName;
            DefaultExpression = defaultExpression;
            ParameterNames = parameterNames;

        }
    }
}
