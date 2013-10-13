using System;
using System.Linq;
using System.Collections.Generic;
using VelocityDb;
using VelocityDb.Session;
using ElementId = System.Int32;
using EdgeId = System.Int32;
using VertexId = System.Int32;
using PropertyTypeId = System.Int32;
using PropertyId = System.Int32;
using TypeId = System.Int32;
using VelocityDb.Collection.BTree;
using Frontenac.Blueprints;
using Frontenac.Blueprints.Util;

namespace VelocityGraph
{
  [Serializable]
  public class EdgeType : OptimizedPersistable, IComparable<EdgeType>, IEqualityComparer<EdgeType>
  {
    internal Graph graph;
    string typeName;
    TypeId typeId;
    internal BTreeMap<EdgeId, VertexId[]> edges;
    BTreeMap<string, PropertyType> stringToPropertyType;
    bool directed;
    ElementId edgeCt;
    VertexType headType;
    VertexType tailType;

    public EdgeType(TypeId aTypeId, string aTypeName, VertexType tailType, VertexType headType, bool directed, Graph graph)
    {
      this.graph = graph;
      this.directed = directed;
      //   if (directed == false)
      //     edgeHeadToTail = new Dictionary<long, long>();
      //   edgeTailToHead = new Dictionary<long, long>();
      typeId = aTypeId;
      typeName = aTypeName;
      edges = new BTreeMap<EdgeId, VertexId[]>(null, graph.Session);
      stringToPropertyType = new BTreeMap<string, PropertyType>(null, graph.Session);
      edgeCt = 0;
      this.tailType = tailType;
      this.headType = headType;
    }

    /// <summary>
    /// Compares two EdgeType objects by id
    /// </summary>
    /// <param name="obj">The object to compare with</param>
    /// <returns>a negative number if less, 0 if equal or else a positive number</returns>
    public int CompareTo(EdgeType obj)
    {
      return typeId.CompareTo(obj.typeId);
    }

    public static int Compare(EdgeType aId, EdgeType bId)
    {
      return aId.typeId.CompareTo(bId.typeId);
    }

    public bool Equals(EdgeType x, EdgeType y)
    {
      return Compare(x, y) == 0;
    }

    public bool Directed
    {
      get
      {
        return directed;
      }
    }

    public Edge GetEdge(Graph g, EdgeId edgeId)
    {
      VertexId[] headTail;
      if (edges.TryGetValue(edgeId, out headTail))
      {
        if (headType != null)
        {
          Vertex head = headType.GetVertex(headTail[1]);
          Vertex tail = tailType.GetVertex(headTail[3]);
          return new Edge(g, this, edgeId, head, tail);
        }
        else
        {
          VertexType vt = g.vertexType[headTail[0]];
          Vertex head = vt.GetVertex(headTail[1]);
          vt = g.vertexType[headTail[2]];
          Vertex tail = vt.GetVertex(headTail[3]);
          return new Edge(g, this, edgeId, head, tail);
        }
      }
      throw new EdgeDoesNotExistException();
    }

    public Edge[] GetEdges(Graph g)
    {
      Edge[] edgeAray = new Edge[edges.Count];
      int i = 0;
      foreach (var m in edges)
      {
        VertexType vt1 = g.vertexType[m.Value[0]];
        Vertex head = vt1.GetVertex(m.Value[1]);
        VertexType vt2 = g.vertexType[m.Value[2]];
        Vertex tail = vt2.GetVertex(m.Value[3]);
        edgeAray[i++] = GetEdge(g, m.Key, head, tail);
      }
      return edgeAray;
    }

    public Edge GetEdge(Graph g, EdgeId edgeId, Vertex headVertex, Vertex tailVertex)
    {
      if (edges.Contains(edgeId))
      {
        return new Edge(g, this, edgeId, headVertex, tailVertex);
      }
      throw new EdgeDoesNotExistException();
    }

    public int GetHashCode(EdgeType aIssue)
    {
      return typeId.GetHashCode();
    }

    /// <summary>
    /// Return all the keys associated with the edge type.
    /// </summary>
    /// <returns>the set of all string keys associated with the edge type</returns>
    public IEnumerable<string> GetPropertyKeys()
    {
      foreach (var pair in stringToPropertyType)
      {
        yield return pair.Key;
      }
    }

