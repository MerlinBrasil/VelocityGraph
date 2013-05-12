using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VelocityDb;
using VelocityDb.Collection.BTree;
using VelocityDb.Session;
using Element = System.Int64;
using ElementId = System.Int32;
using PropertyTypeId = System.Int32;
using PropertyId = System.Int32;
using TypeId = System.Int32;

namespace VelocityGraph
{
  class NodeType : OptimizedPersistable
  {
    string typeName;
    TypeId typeId;
    BTreeMap<string, PropertyTypeBase> stringToPropertyType;
    BTreeMap<EdgeType, BTreeMap<NodeType, BTreeMap<ElementId, BTreeSet<ElementId>>>> tailToHeadEdges;
    BTreeMap<EdgeType, BTreeMap<NodeType, BTreeMap<ElementId, BTreeSet<ElementId>>>> headToTailEdges;
    UInt32 nodeCt;

    public NodeType(TypeId aTypeId, string aTypeName, SessionBase session)
    {
      typeId = (TypeId)aTypeId;
      typeName = aTypeName;
      stringToPropertyType = new BTreeMap<string, PropertyTypeBase>(null, session);
      tailToHeadEdges = new BTreeMap<EdgeType, BTreeMap<NodeType, BTreeMap<ElementId, BTreeSet<ElementId>>>>(null, session);
      headToTailEdges = new BTreeMap<EdgeType, BTreeMap<NodeType, BTreeMap<ElementId, BTreeSet<ElementId>>>>(null, session);
      nodeCt = 0;
    }

    public void NewTailToHeadEdge(EdgeType edgeType, ElementId tail, ElementId head, NodeType headType, SessionBase session)
    {
      BTreeMap<NodeType, BTreeMap<ElementId, BTreeSet<ElementId>>> map;
      BTreeMap<ElementId, BTreeSet<ElementId>> innerMap;
      BTreeSet<ElementId> set;
      if (!tailToHeadEdges.TryGetValue(edgeType, out map))
      {
        map = new BTreeMap<NodeType, BTreeMap<ElementId, BTreeSet<ElementId>>>(null, session);
        innerMap = new BTreeMap<ElementId, BTreeSet<ElementId>>(null, session);
        set = new BTreeSet<ElementId>(null, session);
        innerMap.Add(tail, set);
        map.Add(headType, innerMap);
        tailToHeadEdges.Add(edgeType, map);
      }
      else if (!map.TryGetValue(headType, out innerMap))
      {
        innerMap = new BTreeMap<ElementId, BTreeSet<ElementId>>(null, session);
        set = new BTreeSet<ElementId>(null, session);
        innerMap.Add(tail, set);
        map.Add(headType, innerMap);
      }
      else if (!innerMap.TryGetValue(tail, out set))
      {
        set = new BTreeSet<ElementId>(null, session);
        innerMap.Add(tail, set);
      }
      set.Add(head);
    }

    public void NewHeadToTailEdge(EdgeType edgeType, ElementId tail, ElementId head, NodeType tailType, SessionBase session)
    {
      BTreeMap<NodeType, BTreeMap<ElementId, BTreeSet<ElementId>>> map;
      BTreeMap<ElementId, BTreeSet<ElementId>> innerMap;
      BTreeSet<ElementId> set;
      if (!headToTailEdges.TryGetValue(edgeType, out map))
      {
        map = new BTreeMap<NodeType, BTreeMap<ElementId, BTreeSet<ElementId>>>(null, session);
        innerMap = new BTreeMap<ElementId, BTreeSet<ElementId>>(null, session);
        set = new BTreeSet<ElementId>(null, session);
        innerMap.Add(tail, set);
        map.Add(tailType, innerMap);
        headToTailEdges.Add(edgeType, map);
      }
      else if (!map.TryGetValue(tailType, out innerMap))
      {
        innerMap = new BTreeMap<ElementId, BTreeSet<ElementId>>(null, session);
        set = new BTreeSet<ElementId>(null, session);
        innerMap.Add(tail, set);
        map.Add(tailType, innerMap);
      }
      else if (!innerMap.TryGetValue(tail, out set))
      {
        set = new BTreeSet<ElementId>(null, session);
        innerMap.Add(tail, set);
      }
      set.Add(head);
    }

