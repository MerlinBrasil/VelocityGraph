﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VelocityDb;
using VelocityDb.Collection.BTree;
using VelocityDb.Session;
using VertexId = System.Int32;
using EdgeId = System.Int32;
using PropertyTypeId = System.Int32;
using PropertyId = System.Int32;
using TypeId = System.Int32;
using EdgeIdVertexId = System.UInt64;
using Frontenac.Blueprints;
using VelocityDb.Collection;

namespace VelocityGraph
{
  [Serializable]
  public partial class VertexType : OptimizedPersistable
  {
    internal Graph graph;
    string typeName;
    TypeId typeId;
    VelocityDbList<Range<VertexId>> vertecis;
    internal BTreeMap<string, PropertyType> stringToPropertyType;
    internal PropertyType[] vertexProperties;
    BTreeSet<EdgeType> edgeTypes;
    BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>> tailToHeadEdges;
    BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>> headToTailEdges;

    internal VertexType(TypeId aTypeId, string aTypeName, Graph graph)
    {
      this.graph = graph;
      typeId = (TypeId)aTypeId;
      typeName = aTypeName;
      vertecis = new VelocityDbList<Range<VertexId>>();
      stringToPropertyType = new BTreeMap<string, PropertyType>(null, graph.Session);
      edgeTypes = new BTreeSet<EdgeType>(null, graph.Session);
      tailToHeadEdges = new BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>>(null, graph.Session);
      headToTailEdges = new BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>>(null, graph.Session);
      vertexProperties = new PropertyType[0];
    }

    public BTreeSet<EdgeType> EdgeTypes
    {
      get
      {
        return edgeTypes;
      }
    }

    protected EdgeIdVertexId edgeVertexId(Edge edge, VertexId vertexId)
    {
      EdgeIdVertexId id = (EdgeIdVertexId)edge.EdgeId;
      id <<= 32;
      return id + (EdgeIdVertexId)vertexId;
    }

    public Vertex GetVertex(VertexId vertexId)
    {
      Range<VertexId> range = new Range<VertexId>(vertexId, vertexId);
      bool isEqual;
      int pos = vertecis.BinarySearch(range, out isEqual);
      if (pos >= 0)
      {
        range = vertecis[pos];
        if (range.Contains(vertexId))
          return new Vertex(graph, this, vertexId);    
      }
      throw new VertexDoesNotExistException();
    }

    public void NewTailToHeadEdge(EdgeType edgeType, Edge edge, Vertex tail, Vertex head, SessionBase session)
    {
      BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>> map;
      BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>> innerMap;
      BTreeSet<EdgeIdVertexId> set;
      //lock (tailToHeadEdges)
      {
        if (!tailToHeadEdges.TryGetValue(edgeType, out map))
        {
          map = new BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>(null, session);
          innerMap = new BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>(null, session);
          set = new BTreeSet<EdgeIdVertexId>(null, session);
          if (IsPersistent)
          {
            Session.Persist(map, 100);
            Session.Persist(innerMap, 100);
            Session.Persist(set, 1000);
          }
          innerMap.Add(tail.VertexId, set);
          map.Add(head.VertexType, innerMap);
          tailToHeadEdges.Add(edgeType, map);
          edgeTypes.Add(edgeType);
        }
        else if (!map.TryGetValue(head.VertexType, out innerMap))
        {
          innerMap = new BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>(null, session);
          set = new BTreeSet<EdgeIdVertexId>(null, session);
          if (IsPersistent)
          {
            Session.Persist(innerMap, 100);
            Session.Persist(set, 1000);
          }
          innerMap.Add(tail.VertexId, set);
          map.Add(head.VertexType, innerMap);
        }
        else if (!innerMap.TryGetValue(tail.VertexId, out set))
        {
          set = new BTreeSet<EdgeIdVertexId>(null, session);
          if (IsPersistent)
            Session.Persist(set, 1000);
          innerMap.Add(tail.VertexId, set);
        }
        set.Add(edgeVertexId(edge, head.VertexId));
      }
    }

    public void RemoveTailToHeadEdge(Edge edge)
    {
      BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>> map;
      BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>> innerMap;
      BTreeSet<EdgeIdVertexId> set;
      if (tailToHeadEdges.TryGetValue(edge.EdgeType, out map))
        if (map.TryGetValue(edge.Head.VertexType, out innerMap))
          if (innerMap.TryGetValue(edge.Tail.VertexId, out set))
            set.Remove(edgeVertexId(edge, edge.Head.VertexId));
    }

