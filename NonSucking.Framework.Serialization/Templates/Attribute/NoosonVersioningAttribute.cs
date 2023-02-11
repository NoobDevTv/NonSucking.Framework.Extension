using System;

namespace NonSucking.Framework.Serialization
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    internal class NoosonVersioningAttribute : Attribute
    {
        /// <summary>
        /// The method to call for checking the version.
        /// </summary>
        public string MethodName { get; set; }
        /// <summary>
        /// The c# expression to assign as a default. When empty <see langword="default"/> will be used.
        /// </summary>
        public string DefaultExpression { get; set; }
        /// <summary>
        /// The parameter names to pass to the <see cref="MethodName"/> method. Properties and fields marked with this attribute need to be ordered after these parameters.
        /// </summary>
        public string[] ParameterNames { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="NoosonVersioningAttribute"/> class.
        /// </summary>
        /// <param name="methodName">The method to call for checking the version.</param>
        /// <param name="defaultExpression">The c# expression to assign as a default. When empty <see langword="default"/> will be used.</param>
        /// <param name="parameterNames">The parameter names to pass to the <paramref name="methodName"/> method. Properties and fields marked with this attribute need to be ordered after these parameters.</param>
        public NoosonVersioningAttribute(string methodName, string defaultExpression, params string[] parameterNames)
        {
            MethodName = methodName;
            DefaultExpression = defaultExpression;
            ParameterNames = parameterNames;

        }
    }
}
