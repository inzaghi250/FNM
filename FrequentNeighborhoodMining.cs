using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FNM
{
    struct VLabel
    {
        public int _vid, _vlid;
    }

    class Tools
    {
        public static List<int> MergeSortedArray(List<int> a, List<int> b)
        {
            List<int> ret = new List<int>();
            int i = 0, j = 0;
            while (i < a.Count && j < b.Count)
            {
                if (a[i] < b[j])
                    i++;
                else if (a[i] > b[j])
                    j++;
                else
                {
                    ret.Add(a[i]);
                    i++; j++;
                }
            }
            return ret;
        }
    }

    class NodeInvariantIndex
    {
        Dictionary<Step, List<int>> _index = new Dictionary<Step, List<int>>();

        public NodeInvariantIndex(IndexedGraph db)
        {
            Dictionary<Step, HashSet<int>> _setIndex = new Dictionary<Step, HashSet<int>>();
            foreach (var pair in db._vertexLabelIndex)
            {
                HashSet<int> set = new HashSet<int>(pair.Value);
                Step entry = new Step(pair.Key, StepType.Nlabel);
                _setIndex[entry] = set;
            }

            foreach (Edge e in db._edges)
            {
                Step entry = new Step(e._eLabel, StepType.Forward);
                if (!_setIndex.ContainsKey(entry))
                    _setIndex[entry] = new HashSet<int>();
                _setIndex[entry].Add(e._from);

                entry = new Step(e._eLabel, StepType.Backward);
                if (!_setIndex.ContainsKey(entry))
                    _setIndex[entry] = new HashSet<int>();
                _setIndex[entry].Add(e._to);
            }

            foreach (var pair in _setIndex)
            {
                _index[pair.Key] = pair.Value.ToList();
                _index[pair.Key].Sort();
            }
        }

        //Because of the definition of patterns, result is not null.
        public List<List<int>> GetCandidatesForeachVertex(Graph pattern)
        {
            List<List<int>> ret = new List<List<int>>();
            for (int i = 0; i < pattern._vertexes.Length; i++)
            {
                List<int> vCand = null;
                Vertex v = pattern._vertexes[i];
                foreach (int vlid in v._vLabel)
                {
                    Step entry = new Step(vlid, StepType.Nlabel);
                    List<int> cand;
                    _index.TryGetValue(entry, out cand);
                    if (cand != null)
                    {
                        if (vCand == null)
                            vCand = cand;
                        else
                            vCand = Tools.MergeSortedArray(vCand, cand);
                    }
                }

                foreach (int eid in v._outEdge)
                {
                    Edge e = pattern._edges[eid];
                    Step entry = new Step(e._eLabel, StepType.Forward);
                    List<int> cand;
                    _index.TryGetValue(entry, out cand);
                    if (cand != null)
                    {
                        if (vCand == null)
                            vCand = cand;
                        else
                            vCand = Tools.MergeSortedArray(vCand, cand);
                    }
                }

                foreach (int eid in v._inEdge)
                {
                    Edge e = pattern._edges[eid];
                    Step entry = new Step(e._eLabel, StepType.Backward);
                    List<int> cand;
                    _index.TryGetValue(entry, out cand);
                    if (cand != null)
                    {
                        if (vCand == null)
                            vCand = cand;
                        else
                            vCand = Tools.MergeSortedArray(vCand, cand);
                    }
                }

                ret.Add(vCand);
            }
            return ret;
        }
    }

    class SupportCounter
    {
        IndexedGraph _g;
        NodeInvariantIndex _niindex;
        public SupportCounter(IndexedGraph db)
        {
            _g = db;
            _niindex = new NodeInvariantIndex(db);
        }

        public int CountSupport(Graph pattern)
        {
            List<List<int>> cands = _niindex.GetCandidatesForeachVertex(pattern);
            if (cands.Exists(e => e.Count == 0))
                return 0;

            return 0;
        }
    }

    class ZipperHandler
    {
        public static List<Tuple<int, int>> DetectPotentialZipper(Graph pattern, int radius)
        {
            if (pattern._edges.Length + 1 != pattern._vertexes.Length)
                return null;
            if (pattern._vertexes[0]._vLabel.Length > 0)
                return null;
            int[] dist = pattern.GetDistArray();
            List<int> dangling = new List<int>();
            for (int i = 1; i < pattern._vertexes.Length; i++)
            {
                Vertex v = pattern._vertexes[i];
                if (v._vLabel.Length > 0)
                    return null;
                if (v._inEdge.Length+v._outEdge.Length == 1)
                    dangling.Add(i);
            }
            if (dangling.Count != 2)
                return null;
            foreach (int id in dangling)
                if (dist[id] != radius)
                    return null;
            
            //check for mirrored Y-pattern
            bool isMirrored = true;
            int i0 = dangling[0], i1 = dangling[1];
            while(i0!=i1)
            {
                Vertex v0 = pattern._vertexes[i0], v1 = pattern._vertexes[i1];
                if(v0._inEdge.Length>0)
                {
                    if(v1._inEdge.Length==0)
                    {
                        isMirrored = false;
                        break;
                    }
                    i0 = pattern._edges[v0._inEdge[0]]._from;
                    i1 = pattern._edges[v1._inEdge[0]]._from;
                }
                else
                {
                    if (v1._outEdge.Length == 0)
                    {
                        isMirrored = false;
                        break;
                    }
                    i0 = pattern._edges[v0._outEdge[0]]._to;
                    i1 = pattern._edges[v1._outEdge[0]]._to;
                }
            }

            var ret=new List<Tuple<int, int>>();
            ret.Add(new Tuple<int,int>(dangling[0], dangling[1]));
            if (!isMirrored)
                ret.Add(new Tuple<int, int>(dangling[1], dangling[0]));
            return ret;
        }
    }

    public delegate List<object> MapOperation(Graph g1, IndexedGraph g2, int[] map, object arg);

    public class MapOperationInstances
    {
        public static MapOperation RestoreElement = new MapOperation((g1, g2, map, arg) =>
        {
            List<object> ret = new List<object>();
            if (arg is VLabel)
            {
                VLabel vlabel = (VLabel)arg;
                int targetVID = map[vlabel._vid];
                Vertex v = g2._vertexes[targetVID];
                if (v._vLabel.Contains(vlabel._vlid))
                    return ret;
                IndexedGraph g = g2.ShallowCopy();
                int[] originalLabels = g._vertexes[targetVID]._vLabel;
                g._vertexes[targetVID]._vLabel = new int[originalLabels.Length + 1];
                originalLabels.CopyTo(g._vertexes[targetVID]._vLabel, 0);
                g._vertexes[targetVID]._vLabel[originalLabels.Length] = vlabel._vlid;

                g.GenIndex();
                ret.Add(g);
            }
            else
            {
                Edge e = (Edge)arg;

                Edge newEdge = new Edge();
                newEdge._from = map[e._from]; newEdge._to = map[e._to]; newEdge._eLabel = e._eLabel;

                Vertex v1, v2;

                if (!g2.ContainsEdge(newEdge))
                {
                    IndexedGraph g = g2.ShallowCopy();
                    Edge[] newEdgeList = new Edge[g._edges.Length + 1];
                    g._edges.CopyTo(newEdgeList, 0);
                    newEdgeList[newEdgeList.Length - 1] = newEdge;
                    g._edges = newEdgeList;

                    v1 = g._vertexes[newEdge._from];
                    int[] newOutEdgeList = new int[v1._outEdge.Length + 1];
                    v1._outEdge.CopyTo(newOutEdgeList, 0);
                    newOutEdgeList[newOutEdgeList.Length - 1] = newEdgeList.Length - 1;
                    v1._outEdge = newOutEdgeList;

                    v2 = g._vertexes[newEdge._to];
                    int[] newInEdgeList = new int[v2._inEdge.Length + 1];
                    v2._inEdge.CopyTo(newInEdgeList, 0);
                    newInEdgeList[newInEdgeList.Length - 1] = newEdgeList.Length - 1;
                    v2._inEdge = newInEdgeList;
                    g.GenIndex();
                    ret.Add(g);
                }
            }
            return ret;
        });

        public static MapOperation GetZipperHeads = new MapOperation((g1, g2, map, arg) =>
        {
            List<object> ret = new List<object>();
            List<Tuple<int, int>> headPos = (List<Tuple<int, int>>)arg;
            for (int i = 0; i < headPos.Count;i++ )
            {
                var pair = headPos[i];
                int i1 = map[pair.Item1], i2 = map[pair.Item2];
                Vertex v1 = g2._vertexes[i1];
                foreach (int eid in v1._outEdge)
                {
                    Edge e = g2._edges[eid];
                    if (e._to == i2)
                        ret.Add(new Tuple<int, int>(e._eLabel, i));
                }
            }
            return ret;
        });
    }


    class SubGraphTest
    {
        public List<List<int>> _cands = null;

        public SubGraphTest(bool enumerateSubgraphs = false, MapOperation doSomething = null)
        {
            _enumerateSubgraphs = enumerateSubgraphs;
            _DoSomething = doSomething;
            for (int i = 0; i < _vMapLen; i++)
                _vMap[i] = -1;
        }

        object _arg = null;
        public List<object> _rets = new List<object>();

        public int[] _vMap = new int[_vMapLen];
        static int _vMapLen = 10;
        public IndexedGraph g2 = null;
        Graph _pattern = null;
        int _depth = 0;

        bool _enumerateSubgraphs = false;
        MapOperation _DoSomething = null;

        public bool MatchNeighborhood(Graph pattern, IndexedGraph graph, int vid, object operationArg = null)
        {
            _rets.Clear();
            _arg = operationArg;
            g2 = graph;
            if (_cands == null)
            {
                foreach (int vlabel in pattern._vertexes[0]._vLabel)
                    if (!g2._vertexes[vid]._vLabel.Contains(vlabel))
                        return false;
            }
            else
            {
                if (_cands[0].BinarySearch(vid) < 0)
                    return false;
            }
            for (int i = 0; i < _vMap.Length; i++)
                _vMap[i] = -1;
            _vMap[0] = vid;
            _depth = 1;
            _pattern = pattern;
            return recursiveSearch();
        }

        bool recursiveSearch()
        {
            if (_depth == _pattern._vertexes.Length)
            {
                if (_enumerateSubgraphs)
                {
                    _DoSomething(_pattern, g2, _vMap, _arg).ForEach(e => _rets.Add(e));
                }
                return true;
            }

            HashSet<int> cand = null;

            foreach (Edge e in _pattern._edges)
            {
                HashSet<int> ccand = new HashSet<int>();
                if (e._from == _depth && e._to < _depth)
                {
                    int vconsid = _vMap[e._to];
                    Vertex v = g2._vertexes[vconsid];
                    foreach (int eid in v._inEdge)
                    {
                        Edge eg = g2._edges[eid];
                        if (eg._eLabel == e._eLabel && !_vMap.Contains(eg._from))
                        {
                            ccand.Add(eg._from);
                        }
                    }
                }
                else if (e._to == _depth && e._from < _depth)
                {
                    int vconsid = _vMap[e._from];
                    Vertex v = g2._vertexes[vconsid];
                    foreach (int eid in v._outEdge)
                    {
                        Edge eg = g2._edges[eid];
                        if (eg._eLabel == e._eLabel && !_vMap.Contains(eg._to))
                        {
                            ccand.Add(eg._to);
                        }
                    }
                }
                else
                    continue;
                if (cand == null)
                    cand = ccand;
                else
                    cand.IntersectWith(ccand);
                if (cand.Count == 0)
                    return false;
            }

            /*
            if (_cands == null && _pattern._vertexes[_depth]._vLabel.Length != 0)
            {
                cand = new HashSet<int>();
                int[] tempCand = null;
                _g._vertexLabelIndex.TryGetValue(_pattern._vertexes[_depth]._vLabel[0], out tempCand);
                if (tempCand != null)
                    cand.UnionWith(tempCand);
                for (int i = 1; i < _pattern._vertexes[_depth]._vLabel.Length; i++)
                {
                    _g._vertexLabelIndex.TryGetValue(_pattern._vertexes[_depth]._vLabel[i], out tempCand);
                    if (tempCand != null)
                        cand.IntersectWith(tempCand);
                }
            }

            if (cand != null && cand.Count == 0)
                return false;
            //?
            //inefficient
            if (_cands != null && _cands[_depth]!=null)
                cand.IntersectWith(_cands[_depth]);
            */
            if (cand == null)
            {
                cand = new HashSet<int>();
                if(_cands==null||_cands[_depth]==null)
                {
                    for (int i = 0; i < g2._vertexes.Length; i++)
                        cand.Add(i);
                }
                else
                    cand.UnionWith(_cands[_depth]);
                for (int i = 0; i < _depth; i++)
                    cand.Remove(_vMap[i]);
            }

            foreach (int tryvid in cand)
            {
                bool flag = true;
                foreach (int vlid in _pattern._vertexes[_depth]._vLabel)
                {
                    if (!g2._vertexes[tryvid]._vLabel.Contains(vlid))
                    {
                        flag = false;
                        break;
                    }
                }
                if (!flag)
                    continue;

                _vMap[_depth] = tryvid;
                _depth++;
                if (_enumerateSubgraphs)
                {
                    recursiveSearch();
                }
                else
                {
                    if (recursiveSearch())
                        return true;
                }
                _depth--;
                _vMap[_depth] = -1;
            }
            return false;
        }
    }

    public class FrequentNeighborhoodMining
    {      
        IndexedGraph _g;
        SubGraphTest _subGTester;
        NodeInvariantIndex _niindex;
        public FrequentNeighborhoodMining(IndexedGraph g)
        {
            _g=g;
            _subGTester=new SubGraphTest();
            _niindex = new NodeInvariantIndex(_g);
        }

        SubGraphTest _subgraphEnumerator = new SubGraphTest(true, MapOperationInstances.RestoreElement);

        List<int> GetSupportedVertex(Graph pattern, IndexedGraph data)
        {
            List<int> ret = new List<int>();

            return ret;
        }

        public List<IndexedGraph> GetAllJoins(Graph g1, IndexedGraph g2, object delta1)
        {
            _subgraphEnumerator.MatchNeighborhood(g1, g2, 0, delta1);
            return new List<IndexedGraph>(_subgraphEnumerator._rets.Cast<IndexedGraph>());
        }

        List<IndexedGraph> JoinGraphPair(Graph g1, IndexedGraph g2)
        {
            int i;
            List<IndexedGraph> ret = new List<IndexedGraph>();

            Vertex[] newVertex=new Vertex[g2._vertexes.Length+1],
                originalVertex=g2._vertexes;
            originalVertex.CopyTo(newVertex, 0);
            newVertex[newVertex.Length - 1] = new Vertex();
            g2._vertexes = newVertex;

            for(int j=0;j<g1._vertexes.Length;j++)
            {
                Vertex v=g1._vertexes[j];
                if(v._vLabel.Length==0)
                    continue;
                int[] restLabels = new int[v._vLabel.Length - 1],
                    originalLabels= v._vLabel;
                v._vLabel=restLabels;
                for(i=0;i<restLabels.Length;i++)
                    restLabels[i]=originalLabels[i+1];

                for(i=0;i<originalLabels.Length;i++)
                {
                    if(i>=1)
                        restLabels[i-1]=originalLabels[i-1];
                    VLabel vlabel = new VLabel();
                    vlabel._vid = j; vlabel._vlid = originalLabels[i];

                    ret.AddRange(GetAllJoins(g1, g2, vlabel));
                }
                v._vLabel = originalLabels;
            }

            if (g1._edges.Length > 0)
            {
                Edge[] restEdges = new Edge[g1._edges.Length - 1],
                    originalEdges = g1._edges;
                g1._edges = restEdges;
                for (i = 0; i < restEdges.Length; i++)
                    restEdges[i] = originalEdges[i];
                Edge laste = originalEdges[originalEdges.Length - 1];
                Vertex lastv1 = g1._vertexes[laste._from],
                    lastv2 = g1._vertexes[laste._to];
                for (int j = 0; j < originalEdges.Length; j++)
                {
                    if (j < originalEdges.Length - 1)
                        restEdges[j] = originalEdges[originalEdges.Length - 1];
                    Edge e = originalEdges[j];

                    Vertex v1 = g1._vertexes[e._from],
                        v2 = g1._vertexes[e._to];

                    int[] restOutEdge = new int[v1._outEdge.Length - 1],
                        restInEdge = new int[v2._inEdge.Length - 1],
                        originalOutEdge = v1._outEdge,
                        originalInEdge = v2._inEdge;

                    i = 0;
                    foreach (int eid in originalOutEdge)
                        if (eid != j)
                            restOutEdge[i++] = eid;
                    v1._outEdge = restOutEdge;
                    i = 0;
                    foreach (int eid in originalInEdge)
                        if (eid != j)
                            restInEdge[i++] = eid;
                    v2._inEdge = restInEdge;

                    int[] restOutEdgeLast,restInEdgeLast,
                        originalOutEdgeLast = lastv1._outEdge,
                        originalInEdgeLast = lastv2._inEdge;

                    if (j < originalEdges.Length - 1)
                    {
                        restOutEdgeLast = new int[lastv1._outEdge.Length];
                        restInEdgeLast = new int[lastv2._inEdge.Length];

                        for (i = 0; i < lastv1._outEdge.Length; i++)
                            if (lastv1._outEdge[i] == originalEdges.Length - 1)
                                restOutEdgeLast[i] = j;
                            else
                                restOutEdgeLast[i] = lastv1._outEdge[i];
                        lastv1._outEdge = restOutEdgeLast;
                        for (i = 0; i < lastv2._inEdge.Length; i++)
                            if (lastv2._inEdge[i] == originalEdges.Length - 1)
                                restInEdgeLast[i] = j;
                            else
                                restInEdgeLast[i] = lastv2._inEdge[i];
                        lastv2._inEdge = restInEdgeLast;
                    }

                    ret.AddRange(GetAllJoins(g1, g2, e));

                    if (j < originalEdges.Length - 1)
                    {
                        restEdges[j] = originalEdges[j];
                        lastv1._outEdge = originalOutEdgeLast;
                        lastv2._inEdge = originalInEdgeLast;
                    }
                    v1._outEdge = originalOutEdge;
                    v2._inEdge = originalInEdge;
                }
                g1._edges = originalEdges;
            }

            g2._vertexes = originalVertex;

            for (i = 0; i < ret.Count; i++)
            {
                Graph g = ret[i];
                if (g._vertexes[g._vertexes.Length - 1].Isolated())
                {
                    Vertex[] newV = new Vertex[g._vertexes.Length - 1];
                    for (int j = 0; j < newV.Length; j++)
                        newV[j] = g._vertexes[j];
                    g._vertexes = newV;
                }
            }
            return ret;
        }

        public List<Tuple<IndexedGraph, double, double>> Mine(double minRecall, int maxSize, int[] interestedVertex)
        {
            int supp = (int)(Math.Ceiling(interestedVertex.Length * minRecall));
            if (supp < 2)
                supp = 2;
            List<Tuple<IndexedGraph, double, double>> ret = new List<Tuple<IndexedGraph, double, double>>();
            foreach (var tup in Mine(supp, maxSize, interestedVertex))
            {
                List<List<int>> ccands = _niindex.GetCandidatesForeachVertex(tup.Item1);

                if (ccands.Exists(e => e!=null&&e.Count == 0))
                    continue;

                _subGTester._cands = ccands;

                //int cnt = 0;
                //foreach (int i in ccands[0])
                //    if (_subGTester.MatchNeighborhood(tup.Item1, _g, i))
                //        cnt++;
                int cnt = 1;

                _subGTester._cands = null;

                ret.Add(new Tuple<IndexedGraph, double, double>(
                    tup.Item1,
                    tup.Item2 * 1.0 / interestedVertex.Length,
                    tup.Item2 * 1.0 / cnt));
            }
            return ret;
        }

        public List<Tuple<IndexedGraph, List<int>>> MineAndQBE(int[] interestedVertex,int maxSize)
        {
            int supp = interestedVertex.Length;
            HashSet<int> exampleSet = new HashSet<int>(interestedVertex);
            List<Tuple<IndexedGraph, List<int>>> ret = new List<Tuple<IndexedGraph, List<int>>>();
            foreach (var tup in Mine(supp, maxSize, interestedVertex))
            {
                List<List<int>> ccands = _niindex.GetCandidatesForeachVertex(tup.Item1);

                if (ccands.Exists(e => e != null && e.Count == 0))
                    continue;

                _subGTester._cands = ccands;

                List<int> peers = new List<int>();

                foreach (int i in ccands[0])
                    if (_subGTester.MatchNeighborhood(tup.Item1, _g, i)&&!exampleSet.Contains(i))
                        peers.Add(i);

                _subGTester._cands = null;

                ret.Add(new Tuple<IndexedGraph, List<int>>(
                    tup.Item1,
                    peers));
            }
            return ret;
        }

        public List<Tuple<IndexedGraph, double,double>> Mine(double minRecall, int maxSize, int interestedVLabel)
        {
            int[] constraintedV=_g._vertexLabelIndex[interestedVLabel];
            int supp=(int)(Math.Ceiling(constraintedV.Length*minRecall));
            if(supp<2)
                supp=2;
            List<Tuple<IndexedGraph,double,double>> ret= new List<Tuple<IndexedGraph, double,double>>();
            foreach (var tup in Mine(supp, maxSize, constraintedV).Where(
                e => !e.Item1._vertexes[0]._vLabel.Contains(interestedVLabel)
                ))
            {
                List<List<int>> ccands = _niindex.GetCandidatesForeachVertex(tup.Item1);

                if (ccands.Exists(e => e!=null&&e.Count == 0))
                    continue;

                _subGTester._cands = ccands;

                int cnt = 0;
                foreach (int i in ccands[0])
                    if (_subGTester.MatchNeighborhood(tup.Item1, _g, i))
                        cnt++;
                
                _subGTester._cands = null;

                ret.Add(new Tuple<IndexedGraph, double, double>(
                    tup.Item1,
                    tup.Item2*1.0/constraintedV.Length,
                    tup.Item2*1.0/cnt));
            }
            return ret;
        }

        //public List<Tuple<IndexedGraph, int>> Mine(int minSupp, int maxSize, int[] constraintVSet)
        //{
        //    DateTime begin = DateTime.Now;

        //    //FrequentPathMining fpm = new FrequentPathMiningDepth();
        //    FrequentPathMining fpm = new FrequentPathMiningBreadth();

        //    fpm.Init(_g, minSupp, maxSize, constraintVSet);
        //    //fpm.Init(_g, minSupp, maxSize);

        //    Console.WriteLine("{0} seconds. {1} path results.",
        //        (DateTime.Now - begin).TotalSeconds,
        //        fpm._resultCache.Count);

        //    List<Tuple<IndexedGraph, int>> ret = new List<Tuple<IndexedGraph, int>>();

        //    Console.WriteLine("Adding {0} paths.", fpm.GetPath(1).Count);
        //    List<IndexedGraph> lastResults = fpm.GetPath(1);

        //    ret.AddRange(fpm.GetPathAndSupp(1));

        //    for (int size = 2; size <= maxSize; size++)
        //    {
        //        begin = DateTime.Now;
        //        Console.WriteLine("Computing Size-{0} Candidate Patterns.", size);
        //        List<IndexedGraph> tempResults = new List<IndexedGraph>();
        //        for (int i = 0; i < lastResults.Count; i++)
        //        {
        //            for (int j = i; j < lastResults.Count; j++)
        //            {
        //                List<IndexedGraph> newPatterns = null;
        //                if (j == i)
        //                {
        //                    Graph tempG = lastResults[i].ShallowCopy();
        //                    newPatterns = JoinGraphPair(tempG, lastResults[i]);
        //                }
        //                else
        //                    newPatterns = JoinGraphPair(lastResults[i], lastResults[j]);
        //                foreach (IndexedGraph pattern in newPatterns)
        //                {
        //                    bool hasDup = false;
        //                    foreach (IndexedGraph pattern1 in tempResults)
        //                        if (_subGTester.MatchNeighborhood(pattern, pattern1, 0))
        //                        {
        //                            hasDup = true;
        //                            break;
        //                        }
        //                    if (!hasDup)
        //                        tempResults.Add(pattern);
        //                }
        //            }
        //        }
        //        Console.WriteLine("{0} seconds. {1} candidates.",
        //            (DateTime.Now - begin).TotalSeconds,
        //            tempResults.Count);
        //        begin = DateTime.Now;
        //        Console.WriteLine("Validating Size-{0} Candidate Patterns.", size);

        //        lastResults.Clear();
        //        foreach (IndexedGraph pattern in tempResults)
        //        {
        //            int supp = 0;

        //            List<List<int>> ccands = _niindex.GetCandidatesForeachVertex(pattern);

        //            if (ccands.Exists(e => e != null && e.Count == 0))
        //                continue;

        //            if (ccands[0].Count < minSupp)
        //                continue;

        //            _subGTester._cands = ccands;

        //            foreach (int i in constraintVSet)
        //                if (_subGTester.MatchNeighborhood(pattern, _g, i))
        //                    supp++;

        //            _subGTester._cands = null;

        //            if (supp >= minSupp)
        //            {
        //                ret.Add(new Tuple<IndexedGraph, int>(pattern, supp));

        //                //pattern.Print();
        //                //Console.WriteLine(supp+"\n");

        //                lastResults.Add(pattern);
        //            }
        //        }
        //        Console.WriteLine("{0} seconds. {1} results.",
        //            (DateTime.Now - begin).TotalSeconds,
        //            lastResults.Count);
        //        begin = DateTime.Now;
        //        lastResults.AddRange(fpm.GetPath(size));
        //        Console.WriteLine("Adding {0} paths.", fpm.GetPath(size).Count);
        //        //lastResults.Sort((x, y) => x._vertexes.Length - y._vertexes.Length);
        //        ret.AddRange(fpm.GetPathAndSupp(size));
        //        if (lastResults.Count == 0)
        //            break;
        //    }
        //    return ret;
        //}

        public List<Tuple<IndexedGraph, int>> Mine(int minSupp, int maxSize, int[] constraintVSet)
        {
            bool useVIDList = true;

            List<int> constraintVSetList = constraintVSet.ToList();

            DateTime begin = DateTime.Now;

            //FrequentPathMining fpm = new FrequentPathMiningDepth();
            FrequentPathMining fpm = new FrequentPathMiningBreadth();

            fpm.Init(_g, minSupp, maxSize, constraintVSet,useVIDList);
            //fpm.Init(_g, minSupp, maxSize);

            Console.WriteLine("{0} seconds. {1} path results.",
                (DateTime.Now - begin).TotalSeconds,
                fpm._resultCache.Count);

            List<Tuple<IndexedGraph, int>> ret = new List<Tuple<IndexedGraph, int>>();

            Console.WriteLine("Adding {0} paths.", fpm.GetPathAndInfo(1).Count);
            List<Tuple<IndexedGraph, List<int>>> lastResults = fpm.GetPathAndVID(1);

            ret.AddRange(fpm.GetPathAndCount(1));

            for (int size = 2; size <= maxSize; size++)
            {
                begin = DateTime.Now;
                Console.WriteLine("Computing Size-{0} Candidate Patterns.", size);
                List<Tuple<IndexedGraph, List<int>>> tempResults = new List<Tuple<IndexedGraph, List<int>>>();
                for (int i = 0; i < lastResults.Count; i++)
                {
                    for (int j = i; j < lastResults.Count; j++)
                    {
                        List<IndexedGraph> newPatterns = null;
                        List<int> vids = null;
                        if (j == i)
                        {
                            Graph tempG = lastResults[i].Item1.ShallowCopy();
                            newPatterns = JoinGraphPair(tempG, lastResults[i].Item1);
                            vids = lastResults[i].Item2;
                        }
                        else
                        {
                            if (useVIDList)
                            {
                                vids = Tools.MergeSortedArray(lastResults[i].Item2, lastResults[j].Item2);
                                if (vids.Count < minSupp)
                                    continue;
                            }
                            newPatterns = JoinGraphPair(lastResults[i].Item1, lastResults[j].Item1);
                        }
                        foreach (IndexedGraph pattern in newPatterns)
                        {
                            bool hasDup = false;
                            foreach (var pair in tempResults)
                                if (_subGTester.MatchNeighborhood(pattern, pair.Item1, 0))
                                {
                                    hasDup = true;
                                    break;
                                }
                            if (!hasDup)
                                tempResults.Add(new Tuple<IndexedGraph, List<int>>(pattern, vids));
                        }
                    }
                }
                Console.WriteLine("{0} seconds. {1} candidates.",
                    (DateTime.Now - begin).TotalSeconds,
                    tempResults.Count);
                begin = DateTime.Now;
                Console.WriteLine("Validating Size-{0} Candidate Patterns.", size);

                lastResults.Clear();
                foreach (var pair in tempResults)
                {
                    int supp = 0;

                    List<List<int>> ccands = _niindex.GetCandidatesForeachVertex(pair.Item1);

                    if (ccands.Exists(e => e != null && e.Count == 0))
                        continue;

                    if (ccands[0].Count < minSupp)
                        continue;

                    _subGTester._cands = ccands;

                    List<int> vids=null, filteredVids=null;
                    if (useVIDList)
                    {
                        filteredVids = new List<int>();
                        vids = pair.Item2;
                    }
                    else
                        vids = constraintVSetList;

                    foreach (int i in vids)
                        if (_subGTester.MatchNeighborhood(pair.Item1, _g, i))
                        {
                            supp++;
                            if(useVIDList)
                                filteredVids.Add(i);
                        }

                    _subGTester._cands = null;

                    if (supp >= minSupp)
                    {
                        ret.Add(new Tuple<IndexedGraph, int>(pair.Item1, supp));

                        //pattern.Print();
                        //Console.WriteLine(supp+"\n");

                        lastResults.Add(new Tuple<IndexedGraph, List<int>>(pair.Item1, filteredVids));
                    }
                }
                Console.WriteLine("{0} seconds. {1} results.",
                    (DateTime.Now - begin).TotalSeconds,
                    lastResults.Count);
                begin = DateTime.Now;

                var addpath = fpm.GetPathAndVID(size);
                lastResults.AddRange(addpath);

                Console.WriteLine("Adding {0} paths.", addpath.Count);
                //lastResults.Sort((x, y) => x._vertexes.Length - y._vertexes.Length);
                ret.AddRange(fpm.GetPathAndCount(size));
                if (lastResults.Count == 0)
                    break;
            }
            return ret;
        }

        public List<Tuple<IndexedGraph, int>> Mine(int minSupp, int maxSize)
        {
            int[] constraintVSet = new int[_g._vertexes.Length];
            for (int i = 0; i < _g._vertexes.Length; i++)
                constraintVSet[i] = i;
            return Mine(minSupp, maxSize, constraintVSet);
        }

        public List<Tuple<IndexedGraph, List<int>>> MineEgonet(int minSupp, int maxSize, int maxRadius, int[] constraintVSet)
        {
            //bool useVIDList = false;

            List<int> constraintVSetList = constraintVSet.ToList();

            DateTime begin = DateTime.Now;

            //FrequentPathMining fpm = new FrequentPathMiningDepth();
            FrequentPathMining fpm = new FrequentPathMiningBreadth();

            fpm.Init(_g, minSupp, maxSize, constraintVSet, true, maxRadius);
            //fpm.Init(_g, minSupp, maxSize);

            Console.WriteLine("{0} seconds. {1} path results.",
                (DateTime.Now - begin).TotalSeconds,
                fpm._resultCache.Count);


            List<Tuple<IndexedGraph, List<int>>> ret = new List<Tuple<IndexedGraph, List<int>>>();

            Console.WriteLine("Adding {0} paths.", fpm.GetPathAndVID(1).Count);
            List<Tuple<IndexedGraph, List<int>>> lastResults = fpm.GetPathAndVID(1);

            ret.AddRange(fpm.GetPathAndVID(1));

            //IndexedGraph z = new IndexedGraph();
            //z.Read(@"E:\uqjhan5\1.txt");
            //SubGraphTest sgt1 = new SubGraphTest();

            for (int size = 2; size <= maxSize; size++)
            {
                begin = DateTime.Now;
                Console.WriteLine("Computing Size-{0} Candidate Patterns.", size);
                List<Tuple<IndexedGraph, List<int>>> tempResults = new List<Tuple<IndexedGraph, List<int>>>();

                for (int i = 0; i < lastResults.Count; i++)
                {
                    for (int j = i; j < lastResults.Count; j++)
                    {
                        List<IndexedGraph> newPatterns = null;
                        List<int> vids = null;
                        if (j == i)
                        {
                            Graph tempG = lastResults[i].Item1.ShallowCopy();
                            newPatterns = JoinGraphPair(tempG, lastResults[i].Item1);
                            vids = lastResults[i].Item2;
                        }
                        else
                        {
                            vids = Tools.MergeSortedArray(lastResults[i].Item2, lastResults[j].Item2);
                            if (vids.Count < minSupp)
                                continue;
                            newPatterns = JoinGraphPair(lastResults[i].Item1, lastResults[j].Item1);
                        }
                        foreach (IndexedGraph pattern in newPatterns)
                        {
                            //if (sgt1.MatchNeighborhood(z, pattern, 0))
                            //{
                            //    Console.WriteLine();
                            //}
                            bool hasDup = false;
                            foreach (var pair in tempResults)
                                if (_subGTester.MatchNeighborhood(pattern, pair.Item1, 0))
                                {
                                    hasDup = true;
                                    break;
                                }
                            if (!hasDup)
                                tempResults.Add(new Tuple<IndexedGraph, List<int>>(pattern, vids));
                        }
                    }
                }
                Console.WriteLine("{0} seconds. {1} candidates.",
                    (DateTime.Now - begin).TotalSeconds,
                    tempResults.Count);
                begin = DateTime.Now;
                Console.WriteLine("Validating Size-{0} Candidate Patterns.", size);

                //Compute Zipper Patterns
                List<Tuple<IndexedGraph, List<int>>> zipperPatterns = new List<Tuple<IndexedGraph, List<int>>>();
                if (size > maxRadius + 1 && size <= 2 * maxRadius + 1)
                {
                    SubGraphTest sgt = new SubGraphTest(true, MapOperationInstances.GetZipperHeads);
                    foreach (var tup in lastResults)
                    {
                        //if(sgt1.MatchNeighborhood(z,tup.Item1,0))
                        //    tup.Item1.Print();
                        var headPosList = ZipperHandler.DetectPotentialZipper(tup.Item1, maxRadius);
                        if (headPosList == null)
                            continue;
                        List<int> vids = null;
                        Dictionary<Tuple<int, int>, List<int>> mapELabel2Vids = new Dictionary<Tuple<int, int>, List<int>>();
                        vids = tup.Item2;
                        foreach (int i in vids)
                        {
                            sgt.MatchNeighborhood(tup.Item1, _g, i, headPosList);
                            HashSet<Tuple<int, int>> eLabels = new HashSet<Tuple<int, int>>();
                            sgt._rets.ForEach(e => eLabels.Add((Tuple<int, int>)e));
                            foreach (var pair in eLabels)
                            {
                                if (!mapELabel2Vids.ContainsKey(pair))
                                {
                                    mapELabel2Vids[pair] = new List<int>();
                                }
                                mapELabel2Vids[pair].Add(i);
                            }
                        }
                        foreach (var pair in mapELabel2Vids)
                            if (pair.Value.Count >= minSupp)
                            {
                                Edge newEdge = new Edge();

                                newEdge._from = headPosList[pair.Key.Item2].Item1;
                                newEdge._to = headPosList[pair.Key.Item2].Item2;

                                newEdge._eLabel = pair.Key.Item1;
                                IndexedGraph g = tup.Item1.ShallowCopy();
                                Edge[] newEdgeList = new Edge[g._edges.Length + 1];
                                g._edges.CopyTo(newEdgeList, 0);
                                newEdgeList[newEdgeList.Length - 1] = newEdge;
                                g._edges = newEdgeList;

                                Vertex v1 = g._vertexes[newEdge._from];
                                int[] newOutEdgeList = new int[v1._outEdge.Length + 1];
                                v1._outEdge.CopyTo(newOutEdgeList, 0);
                                newOutEdgeList[newOutEdgeList.Length - 1] = newEdgeList.Length - 1;
                                v1._outEdge = newOutEdgeList;

                                Vertex v2 = g._vertexes[newEdge._to];
                                int[] newInEdgeList = new int[v2._inEdge.Length + 1];
                                v2._inEdge.CopyTo(newInEdgeList, 0);
                                newInEdgeList[newInEdgeList.Length - 1] = newEdgeList.Length - 1;
                                v2._inEdge = newInEdgeList;
                                g.GenIndex();

                                zipperPatterns.Add(new Tuple<IndexedGraph, List<int>>(g, pair.Value));
                            }
                    }
                }
                lastResults.Clear();
                foreach (var pair in tempResults)
                {
                    int supp = 0;

                    List<List<int>> ccands = _niindex.GetCandidatesForeachVertex(pair.Item1);

                    if (ccands.Exists(e => e != null && e.Count == 0))
                        continue;

                    //if (ccands[0].Count < minSupp)
                    //continue;

                    _subGTester._cands = ccands;

                    List<int> vids = null, filteredVids = null;
                    filteredVids = new List<int>();
                    vids = pair.Item2;

                    foreach (int i in vids)
                        if (_subGTester.MatchNeighborhood(pair.Item1, _g, i))
                        {
                            supp++;
                            filteredVids.Add(i);
                        }

                    _subGTester._cands = null;

                    if (supp >= minSupp)
                    {
                        ret.Add(new Tuple<IndexedGraph, List<int>>(pair.Item1, filteredVids));

                        //pattern.Print();
                        //Console.WriteLine(supp+"\n");

                        lastResults.Add(new Tuple<IndexedGraph, List<int>>(pair.Item1, filteredVids));
                    }
                }
                Console.WriteLine("{0} seconds. {1} results.",
                    (DateTime.Now - begin).TotalSeconds,
                    lastResults.Count);
                begin = DateTime.Now;

                if (size <= maxRadius + 1)
                {
                    var addpath = fpm.GetPathAndVID(size);
                    lastResults.AddRange(addpath);

                    Console.WriteLine("Adding {0} paths.", addpath.Count);
                    //lastResults.Sort((x, y) => x._vertexes.Length - y._vertexes.Length);
                    ret.AddRange(fpm.GetPathAndVID(size));
                }
                else
                {
                    //add Zipper Patterns
                    Console.WriteLine("Adding {0} Zippers.", zipperPatterns.Count);
                    lastResults.AddRange(zipperPatterns);
                    ret.AddRange(zipperPatterns);
                }
                if (lastResults.Count == 0)
                    break;
            }
            return ret;
        }
        public List<Tuple<IndexedGraph, List<int>>> MineEgonet(int minSupp, int maxSize, int maxRadius)
        {
            int[] constraintVSet = new int[_g._vertexes.Length];
            for (int i = 0; i < _g._vertexes.Length; i++)
                constraintVSet[i] = i;
            return MineEgonet(minSupp, maxSize, maxRadius, constraintVSet);
        }
    }

}
