using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FNM
{
    public class Program
    {
        static void Main(string[] args)
        {
            TestNBMining();
            //TestPathMining();
            return;
        }
        public class ShowingNames
        {
            public Dictionary<int, string> _vertexLabelNames = new Dictionary<int, string>();
            public Dictionary<int, string> _edgeLabelNames = new Dictionary<int, string>();
            public Dictionary<int, string> _vertexNames = new Dictionary<int, string>();
            public ShowingNames(string dir)
            {
                StreamReader sr = new StreamReader(dir + ".names.vertexlabel.txt");
                string line;
                while (true)
                {
                    line = sr.ReadLine();
                    if (line == null)
                        break;
                    string[] parts = line.Split("\t".ToCharArray());
                    _vertexLabelNames[int.Parse(parts[0])] = parts[1];
                }
                sr.Close();
                sr = new StreamReader(dir + ".names.edgelabel.txt");
                while (true)
                {
                    line = sr.ReadLine();
                    if (line == null)
                        break;
                    string[] parts = line.Split("\t".ToCharArray());
                    _edgeLabelNames[int.Parse(parts[0])] = parts[1];
                }
                sr.Close();
                sr = new StreamReader(dir + ".names.vertex.txt");
                while (true)
                {
                    line = sr.ReadLine();
                    if (line == null)
                        break;
                    string[] parts = line.Split("\t".ToCharArray());
                    _vertexNames[int.Parse(parts[0])] = parts[1];
                }
                sr.Close();
            }
        }

        private static void ShowResult(List<Tuple<Path, object>> ret, ShowingNames showName)
        {
            foreach (var pair in ret.OrderByDescending(e => e.Item2))
            {
                Console.Write(pair.Item2 + "\t");
                foreach (Step step in pair.Item1)
                {
                    if (step.Item2 == StepType.Nlabel)
                        Console.Write(showName._vertexLabelNames[step.Item1] + " ");
                    else if (step.Item2 == StepType.Backward)
                        Console.Write(showName._edgeLabelNames[step.Item1] + "- ");
                    else
                        Console.Write(showName._edgeLabelNames[step.Item1] + " ");
                }
                Console.WriteLine();
            }
        }

        static void TestPathMining()
        {
            IndexedGraph g = new IndexedGraph();
            g.Read(@"D:\Users\v-jiahan\HORM\Data\ex3_graph.txt");
            FrequentPathMining fpm=new FrequentPathMiningBreadth();
            fpm.Init(g, 1000, 3,true);
            ShowingNames showname = new ShowingNames(@"D:\Users\v-jiahan\HORM\Data\ex3_graph");
            ShowResult(fpm._resultCache, showname);
        }

        static void TestCocitation(IndexedGraph g)
        {
            for (int i = 6713; i < 923691; i++)
            {
                Vertex author = g._vertexes[i];
                HashSet<Tuple<int, int>> cit = new HashSet<Tuple<int, int>>();
                foreach (int aid in author._inEdge)
                {
                    bool flag = false;
                    Vertex paper = g._vertexes[g._edges[aid]._from];
                    foreach (int eid in paper._outEdge)
                    {
                        Edge e = g._edges[eid];
                        if (!cit.Contains(new Tuple<int, int>(e._to, e._from)))
                            cit.Add(new Tuple<int, int>(e._from, e._to));
                        else
                        {
                            flag = true;
                            Console.WriteLine(i + "\t" + e._from + "\t" + e._to);
                            break;
                        }
                    }
                    if (flag)
                        break;
                }
            }
        }

        static void TestPattern(IndexedGraph g)
        {
            Graph p = new Graph();
            Edge e0=new Edge();
            e0._from=0;
            e0._to=1;
            e0._eLabel=0;
            Edge e1=new Edge();
            e1._from=0;
            e1._to=2;
            e1._eLabel=1;
            Edge e2=new Edge();
            e2._from=3;
            e2._to=2;
            e2._eLabel=1;
            p._edges=new Edge[]{e0,e1,e2};
            Vertex v0 = new Vertex();
            v0._inEdge = Vertex.zeroL;
            v0._outEdge = new int[] { 0,1 };
            v0._vLabel = Vertex.zeroL;
            Vertex v1 = new Vertex();
            v1._inEdge = new int[] { 0 };
            v1._outEdge = Vertex.zeroL;
            v1._vLabel = Vertex.zeroL;
            Vertex v2 = new Vertex();
            v2._inEdge = new int[] { 1, 2 };
            v2._outEdge = Vertex.zeroL;
            v2._vLabel = Vertex.zeroL;
            Vertex v3 = new Vertex();
            v3._inEdge = Vertex.zeroL;
            v3._outEdge = new int[] { 2 };
            v3._vLabel = Vertex.zeroL;
            p._vertexes = new Vertex[] { v0, v1, v2, v3 };
            SubGraphTest sgt = new SubGraphTest();
            int cnt = 0;
            for (int i = 923692; i < 2495971; i++)
                if (!sgt.MatchNeighborhood(p, g, i))
                {
                    cnt++;
                    Console.WriteLine(1.0 * cnt / (i - 923691));
                }
            Console.WriteLine(cnt);
        }

        private static void Shuffle(int[] a)
        {
            Random r = new Random(1);
            for (int i = a.Length - 1; i >= 0; i--)
            {
                int j = (int)(i * r.NextDouble());
                int temp = a[i];
                a[i] = a[j];
                a[j] = temp;
            }
            return;
        }

        private static void TestNBMining()
        {
            IndexedGraph g = new IndexedGraph();
            g.Read(@"E:\MSRA备份\wdm7备份\Data\HORM data\ex3_graph.txt");
            FrequentNeighborhoodMining fnMiner = new FrequentNeighborhoodMining(g);
            var ret = fnMiner.Mine(1000, 4);
            /*
            foreach (var pair in ret)
            {
                pair.Item1.Print();
                Console.WriteLine(pair.Item1.Is_R_EgoNet(1));
                Console.WriteLine();
                Console.ReadKey();
            }
             * */
            Console.WriteLine(100.0*ret.Count(e=>e.Item1.Is_R_EgoNet(2))/ret.Count);
            return;
        }
    }
}
