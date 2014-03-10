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
using VelocityDb.Collection;

namespace VelocityGraph
{
  [Serializable]
  public partial class EdgeType : OptimizedPersistable, IComparable<EdgeType>, IEqualityComparer<EdgeType>
  {
    internal Graph graph;
    EdgeType baseType;
    internal VelocityDbList<EdgeType> subType;
    string typeName;
    TypeId typeId;
    VelocityDbList<Range<EdgeId>> edgeRanges;
    internal BTreeMap<EdgeId, UnrestrictedEdge> unrestrictedEdges;
    internal BTreeMap<EdgeId, UInt64> restrictedEdges;
    internal BTreeMap<string, PropertyType> stringToPropertyType;
    bool birectional;
    VertexType headType;
    VertexType tailType;

    public EdgeType(TypeId aTypeId, string aTypeName, VertexType tailType, VertexType headType, bool birectional, EdgeType baseType, Graph graph)
    {
      this.graph = graph;
      this.baseType = baseType;
      subType = new VelocityDbList<EdgeType>();
      if (baseType != null)
        baseType.subType.Add(this);
      this.birectional = birectional;
      typeId = aTypeId;
      typeName = aTypeName;
      this.tailType = tailType;
      this.headType = headType;
      if (Unrestricted)
        unrestrictedEdges = new BTreeMap<EdgeId, UnrestrictedEdge>(null, graph.Session);
      else
        restrictedEdges = new BTreeMap<EdgeId, ulong>(null, graph.Session);
      stringToPropertyType = new BTreeMap<string, PropertyType>(null, graph.Session);
      edgeRanges = new VelocityDbList<Range<EdgeId>>();
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
        return birectional;
      }
    }

    public Edge GetEdge(EdgeId edgeId)
    {
      if (Unrestricted)
      {
        UnrestrictedEdge headTail;
        if (unrestrictedEdges.TryGetValue(edgeId, out headTail))
        {
          VertexType vt = headTail.headVertexType;
          Vertex head = vt.GetVertex(headTail.headVertexId);
          vt = headTail.tailVertexType;
          Vertex tail = vt.GetVertex(headTail.tailVertexId);
          return new Edge(graph, this, edgeId, head, tail);
        }
      }
      else
      {
        UInt64 vertexVertex;
        if (restrictedEdges.TryGetValue(edgeId, out vertexVertex))
        {
          VertexId headId = (VertexId)vertexVertex >> 32;
          Vertex head = headType.GetVertex(headId);
          Vertex tail = tailType.GetVertex((VertexId)vertexVertex);
          return new Edge(graph, this, edgeId, head, tail);
        }
      }

      throw new EdgeDoesNotExistException();
    }

    /// <summary>
    /// Enumerates all edges of this type
    /// </summary>
    /// <param name="polymorphic">If true, also include all edges of sub types</param>
    /// <returns>Enumeration of edges of this type</returns>
    public IEnumerable<Edge> GetEdges(bool polymorphic = false)
    {
      if (Unrestricted)
        foreach (var m in unrestrictedEdges)
        {
          VertexType vt1 = m.Value.headVertexType;
          Vertex head = vt1.GetVertex(m.Value.headVertexId);
          VertexType vt2 = m.Value.tailVertexType;
          Vertex tail = vt2.GetVertex(m.Value.tailVertexId);
          yield return GetEdge(graph, m.Key, tail, head);
        }
      else
        foreach (var m in restrictedEdges)
        {
          VertexType vt1 = headType;
          VertexId vId = (VertexId) m.Value >> 32;
          Vertex head = vt1.GetVertex(vId);
          VertexType vt2 = tailType;
          vId = (VertexId) m.Value;
          Vertex tail = vt2.GetVertex(vId);
          yield return GetEdge(graph, m.Key, tail, head);
        }
      if (polymorphic)
      {
        foreach (EdgeType et in subType)
          foreach (Edge e in et.GetEdges(polymorphic))
            yield return e;
      }
    }

    public long CountEdges()
    {
      long ct = 0;
      foreach (Range<EdgeId> range in edgeRanges)
        ct += range.Max - range.Min + 1;
      return ct;
    }