    public void RemoveHeadToTailEdge(Edge edge)
    {
      BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>> map;
      BTreeMap<EdgeId, BTreeSet<EdgeIdVertexId>> innerMap;
      BTreeSet<EdgeIdVertexId> set;
      if (headToTailEdges.TryGetValue(edge.EdgeType, out map))
        if (map.TryGetValue(edge.Tail.VertexType, out innerMap))
          if (innerMap.TryGetValue(edge.Head.VertexId, out set))
            set.Remove(edgeVertexId(edge, edge.Tail.VertexId));
    }

    public void NewHeadToTailEdge(EdgeType edgeType, Edge edge, Vertex tail, Vertex head, SessionBase session)
    {
      BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>> map;
      BTreeMap<EdgeId, BTreeSet<EdgeIdVertexId>> innerMap;
      BTreeSet<EdgeIdVertexId> set;
      //lock (headToTailEdges)
      {
        if (!headToTailEdges.TryGetValue(edgeType, out map))
        {
          map = new BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>(null, session);
          innerMap = new BTreeMap<EdgeId, BTreeSet<EdgeIdVertexId>>(null, session);
          set = new BTreeSet<EdgeIdVertexId>(null, session);
          if (IsPersistent)
          {
            Session.Persist(map, 100);
            Session.Persist(innerMap, 100);
            Session.Persist(set, 1000);
          }
          innerMap.Add(head.VertexId, set);
          map.Add(tail.VertexType, innerMap);
          headToTailEdges.Add(edgeType, map);
          edgeTypes.Add(edgeType);
        }
        else if (!map.TryGetValue(tail.VertexType, out innerMap))
        {
          innerMap = new BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>(null, session);
          set = new BTreeSet<EdgeIdVertexId>(null, session);
          if (IsPersistent)
          {
            Session.Persist(innerMap, 100);
            Session.Persist(set, 1000);
          }
          innerMap.Add(head.VertexId, set);
          map.Add(tail.VertexType, innerMap);
        }
        else if (!innerMap.TryGetValue(head.VertexId, out set))
        {
          set = new BTreeSet<EdgeIdVertexId>(null, session);
          if (IsPersistent)
            Session.Persist(set, 1000);
          innerMap.Add(head.VertexId, set);
        }
        set.Add(edgeVertexId(edge, tail.VertexId));
      }
    }

    public Vertex NewVertex(Graph g)
    {
      Range<VertexId> range;
      VertexId vId = 1;
      switch  (vertecis.Count)
      {
        case 0:
          range = new Range<VertexId>(1, 1);
          vertecis.Add(range);
          break;
        case 1:
          range = vertecis.First();

          if (range.Min == 1)
          {
            vId = range.Max + 1;
            range = new Range<VertexId>(1, vId);
          }
          else
          {
            vId = range.Min - 1;
            range = new Range<VertexId>(vId, range.Max);
          }
          vertecis[0] = range;
          break;
        default:
          {
            range = vertecis.First();
            if (range.Min > 1)
            {
              vId = range.Min - 1;
              range = new Range<VertexId>(vId, range.Max);
            }
            else
            {
              Range<VertexId> nextRange = vertecis[1];
              if (range.Max + 2 == nextRange.Min)
              {
                vertecis.Remove(range);
                vId = range.Max + 1;
                range = new Range<VertexId>(range.Min, nextRange.Max);
              }
              else
              {
                range = new Range<VertexId>(range.Min, range.Max + 1);
                vId = range.Max + 1;
              }
              vertecis.Add(range);
            }
          }
          break;
      }
      return new Vertex(g, this, vId);
    }

