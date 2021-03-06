﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using System.IO;
using VagueRegionModelling.DataOperator;

namespace VagueRegionModelling.ClusteringAlgorithm
{
    public class DBSCAN
    {
        private List<DBSCANPoint> m_DBSCANPnts = null;  //将IPoint转化为DBSCANPoint进行计算
        private Clusters m_Clusters = new Clusters();   //聚类结果
        private double m_dEps = 0;  //半径
        private int m_nMinPts = 0;  //最小点数
        private DataInformation m_dataInfo; //相关信息

        public DBSCAN(DataInformation dataInfo, double eps, int minPts)
        {
            m_dataInfo = dataInfo;
            m_dEps = eps;
            m_nMinPts = minPts;
        }

        /// <summary>
        /// 进行聚类操作，并获得结果
        /// </summary>
        /// <returns></returns>
        public Clusters GetClusters()
        {
            //建立DBSCANPoints
            m_DBSCANPnts = CreateDBSCANPoints();
            //            test(pnts[1].GetPoint());
            //计算每个数据点相邻的数据点，找出核心点
            FindNeighborPoints();
            //邻域内包含某个核心点的非核心点，定义为边界点
            FindBorderPoints();
            //各个核心点与其邻域内的所有核心点放在同一个簇中 //创建每个簇的凸包
            CreateClusters();
           
            return m_Clusters;
        }

        /// <summary>
        /// 通过IPoint创建DBSCANPoints
        /// </summary>
        /// <returns></returns>
        private List<DBSCANPoint> CreateDBSCANPoints()
        {
            List<DBSCANPoint> pnts = new List<DBSCANPoint>();
            IFeatureLayer featurelayer = m_dataInfo.GetInputLayer() as IFeatureLayer;
            IFeatureCursor featureCursor = featurelayer.FeatureClass.Search(null, false);
            IFeature feature = featureCursor.NextFeature();

            DBSCANPoint pnt;
            while (feature != null)
            {
                pnt = new DBSCANPoint(feature.Shape as IPoint, feature.OID);
                pnts.Add(pnt);
                feature = featureCursor.NextFeature();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(featureCursor);
            return pnts;
        }

        /// <summary>
        /// 计算每个数据点相邻的数据点，找出核心点
        /// </summary>
        private void FindNeighborPoints()
        {
            for (int i = 0; i < m_DBSCANPnts.Count; i++)
            {
                List<DBSCANPoint> neighborPnts = new List<DBSCANPoint>();
                IPoint point = m_DBSCANPnts[i].GetPoint();
                ITopologicalOperator topologicalOperator = point as ITopologicalOperator;
                IGeometry pointBuffer = topologicalOperator.Buffer(m_dEps);//缓冲距离
                topologicalOperator.Simplify();
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = pointBuffer;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
                IFeatureLayer featurelayer = m_dataInfo.GetInputLayer() as IFeatureLayer;
                IFeatureCursor featureCursor = featurelayer.FeatureClass.Search(spatialFilter, false);
                IFeature feature = featureCursor.NextFeature();
                while (feature != null)
                {
                    neighborPnts.Add(GetDBSCANPointByOID(feature.OID));
                    feature = featureCursor.NextFeature();
                }
                m_DBSCANPnts[i].SetNeighborPoints(neighborPnts);
                //标记核心点
                if (m_DBSCANPnts[i].GetNeighborPoints().Count >= m_nMinPts)
                    m_DBSCANPnts[i].SetPointType(1);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(featureCursor);
            }

        }

        /// <summary>
        /// 标记边界点
        /// </summary>
        private void FindBorderPoints()
        {
            for (int i = 0; i < m_DBSCANPnts.Count; i++)
            {
                if (m_DBSCANPnts[i].GetPointType() == 1)
                    continue;
                List<DBSCANPoint> neighbors = m_DBSCANPnts[i].GetNeighborPoints();
                for (int j = 0; j < neighbors.Count; j++)
                {
                    //邻域内包含某个核心点的非核心点，定义为边界点
                    if (neighbors[j].GetPointType() == 1)
                    {
                        m_DBSCANPnts[i].SetPointType(2);
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// 通过一点的OID获得一个DBSCANPoint
        /// </summary>
        /// <param name="OID"></param>
        /// <returns></returns>
        private DBSCANPoint GetDBSCANPointByOID(int OID)
        {
            for (int i = 0; i < m_DBSCANPnts.Count; i++)
            {
                if (m_DBSCANPnts[i].GetOID() == OID)
                {
                    return m_DBSCANPnts[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 通过一点的OID获得一个DBSCANPoint在m_DBSCANPnts的索引号，以便对m_DBSCANPnts进行修改
        /// </summary>
        /// <param name="OID"></param>
        /// <returns></returns>
        private int GetDBSCANPointIndexByOID(int OID)
        {
            for (int i = 0; i < m_DBSCANPnts.Count; i++)
            {
                if (m_DBSCANPnts[i].GetOID() == OID)
                {
                    return i;
                }
            }
            return -999;
        }

        /// <summary>
        /// 将聚类点分为不同的簇
        /// </summary>
        private void CreateClusters()
        {
            int clusterIndex = 1;
            for (int i = 0; i < m_DBSCANPnts.Count; i++)
            {
                if (m_DBSCANPnts[i].IsVisited() == true)
                    continue;

                m_DBSCANPnts[i].SetAsVisited();  //已经访问               
                //作为核心点，根据该点创建一个类别                
                if (m_DBSCANPnts[i].GetPointType() == 1)
                {
                    Cluster cluster = new Cluster();
                    cluster.SetClusterIndex(clusterIndex);
                    cluster = ExpandCluster(cluster, i);
                    cluster.CreateConvexHull(m_dataInfo.GetSpatialReference());
                    m_Clusters.AddCluster(cluster);
                    clusterIndex++;
                }
            }
        }

        /// <summary>
        /// 通过核心点对簇进行扩展
        /// </summary>
        /// <param name="cluster"></param>
        /// <param name="pntIndex"></param>
        /// <returns></returns>
        private Cluster ExpandCluster(Cluster cluster, int pntIndex)
        {
            cluster.AddPoint(m_DBSCANPnts[pntIndex].GetPoint());
            List<DBSCANPoint> neighbors = m_DBSCANPnts[pntIndex].GetNeighborPoints();
            for (int i = 0; i < neighbors.Count; i++)
            {
                int neighborIndex = GetDBSCANPointIndexByOID(neighbors[i].GetOID());
                if (m_DBSCANPnts[neighborIndex].IsVisited() == true)
                    continue;
                m_DBSCANPnts[neighborIndex].SetAsVisited();
                //迭代
                //边界点跟其邻域内的某个核心点放在同一个簇中
                //  if (m_DBSCANPnts[neighborIndex].GetPointType() != 0)
                if (m_DBSCANPnts[neighborIndex].GetPointType() == 1)
                    ExpandCluster(cluster, neighborIndex);
            }
            return cluster;
        }

    }
}
