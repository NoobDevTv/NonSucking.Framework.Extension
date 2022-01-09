using System.Collections.Generic;
using NonSucking.Framework.Serialization;
using System.Collections.Concurrent;

namespace DEMO;

[Nooson]
public partial class ListsContainer
{
    public Dictionary<int, string> ABC { get; set; }
    public HashSet<bool> HashSet { get; set; }
    public SortedSet<bool> SortedSet { get; set; }
    public ConcurrentBag<bool> ConcBag { get; set; }
    public Queue<bool> Queue { get; set; }
    public Stack<bool> Stack { get; set; }

    public ConcurrentDictionary<bool, int> ConcDic { get; set; }
    public ConcurrentQueue<bool> ConcQueue { get; set; }
    public ConcurrentStack<bool> ConcStack { get; set; }
}

