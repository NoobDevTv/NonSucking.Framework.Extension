using System.Linq.Expressions;

namespace NonSucking.Framework.Extension.Generators.CodeBuilding
{
    /*
     string str;
    if(abc == def)
        str = "§";
    Deserialize();
    if(des = 123)
        str += "123";

    ------

    var str = mehtodBuilder.AddVariable("string", "str");

    ifBuilder
    str.Assign("§");
    

    ------

    string str;
    str = "§";
    if(abc == def)
    
     */

    public interface IVariableBuilder
    {

    }

    public static class IVariableBuilderExtension
    {
        public static IVariableBuilder Assign(this IVariableBuilder builder, string value)
        {
            
        }
        public static IVariableBuilder CustomAssign(this IVariableBuilder builder, string customOperator, string value);
    }
}
