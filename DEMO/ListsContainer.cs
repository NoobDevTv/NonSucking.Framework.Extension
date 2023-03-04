using NonSucking.Framework.Serialization;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace DEMO;

public  class SomethingDifferent
{
    public IReadOnlyCollection<int> ReadOnlyBase { get; set; }
    public IReadOnlyList<int> ReadOnlyListBase { get; set; }
    public virtual void Serialize(BinaryWriter writer)
    {
        // ...
    }

    public static void Deserialize(BinaryReader writer, out IReadOnlyCollection<int> readOnlyBase, out IReadOnlyList<int> readOnlyListBase)
    {
        readOnlyBase = readOnlyListBase = new List<int>();
    }
}

public class SomethingVastlyDifferent : SomethingDifferent
{
    public int Abc { get; set; }

}

[Nooson]
public partial class ListsContainer : SomethingVastlyDifferent
{
    public IReadOnlyCollection<int> ReadOnly { get; set; }
    public IReadOnlyList<int> ReadOnlyList { get; set; }
    public IReadOnlyDictionary<int, int> ReadOnlyDictionary { get; set; }
    public IReadOnlySet<int> ReadOnlySet { get; set; }

    public Dictionary<int, string> ABC { get; set; }
    public HashSet<bool> HashSet { get; set; }
    public SortedSet<bool> SortedSet { get; set; }
    public ConcurrentBag<bool> ConcBag { get; set; }
    public Queue<bool> Queue { get; set; }
    public Stack<bool> Stack { get; set; }
    public List<Queue<int>> ListWithQueue { get; set; }


    public int[] TestArray { get; set; }
    public int[,,] MultiDimension { get; set; }

    public IReadOnlyDictionary<short, short> ReadOnlyDicSetable { get; set; }
    public int[][] MultiArray { get; set; }

    public List<Queue<int[][][][][]>> YouAreGonnaHateMe { get; set; }

    public List<int>[] ListInArray { get; set; }
    public List<int[]> ArrayInList { get; set; }

    public ConcurrentDictionary<bool, int> ConcDic { get; set; }
    public ConcurrentQueue<bool> ConcQueue { get; set; }
    public ConcurrentStack<bool> ConcStack { get; set; }


}

