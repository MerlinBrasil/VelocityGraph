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
using VertexTypeId = System.Int32;
using EdgeTypeId = System.Int32;

namespace VelocityGraph
{
  using Vertexes = System.Collections.Generic.HashSet<Vertex>;
  using Edges = System.Collections.Generic.HashSet<Edge>;

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

  /*public enum ObjectType
  {
    Node,
    Edge
  }*/

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
    BTreeMap<string, VertexType> stringToVertexType;
    internal VertexType[] vertexType;
    BTreeMap<string, EdgeType> stringToEdgeType;
    internal EdgeType[] edgeType;
    BTreeMap<string, EdgeType> stringToRestrictedEdgeType;
    internal EdgeType[] restrictedEdgeType;
    int nodeOrEdgeTypeCt;
    internal PropertyTypeBase[] propertyType;
    [NonSerialized]
    SessionBase session;

    public Graph(SessionBase session)
    {
      nodeOrEdgeTypeCt = 0;
      stringToVertexType = new BTreeMap<string, VertexType>(null, session);
      vertexType = new VertexType[0];
      stringToEdgeType = new BTreeMap<string, EdgeType>(null, session);
      edgeType = new EdgeType[0];
      stringToRestrictedEdgeType = new BTreeMap<string, EdgeType>(null, session);
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

    public static Vertexes CombineIntersection(Vertexes objs1, Vertexes objs2)
    {
      Vertexes clone = new Vertexes(objs1);
      clone.IntersectWith(objs2);
      return clone;
    }

    public static Edges CombineIntersection(Edges objs1, Edges objs2)
    {
      Edges clone = new Edges(objs1);
      clone.IntersectWith(objs2);
      return clone;
    }

    /// <summary>
    /// Creates a new node type.
    /// </summary>
    /// <param name="name">Unique name for the new vertex type.</param>
    /// <returns>Unique graph type identifier.</returns>
    public VertexTypeId NewVertexType(string name)
    {
      VertexType aType;
      if (stringToVertexType.TryGetValue(name, out aType) == false)
      {
        int pos = nodeOrEdgeTypeCt;
        Update();
        Array.Resize(ref vertexType, (int)++nodeOrEdgeTypeCt);
        aType = new VertexType(pos, name, Session);
        vertexType[pos] = aType;
        stringToVertexType.Add(name, aType);
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
    public EdgeTypeId NewEdgeType(string name, bool directed, bool neighbors)
    {
      EdgeType aType;
      if (stringToEdgeType.TryGetValue(name, out aType) == false)
      {
        int pos = nodeOrEdgeTypeCt;
        Update();
        Array.Resize(ref edgeType, ++nodeOrEdgeTypeCt);
        aType = new EdgeType(pos, name, directed, neighbors, Session);
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
    public EdgeTypeId NewRestrictedEdgeType(string name, int tail, int head, bool neighbors)
    {
      EdgeType aType;
      if (stringToRestrictedEdgeType.TryGetValue(name, out aType) == false)
      {
        int pos = nodeOrEdgeTypeCt;
        Update();
        Array.Resize(ref restrictedEdgeType, ++nodeOrEdgeTypeCt);
        VertexType tailType = vertexType[tail];
        VertexType headType = vertexType[head];
        aType = new EdgeType(pos, name, tailType, headType, neighbors, true, Session);
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
    public PropertyId NewVertexProperty(VertexTypeId type, string name, DataType dt, PropertyKind kind)
    {
      VertexType aType = null;
      if (vertexType.Length > type)
        aType = vertexType[type];
      if (aType != null)
        return aType.NewProperty(ref propertyType, name, dt, kind);
      throw new InvalidTypeIdException();
    }

    /// <summary>
    /// Creates a new Property. 
    /// </summary>
    /// <param name="type">Node or edge type identifier.</param>
    /// <param name="name">Unique name for the new Property.</param>
    /// <param name="dt">Data type for the new Property.</param>
    /// <param name="kind">Property kind.</param>
    /// <returns>Unique Property identifier.</returns>
    public PropertyId NewEdgeProperty(EdgeTypeId type, string name, DataType dt, PropertyKind kind)
    {
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

    public PropertyId NewProperty(VertexTypeId type, string name, ElementType dt, PropertyKind kind, object defaultValue)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Creates a new node instance.
    /// </summary>
    /// <param name="type">Node type identifier.</param>
    /// <returns>Unique OID of the new node instance.</returns>
    public Vertex NewVertex(VertexTypeId type)
    {
      VertexType aType = vertexType[type];
      return aType.NewVertex(this);
    }

    /// <summary>
    /// Creates a new edge instance.
    /// </summary>
    /// <param name="type">Edge type identifier.</param>
    /// <param name="tail">Source OID.</param>
    /// <param name="head">Target OID. </param>
    /// <returns>Unique OID of the new edge instance.</returns>
    public Edge NewEdge(EdgeTypeId type, Vertex tail, Vertex head)
    {
      VertexTypeId tailTypeId = tail.VertexType;
      VertexType tailType = vertexType[tailTypeId];
      VertexTypeId headTypeId = head.VertexType;
      VertexType headType = vertexType[headTypeId];
      EdgeType anEdgeType = null;
      if (edgeType.Length > type)
        anEdgeType = edgeType[type];
      if (anEdgeType != null)
        return anEdgeType.NewEdge(this, tail, tailType, head, headType, Session);
      anEdgeType = restrictedEdgeType[type];
      return anEdgeType.NewEdge(this, tail, tailType, head, headType, Session);
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
    public Edge NewEdge(EdgeTypeId type, PropertyId tailAttr, object tailV, PropertyId headAttr, object headV)
    {
      EdgeType anEdgeType = null;
      if (edgeType.Length > type)
        anEdgeType = edgeType[type];
      if (anEdgeType != null)
        return anEdgeType.NewEdgeX(propertyType, tailAttr, tailV, headAttr, headV, Session);
      anEdgeType = restrictedEdgeType[type];
      return anEdgeType.NewEdgeX(propertyType, tailAttr, tailV, headAttr, headV, Session);
    }

    /// <summary>
    /// Selects all neighbor nodes from or to each of the node OID in the given collection and for the given edge type.
    /// </summary>
    /// <param name="objs">Node OID collection.</param>
    /// <param name="etype">Edge type identifier.</param>
    /// <param name="dir">Direction.</param>
    /// <returns>All neighbor nodes collection</returns>
    public Vertexes Neighbors(Vertexes objs, EdgeTypeId etype, EdgesDirection dir)
    {
      Vertexes result = new Vertexes();
      foreach (Vertex e in objs)
      {
        Vertexes t = e.Neighbors(etype, dir);
        result.UnionWith(t);
      }
      return result;
    }

    /// <summary>
    /// Finds one object having the given value for the given property. 
    /// </summary>
    /// <param name="property"></param>
    /// <param name="v"></param>
    /// <returns>the Oid of the object matching</returns>
    public Vertex FindElement(PropertyId property, object v)
    {
      if (propertyType.Length <= property)
        throw new InvalidPropertyIdException();
      PropertyTypeBase aPropertyType = propertyType[property];
      //long obj = aPropertyType.TypeId;
      //obj = obj << 32;
      return new Vertex(this, aPropertyType.TypeId, aPropertyType.GetPropertyElementId(v));
    }

    public long CountVertexes()
    {
      return vertexType.Length;
    }

    public long CountEdges()
    {
      return edgeType.Length + restrictedEdgeType.Length;
    }

    public void Drop(Vertexes objs)
    {
      throw new NotImplementedException();
    }

    public int NewSessionProperty(VertexTypeId type, DataType dt, PropertyKind kind)
    {
      throw new NotImplementedException();
    }

    public int NewSessionProperty(EdgeTypeId type, DataType dt, PropertyKind kind, object defaultValue)
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
    /// Finds the type id associated with a particular edge type. Lookup by name.
    /// </summary>
    /// <param name="name">The name of the edge/node type being looked up</param>
    /// <returns>A node/edge type id or -1 if not found.</returns>
    public EdgeTypeId FindEdgeType(string name)
    {
      EdgeType eType;
      if (stringToRestrictedEdgeType.TryGetValue(name, out eType))
        return eType.TypeId;
      if (stringToEdgeType.TryGetValue(name, out eType))
        return eType.TypeId;
      return -1;
    }

    /// <summary>
    /// Finds the type id associated with a particular vertexe type. Lookup by name.
    /// </summary>
    /// <param name="name">The name of the edge/node type being looked up</param>
    /// <returns>A node/edge type id or -1 if not found.</returns>
    public VertexTypeId FindVertexType(string name)
    {
      VertexType nType;
      if (stringToVertexType.TryGetValue(name, out nType))
        return nType.TypeId;
      return -1;
    }

    /*  public ElementType GetType(TypeId type)
      {
        throw new NotImplementedException();
      }*/

    public void RemoveVertexType(VertexTypeId type)
    {
      throw new NotImplementedException();
    }

    public int FindVertexProperty(VertexTypeId type, string name)
    {
      VertexType aType = null;
      if (vertexType.Length > type)
        aType = vertexType[type];
      if (aType != null)
        return aType.FindProperty(name);
      throw new InvalidTypeIdException();
    }

    public int FindEdgeProperty(EdgeTypeId type, string name)
    {
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

    public Vertexes Select(VertexTypeId type)
    {
      throw new NotImplementedException();
    }

    public Vertexes Select(PropertyId attr, Condition cond, object v)
    {
      throw new NotImplementedException();
    }

    public Vertexes Select(PropertyId attr, Condition cond, object lower, object higher)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Selects all edges from or to each of the vertex ids in the given collection and for the given edge type. 
    /// </summary>
    /// <param name="vertexes">a set of vertexes</param>
    /// <param name="etype">the id of an EdgeType</param>
    /// <param name="dir">direction, one of: Ingoing, Outgoing, Any</param>
    /// <returns>a set of Vertex</returns>
    public Vertexes GetVertexes(Vertexes vertexes, EdgeTypeId etype, EdgesDirection dir)
    {
      throw new NotImplementedException();
    }

    public Edges Edges(EdgeTypeId etype, Vertex tail, Vertex head)
    {
      throw new NotImplementedException();
    }

    public Vertex FindEdge(EdgeTypeId etype, Vertex tail, Vertex head)
    {
      throw new NotImplementedException();
    }

    public Vertexes Tails(Vertexes edges)
    {
      throw new NotImplementedException();
    }

    public Vertexes Heads(Vertexes edges)
    {
      throw new NotImplementedException();
    }

    public void TailsAndHeads(Edges edges, Vertexes tails, Vertexes heads)
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

    public Property[] FindVertexProperties(VertexTypeId type)
    {
      throw new NotImplementedException();
    }

    public object[] GetValues(PropertyId property)
    {
      throw new NotImplementedException();
    }
  }
}
