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
  using Vertexes = System.Collections.Generic.HashSet<Vertex>;
  using Edges = System.Collections.Generic.HashSet<Edge>;

  class VertexType : OptimizedPersistable
  {
    string typeName;
    TypeId typeId;
    BTreeMap<string, PropertyTypeBase> stringToPropertyType;
    BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>> tailToHeadEdges;
    BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>> headToTailEdges;
    VertexId nodeCt;

    public VertexType(TypeId aTypeId, string aTypeName, SessionBase session)
    {
      typeId = (TypeId)aTypeId;
      typeName = aTypeName;
      stringToPropertyType = new BTreeMap<string, PropertyTypeBase>(null, session);
      tailToHeadEdges = new BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>>(null, session);
      headToTailEdges = new BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>>(null, session);
      nodeCt = 0;
    }

    protected EdgeIdVertexId edgeVertexId(Edge edge, VertexId vertexId)
    {
      EdgeIdVertexId id = (EdgeIdVertexId)edge.EdgeId;
      id <<= 32;
      return id + (EdgeIdVertexId) vertexId;
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

    public Vertex NewVertex()
    {
      Update();
      return new Vertex(typeId, nodeCt++);
    }

    public Vertexes Neighbors(VertexId oid, EdgeType etype, EdgesDirection dir)
    {
      Vertexes result = new Vertexes();
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
              if (innerMap.TryGetValue(oid, out set))
              {
                foreach (EdgeIdVertexId id in set)
                {
                  result.Add(new Vertex(pair.Key.TypeId, (VertexId) id));
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
              if (innerMap.TryGetValue(oid, out set))
              {
                foreach (EdgeIdVertexId id in set)
                {
                  result.Add(new Vertex(pair.Key.TypeId, (VertexId)id));
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
              if (innerMap.TryGetValue(oid, out set))
              {
                foreach (EdgeIdVertexId id in set)
                {
                  result.Add(new Vertex(pair.Key.TypeId, (VertexId)id));
                }
              }
            }
          }
          if (headToTailEdges.TryGetValue(etype, out map))
          {
            foreach (KeyValuePair<VertexType, BTreeMap<EdgeId, BTreeSet<EdgeIdVertexId>>> pair in map)
            {
              BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>> innerMap = pair.Value;
              if (innerMap.TryGetValue(oid, out set))
              {
                foreach (EdgeIdVertexId id in set)
                {
                  result.Add(new Vertex(pair.Key.TypeId, (VertexId)id));
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
    public PropertyId NewProperty(ref PropertyTypeBase[] propertyType, string name, DataType dt, PropertyKind kind)
    {
      PropertyTypeBase aType;
      if (stringToPropertyType.TryGetValue(name, out aType) == false)
      {
        int pos = propertyType.Length;
        Array.Resize(ref propertyType, pos + 1);
        switch (dt)
        {
          case DataType.Boolean:
            aType = new PropertyType<bool>(typeId, pos, name, kind, Session);
            break;
          case DataType.Integer:
            aType = new PropertyType<int>(typeId,pos, name, kind, Session);
            break;
          case DataType.Long:
            aType = new PropertyType<long>(typeId,pos, name, kind, Session);
            break;
          case DataType.Double:
            aType = new PropertyType<double>(typeId,pos, name, kind, Session);
            break;
          case DataType.DateTime:
            aType = new PropertyType<DateTime>(typeId,pos, name, kind, Session);
            break;
          case DataType.String:
            aType = new PropertyType<string>(typeId,pos, name, kind, Session);
            break;
          case DataType.Object:
            aType = new PropertyType<object>(typeId,pos, name, kind, Session);
            break;
          case DataType.OID:
            aType = new PropertyType<long>(typeId,pos, name, kind, Session);
            break;
        }
        propertyType[pos] = aType;
        stringToPropertyType.Add(name, aType);
      }
      return aType.PropertyId;
    }

    public TypeId TypeId
    {
      get
      {
        return typeId;
      }
    }

    public PropertyId FindProperty(string name)
    {
      PropertyTypeBase anPropertyType;
      if (stringToPropertyType.TryGetValue(name, out anPropertyType))
      {
        return anPropertyType.PropertyId;
      }
      return -1;
    }

    public object GetPropertyValue(PropertyTypeBase[] propertyType, VertexId vertexId, PropertyId propertyId)
    {
      PropertyTypeBase anPropertyType = propertyType[propertyId];
      return anPropertyType.GetPropertyValue(vertexId);
    }

    public void SetPropertyValue(PropertyTypeBase[] propertyType, VertexId vertexId, PropertyId propertyId, object v)
    {
      PropertyTypeBase anPropertyType = propertyType[propertyId];
      anPropertyType.SetPropertyValue(vertexId, v);
    }
  }
}