    public Edge GetEdge(Graph g, EdgeId edgeId, Vertex tailVertex, Vertex headVertex)
    {
      if (Unrestricted)
      {
        if (unrestrictedEdges.Contains(edgeId))
          return new Edge(g, this, edgeId, headVertex, tailVertex);
      }
      else if (restrictedEdges.Contains(edgeId))
        return new Edge(g, this, edgeId, headVertex, tailVertex);
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
    /// Gets the Head VertexType of the edge (might not be set)
    /// </summary>
    public VertexType HeadType
    {
      get
      {
        return headType;
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
        graph.Update();
        int pos = -1;
        int i = 0;
        foreach (PropertyType pt in graph.propertyType)
          if (pt == null)
          {
            pos = i;
            break;
          }
          else
            ++i;
        if (pos < 0)
        {
          pos = graph.propertyType.Length;
          Array.Resize(ref graph.propertyType, pos + 1);
        }
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
            aType = new PropertyTypeT<IComparable>(false, this.TypeId, pos, name, kind, Session);
            break;
        }
        graph.propertyType[pos] = aType;
        stringToPropertyType.AddFast(name, aType);
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
            aType = new PropertyTypeT<IComparable>(false, this.TypeId, pos, name, kind, Session);
            break;
        }
        graph.propertyType[pos] = aType;
        stringToPropertyType.AddFast(name, aType);
      }
      return aType;
    }

    EdgeId NewEdgeId(Graph g)
    {
      Range<EdgeId> range;
      EdgeId eId = 1;
      switch (edgeRanges.Count)
      {
        case 0:
          range = new Range<EdgeId>(1, 1);
          edgeRanges.Add(range);
          break;
        case 1:
          range = edgeRanges.First();

          if (range.Min == 1)
          {
            eId = range.Max + 1;
            range = new Range<EdgeId>(1, eId);
          }
          else
          {
            eId = range.Min - 1;
            range = new Range<EdgeId>(eId, range.Max);
          }
          edgeRanges[0] = range;
          break;
        default:
          {
            range = edgeRanges.First();
            if (range.Min > 1)
            {
              eId = range.Min - 1;
              range = new Range<VertexId>(eId, range.Max);
            }
            else
            {
              Range<VertexId> nextRange = edgeRanges[1];
              if (range.Max + 2 == nextRange.Min)
              {
                edgeRanges.Remove(range);
                eId = range.Max + 1;
                range = new Range<VertexId>(range.Min, nextRange.Max);
              }
              else
              {
                range = new Range<VertexId>(range.Min, range.Max + 1);
                eId = range.Max + 1;
              }
              edgeRanges.Add(range);
            }
          }
          break;
      }
      return eId;
    }

    public Edge NewEdge(Graph g, Vertex tail, Vertex head, SessionBase session)
    {
      if (tailType != null && tail.VertexType != tailType)
        throw new InvalidTailVertexTypeException();
      if (headType != null && head.VertexType != headType)
        throw new InvalidHeadVertexTypeException();
      EdgeId eId = NewEdgeId(g);
      if (Unrestricted)
        unrestrictedEdges.AddFast(eId, new UnrestrictedEdge { headVertexType = head.VertexType, headVertexId = head.VertexId, tailVertexType = tail.VertexType, tailVertexId = tail.VertexId });
      else
      {
        UInt64 vertexVertex = (UInt64)head.VertexId;
        vertexVertex = vertexVertex << 32;
        vertexVertex += (UInt64)tail.VertexId;
        restrictedEdges.AddFast(eId, vertexVertex);
      }
      Edge edge = new Edge(g, this, eId, head, tail);
      if (birectional)
      {
        tail.VertexType.NewTailToHeadEdge(this, edge, tail, head, session);
        head.VertexType.NewHeadToTailEdge(this, edge, tail, head, session);
      }
      else
        tail.VertexType.NewTailToHeadEdge(this, edge, tail, head, session);
      return edge;
    }

    public void RemoveEdge(Edge edge)
    {
      if (Unrestricted)
      {
        UnrestrictedEdge unrestrictedEdge = unrestrictedEdges[edge.EdgeId];
        unrestrictedEdge.Unpersist(graph.Session);
        unrestrictedEdges.Remove(edge.EdgeId);
      }
      else
        restrictedEdges.Remove(edge.EdgeId);
      if (birectional)
      {
        edge.Tail.VertexType.RemoveTailToHeadEdge(edge);
        edge.Head.VertexType.RemoveHeadToTailEdge(edge);
      }
      else
        edge.Tail.VertexType.RemoveTailToHeadEdge(edge);
      foreach (string key in GetPropertyKeys())
        edge.RemoveProperty(key);
      Range<EdgeId> range = new Range<EdgeId>(edge.EdgeId, edge.EdgeId);
      bool isEqual;
      int pos = edgeRanges.BinarySearch(range, out isEqual);
      if (pos >= 0)
      {
        if (pos == edgeRanges.Count || (pos > 0 && edgeRanges[pos].Min > edge.EdgeId))
          --pos;
        range = edgeRanges[pos];
        if (range.Min == edge.EdgeId)
        {
          if (range.Max == edge.EdgeId)
            edgeRanges.RemoveAt(pos);
          else
            edgeRanges[pos] = new Range<EdgeId>(range.Min + 1, range.Max);
        }
        else if (range.Max == edge.EdgeId)
          edgeRanges[pos] = new Range<EdgeId>(range.Min, range.Max + 1);
        else
        {
          edgeRanges[pos] = new Range<EdgeId>(range.Min, edge.EdgeId - 1);
          edgeRanges.Insert(pos + 1, new Range<EdgeId>(edge.EdgeId + 1, range.Max));
        }
      }
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

    public IComparable GetPropertyValue(ElementId elementId, PropertyType property)
    {
      return property.GetPropertyValue(elementId);
    }

    public void SetPropertyValue(ElementId elementId, PropertyType property, IComparable v)
    {
      property.SetPropertyValue(elementId, v);
    }

    /// <summary>
    /// Gets the session managing this object
    /// </summary>
    public override SessionBase Session
    {
      get
      {
        return graph.Session != null ? graph.Session : base.Session;
      }
    }

    /// <summary>
    /// Gets the Tail VertexType of the edge (might not be set)
    /// </summary>
    public VertexType TailType
    {
      get
      {
        return tailType;
      }
    }

    public override string ToString()
    {
      return "EdgeType: " + typeName;
    }

    public bool Unrestricted
    {
      get
      {
        return headType == null;
      }
    }
  }
}