    public Dictionary<Vertex, Edge> Traverse(Graph g, Vertex vertex1, EdgeType etype, Direction dir)
    {
      Dictionary<Vertex, Edge> result = new Dictionary<Vertex, Edge>(10);
      if (etype.Directed == false)
      {
        foreach (var pair in etype.edges)
        {
          int[] ids = pair.Value;
          bool headSameVertex = vertex1.VertexType.TypeId == ids[0] && vertex1.VertexId == ids[1];
          bool tailSameVertex = vertex1.VertexType.TypeId == ids[2] && vertex1.VertexId == ids[3];
          Vertex other;
          if (headSameVertex)
          {
            VertexType vt = g.vertexType[ids[2]];
            other = vt.GetVertex(ids[3]);
          }
          else
          {
            if (tailSameVertex == false)
              continue;
            VertexType vt = g.vertexType[ids[0]];
            other = vt.GetVertex(ids[1]);
          }
#if EdgeDebug
          Edge edge = etype.GetEdge(g, pair.Key, vertex1, other);
#else
          Edge edge = new Edge(g, etype, pair.Key, vertex1, other);
#endif
          result.Add(other, edge);
        }
      }
      else
      {
        BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>> map;
        BTreeSet<EdgeIdVertexId> set;
        switch (dir)
        {
          case Direction.Out:
            if (tailToHeadEdges.TryGetValue(etype, out map))
            {
              foreach (KeyValuePair<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>> pair in map)
              {
                BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>> innerMap = pair.Value;
                if (innerMap.TryGetValue(vertex1.VertexId, out set))
                {
                  foreach (EdgeIdVertexId id in set)
                  {
                    Vertex vertex2 = new Vertex(g, pair.Key, (VertexId)id);
                    EdgeId eId = (EdgeId)(id >> 32);
#if EdgeDebug
                    Edge edge = etype.GetEdge(g, eId, vertex1, vertex2);
#else
                    Edge edge = new Edge(g, etype, eId, vertex1, vertex2);
#endif
                    result.Add(vertex2, edge);
                  }
                }
              }
            }
            break;
          case Direction.In:
            if (headToTailEdges.TryGetValue(etype, out map))
            {
              foreach (KeyValuePair<VertexType, BTreeMap<EdgeId, BTreeSet<EdgeIdVertexId>>> pair in map)
              {
                BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>> innerMap = pair.Value;
                if (innerMap.TryGetValue(vertex1.VertexId, out set))
                {
                  foreach (EdgeIdVertexId id in set)
                  {
                    Vertex vertex2 = new Vertex(g, pair.Key, (VertexId)id);
                    EdgeId eId = (EdgeId)(id >> 32);
#if EdgeDebug
                    Edge edge = etype.GetEdge(g, eId, vertex1, vertex2);
#else
                    Edge edge = new Edge(g, etype, eId, vertex1, vertex2);
#endif
                    result.Add(vertex2, edge);
                  }
                }
              }
            }
            break;
          case Direction.Both:
            if (tailToHeadEdges.TryGetValue(etype, out map))
            {
              foreach (KeyValuePair<VertexType, BTreeMap<EdgeId, BTreeSet<EdgeIdVertexId>>> pair in map)
              {
                BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>> innerMap = pair.Value;
                if (innerMap.TryGetValue(vertex1.VertexId, out set))
                {
                  foreach (EdgeIdVertexId id in set)
                  {
                    Vertex vertex2 = new Vertex(g, pair.Key, (VertexId)id);
                    EdgeId eId = (EdgeId)(id >> 32);
#if EdgeDebug
                    Edge edge = etype.GetEdge(g, eId, vertex1, vertex2);
#else
                    Edge edge = new Edge(g, etype, eId, vertex1, vertex2);
#endif
                    result.Add(vertex2, edge);
                  }
                }
              }
            }
            if (headToTailEdges.TryGetValue(etype, out map))
            {
              foreach (KeyValuePair<VertexType, BTreeMap<EdgeId, BTreeSet<EdgeIdVertexId>>> pair in map)
              {
                BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>> innerMap = pair.Value;
                if (innerMap.TryGetValue(vertex1.VertexId, out set))
                {
                  foreach (EdgeIdVertexId id in set)
                  {
                    Vertex vertex2 = new Vertex(g, pair.Key, (VertexId)id);
                    EdgeId eId = (EdgeId)(id >> 32);
#if EdgeDebug
                    Edge edge = etype.GetEdge(g, eId, vertex1, vertex2);
#else
                    Edge edge = new Edge(g, etype, eId, vertex1, vertex2);
#endif
                    result.Add(vertex2, edge);
                  }
                }
              }
            }
            break;
        }
      }
      return result;
    }

