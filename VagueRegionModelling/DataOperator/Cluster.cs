﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ESRI.ArcGIS.Geometry;

namespace VagueRegionModelling.DataOperator
{
    /// <summary>
    /// 聚类得到的一个簇
    /// </summary>
    public class Cluster
    {
        private List<IPoint> m_pointsList; //簇的点集
        private IPolygon m_convexHull; //簇的凸包
        private int m_nClusterIndex; //簇的类别

        public Cluster()
        {
            m_pointsList = new List<IPoint>();
            m_convexHull = null;
            m_nClusterIndex = -999;
        }

        #region Get方法集合
        public List<IPoint> GetPointsList()
        {
            return m_pointsList;
        }

        public IPolygon GetConvexHull()
        {
            if (m_pointsList != null && m_convexHull == null)
            {
                m_convexHull = CreateConvexHull();
            }
            return m_convexHull;
        }

        public int GetClusterIndex()
        {
            return m_nClusterIndex;
        }
        #endregion

        #region Set方法集合
        public void SetPointsList(List<IPoint> pointsList)
        {
            m_pointsList = pointsList;
        }

        public void SetClusterIndex(int index)
        {
            m_nClusterIndex = index;
        }
        #endregion

        /// <summary>
        /// 向簇中添加聚类点
        /// </summary>
        /// <param name="newPoint"></param>
        public void AddPoint(IPoint newPoint)
        {
            m_pointsList.Add(newPoint);
        }

        /// <summary>
        /// 通过簇的点集计算簇的凸包
        /// </summary>
        /// <returns></returns>
        public IPolygon CreateConvexHull()
        {
            //少于3个点就不做
            IPolygon convexHull = new PolygonClass();
            //待完善
            return convexHull;
        }
    }
}