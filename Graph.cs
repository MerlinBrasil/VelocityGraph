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
  public enum Condition
  {
    Equal,
    GreaterEqual,
    GreaterThan,
    LessEqual,
    LessThan,
    NotEqual,
    Like,
    LikeNoCase,
    Between,
    RegExp
  }

  public enum DataType
  {
    Boolean,
    Integer,
    Long,
    Double,
    DateTime,
    String,
    Object,
    OID
  }

  public enum EdgesDirection
  {
    Ingoing,
    Outgoing,
    Any
  }

  public enum ObjectType
  {
    Node,
    Edge
  }

  public enum Order
  {
    Ascendent,
    Descendent
  }

  public enum PropertyKind
  {
    Basic,
    Indexed,
    Unique
  }

  public class Graph : OptimizedPersistable
  {
    Dictionary<string, NodeType> stringToNodeType;
    NodeType[] nodeType;
    Dictionary<string, EdgeType> stringToEdgeType;
    EdgeType[] edgeType;
    Dictionary<string, EdgeType> stringToRestrictedEdgeType;
    EdgeType[] restrictedEdgeType;
    int nodeOrEdgeTypeCt;
    PropertyTypeBase[] propertyType;
    [NonSerialized]
    SessionBase session;

    public Graph(SessionBase session)
    {
      nodeOrEdgeTypeCt = 0;
      stringToNodeType = new Dictionary<string, NodeType>();
      nodeType = new NodeType[0];
      stringToEdgeType = new Dictionary<string, EdgeType>();
      edgeType = new EdgeType[0];
      stringToRestrictedEdgeType = new Dictionary<string, EdgeType>();
      restrictedEdgeType = new EdgeType[0];
      propertyType = new PropertyTypeBase[0];
      this.session = session;
    }

    public override SessionBase Session
    {
      get
      {
        if (session != null)
          return session;
        return base.Session;
      }
    }

    /// <summary>
    /// Creates a new node type.
    /// </summary>
    /// <param name="name">Unique name for the new node type.</param>
    /// <returns>Unique graph type identifier.</returns>
    public TypeId NewNodeType(string name)
    {
      NodeType aType;
      if (stringToNodeType.TryGetValue(name, out aType) == false)
      {
        int pos = nodeOrEdgeTypeCt;
        Update();
        Array.Resize(ref nodeType, (int) ++nodeOrEdgeTypeCt);
        aType = new NodeType(pos, name, Session);
        nodeType[pos] = aType;
        stringToNodeType.Add(name, aType);
      }
      return aType.TypeId;
    }

    /// <summary>
    /// Creates a new edge type.
    /// </summary>
    /// <param name="name">Unique name for the new edge type.</param>
    /// <param name="directed">If true, this creates a directed edge type, otherwise this creates a undirected edge type.</param>
    /// <param name="neighbors">If true, this indexes neighbor nodes, otherwise not.</param>
    /// <returns>Unique type identifier.</returns>
    public TypeId NewEdgeType(string name, bool directed, bool neighbors)
    {
      EdgeType aType;
      if (stringToEdgeType.TryGetValue(name, out aType) == false)
      {
        int pos = nodeOrEdgeTypeCt;
        Update();
        Array.Resize(ref edgeType, ++nodeOrEdgeTypeCt);
        aType = new EdgeType(pos, name, directed, neighbors);
        edgeType[pos] = aType;
        stringToEdgeType.Add(name, aType);
      }
      return aType.TypeId;
    }

    /// <summary>
    /// Creates a new restricted edge type.
    /// </summary>
    /// <param name="name">Unique name for the new edge type.</param>
    /// <param name="tail">Tail node type identifier.</param>
    /// <param name="head">Head node type identifier.</param>
    /// <param name="neighbors">If true, this indexes neighbor nodes, otherwise not.</param>
    /// <returns>Unique type identifier.</returns>
    public TypeId NewRestrictedEdgeType(string name, int tail, int head, bool neighbors)
    {
      EdgeType aType;
      if (stringToRestrictedEdgeType.TryGetValue(name, out aType) == false)
      {
        int pos = nodeOrEdgeTypeCt;
        Update();
        Array.Resize(ref restrictedEdgeType, ++nodeOrEdgeTypeCt);
        NodeType tailType = nodeType[tail];
        NodeType headType = nodeType[head];
        aType = new EdgeType(pos, name, tailType, headType, neighbors, true);
        restrictedEdgeType[pos] = aType;
        stringToRestrictedEdgeType.Add(name, aType);
      }
      return aType.TypeId;
    }

    /// <summary>
    /// Creates a new Property. 
    /// </summary>
    /// <param name="type">Node or edge type identifier.</param>
    /// <param name="name">Unique name for the new Property.</param>
    /// <param name="dt">Data type for the new Property.</param>
    /// <param name="kind">Property kind.</param>
    /// <returns>Unique Property identifier.</returns>
    public PropertyId NewProperty(int type, string name, DataType dt, PropertyKind kind)
    {
      NodeType aType = null;
      if (nodeType.Length > type)
        aType = nodeType[type];
      if (aType != null)
        return aType.NewProperty(ref propertyType, name, dt, kind);
      EdgeType anEdgeType = null;
      if (edgeType.Length > type)
        anEdgeType = edgeType[type];
      else
        if (restrictedEdgeType.Length > type)
          anEdgeType = restrictedEdgeType[type];
      if (anEdgeType != null)
        return anEdgeType.NewProperty(ref propertyType, name, dt, kind);
      throw new InvalidTypeIdException();
    }

    public PropertyId NewProperty(TypeId type, string name, ElementType dt, PropertyKind kind, object defaultValue)
    {
      throw new NotImplementedException();
    }

    public void SetProperty(Element element, PropertyId property, object v)
    {
      TypeId typeId = (TypeId)(element >> 32);
      NodeType aType = null;
      if (nodeType.Length > typeId)
        aType = nodeType[(int) typeId];
      if (aType != null)
        aType.SetPropertyValue(propertyType, (TypeId)element, property, v);
      else
      {
        EdgeType anEdgeType = edgeType[(int) typeId];
        anEdgeType.SetPropertyValue(propertyType, (ElementId)element, property, v);
      }    
    }

    /// <summary>
    /// Creates a new node instance.
    /// </summary>
    /// <param name="type">Node type identifier.</param>
    /// <returns>Unique OID of the new node instance.</returns>
    public long NewNode(TypeId type)
    {
      NodeType aType = nodeType[type];
      return (long) aType.NewNode();
    }

    /// <summary>
    /// Creates a new edge instance.
    /// </summary>
    /// <param name="type">Edge type identifier.</param>
    /// <param name="tail">Source OID.</param>
    /// <param name="head">Target OID. </param>
    /// <returns>Unique OID of the new edge instance.</returns>
    public long NewEdge(TypeId type, Element tail, Element head)
    {
      ulong utail = (ulong)tail;
      UInt32 tailTypeId = (UInt32)(utail >> 32);
      NodeType tailType = nodeType[(int) tailTypeId];
      ulong uhead = (ulong)head;
      UInt32 headTypeId = (UInt32) (uhead >> 32);
      NodeType headType = nodeType[(int) headTypeId];
      EdgeType anEdgeType = null;
      if (edgeType.Length > type)
        anEdgeType = edgeType[type];
      if (anEdgeType != null)
        return anEdgeType.NewEdge(tail, tailType, head, headType, Session);
      anEdgeType = restrictedEdgeType[type];      

      return anEdgeType.NewEdge(tail, tailType, head, headType, Session);
    }

    /// <summary>
    /// Creates a new edge instance.
    /// The tail of the edge will be any node having the given tailV Value for the given tailAttr Property identifier,
    /// and the head of the edge will be any node having the given headV Value for the given headAttr Property identifier. 
    /// </summary>
    /// <param name="type">Node or edge type identifier.</param>
    /// <param name="tailAttr">Property identifier.</param>
    /// <param name="tailV">Tail value</param>
    /// <param name="headAttr">Property identifier.</param>
    /// <param name="headV">Head value</param>
    /// <returns>Unique OID of the new edge instance.</returns>
    public Element NewEdge(TypeId type, PropertyId tailAttr, object tailV, PropertyId headAttr, object headV)
    {
      EdgeType anEdgeType = null;
      if (edgeType.Length > type)
        anEdgeType = edgeType[type];
      if (anEdgeType != null)
        return anEdgeType.NewEdge(propertyType, tailAttr, tailV, headAttr, headV, Session);
      anEdgeType = restrictedEdgeType[type];      
      return anEdgeType.NewEdge(propertyType, tailAttr, tailV, headAttr, headV, Session);
    }

    /// <summary>
    /// Selects all neighbor nodes from or to the given node OID and for the given edge type.
    /// </summary>
    /// <param name="oid">Node OID</param>
    /// <param name="etype">Edge type identifier.</param>
    /// <param name="dir">Direction</param>
    /// <returns>Objects instance</returns>
    public Elements Neighbors(Element oid, TypeId etype, EdgesDirection dir)
    {
      UInt32 typeId = (UInt32) (oid >> 32);
      NodeType aNodeType = nodeType[(int) typeId];
      EdgeType anEdgeType = null;
      if (edgeType.Length > etype)
        anEdgeType = edgeType[etype];
      if (anEdgeType != null)
        return aNodeType.Neighbors((ElementId)oid, anEdgeType, dir);
      anEdgeType = restrictedEdgeType[etype];
      return aNodeType.Neighbors((ElementId)oid, anEdgeType, dir);
    }

    /// <summary>
    /// Selects all neighbor nodes from or to each of the node OID in the given collection and for the given edge type.
    /// </summary>
    /// <param name="objs">Node OID collection.</param>
    /// <param name="etype">Edge type identifier.</param>
    /// <param name="dir">Direction.</param>
    /// <returns>All neighbor nodes collection</returns>
    public Elements Neighbors(Elements objs, TypeId etype, EdgesDirection dir)
    {
      Elements result = new Elements();
      foreach (long oid in objs)
      {
        Elements t = Neighbors(oid, etype, dir);
        result.Union(t);
      }
      return result;
    }

    /// <summary>
    /// Finds one object having the given value for the given property. 
    /// </summary>
    /// <param name="property"></param>
    /// <param name="v"></param>
    /// <returns>the Oid of the object matching</returns>
    public Element FindElement(PropertyId property, object v)
    {
      if (propertyType.Length <= property)
        throw new InvalidPropertyIdException();
      PropertyTypeBase aPropertyType = propertyType[property];
      long obj = aPropertyType.TypeId;
      obj = obj << 32;
      return obj + aPropertyType.GetPropertyElementId(v);
    }

    /// <summary>
    /// Gets the Value for the given Property and OID.
    /// </summary>
    /// <param name="oid">OID</param>
    /// <param name="property">Property type identifier.</param>
    /// <param name="v">Value for the given Property and for the given OID.</param>
    public object GetProperty(Element oid, PropertyId property)
    {
      TypeId typeId = (TypeId)(oid >> 32);
      NodeType aNodeType = null;
      if (nodeType.Length > typeId)
        aNodeType = nodeType[(int)typeId];
      if (aNodeType != null)
      {
        return aNodeType.GetPropertyValue(propertyType, (PropertyTypeId) oid, property);
      }
      else
      {
        EdgeType anEdgeType = null;
        if (edgeType.Length > typeId)
          anEdgeType = edgeType[typeId];
        if (anEdgeType != null)
          return anEdgeType.GetPropertyValue(propertyType, (PropertyTypeId)oid, property);
        else
        {
          anEdgeType = restrictedEdgeType[typeId];
          return anEdgeType.GetPropertyValue(propertyType, (PropertyTypeId)oid, property);
        }
      }
    }

    public long CountNodes()
    {
      return nodeType.Length;
    }

    public long CountEdges()
    {
      return edgeType.Length + restrictedEdgeType.Length;
    }

    public EdgeData GetEdgeData(Element edge)
    {
      throw new NotImplementedException();
    }

    public long GetEdgePeer(Element edge, long node)
    {
      throw new NotImplementedException();
    }

    public void Drop(Element oid)
    {
      throw new NotImplementedException();
    }

    public void Drop(Elements objs)
    {
      throw new NotImplementedException();
    }

    public TypeId GetObjectType(Element oid)
    {
      TypeId typeId = (TypeId) (oid >> 32);
      return typeId;
    }

    public int NewSessionProperty(TypeId type, DataType dt, PropertyKind kind)
    {
      throw new NotImplementedException();
    }

    public int NewSessionProperty(TypeId type, DataType dt, PropertyKind kind, object defaultValue)
    {
      throw new NotImplementedException();
    }

    public void SetPropertyDefaultValue(PropertyId property, object v)
    {
      throw new NotImplementedException();
    }

    public void IndexProperty(PropertyId property, PropertyKind kind)
    {
      throw new NotImplementedException();
    }

  /*  public PropertyStatistics GetPropertyStatistics(int attr, bool basic)
    {
      throw new NotImplementedException();
    }*/

    public long GetPropertyIntervalCount(PropertyId attr, object lower, bool includeLower, object higher, bool includeHigher)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Finds the type id associated with a particular edge/node type. Lookup by name.
    /// </summary>
    /// <param name="name">The name of the edge/node type being looked up</param>
    /// <returns>A node/edge type id or -1 if not found.</returns>
    public TypeId FindType(string name)
    {
      EdgeType eType;
      if (stringToRestrictedEdgeType.TryGetValue(name, out eType))
        return eType.TypeId;
      if (stringToEdgeType.TryGetValue(name, out eType))
        return eType.TypeId;
      NodeType nType;
      if (stringToNodeType.TryGetValue(name, out nType))
        return nType.TypeId;
      return -1;
    }

    public ElementType GetType(TypeId type)
    {
      throw new NotImplementedException();
    }

    public void RemoveType(int type)
    {
      throw new NotImplementedException();
    }

    public int FindProperty(TypeId type, string name)
    {
       NodeType aType = null;
      if (nodeType.Length > type)
        aType = nodeType[type];
      if (aType != null)
        return aType.FindProperty(name);
      EdgeType anEdgeType = null;
      if (edgeType.Length > type)
        anEdgeType = edgeType[type];
      else
        if (restrictedEdgeType.Length > type)
          anEdgeType = restrictedEdgeType[type];
      if (anEdgeType != null)
        return anEdgeType.FindProperty(name);
      throw new InvalidTypeIdException();
    }

    public Property GetProperty(PropertyId attr)
    {
      throw new NotImplementedException();
    }

    public void RemoveProperty(PropertyId attr)
    {
      throw new NotImplementedException();
    }

    public Elements Select(TypeId type)
    {
      throw new NotImplementedException();
    }

    public Elements Select(PropertyId attr, Condition cond, object v)
    {
      throw new NotImplementedException();
    }

    public Elements Select(PropertyId attr, Condition cond, object lower, object higher)
    {
      throw new NotImplementedException();
    }

    public Elements Explode(Element oid, TypeId etype, EdgesDirection dir)
    {
      throw new NotImplementedException();
    }

    public Elements Explode(Elements objs, TypeId etype, EdgesDirection dir)
    {
      throw new NotImplementedException();
    }

    public long Degree(Element oid, TypeId etype, EdgesDirection dir)
    {
      throw new NotImplementedException();
    }

    public Elements Edges(TypeId etype, Element tail, Element head)
    {
      throw new NotImplementedException();
    }

    public Element FindEdge(TypeId etype, Element tail, Element head)
    {
      throw new NotImplementedException();
    }

    public Elements Tails(Elements edges)
    {
      throw new NotImplementedException();
    }

    public Elements Heads(Elements edges)
    {
      throw new NotImplementedException();
    }

    public void TailsAndHeads(Elements edges, Elements tails, Elements heads)
    {
      throw new NotImplementedException();
    }

    public ElementType[] FindNodeTypes()
    {
      throw new NotImplementedException();
    }

    public ElementType[] FindEdgeTypes()
    {
      throw new NotImplementedException();
    }

    public ElementType[] FindTypes()
    {
      throw new NotImplementedException();
    }

    public Property[] FindProperties(TypeId type)
    {
      throw new NotImplementedException();
    }

    public Property[] GetProperties(Element oid)
    {
      throw new NotImplementedException();
    }

    public object[] GetValues(PropertyId property)
    {
      throw new NotImplementedException();
    }
  }
}