    public UInt64 NewNode()
    {
      Update();
      UInt64 nodeId = (UInt64) typeId;
      nodeId <<= 32;
      nodeId += nodeCt++;
      return nodeId;
    }

    public Elements Neighbors(ElementId oid, EdgeType etype, EdgesDirection dir)
    {
      Elements result = new Elements();
      BTreeMap<NodeType, BTreeMap<ElementId, BTreeSet<ElementId>>> map;
      BTreeSet<ElementId> set;
      switch (dir)
      {
        case EdgesDirection.Outgoing:
          if (tailToHeadEdges.TryGetValue(etype, out map))
          {
            foreach (KeyValuePair<NodeType, BTreeMap<ElementId, BTreeSet<ElementId>>> pair in map)
            {
              BTreeMap<ElementId, BTreeSet<ElementId>> innerMap = pair.Value;
              if (innerMap.TryGetValue(oid, out set))
              {
                foreach (uint u in set)
                {
                  Element otherId = (Element)pair.Key.TypeId;
                  otherId <<= 32;
                  otherId += u;
                  result.Add((Element)otherId);
                }
              }
            }
          }
          break;
        case EdgesDirection.Ingoing:
          if (headToTailEdges.TryGetValue(etype, out map))
          {
            foreach (KeyValuePair<NodeType, BTreeMap<ElementId, BTreeSet<ElementId>>> pair in map)
            {
              BTreeMap<ElementId, BTreeSet<ElementId>> innerMap = pair.Value;
              if (innerMap.TryGetValue(oid, out set))
              {
                foreach (ElementId u in set)
                {
                  Element otherId = (Element)pair.Key.TypeId;
                  otherId <<= 32;
                  otherId += u;
                  result.Add(otherId);
                }
              }
            }
          }
          break;
        case EdgesDirection.Any:
          if (tailToHeadEdges.TryGetValue(etype, out map))
          {
            foreach (KeyValuePair<NodeType, BTreeMap<ElementId, BTreeSet<ElementId>>> pair in map)
            {
              BTreeMap<ElementId, BTreeSet<ElementId>> innerMap = pair.Value;
              if (innerMap.TryGetValue(oid, out set))
              {
                foreach (uint u in set)
                {
                  UInt64 otherId = (UInt64)pair.Key.TypeId;
                  otherId <<= 32;
                  otherId += u;
                  result.Add((long)otherId);
                }
              }
            }
          }
          if (headToTailEdges.TryGetValue(etype, out map))
          {
            foreach (KeyValuePair<NodeType, BTreeMap<ElementId, BTreeSet<ElementId>>> pair in map)
            {
              BTreeMap<ElementId, BTreeSet<ElementId>> innerMap = pair.Value;
              if (innerMap.TryGetValue(oid, out set))
              {
                foreach (ElementId u in set)
                {
                  Element otherId = (Element)pair.Key.TypeId;
                  otherId <<= 32;
                  otherId += u;
                  result.Add(otherId);
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

    public object GetPropertyValue(PropertyTypeBase[] propertyType, ElementId elementId, PropertyId propertyId)
    {
      PropertyTypeBase anPropertyType = propertyType[propertyId];
      return anPropertyType.GetPropertyValue(elementId);
    }

    public void SetPropertyValue(PropertyTypeBase[] propertyType, ElementId elementId, PropertyId propertyId, object v)
    {
      PropertyTypeBase anPropertyType = propertyType[propertyId];
      anPropertyType.SetPropertyValue(elementId, v);
    }
  }
}
