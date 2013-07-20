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
  public class EdgeType : OptimizedPersistable, IComparable<EdgeType>, IEqualityComparer<EdgeType>
  {
    string typeName;
    TypeId typeId;
    BTreeMap<EdgeId, VertexId[]> edges;
    BTreeMap<string, PropertyType> stringToPropertyType;
    bool directed;
    ElementId edgeCt;
    VertexType headType;
    VertexType tailType;

    public EdgeType(TypeId aTypeId, string aTypeName, VertexType tailType, VertexType headType, bool directed, SessionBase session)
    {
      this.directed = directed;
      //   if (directed == false)
      //     edgeHeadToTail = new Dictionary<long, long>();
      //   edgeTailToHead = new Dictionary<long, long>();
      typeId = aTypeId;
      typeName = aTypeName;
      edges = new BTreeMap<EdgeId, VertexId[]>(null, session);
      stringToPropertyType = new BTreeMap<string, PropertyType>(null, session);
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

    public Edge GetEdge(Graph g, EdgeId edgeId)
    {
      VertexId[] headTail;
      if (edges.TryGetValue(edgeId, out headTail))
      {
        if (headType != null)
        {
          Vertex head = headType.GetVertex(g, headTail[0]);
          Vertex tail = tailType.GetVertex(g, headTail[1]);
          return new Edge(g, this, edgeId, head, tail);
        }
        else
          throw new UnexpectedException("Don't no head and tail of edge");
      }
      throw new EdgeDoesNotExistException();
    }

    public IEnumerable<Edge> GetEdges(Graph g)
    {
      foreach (var m in edges)
      {
        yield return GetEdge(g, m.Key);
      }
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
    public PropertyType NewProperty(ref PropertyType[] propertyType, string name, DataType dt, PropertyKind kind)
    {
      PropertyType aType;
      if (stringToPropertyType.TryGetValue(name, out aType) == false)
      {
        int pos = propertyType.Length;
        Array.Resize(ref propertyType, pos + 1);
        switch (dt)
        {
          case DataType.Boolean:
            aType = new PropertyTypeT<bool>(false, this.TypeId, pos, name, kind, Session);
            break;
          case DataType.Integer:
            aType = new PropertyTypeT<int>(false, this.TypeId,pos, name, kind, Session);
            break;
          case DataType.Long:
            aType = new PropertyTypeT<long>(false, this.TypeId,pos, name, kind, Session);
            break;
          case DataType.Double:
            aType = new PropertyTypeT<double>(false, this.TypeId,pos, name, kind, Session);
            break;
          case DataType.DateTime:
            aType = new PropertyTypeT<DateTime>(false, this.TypeId,pos, name, kind, Session);
            break;
          case DataType.String:
            aType = new PropertyTypeT<string>(false, this.TypeId,pos, name, kind, Session);
            break;
          case DataType.Object:
            aType = new PropertyTypeT<object>(false,this.TypeId,pos, name, kind, Session);
            break;
          case DataType.OID:
            aType = new PropertyTypeT<long>(false, this.TypeId,pos, name, kind, Session);
            break;
        }
        propertyType[pos] = aType;
        stringToPropertyType.Add(name, aType);
      }
      return aType;
    }

    public Edge NewEdge(Graph g, Vertex tail, Vertex head, SessionBase session)
    {
      Update();
      edges.Add(edgeCt, new VertexId[] { head.VertexId, tail.VertexId });
      Edge edge = new Edge(g, this, edgeCt++, head, tail);
      tail.VertexType.NewTailToHeadEdge(this, edge, tail.VertexId, head.VertexId, head.VertexType, session);
      if (directed == false)
        head.VertexType.NewHeadToTailEdge(this, edge, tail.VertexId, head.VertexId, tail.VertexType, session);
      return edge;
    }

    public void RemoveEdge(Edge edge)
    {
      Update();
      edges.Remove(edge.EdgeId);
      tailType.RemoveTailToHeadEdge(this, edge, edge.Tail.VertexId, edge.Head.VertexId, headType);
      if (directed == false)
        headType.RemoveHeadToTailEdge(this, edge, edge.Tail.VertexId, edge.Head.VertexId, tailType);
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