    /// <summary>
    /// Creates a new Property. 
    /// </summary>
    /// <param name="name">Unique name for the new Property.</param>
    /// <param name="dt">Data type for the new Property.</param>
    /// <param name="kind">Property kind.</param>
    /// <returns>Unique Property identifier.</returns>
    public PropertyType NewProperty(string name, DataType dt, PropertyKind kind)
    {
      PropertyType aType;
      if (stringToPropertyType.TryGetValue(name, out aType) == false)
      {
        int pos = graph.propertyType.Length;
        graph.Update();
        Array.Resize(ref graph.propertyType, pos + 1);
        Array.Resize(ref vertexProperties, vertexProperties.Length + 1);
        switch (dt)
        {
          case DataType.Boolean:
            aType = new PropertyTypeT<bool>(true, typeId, pos, name, kind, Session);
            break;
          case DataType.Integer:
            aType = new PropertyTypeT<int>(true, typeId, pos, name, kind, Session);
            break;
          case DataType.Long:
            aType = new PropertyTypeT<long>(true, typeId, pos, name, kind, Session);
            break;
          case DataType.Double:
            aType = new PropertyTypeT<double>(true, typeId, pos, name, kind, Session);
            break;
          case DataType.DateTime:
            aType = new PropertyTypeT<DateTime>(true, typeId, pos, name, kind, Session);
            break;
          case DataType.String:
            aType = new PropertyTypeT<string>(true, typeId, pos, name, kind, Session);
            break;
          case DataType.Object:
            aType = new PropertyTypeT<object>(true, typeId, pos, name, kind, Session);
            break;
        }
        if (IsPersistent)
          graph.Session.Persist(aType);
        graph.propertyType[pos] = aType;
        vertexProperties[vertexProperties.Length - 1] = aType;
        stringToPropertyType.Add(name, aType);
      }
      return aType;
    }

    public TypeId TypeId
    {
      get
      {
        return typeId;
      }
    }

    public PropertyType FindProperty(string name)
    {
      PropertyType anPropertyType;
      if (stringToPropertyType.TryGetValue(name, out anPropertyType))
      {
        return anPropertyType;
      }
      return null;
    }

    /// <summary>
    /// Return all the keys associated with the vertex type.
    /// </summary>
    /// <returns>the set of all string keys associated with the vertex type</returns>
    public IEnumerable<string> GetPropertyKeys()
    {
      foreach (var pair in stringToPropertyType)
      {
        yield return pair.Key;
      }
    }

