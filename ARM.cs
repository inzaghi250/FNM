using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;

namespace FNM
{
    class ARM
    {
        List<Tuple<HashSet<int>,double>> _baskets = new List<Tuple<HashSet<int>,double>>();

        int[] SetToSortedArray(HashSet<int> set)
        {
            if(set.Count==0)
                return null;
            int[] ret=new int[set.Count];
            int i=0;
            foreach(int e in set.OrderBy(e=>e))
                ret[i++]=e;
            return ret;
        }

        bool HasSamePrefix(int[] a,int[] b)
        {
            for(int i=0;i<a.Length-1;i++)
                if(a[i]!=b[i])
                    return false;
            return true;
        }

        public void Read(string file)
        {
            _baskets.Clear();
            bool weighted=false;
            StreamReader sr=new StreamReader(file);
            string line;
            if(sr.ReadLine()=="Weighted")
                weighted=true;
            while((line=sr.ReadLine())!=null)
            {
                double weight=1;
                string[] parts;
                if(weighted)
                {
                    parts=line.Split("\t".ToCharArray());
                    weight=double.Parse(parts[1]);
                    line=parts[0];
                }
                parts=line.Split(",".ToCharArray());
                HashSet<int> basket=new HashSet<int>();
                foreach(string id in parts)
                    basket.Add(int.Parse(id));
                _baskets.Add(new Tuple<HashSet<int>,double>(basket,weight));
            }
            sr.Close();
        }

        public static void Show(List<Tuple<List<int>, int, double, double>> result, Dictionary<int,string> names=null)
        {
            foreach (var tup in result)
            {
                foreach (int pre in tup.Item1)
                {
                    if(names==null||!names.ContainsKey(pre))
                        Console.Write(pre);
                    else
                        Console.Write("\\"+names[pre]);
                    Console.Write(" ");
                }
                Console.Write("-> \\");
                if (names == null || !names.ContainsKey(tup.Item2))
                    Console.Write(tup.Item2);
                else
                    Console.Write(names[tup.Item2]);
                Console.Write(" , s=");
                Console.Write(tup.Item3);
                Console.Write(", p=");
                Console.WriteLine(tup.Item4);
            }
        }

        public List<Tuple<List<int>, int, double, double>> Mine(double support, double confidence)
        {
            List<Tuple<List<int>, int, double, double>> ret = new List<Tuple<List<int>, int, double, double>>();

            List<Tuple<int[], double>> freqSets = new List<Tuple<int[], double>>();
            Dictionary<int,double> freqTable=new Dictionary<int,double>();

            Comparison<Tuple<int[],double>> comp=new Comparison<Tuple<int[],double>>((e1,e2) =>
                {
                    if(e1.Item1.Length!=e2.Item1.Length||e1.Item1.Length==0)
                        throw(new Exception());
                    for(int i=0;i<e1.Item1.Length;i++)
                    {
                        if(e1.Item1[i]<e2.Item1[i])
                            return -1;
                        if(e1.Item1[i]>e2.Item1[i])
                            return 1;
                    }
                    return 0;
                });

            double sumWt = 0;
            
            foreach(var pair in _baskets)
            {
                foreach(int id in pair.Item1)
                {
                    if(!freqTable.ContainsKey(id))
                        freqTable[id]=0;
                    freqTable[id]+=pair.Item2;
                }
                sumWt+=pair.Item2;
            }

            foreach(var pair in freqTable)
            {
                if(pair.Value<sumWt*support)
                    continue;
                freqSets.Add(new Tuple<int[],double>(new int[1]{pair.Key},pair.Value));
            }

            while (true)
            {
                if (freqSets.Count == 0)
                    break;
                freqSets.Sort(comp);
                int i = 0;
                List<Tuple<int[], double>> tempFreqSets = new List<Tuple<int[], double>>();

                for (i = 0; i < freqSets.Count; i++)
                {
                    int j = i + 1;
                    while (j<freqSets.Count&&HasSamePrefix(freqSets[i].Item1, freqSets[j].Item1))
                    {
                        int[] newSet = new int[freqSets[i].Item1.Length + 1];
                        for (int k = 0; k < freqSets[i].Item1.Length; k++)
                            newSet[k] = freqSets[i].Item1[k];
                        newSet[newSet.Length - 1] = freqSets[j].Item1[freqSets[j].Item1.Length - 1];

                        double sumWeight = 0;
                        double[] partialWeight = new double[newSet.Length];
                        foreach (var pair in _baskets)
                        {
                            int remained=-1;
                            bool flag = false;
                            foreach (int id in newSet)
                            {
                                if (!pair.Item1.Contains(id))
                                {
                                    if (remained == -1)
                                        remained = id;
                                    else
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                            }
                            if (flag)
                                continue;

                            if (remained== -1)
                            {
                                sumWeight += pair.Item2;
                                for (int k = 0; k < partialWeight.Length; k++)
                                    partialWeight[k] += pair.Item2;
                            }
                            else
                            {
                                for (int k = 0; k < partialWeight.Length; k++)
                                    if (newSet[k] == remained)
                                        partialWeight[k] += pair.Item2;
                            }
                        }
                        if (sumWeight >= support * sumWt)
                        {
                            tempFreqSets.Add(new Tuple<int[], double>(newSet, sumWeight));
                            
                            /*
                            for (int k = 0; k < partialWeight.Length; k++)
                            {
                                if (sumWeight / partialWeight[k] >= confidence)
                                {
                                    List<int> pres = new List<int>(newSet.Where((num, index) => index != k));
                                    ret.Add(new Tuple<List<int>, int, double, double>(pres, newSet[k],
                                        sumWeight / sumWt,
                                        sumWeight / partialWeight[k]));
                                }
                            }
                             * */
                            

                            int mink=0;
                            double min = partialWeight[0];
                            for (int k = 0; k < partialWeight.Length; k++)
                            {
                                if (partialWeight[k] < min)
                                {
                                    mink = k;
                                    min = partialWeight[k];
                                }
                            }
                            if (sumWeight / partialWeight[mink] >= confidence)
                            {
                                List<int> pres = new List<int>(newSet.Where((num, index) => index != mink));
                                ret.Add(new Tuple<List<int>, int, double, double>(pres, newSet[mink],
                                    sumWeight / sumWt,
                                    sumWeight / partialWeight[mink]));
                            }
                        }

                        j++;
                    }
                }
                freqSets = tempFreqSets;
            }

            return ret;
        }

        static void Example()
        {
            ARM arm = new ARM();
            arm.Read(@"D:\Users\v-jiahan\HORM\ex3_basket.withedge.txt");
            var ret = arm.Mine(0.001, 0.3);

            Dictionary<int, string> names = new Dictionary<int, string>();
            StreamReader sr = new StreamReader(@"D:\Users\v-jiahan\HORM\ex3_itemname.withedge.txt");
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] parts = line.Split("\t".ToCharArray());
                names[int.Parse(parts[0])] = parts[1];
            }
            sr.Close();

            ARM.Show(ret, names);
            return;
        }
    }
}
