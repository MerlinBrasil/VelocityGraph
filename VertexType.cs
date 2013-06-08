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

namespace VelocityGraph
{
  public class VertexType : OptimizedPersistable
  {
    string typeName;
    TypeId typeId;
    BTreeSet<VertexId> vertecis;
    internal BTreeMap<string, PropertyTypeBase> stringToPropertyType;
    internal PropertyTypeBase[] vertexProperties;
    BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>> tailToHeadEdges;
    BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>> headToTailEdges;
    VertexId nodeCt;

    internal VertexType(TypeId aTypeId, string aTypeName, SessionBase session)
    {
      typeId = (TypeId)aTypeId;
      typeName = aTypeName;
      vertecis = new BTreeSet<VertexId>(null, session);
      stringToPropertyType = new BTreeMap<string, PropertyTypeBase>(null, session);
      tailToHeadEdges = new BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>>(null, session);
      headToTailEdges = new BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>>(null, session);
      vertexProperties = new PropertyTypeBase[0];
      nodeCt = 0;
    }

    protected EdgeIdVertexId edgeVertexId(Edge edge, VertexId vertexId)
    {
      EdgeIdVertexId id = (EdgeIdVertexId)edge.EdgeId;
      id <<= 32;
      return id + (EdgeIdVertexId) vertexId;
    }

    public Vertex GetVertex(Graph g, VertexId vertexId)
    {
      if (vertecis.Contains(vertexId))
        return new Vertex(g, this, vertexId);
      throw new VertexDoesNotExistException();
    }

    public void NewTailToHeadEdge(EdgeType edgeType, Edge edge, VertexId tail, VertexId head, VertexType headType, SessionBase session)
    {
      BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>> map;
      BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>> innerMap;
      BTreeSet<EdgeIdVertexId> set;
      if (!tailToHeadEdges.TryGetValue(edgeType, out map))
      {
        map = new BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>(null, session);
        innerMap = new BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>(null, session);
        set = new BTreeSet<EdgeIdVertexId>(null, session);
        innerMap.Add(tail, set);
        map.Add(headType, innerMap);
        tailToHeadEdges.Add(edgeType, map);
      }
      else if (!map.TryGetValue(headType, out innerMap))
      {
        innerMap = new BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>(null, session);
        set = new BTreeSet<EdgeIdVertexId>(null, session);
        innerMap.Add(tail, set);
        map.Add(headType, innerMap);
      }
      else if (!innerMap.TryGetValue(tail, out set))
      {
        set = new BTreeSet<EdgeIdVertexId>(null, session);
        innerMap.Add(tail, set);
      }
      set.Add(edgeVertexId(edge, head));
    }

    public void NewHeadToTailEdge(EdgeType edgeType, Edge edge, VertexId tail, VertexId head, VertexType tailType, SessionBase session)
    {
      BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>> map;
      BTreeMap<EdgeId, BTreeSet<EdgeIdVertexId>> innerMap;
      BTreeSet<EdgeIdVertexId> set;
      if (!headToTailEdges.TryGetValue(edgeType, out map))
      {
        map = new BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>(null, session);
        innerMap = new BTreeMap<EdgeId, BTreeSet<EdgeIdVertexId>>(null, session);
        set = new BTreeSet<EdgeIdVertexId>(null, session);
        innerMap.Add(tail, set);
        map.Add(tailType, innerMap);
        headToTailEdges.Add(edgeType, map);
      }
      else if (!map.TryGetValue(tailType, out innerMap))
      {
        innerMap = new BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>(null, session);
        set = new BTreeSet<EdgeIdVertexId>(null, session);
        innerMap.Add(tail, set);
        map.Add(tailType, innerMap);
      }
      else if (!innerMap.TryGetValue(tail, out set))
      {
        set = new BTreeSet<EdgeIdVertexId>(null, session);
        innerMap.Add(tail, set);
      }
      set.Add(edgeVertexId(edge, head));
    }

    public Vertex NewVertex(Graph g)
    {
      Update();
      VertexType aVertexType = null;
      if (g.vertexType.Length > typeId)
        aVertexType = g.vertexType[typeId];
      if (aVertexType == null)
        throw new InvalidTypeIdException();
      vertecis.Add(nodeCt);
      return new Vertex(g, aVertexType, nodeCt++);
    }

    public Dictionary<Vertex, HashSet<Edge>> Traverse(Graph g, Vertex vertex1, EdgeType etype, EdgesDirection dir)
    {
      Dictionary<Vertex, HashSet<Edge>> result = new Dictionary<Vertex, HashSet<Edge>>();
      BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>> map;
      BTreeSet<EdgeIdVertexId> set;
      switch (dir)
      {
        case EdgesDirection.Outgoing:
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
                  EdgeId eId = (EdgeId) id >> 32;
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
        case EdgesDirection.Ingoing:
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
                  EdgeId eId = (EdgeId)id >> 32;
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
        case EdgesDirection.Any:
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
                  EdgeId eId = (EdgeId)id >> 32;
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
                  EdgeId eId = (EdgeId)id >> 32;
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
      return result;
    }

    /// <summary>
    /// Creates a new Property. 
    /// </summary>
    /// <param name="name">Unique name for the new Property.</param>
    /// <param name="dt">Data type for the new Property.</param>
    /// <param name="kind">Property kind.</param>
    /// <returns>Unique Property identifier.</returns>
    public PropertyTypeBase NewProperty(ref PropertyTypeBase[] propertyType, string name, DataType dt, PropertyKind kind)
    {
      PropertyTypeBase aType;
      if (stringToPropertyType.TryGetValue(name, out aType) == false)
      {
        int pos = propertyType.Length;
        Array.Resize(ref propertyType, pos + 1);
        Array.Resize(ref vertexProperties, vertexProperties.Length + 1);
        switch (dt)
        {
          case DataType.Boolean:
            aType = new PropertyType<bool>(true, typeId, pos, name, kind, Session);
            break;
          case DataType.Integer:
            aType = new PropertyType<int>(true,typeId,pos, name, kind, Session);
            break;
          case DataType.Long:
            aType = new PropertyType<long>(true,typeId,pos, name, kind, Session);
            break;
          case DataType.Double:
            aType = new PropertyType<double>(true,typeId,pos, name, kind, Session);
            break;
          case DataType.DateTime:
            aType = new PropertyType<DateTime>(true,typeId,pos, name, kind, Session);
            break;
          case DataType.String:
            aType = new PropertyType<string>(true,typeId,pos, name, kind, Session);
            break;
          case DataType.Object:
            aType = new PropertyType<object>(true,typeId,pos, name, kind, Session);
            break;
          case DataType.OID:
            aType = new PropertyType<long>(true,typeId,pos, name, kind, Session);
            break;
        }
        propertyType[pos] = aType;
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

    public PropertyTypeBase FindProperty(string name)
    {
      PropertyTypeBase anPropertyType;
      if (stringToPropertyType.TryGetValue(name, out anPropertyType))
      {
        return anPropertyType;
      }
      return null;
    }

    public object GetPropertyValue(VertexId vertexId, PropertyTypeBase propertyId)
    {
      return propertyId.GetPropertyValue(vertexId);
    }

    public void SetPropertyValue(VertexId vertexId, PropertyTypeBase propertyType, object v)
    {
      propertyType.SetPropertyValue(vertexId, v);
    }
  }
}