    public IEnumerable<IEdge> GetEdges(Graph g, EdgeType etype, Direction dir)
    {
      BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>> map;
      switch (dir)
      {
        case Direction.Out:
          if (tailToHeadEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
              foreach (var p2 in p1.Value)
              {
                Vertex vertex1 = GetVertex(p2.Key);
                foreach (UInt64 l in p2.Value)
                {
                  VertexId vId = (int)l;
                  Vertex vertex2 = p1.Key.GetVertex(vId);
                  EdgeId eId = (int)(l >> 32);
                  Edge edge = etype.GetEdge(g, eId, vertex2, vertex1);
                  yield return edge;
                }
              }
          break;
        case Direction.In:
          if (headToTailEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
              foreach (var p2 in p1.Value)
              {
                Vertex vertex1 = GetVertex(p2.Key);
                foreach (UInt64 l in p2.Value)
                {
                  VertexId vId = (int)l;
                  Vertex vertex2 = p1.Key.GetVertex(vId);
                  EdgeId eId = (int)(l >> 32);
                  Edge edge = etype.GetEdge(g, eId, vertex1, vertex2);
                  yield return edge;
                }
              }
          break;
        case Direction.Both:
          if (tailToHeadEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
              foreach (var p2 in p1.Value)
              {
                Vertex vertex1 = GetVertex(p2.Key);
                foreach (UInt64 l in p2.Value)
                {
                  VertexId vId = (int)l;
                  Vertex vertex2 = p1.Key.GetVertex(vId);
                  EdgeId eId = (int)(l >> 32);
                  Edge edge = etype.GetEdge(g, eId, vertex2, vertex1);
                  yield return edge;
                }
              };
          if (headToTailEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
              foreach (var p2 in p1.Value)
              {
                Vertex vertex1 = GetVertex(p2.Key);
                foreach (UInt64 l in p2.Value)
                {
                  VertexId vId = (int)l;
                  Vertex vertex2 = p1.Key.GetVertex(vId);
                  EdgeId eId = (int)(l >> 32);
                  Edge edge = etype.GetEdge(g, eId, vertex1, vertex2);
                  yield return edge;
                }
              }
          break;
      }
    }

    public IEnumerable<IEdge> GetEdges(Graph g, Vertex vertex1, Direction dir)
    {
      switch (dir)
      {
        case Direction.Out:
          foreach (var p0 in tailToHeadEdges)
            foreach (var p1 in p0.Value)
            {
              BTreeSet<EdgeIdVertexId> edgeVertexSet;
              if (p1.Value.TryGetValue(vertex1.VertexId, out edgeVertexSet))
              {
                foreach (UInt64 l in edgeVertexSet)
                {
                  VertexId vId = (int)l;
                  Vertex vertex2 = p1.Key.GetVertex(vId);
                  EdgeId eId = (int)(l >> 32);
                  Edge edge = p0.Key.GetEdge(g, eId, vertex2, vertex1);
                  yield return edge;
                }
              }
            }
          break;
        case Direction.In:
          foreach (var p0 in headToTailEdges)
            foreach (var p1 in p0.Value)
            {
              BTreeSet<EdgeIdVertexId> edgeVertexSet;
              if (p1.Value.TryGetValue(vertex1.VertexId, out edgeVertexSet))
              {
                foreach (UInt64 l in edgeVertexSet)
                {
                  VertexId vId = (int)l;
                  Vertex vertex2 = p1.Key.GetVertex(vId);
                  EdgeId eId = (int)(l >> 32);
                  Edge edge = p0.Key.GetEdge(g, eId, vertex1, vertex2);
                  yield return edge;
                }
              }
            }
          break;
        case Direction.Both:
          foreach (var p0 in tailToHeadEdges)
            foreach (var p1 in p0.Value)
            {
              BTreeSet<EdgeIdVertexId> edgeVertexSet;
              if (p1.Value.TryGetValue(vertex1.VertexId, out edgeVertexSet))
              {
                foreach (UInt64 l in edgeVertexSet)
                {
                  VertexId vId = (int)l;
                  Vertex vertex2 = p1.Key.GetVertex(vId);
                  EdgeId eId = (int)(l >> 32);
                  Edge edge = p0.Key.GetEdge(g, eId, vertex2, vertex1);
                  yield return edge;
                }
              }
            }
          foreach (var p0 in headToTailEdges)
            foreach (var p1 in p0.Value)
            {
              BTreeSet<EdgeIdVertexId> edgeVertexSet;
              if (p1.Value.TryGetValue(vertex1.VertexId, out edgeVertexSet))
              {
                foreach (UInt64 l in edgeVertexSet)
                {
                  VertexId vId = (int)l;
                  Vertex vertex2 = p1.Key.GetVertex(vId);
                  EdgeId eId = (int)(l >> 32);
                  Edge edge = p0.Key.GetEdge(g, eId, vertex1, vertex2);
                  yield return edge;
                }
              }
            }
          break;
      }
    }

    public IEnumerable<IEdge> GetEdges(Graph g, EdgeType edgeType, Vertex vertex1, Direction dir)
    {
      BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>> map;
      switch (dir)
      {
        case Direction.Out:
          if (tailToHeadEdges.TryGetValue(edgeType, out map))
            foreach (var p1 in map)
            {
              BTreeSet<EdgeIdVertexId> edgeVertexSet;
              if (p1.Value.TryGetValue(vertex1.VertexId, out edgeVertexSet))
              {
                foreach (UInt64 l in edgeVertexSet)
                {
                  VertexId vId = (int)l;
#if VertexDebug
                  Vertex vertex2 = p1.Key.GetVertex(vId);
#else
                  Vertex vertex2 = new Vertex(g, p1.Key, vId);
#endif
                  EdgeId eId = (int)(l >> 32);
#if EdgeDebug
                  Edge edge = edgeType.GetEdge(g, eId, vertex2, vertex1);
#else
                  Edge edge = new Edge(g, edgeType, eId, vertex2, vertex1);
#endif
                  yield return edge;
                }
              }
            }
          break;
        case Direction.In:
          if (headToTailEdges.TryGetValue(edgeType, out map))
            foreach (var p1 in map)
            {
              BTreeSet<EdgeIdVertexId> edgeVertexSet;
              if (p1.Value.TryGetValue(vertex1.VertexId, out edgeVertexSet))
              {
                foreach (UInt64 l in edgeVertexSet)
                {
                  VertexId vId = (int)l;
                  Vertex vertex2 = p1.Key.GetVertex(vId);
                  EdgeId eId = (int)(l >> 32);
                  Edge edge = edgeType.GetEdge(g, eId, vertex1, vertex2);
                  yield return edge;
                }
              }
            }
          break;
        case Direction.Both:
          if (tailToHeadEdges.TryGetValue(edgeType, out map))
            foreach (var p1 in map)
            {
              BTreeSet<EdgeIdVertexId> edgeVertexSet;
              if (p1.Value.TryGetValue(vertex1.VertexId, out edgeVertexSet))
              {
                foreach (UInt64 l in edgeVertexSet)
                {
                  VertexId vId = (int)l;
                  Vertex vertex2 = p1.Key.GetVertex(vId);
                  EdgeId eId = (int)(l >> 32);
                  Edge edge = edgeType.GetEdge(g, eId, vertex2, vertex1);
                  yield return edge;
                }
              }
            }
          if (headToTailEdges.TryGetValue(edgeType, out map))
            foreach (var p1 in map)
            {
              BTreeSet<EdgeIdVertexId> edgeVertexSet;
              if (p1.Value.TryGetValue(vertex1.VertexId, out edgeVertexSet))
              {
                foreach (UInt64 l in edgeVertexSet)
                {
                  VertexId vId = (int)l;
                  Vertex vertex2 = p1.Key.GetVertex(vId);
                  EdgeId eId = (int)(l >> 32);
                  Edge edge = edgeType.GetEdge(g, eId, vertex1, vertex2);
                  yield return edge;
                }
              }
            }
          break;
      }
    }

    public long GetNumberOfEdges(EdgeType etype, Direction dir)
    {
      BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>> map;
      long numberOfEdges = 0;
      switch (dir)
      {
        case Direction.Out:
          if (tailToHeadEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
              foreach (var p2 in p1.Value)
              {
                numberOfEdges += p2.Value.Count;
              }
          break;
        case Direction.In:
          if (headToTailEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
              foreach (var p2 in p1.Value)
              {
                numberOfEdges += p2.Value.Count;
              }
          break;
        case Direction.Both:
          if (tailToHeadEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
              foreach (var p2 in p1.Value)
              {
                numberOfEdges += p2.Value.Count;
              }
          if (headToTailEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
              foreach (var p2 in p1.Value)
              {
                numberOfEdges += p2.Value.Count;
              }
          break;
      }
      return numberOfEdges;
    }
    
    /// <summary>
    /// Get the top vertices with the most number of edges of the given edge type
    /// </summary>
    /// <param name="etype">The edge type to look for</param>
    /// <param name="howMany">How many top ones to collect</param>
    /// <param name="dir">What end of edges to look at</param>
    /// <returns></returns>
    public VertexId[] GetTopNumberOfEdges(EdgeType etype, int howMany, Direction dir)
    {
      VertexId[] top = new VertexId[howMany];
      int[] topCt = new int[howMany];
      int lastIndex = topCt.Length - 1;
      BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>> map;
      switch (dir)
      {
        case Direction.Out:
          if (tailToHeadEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
              foreach (var p2 in p1.Value)
              {
                int pos = Array.BinarySearch(topCt, p2.Value.Count);
                if (pos < 0) 
                  pos = ~pos;
                if (pos > 0)
                {
                  --pos;
                  Array.Copy(topCt, 1, topCt, 0, pos);
                  Array.Copy(top, 1, top, 0, pos);
                }
                if (topCt[pos] != p2.Value.Count)
                {
                  topCt[pos] = p2.Value.Count;
                  top[pos] = p2.Key;
                }
              }
          break;
        case Direction.In:
          if (headToTailEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
              foreach (var p2 in p1.Value)
              {
                int pos = Array.BinarySearch(topCt, p2.Value.Count);
                if (pos < 0)
                  pos = ~pos;
                if (pos > 0)
                {
                  --pos;
                  Array.Copy(topCt, 1, topCt, 0, pos);
                  Array.Copy(top, 1, top, 0, pos);
                }
                if (topCt[pos] != p2.Value.Count)
                {
                  topCt[pos] = p2.Value.Count;
                  top[pos] = p2.Key;
                }
              }
          break;
        case Direction.Both:
          if (tailToHeadEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
              foreach (var p2 in p1.Value)
              {
                int pos = Array.BinarySearch(topCt, p2.Value.Count);
                if (pos < 0)
                  pos = ~pos;
                if (pos > 0)
                {
                  --pos;
                  Array.Copy(topCt, 1, topCt, 0, pos);
                  Array.Copy(top, 1, top, 0, pos);
                }
                if (topCt[pos] != p2.Value.Count)
                {
                  topCt[pos] = p2.Value.Count;
                  top[pos] = p2.Key;
                }
              }
          if (headToTailEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
              foreach (var p2 in p1.Value)
              {
                int pos = Array.BinarySearch(topCt, p2.Value.Count);
                if (pos < 0)
                  pos = ~pos;
                if (pos > 0)
                {
                  --pos;
                  Array.Copy(topCt, 1, topCt, 0, pos);
                  Array.Copy(top, 1, top, 0, pos);
                }
                if (topCt[pos] != p2.Value.Count)
                {
                  topCt[pos] = p2.Value.Count;
                  top[pos] = p2.Key;
                }
              }
          break;
      }
      return top;
    }

    public long GetNumberOfEdges(EdgeType etype, VertexId vertexId, Direction dir)
    {
      BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>> map;
      long numberOfEdges = 0;
      switch (dir)
      {
        case Direction.Out:
          if (tailToHeadEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
            {
              BTreeSet<EdgeIdVertexId> edgeVertexSet;
              if (p1.Value.TryGetValue(vertexId, out edgeVertexSet))
                numberOfEdges += edgeVertexSet.Count;
            }
          break;
        case Direction.In:
          if (headToTailEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
            {
              BTreeSet<EdgeIdVertexId> edgeVertexSet;
              if (p1.Value.TryGetValue(vertexId, out edgeVertexSet))
                numberOfEdges += edgeVertexSet.Count;
            }
          break;
        case Direction.Both:
          if (tailToHeadEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
            {
              BTreeSet<EdgeIdVertexId> edgeVertexSet;
              if (p1.Value.TryGetValue(vertexId, out edgeVertexSet))
                numberOfEdges += edgeVertexSet.Count;
            }
          if (headToTailEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
            {
              BTreeSet<EdgeIdVertexId> edgeVertexSet;
              if (p1.Value.TryGetValue(vertexId, out edgeVertexSet))
                numberOfEdges += edgeVertexSet.Count;
            }
          break;
      }
      return numberOfEdges;
    }

    public IEnumerable<Vertex> GetVertices(Graph g)
    {
      foreach (Range<VertexId> range in vertecis)
        foreach (VertexId vId in Enumerable.Range((int) range.Min, (int) range.Max - range.Min + 1))
          yield return new Vertex(graph, this, vId);
    }

    public IEnumerable<VertexId> GetVerticeIds(Graph g)
    {
      foreach (Range<VertexId> range in vertecis)
        foreach (VertexId vId in Enumerable.Range((int)range.Min, (int)range.Max - range.Min + 1))
          yield return vId;
    }

    public IEnumerable<IVertex> GetVertices(Graph g, EdgeType etype, Vertex vertex1, Direction dir)
    {
      BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>> map;
      BTreeSet<EdgeIdVertexId> set;
      switch (dir)
      {
        case Direction.Out:
          if (tailToHeadEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
            {
              if (p1.Value.TryGetValue(vertex1.VertexId, out set))
              {
                foreach (long l in set)
                {
                  VertexId vId = (int)l;
                  Vertex vertex2 = p1.Key.GetVertex(vId);
                  yield return vertex2;
                }
              }
            }
          break;
        case Direction.In:
          if (headToTailEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
            {
              if (p1.Value.TryGetValue(vertex1.VertexId, out set))
              {
                foreach (long l in set)
                {
                  VertexId vId = (int)l;
                  Vertex vertex2 = p1.Key.GetVertex(vId);
                  yield return vertex2;
                }
              }
            }
          break;
        case Direction.Both:
          if (tailToHeadEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
            {
              if (p1.Value.TryGetValue(vertex1.VertexId, out set))
              {
                foreach (long l in set)
                {
                  VertexId vId = (int)l;
                  Vertex vertex2 = p1.Key.GetVertex(vId);
                  yield return vertex2;
                }
              }
            }
          if (headToTailEdges.TryGetValue(etype, out map))
            foreach (var p1 in map)
            {
              if (p1.Value.TryGetValue(vertex1.VertexId, out set))
              {
                foreach (long l in set)
                {
                  VertexId vId = (int)l;
                  Vertex vertex2 = p1.Key.GetVertex(vId);
                  yield return vertex2;
                }
              }
            }
          break;
      }
    }

    public void RemoveVertex(Vertex vertex)
    {
      BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>> innerMap;
      BTreeSet<EdgeIdVertexId> edgeVertexSet;
      foreach (var m in headToTailEdges)
      {
        List<Edge> edgesToRemove = new List<Edge>();
        if (m.Value.TryGetValue(this, out innerMap))
          if (innerMap.TryGetValue(vertex.VertexId, out edgeVertexSet))
          {
            foreach (UInt64 l in edgeVertexSet)
            {
              VertexId vId = (int)l;
              Vertex vertex2 = GetVertex(vId);
              EdgeId eId = (int)(l >> 32);
              Edge edge = m.Key.GetEdge(graph, eId, vertex2, vertex);
              edgesToRemove.Add(edge);
            }
          }
          else
          {
            foreach (var n in innerMap)
            {
              List<EdgeIdVertexId> toRemove = new List<EdgeIdVertexId>();
              foreach (EdgeIdVertexId l in n.Value)
              {
                int i = (int)l;
                if (i == vertex.VertexId)
                  toRemove.Add(l);
              }
              foreach (EdgeIdVertexId l in toRemove)
              {
                VertexId vId = (int)l;
                Vertex vertex2 = GetVertex(vId);
                EdgeId eId = (int)(l >> 32);
                Edge edge = m.Key.GetEdge(graph, eId, vertex2, vertex);
                edgesToRemove.Add(edge);
              }
            }
          }
        foreach (Edge edge in edgesToRemove)
          m.Key.RemoveEdge(edge);
      }

      foreach (var m in headToTailEdges)
      {
        if (m.Value.TryGetValue(this, out innerMap))
          if (innerMap.TryGetValue(vertex.VertexId, out edgeVertexSet))
          {
            innerMap.Remove(vertex.VertexId);
            edgeVertexSet.Unpersist(Session);
          }
          else
          {
            foreach (var n in innerMap)
            {
              List<EdgeIdVertexId> toRemove = new List<EdgeIdVertexId>();
              foreach (EdgeIdVertexId l in n.Value)
              {
                int i = (int)l;
                if (i == vertex.VertexId)
                  toRemove.Add(l);
              }
              foreach (EdgeIdVertexId l in toRemove)
              {
                n.Value.Remove(l);
              }
            }
          }
      }

      foreach (var m in tailToHeadEdges)
        if (m.Value.TryGetValue(this, out innerMap))
          if (innerMap.TryGetValue(vertex.VertexId, out edgeVertexSet))
          {
            innerMap.Remove(vertex.VertexId);
            edgeVertexSet.Unpersist(Session);
          }
          else
          {
            foreach (var n in innerMap)
            {
              List<EdgeIdVertexId> toRemove = new List<EdgeIdVertexId>();
              foreach (EdgeIdVertexId l in n.Value)
              {
                int i = (int)l;
                if (i == vertex.VertexId)
                  toRemove.Add(l);
              }
              foreach (EdgeIdVertexId l in toRemove)
              {
                n.Value.Remove(l);
              }
            }
          }
      foreach (string key in GetPropertyKeys())
        vertex.RemoveProperty(key);

      Range<VertexId> range = new Range<VertexId>(vertex.VertexId, vertex.VertexId);
      bool isEqual;
      int pos = vertecis.BinarySearch(range, out isEqual);
      if (pos >= 0)
      {
        range = vertecis[pos];
        if (range.Min == vertex.VertexId)
        {
          if (range.Max == vertex.VertexId)
            vertecis.RemoveAt(pos);
          else
            vertecis[pos] = new Range<VertexId>(range.Min + 1, range.Max);
        }
        else if (range.Max == vertex.VertexId)
          vertecis[pos] = new Range<VertexId>(range.Min, range.Max + 1);
        else
        {
          vertecis[pos] = new Range<VertexId>(range.Min, vertex.VertexId - 1);
          vertecis.Insert(pos + 1, new Range<VertexId>(vertex.VertexId + 1, range.Max));
        }
      }
    }

    public object GetPropertyValue(VertexId vertexId, PropertyType propertyId)
    {
      return propertyId.GetPropertyValue(vertexId);
    }

    public void SetPropertyValue(VertexId vertexId, PropertyType propertyType, object v)
    {
      propertyType.SetPropertyValue(vertexId, v);
    }

    public string TypeName
    {
      get
      {
        return typeName;
      }
    }

    public override string ToString()
    {
      return "VertexType: " + typeName;
    }
  }
}
