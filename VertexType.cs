using System;
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

namespace VelocityGraph
{
  public class VertexType : OptimizedPersistable
  {
    internal Graph graph;
    string typeName;
    TypeId typeId;
    BTreeSet<VertexId> vertecis;
    internal BTreeMap<string, PropertyType> stringToPropertyType;
    internal PropertyType[] vertexProperties;
    BTreeSet<EdgeType> edgeTypes;
    BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>> tailToHeadEdges;
    BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>> headToTailEdges;
    VertexId vertexCt;

    internal VertexType(TypeId aTypeId, string aTypeName, Graph graph)
    {
      this.graph = graph;
      typeId = (TypeId)aTypeId;
      typeName = aTypeName;
      vertecis = new BTreeSet<VertexId>(null, graph.Session);
      stringToPropertyType = new BTreeMap<string, PropertyType>(null, graph.Session);
      edgeTypes = new BTreeSet<EdgeType>(null, graph.Session);
      tailToHeadEdges = new BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>>(null, graph.Session);
      headToTailEdges = new BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>>(null, graph.Session);
      vertexProperties = new PropertyType[0];
      vertexCt = 0;
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
      if (vertecis.Contains(vertexId))
        return new Vertex(graph, this, vertexId);
      throw new VertexDoesNotExistException();
    }

    public void NewTailToHeadEdge(EdgeType edgeType, Edge edge, Vertex tail, Vertex head, SessionBase session)
    {
      BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>> map;
      BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>> innerMap;
      BTreeSet<EdgeIdVertexId> set;
      if (!tailToHeadEdges.TryGetValue(edgeType, out map))
      {
        map = new BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>(null, session);
        innerMap = new BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>(null, session);
        set = new BTreeSet<EdgeIdVertexId>(null, session);        
        if (IsPersistent)
        {
          Session.Persist(map);
          Session.Persist(innerMap);
          Session.Persist(set);
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
          Session.Persist(innerMap);
          Session.Persist(set);
        }
        innerMap.Add(tail.VertexId, set);
        map.Add(head.VertexType, innerMap);
      }
      else if (!innerMap.TryGetValue(tail.VertexId, out set))
      {
        set = new BTreeSet<EdgeIdVertexId>(null, session);
        if (IsPersistent)
          Session.Persist(set);
        innerMap.Add(tail.VertexId, set);
      }
      set.Add(edgeVertexId(edge, head.VertexId));
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
      if (!headToTailEdges.TryGetValue(edgeType, out map))
      {
        map = new BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>(null, session);
        innerMap = new BTreeMap<EdgeId, BTreeSet<EdgeIdVertexId>>(null, session);
        set = new BTreeSet<EdgeIdVertexId>(null, session);
        if (IsPersistent)
        {
          Session.Persist(map);
          Session.Persist(innerMap);
          Session.Persist(set);
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
          Session.Persist(innerMap);
          Session.Persist(set);
        }
        innerMap.Add(head.VertexId, set);
        map.Add(tail.VertexType, innerMap);
      }
      else if (!innerMap.TryGetValue(head.VertexId, out set))
      {
        set = new BTreeSet<EdgeIdVertexId>(null, session);
        if (IsPersistent)
          Session.Persist(set);
        innerMap.Add(head.VertexId, set);
      }
      set.Add(edgeVertexId(edge, tail.VertexId));
    }

    public Vertex NewVertex(Graph g)
    {
      Update();
      vertecis.Add(++vertexCt);
      return new Vertex(g, this, vertexCt);
    }

    public Dictionary<Vertex, HashSet<Edge>> Traverse(Graph g, Vertex vertex1, EdgeType etype, Direction dir)
    {
      Dictionary<Vertex, HashSet<Edge>> result = new Dictionary<Vertex, HashSet<Edge>>();
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
          Edge edge = etype.GetEdge(g, pair.Key, vertex1, other);
          HashSet<Edge> edges;
          if (!result.TryGetValue(other, out edges))
          { // vertex not reached by any other edge
            edges = new HashSet<Edge>();
            result.Add(other, edges);
          }
          edges.Add(edge);
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
                    Edge edge = etype.GetEdge(g, eId, vertex1, vertex2);
                    HashSet<Edge> edges;
                    if (!result.TryGetValue(vertex2, out edges))
                    { // vertex not reached by any other edge
                      edges = new HashSet<Edge>();
                      result.Add(vertex2, edges);
                    }
                    edges.Add(edge);
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
                    Edge edge = etype.GetEdge(g, eId, vertex1, vertex2);
                    HashSet<Edge> edges;
                    if (!result.TryGetValue(vertex2, out edges))
                    { // vertex not reached by any other edge
                      edges = new HashSet<Edge>();
                      result.Add(vertex2, edges);
                    }
                    edges.Add(edge);
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
                    Edge edge = etype.GetEdge(g, eId, vertex1, vertex2);
                    HashSet<Edge> edges;
                    if (!result.TryGetValue(vertex2, out edges))
                    { // vertex not reached by any other edge
                      edges = new HashSet<Edge>();
                      result.Add(vertex2, edges);
                    }
                    edges.Add(edge);
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
                    Edge edge = etype.GetEdge(g, eId, vertex1, vertex2);
                    HashSet<Edge> edges;
                    if (!result.TryGetValue(vertex2, out edges))
                    { // vertex not reached by any other edge
                      edges = new HashSet<Edge>();
                      result.Add(vertex2, edges);
                    }
                    edges.Add(edge);
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
                  Vertex vertex2 = GetVertex(vId);
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
                  Vertex vertex2 = GetVertex(vId);
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
                  Vertex vertex2 = GetVertex(vId);
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
                  Vertex vertex2 = GetVertex(vId);
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
                  Vertex vertex2 = GetVertex(vId);
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
                  Vertex vertex2 = GetVertex(vId);
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
                  Vertex vertex2 = GetVertex(vId);
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
                  Vertex vertex2 = GetVertex(vId);
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
                  Vertex vertex2 = GetVertex(vId);
                  EdgeId eId = (int)(l >> 32);
                  Edge edge = edgeType.GetEdge(g, eId, vertex2, vertex1);
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
                  Vertex vertex2 = GetVertex(vId);
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
                  Vertex vertex2 = GetVertex(vId);
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
                  Vertex vertex2 = GetVertex(vId);
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

    public Vertex[] GetVertices(Graph g)
    {
      Vertex[] vArray = new Vertex[vertecis.Count];
      int i = 0;
      foreach (VertexId vId in vertecis)
      {
        vArray[i++] = GetVertex(vId);
      }
      return vArray;
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
                  Vertex vertex2 = GetVertex(vId);
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
                  Vertex vertex2 = GetVertex(vId);
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
                  Vertex vertex2 = GetVertex(vId);
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
                  Vertex vertex2 = GetVertex(vId);
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
      vertecis.Remove(vertex.VertexId);
    }

    public object GetPropertyValue(VertexId vertexId, PropertyType propertyId)
    {
      return propertyId.GetPropertyValue(vertexId);
    }

    public void SetPropertyValue(VertexId vertexId, PropertyType propertyType, object v)
    {
      propertyType.SetPropertyValue(vertexId, v);
    }
  }
}
