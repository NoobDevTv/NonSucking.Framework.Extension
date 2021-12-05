using System.Drawing;
using NonSucking.Framework.Serialization;

namespace DEMO
{
    public class SinglePropFirst
    {
        public int Test2 { get; set; }
    }

    [Nooson]
    public partial class SinglePropTest : SinglePropFirst
    {
        //[NoosonOrder(2)]
        public bool IsEmpty { get; set; }
        [NoosonOrder(1)]
        public Point Position2 { get; set; }
        [NoosonOrder(0)]
        public Point Position { get; set; }
        //public IEnumerable HelloWorld { get; set; }


    }
}
