﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VagueRegionModelling.DataOperator
{
    /// <summary>
    /// 聚类后簇的集合
    /// </summary>
    public class Clusters
    {
        private List<Cluster> m_clusters;

        public Clusters()
        {
            m_clusters = new List<Cluster>();
        }

        public List<Cluster> GetClusters()
        {
            return m_clusters;
        }

        public Cluster GetCluster(int index)
        {
            if (index >= 0 && index < m_clusters.Count)
                return m_clusters[index];
            else
                return null;
        }

        public int GetClusterCount()
        {
            return m_clusters.Count;
        }
        /// <summary>
        /// 添加新簇
        /// </summary>
        /// <param name="newCluster"></param>
        public void AddCluster(Cluster newCluster)
        {
            m_clusters.Add(newCluster);
        }
    }
}