    /// <summary>
    /// Creates a new Property. 
    /// </summary>
    /// <param name="name">Unique name for the new Property.</param>
    /// <param name="dt">Data type for the new Property.</param>
    /// <param name="kind">Property kind.</param>
    /// <returns>a Property.</returns>
    public PropertyType NewProperty(string name, DataType dt, PropertyKind kind)
    {
      PropertyType aType;
      if (stringToPropertyType.TryGetValue(name, out aType) == false)
      {
        int pos = graph.propertyType.Length;
        graph.Update();
        Array.Resize(ref graph.propertyType, pos + 1);
        switch (dt)
        {
          case DataType.Boolean:
            aType = new PropertyTypeT<bool>(false, this.TypeId, pos, name, kind, Session);
            break;
          case DataType.Integer:
            aType = new PropertyTypeT<int>(false, this.TypeId, pos, name, kind, Session);
            break;
          case DataType.Long:
            aType = new PropertyTypeT<long>(false, this.TypeId, pos, name, kind, Session);
            break;
          case DataType.Double:
            aType = new PropertyTypeT<double>(false, this.TypeId, pos, name, kind, Session);
            break;
          case DataType.DateTime:
            aType = new PropertyTypeT<DateTime>(false, this.TypeId, pos, name, kind, Session);
            break;
          case DataType.String:
            aType = new PropertyTypeT<string>(false, this.TypeId, pos, name, kind, Session);
            break;
          case DataType.Object:
            aType = new PropertyTypeT<object>(false, this.TypeId, pos, name, kind, Session);
            break;
        }
        if (IsPersistent)
          Session.Persist(aType);
        graph.propertyType[pos] = aType;
        stringToPropertyType.Add(name, aType);
      }
      return aType;
    }

    /// <summary>
    /// Creates a new Property. 
    /// </summary>
    /// <param name="name">Unique name for the new Property.</param>
    /// <param name="value">Object guiding the type of the property.</param>
    /// <param name="kind">Property kind.</param>
    /// <returns>a Property.</returns>
    internal PropertyType NewProperty(string name, object value, PropertyKind kind)
    {
      PropertyType aType;
      if (stringToPropertyType.TryGetValue(name, out aType) == false)
      {
        int pos = graph.propertyType.Length;
        graph.Update();
        Array.Resize(ref graph.propertyType, pos + 1);
        switch (Type.GetTypeCode(value.GetType()))
        {
          case TypeCode.Boolean:
            aType = new PropertyTypeT<bool>(false, this.TypeId, pos, name, kind, Session);
            break;
          case TypeCode.Int32:
            aType = new PropertyTypeT<int>(false, this.TypeId, pos, name, kind, Session);
            break;
          case TypeCode.Int64:
            aType = new PropertyTypeT<long>(false, this.TypeId, pos, name, kind, Session);
            break;
          case TypeCode.Single:
            aType = new PropertyTypeT<Single>(false, this.TypeId, pos, name, kind, Session);
            break;
          case TypeCode.Double:
            aType = new PropertyTypeT<double>(false, this.TypeId, pos, name, kind, Session);
            break;
          case TypeCode.DateTime:
            aType = new PropertyTypeT<DateTime>(false, this.TypeId, pos, name, kind, Session);
            break;
          case TypeCode.String:
            aType = new PropertyTypeT<string>(false, this.TypeId, pos, name, kind, Session);
            break;
          case TypeCode.Object:
            aType = new PropertyTypeT<object>(false, this.TypeId, pos, name, kind, Session);
            break;
        }
        if (IsPersistent)
          Session.Persist(aType);
        graph.propertyType[pos] = aType;
        stringToPropertyType.Add(name, aType);
      }
      return aType;
    }

    public Edge NewEdge(Graph g, Vertex tail, Vertex head, SessionBase session)
    {
      Update();
     // lock (edges)
        edges.Add(++edgeCt, new ElementId[] { head.VertexType.TypeId, head.VertexId, tail.VertexType.TypeId, tail.VertexId });
      Edge edge = new Edge(g, this, edgeCt, head, tail);
      if (directed)
      {
        tail.VertexType.NewTailToHeadEdge(this, edge, tail, head, session);
        head.VertexType.NewHeadToTailEdge(this, edge, tail, head, session);
      }
      return edge;
    }

    public void RemoveEdge(Edge edge)
    {
      Update();
      edges.Remove(edge.EdgeId);
      if (directed)
      {
        edge.Tail.VertexType.RemoveTailToHeadEdge(edge);
        edge.Head.VertexType.RemoveHeadToTailEdge(edge);
      }
      foreach (string key in GetPropertyKeys())
        edge.RemoveProperty(key);
    }

    public Edge NewEdgeX(PropertyType[] propertyType, PropertyType tailAttr, object tailV, PropertyType headAttr, object headV, SessionBase session)
    {
      throw new NotImplementedException("don't yet know what it is supposed to do");
    }

    public TypeId TypeId
    {
      get
      {
        return typeId;
      }
    }

    public string TypeName
    {
      get
      {
        return typeName;
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

    public object GetPropertyValue(ElementId elementId, PropertyType property)
    {
      return property.GetPropertyValue(elementId);
    }

    public void SetPropertyValue(ElementId elementId, PropertyType property, object v)
    {
      property.SetPropertyValue(elementId, v);
    }
  }
}
