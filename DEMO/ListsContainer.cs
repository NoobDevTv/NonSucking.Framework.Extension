using System.Collections.Generic;
using NonSucking.Framework.Serialization;
using System.Collections.Concurrent;

namespace DEMO;



[Nooson]
public partial class ListsContainer
{
    public IReadOnlyCollection<int> ReadOnly { get; set; }

    public Dictionary<int, string> ABC { get; set; }
    public HashSet<bool> HashSet { get; set; }
    public SortedSet<bool> SortedSet { get; set; }
    public ConcurrentBag<bool> ConcBag { get; set; }
    public Queue<bool> Queue { get; set; }
    public Stack<bool> Stack { get; set; }
    public List<Queue<int>> ListWithQueue { get; set; }

    public int[] TestArray { get; set; }
    public int[,,] MultiDemension { get; set; }
    public int[][] MultiArray { get; set; }

    public List<Queue<int[][][][][]>> YouAreGonnaHateMe { get; set; }

    public List<int>[] ListInArray { get; set; }
    public List<int[]> ArrayInList { get; set; }

    public ConcurrentDictionary<bool, int> ConcDic { get; set; }
    public ConcurrentQueue<bool> ConcQueue { get; set; }
    public ConcurrentStack<bool> ConcStack { get; set; }
}

