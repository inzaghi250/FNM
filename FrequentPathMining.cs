using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FNM
{
    public enum StepType
    {
        Forward, Backward, Nlabel
    }

    public class Step : Tuple<int, StepType>
    {
        public Step(int id, StepType type) : base(id, type) { }
        public override int GetHashCode()
        {
            return Item1.GetHashCode() & Item2.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            Step step = obj as Step;
            return Item1 == step.Item1 && Item2 == step.Item2;
        }
    }

    public class Path : List<Step>
    {
        public Path()
        {
        }
        public Path(Path path, Step step)
        {
            int i;
            for (i = 0; i < path.Count; i++)
                Add(path[i]);
            Add(step);
        }
        public IndexedGraph ToIndexedGraph()
        {
            IndexedGraph ret = new IndexedGraph();
            if (this[this.Count - 1].Item2 == StepType.Nlabel)
            {
                ret._vertexes = new Vertex[this.Count];
                for (int i = 0; i < ret._vertexes.Length; i++)
                    ret._vertexes[i] = new Vertex();
                ret._vertexes[this.Count - 1]._vLabel = new int[1] { this[this.Count - 1].Item1 };

                ret._edges = new Edge[this.Count - 1];
                for (int i = 0; i < this.Count - 1; i++)
                {
                    Edge e = new Edge();
                    e._eLabel = this[i].Item1;
                    if (this[i].Item2 == StepType.Forward)
                    {
                        e._from = i;
                        e._to = i + 1;
                    }
                    else
                    {
                        e._to = i;
                        e._from = i + 1;
                    }
                    ret._edges[i] = e;

                    if (i == 0)
                    {
                        if (this[0].Item2 == StepType.Forward)
                            ret._vertexes[0]._outEdge = new int[1] { 0 };
                        else
                            ret._vertexes[0]._inEdge = new int[1] { 0 };
                    }
                    if (i == this.Count - 2)
                    {
                        if (this[i].Item2 == StepType.Forward)
                            ret._vertexes[i + 1]._inEdge = new int[1] { i };
                        else
                            ret._vertexes[i + 1]._outEdge = new int[1] { i };
                    }
                    else
                    {
                        if (this[i].Item2 == StepType.Forward)
                        {
                            if (this[i + 1].Item2 == StepType.Forward)
                            {
                                ret._vertexes[i + 1]._inEdge = new int[1] { i };
                                ret._vertexes[i + 1]._outEdge = new int[1] { i + 1 };
                            }
                            else
                            {
                                ret._vertexes[i + 1]._inEdge = new int[2] { i, i + 1 };
                            }
                        }
                        else
                        {
                            if (this[i + 1].Item2 == StepType.Forward)
                                ret._vertexes[i + 1]._outEdge = new int[2] { i, i + 1 };
                            else
                            {
                                ret._vertexes[i + 1]._inEdge = new int[1] { i + 1 };
                                ret._vertexes[i + 1]._outEdge = new int[1] { i };
                            }
                        }
                    }

                }
            }
            else
            {
                ret._vertexes = new Vertex[this.Count + 1];
                for (int i = 0; i < ret._vertexes.Length; i++)
                    ret._vertexes[i] = new Vertex();

                ret._edges = new Edge[this.Count];
                for (int i = 0; i < this.Count; i++)
                {
                    Edge e = new Edge();
                    e._eLabel = this[i].Item1;
                    if (this[i].Item2 == StepType.Forward)
                    {
                        e._from = i;
                        e._to = i + 1;
                    }
                    else
                    {
                        e._to = i;
                        e._from = i + 1;
                    }
                    ret._edges[i] = e;

                    if (i == 0)
                    {
                        if (this[0].Item2 == StepType.Forward)
                            ret._vertexes[0]._outEdge = new int[1] { 0 };
                        else
                            ret._vertexes[0]._inEdge = new int[1] { 0 };
                    }
                    if (i == this.Count - 1)
                    {
                        if (this[i].Item2 == StepType.Forward)
                            ret._vertexes[i + 1]._inEdge = new int[1] { i };
                        else
                            ret._vertexes[i + 1]._outEdge = new int[1] { i };
                    }
                    else
                    {
                        if (this[i].Item2 == StepType.Forward)
                        {
                            if (this[i + 1].Item2 == StepType.Forward)
                            {
                                ret._vertexes[i + 1]._inEdge = new int[1] { i };
                                ret._vertexes[i + 1]._outEdge = new int[1] { i + 1 };
                            }
                            else
                            {
                                ret._vertexes[i + 1]._inEdge = new int[2] { i, i + 1 };
                            }
                        }
                        else
                        {
                            if (this[i + 1].Item2 == StepType.Forward)
                                ret._vertexes[i + 1]._outEdge = new int[2] { i, i + 1 };
                            else
                            {
                                ret._vertexes[i + 1]._inEdge = new int[1] { i + 1 };
                                ret._vertexes[i + 1]._outEdge = new int[1] { i };
                            }
                        }
                    }
                }
            }
            ret.GenIndex();
            return ret;
        }
    }

    class FrequentPathMining
    {
        public List<Tuple<Path, object>> _resultCache = new List<Tuple<Path, object>>();

        public void Init(Graph g, int minSupp, int maxSize,bool useVIDList)
        {
            int[] constraintVSet = new int[g._vertexes.Length];
            for (int i = 0; i < g._vertexes.Length; i++)
                constraintVSet[i] = i;
            Init(g, minSupp, maxSize, constraintVSet,useVIDList);
        }

        public virtual void Init(Graph g, int minSupp, int maxSize, int[] constraintVSet, bool useVIDList)
        {
            throw new NotImplementedException();
        }

        public List<IndexedGraph> GetPath(int length)
        {
            List<IndexedGraph> ret = new List<IndexedGraph>();
            foreach (var pair in _resultCache.Where(e => e.Item1.Count == length))
                ret.Add(pair.Item1.ToIndexedGraph());
            return ret;
        }

        public List<Tuple<IndexedGraph,object>> GetPathAndInfo(int length)
        {
            List<Tuple<IndexedGraph, object>> ret = new List<Tuple<IndexedGraph, object>>();
            foreach (var pair in _resultCache.Where(e => e.Item1.Count == length))
                ret.Add(new Tuple<IndexedGraph,object>(pair.Item1.ToIndexedGraph(),pair.Item2));
            return ret;
        }

        public List<Tuple<IndexedGraph, int>> GetPathAndCount(int length)
        {
            List<Tuple<IndexedGraph, int>> ret = new List<Tuple<IndexedGraph, int>>();
            foreach (var pair in _resultCache.Where(e => e.Item1.Count == length))
            {
                if(pair.Item2 is List<int>)
                    ret.Add(new Tuple<IndexedGraph, int>(pair.Item1.ToIndexedGraph(), ((List<int>)pair.Item2).Count));
                else
                    ret.Add(new Tuple<IndexedGraph, int>(pair.Item1.ToIndexedGraph(), (int)pair.Item2));
            }
            return ret;
        }

        public List<Tuple<IndexedGraph, List<int>>> GetPathAndVID(int length)
        {
            List<Tuple<IndexedGraph, List<int>>> ret = new List<Tuple<IndexedGraph, List<int>>>();
            foreach (var pair in _resultCache.Where(e => e.Item1.Count == length))
            {
                if(pair.Item2 is List<int>)
                    ret.Add(new Tuple<IndexedGraph, List<int>>(pair.Item1.ToIndexedGraph(), pair.Item2 as List<int>));
                else
                    ret.Add(new Tuple<IndexedGraph, List<int>>(pair.Item1.ToIndexedGraph(), null));
            }
            return ret;
        }
    }

    class FrequentPathMiningBreadth:FrequentPathMining
    {
        class GetNextStepGivenNode
        {
            static Path _path;
            static Graph _g;
            static int _nodeId;

            static HashSet<Step> _ret = new HashSet<Step>();
            static HashSet<int> _nodeUsed = new HashSet<int>();

            public static List<Step> GetNextStep(Path path, Graph g, int nodeId)
            {
                _path = path; _g = g; _nodeId = nodeId;
                _ret.Clear();
                _nodeUsed.Clear();
                _nodeUsed.Add(nodeId);
                Search(0, nodeId);
                List<Step> ret = new List<Step>(_ret);
                return ret;
            }

            static void Search(int depth, int nodeId)
            {
                Vertex v = _g._vertexes[nodeId];
                if (depth == _path.Count)
                {
                    foreach (int eid in v._inEdge)
                    {
                        Edge e = _g._edges[eid];
                        if (_nodeUsed.Contains(e._from))
                            continue;
                        Step step = new Step(e._eLabel, StepType.Backward);
                        _ret.Add(step);
                    }
                    foreach (int eid in v._outEdge)
                    {
                        Edge e = _g._edges[eid];
                        if (_nodeUsed.Contains(e._to))
                            continue;
                        Step step = new Step(e._eLabel, StepType.Forward);
                        _ret.Add(step);
                    }
                    foreach (int lid in v._vLabel)
                    {
                        Step step = new Step(lid, StepType.Nlabel);
                        _ret.Add(step);
                    }
                }
                else
                {
                    Step step = _path[depth];
                    switch (step.Item2)
                    {
                        case StepType.Backward:
                            foreach (int eid in v._inEdge)
                            {
                                Edge e = _g._edges[eid];
                                if (e._eLabel == step.Item1 && !_nodeUsed.Contains(e._from))
                                {
                                    _nodeUsed.Add(e._from);
                                    Search(depth + 1, e._from);
                                    _nodeUsed.Remove(e._from);
                                }
                            }
                            break;
                        case StepType.Forward:
                            foreach (int eid in v._outEdge)
                            {
                                Edge e = _g._edges[eid];
                                if (e._eLabel == step.Item1 && !_nodeUsed.Contains(e._to))
                                {
                                    _nodeUsed.Add(e._to);
                                    Search(depth + 1, e._to);
                                    _nodeUsed.Remove(e._to);
                                }
                            }
                            break;
                    }
                }
                return;
            }
        }

        //public override void Init(Graph g, int minSupp, int maxSize, int[] constraintVSet)
        //{
        //    _resultCache.Clear();
        //    List<Path> queue = new List<Path>();
        //    int fronti = 0;
        //    Path emptyPath = new Path();
        //    queue.Add(emptyPath);

        //    Dictionary<Step, int> mapStep2Count = new Dictionary<Step, int>();

        //    while (fronti < queue.Count)
        //    {
        //        mapStep2Count.Clear();
        //        Path thisPath = queue[fronti++];
        //        //Console.WriteLine("Expanding...");
        //        if (thisPath.Count > maxSize - 1)
        //            break;
        //        foreach (int i in constraintVSet)
        //        {
        //            List<Step> steps = GetNextStepGivenNode.GetNextStep(thisPath, g, i);
        //            foreach (Step step in steps)
        //            {
        //                if (!mapStep2Count.ContainsKey(step))
        //                    mapStep2Count[step] = 0;
        //                mapStep2Count[step]++;
        //            }
        //        }
        //        foreach (var pair in mapStep2Count)
        //        {
        //            if (pair.Value < minSupp)
        //                continue;
        //            Path newPath = new Path(thisPath, pair.Key);
        //            _resultCache.Add(new Tuple<Path, List<int>>(newPath, null));
        //            if (pair.Key.Item2 != StepType.Nlabel)
        //                queue.Add(newPath);
        //        }
        //    }
        //}

        public override void Init(Graph g, int minSupp, int maxSize, int[] constraintVSet, bool useVIDList)
        {
            _resultCache.Clear();
            List<Tuple<Path, object>> queue = new List<Tuple<Path, object>>();
            int fronti = 0;
            Path emptyPath = new Path();
            queue.Add(new Tuple<Path, object>(emptyPath, constraintVSet.ToList()));

            Dictionary<Step, object> mapStep2VidsOrCounts = new Dictionary<Step, object>();

            while (fronti < queue.Count)
            {
                mapStep2VidsOrCounts.Clear();
                var pair1 = queue[fronti++];
                Path thisPath = pair1.Item1;
                //Console.WriteLine("Expanding...");
                if (thisPath.Count >= maxSize)
                    break;
                List<int> scanedVID=null;
                if (useVIDList)
                    scanedVID = pair1.Item2 as List<int>;
                else
                    scanedVID = constraintVSet.ToList();
                foreach (int i in scanedVID)
                {
                    List<Step> steps = GetNextStepGivenNode.GetNextStep(thisPath, g, i);
                    foreach (Step step in steps)
                    {
                        if (!mapStep2VidsOrCounts.ContainsKey(step))
                        {
                            if (useVIDList)
                                mapStep2VidsOrCounts[step] = new List<int>();
                            else
                                mapStep2VidsOrCounts[step] = 0;
                        }
                        if (useVIDList)
                            ((List<int>)mapStep2VidsOrCounts[step]).Add(i);
                        else
                            mapStep2VidsOrCounts[step] = (int)mapStep2VidsOrCounts[step] + 1;
                    }
                }
                foreach (var pair in mapStep2VidsOrCounts)
                {
                    if (useVIDList)
                    {
                        if (((List<int>)pair.Value).Count < minSupp)
                            continue;
                    }
                    else
                    {
                        if ((int)pair.Value < minSupp)
                            continue;
                    }
                    Path newPath = new Path(thisPath, pair.Key);
                    object info = pair.Value;
                    _resultCache.Add(new Tuple<Path, object>(newPath, info));
                    if (pair.Key.Item2 != StepType.Nlabel)
                        queue.Add(new Tuple<Path, object>(newPath, info));
                }
            }
        }
    }

//    class FrequentPathMiningDepth:FrequentPathMining
//    {
//        class PFTreeNode
//        {
//            public void Clear()
//            {
//                if(_steps!=null)
//                    _steps.Clear();
//                _cnt=0;
//            }
//            public Dictionary<Step, PFTreeNode> _steps=null;
//            public int _cnt=0;
//        }

//        Graph _g;
//        int _minSupp,_maxSize;
//        PFTreeNode _pftRoot=new PFTreeNode();

//        HashSet<int> _vUsed = new HashSet<int>();

//        HashSet<PFTreeNode> _nodeVisited = new HashSet<PFTreeNode>();

//        void Search(PFTreeNode tNode, int vid, int depth)
//        {
//            //经过一个节点只加一次cnt
//            _vUsed.Add(vid);
//            Vertex v=_g._vertexes[vid];
//            foreach(int eid in v._inEdge)
//            {
//                Edge e=_g._edges[eid];
//                if(_vUsed.Contains(e._from))
//                    continue;
//                if(tNode._steps==null)
//                    tNode._steps=new Dictionary<Step,PFTreeNode>();
//                Step entry=new Step(e._eLabel,StepType.Backward);
//                PFTreeNode nextNode=null;
//                tNode._steps.TryGetValue(entry,out nextNode);
//                if(nextNode==null)
//                    nextNode=(tNode._steps[entry]=new PFTreeNode());
//                if (!_nodeVisited.Contains(nextNode))
//                {
//                    nextNode._cnt++;
//                    _nodeVisited.Add(nextNode);
//                }
//                if(depth<_maxSize-1)
//                    Search(nextNode,e._from,depth+1);
//            }
//            foreach(int eid in v._outEdge)
//            {
//                Edge e=_g._edges[eid];
//                if(_vUsed.Contains(e._to))
//                    continue;
//                if(tNode._steps==null)
//                    tNode._steps=new Dictionary<Step,PFTreeNode>();
//                Step entry=new Step(e._eLabel,StepType.Forward);
//                PFTreeNode nextNode=null;
//                tNode._steps.TryGetValue(entry,out nextNode);
//                if(nextNode==null)
//                    nextNode=(tNode._steps[entry]=new PFTreeNode());
//                if (!_nodeVisited.Contains(nextNode))
//                {
//                    nextNode._cnt++;
//                    _nodeVisited.Add(nextNode);
//                }
//                if(depth<_maxSize-1)
//                    Search(nextNode,e._to,depth+1);
//            }
//            foreach(int vlid in v._vLabel)
//            {
//                if (tNode._steps == null)
//                    tNode._steps = new Dictionary<Step, PFTreeNode>();
//                Step entry=new Step(vlid,StepType.Nlabel);
//                PFTreeNode nextNode=null;
//                tNode._steps.TryGetValue(entry,out nextNode);
//                if(nextNode==null)
//                    nextNode=(tNode._steps[entry]=new PFTreeNode());
//                if (!_nodeVisited.Contains(nextNode))
//                {
//                    nextNode._cnt++;
//                    _nodeVisited.Add(nextNode);
//                }
//            }
//            _vUsed.Remove(vid);
//        }

//        void GenFrequentPaths(PFTreeNode tNode, Path path)
//        {
//            foreach (var pair in tNode._steps)
//            {
//                if (pair.Value._cnt < _minSupp)
//                    continue;
//                Path newPath = new Path(path, pair.Key);
//                _resultCache.Add(new Tuple<Path, int>(newPath, pair.Value._cnt));
//                if(pair.Value._steps!=null)
//                    GenFrequentPaths(pair.Value, newPath);
//            }
//        }

//        public override void Init(Graph g, int minSupp, int maxSize, int[] constraintVSet)
//        {
//            _g=g;
//            _minSupp=minSupp;
//            _maxSize=maxSize;
//            _pftRoot.Clear();

//            Console.WriteLine("Initialize Path Pattern Tree.");
//            int cnt = 0,onePstCnt=constraintVSet.Length/100;
//            foreach (int vid in constraintVSet)
//            {
//                _pftRoot._cnt++;
//                _nodeVisited.Clear();
//                Search(_pftRoot, vid, 0);
//                cnt++;
//                if (cnt % onePstCnt == 0)
//                    Console.Write("*");
//            }
//            Console.WriteLine("\nPath Pattern Tree OK.");

//            GenFrequentPaths(_pftRoot,new Path());
//            return;
//        }
//    }

}