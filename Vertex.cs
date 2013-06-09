using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeId = System.Int32;
using VertexId = System.Int32;
using VertexTypeId = System.Int32;
using PropertyId = System.Int32;
using EdgeTypeId = System.Int32;

namespace VelocityGraph
{
  /// <summary>
  /// A vertex maintains pointers to both a set of incoming and outgoing edges.
  /// The outgoing edges are those edges for which the vertex is the tail.
  /// The incoming edges are those edges for which the vertex is the head.
  /// Diagrammatically, ---inEdges---> vertex ---outEdges--->
  /// </summary>
  public struct Vertex
  {
    VertexType vertexType;
    VertexId vertextId;
    Graph graph; // back pointer

    internal Vertex(Graph g, VertexType eType, VertexId eId)
    {
      vertexType = eType;
      vertextId = eId;
      graph = g;
    }

    /// <summary>
    /// Drops this vertex. It also removes its egdges as well as its attribute values. 
    /// </summary>
    public void Drop()
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the number of edges from or to this vertex and for the given edge type. 
    /// </summary>
    /// <param name="etype">the id of an EdgeType</param>
    /// <param name="dir">direction, one of: Ingoing, Outgoing, Any</param>
    /// <returns>The number of edges.</returns>
    public long GetNumberOfEdges(EdgeTypeId etype, EdgesDirection dir)
    {
      throw new NotImplementedException();
    }


    public PropertyType[] GetProperties()
    {
      if (vertexType != null)
        return vertexType.vertexProperties;
      throw new InvalidTypeIdException();
    }

    /// <summary>
    /// Gets the Value for the given Property id
    /// </summary>
    /// <param name="property">Property type identifier.</param>
    /// <param name="v">Value for the given Property and for the given id.</param>
    public object GetProperty(PropertyType property)
    {
      if (vertexType != null)
        return vertexType.GetPropertyValue(VertexId, property);
      throw new InvalidTypeIdException();
    }

    /// <summary>
    /// Selects all edges from or to the given vertex oid and for the given edge type. 
    /// </summary>
    /// <param name="etype">the id of an EdgeType</param>
    /// <param name="dir">direction, one of: Ingoing, Outgoing, Any</param>
    /// <returns>a set of Edge</returns>
    public HashSet<Edge> GetEdges(EdgeTypeId etype, EdgesDirection dir)
    {
      throw new NotImplementedException();
    }

    public VertexId VertexId
    {
      get
      {
        return vertextId;
      }
    }

    internal VertexType VertexType
    {
      get
      {
        return vertexType;
      }
    }

    /// <summary>
    /// Selects all neighbor Vertices from or to this vertex and for the given edge type.
    /// </summary>
    /// <param name="etype">Edge type identifier.</param>
    /// <param name="dir">Direction</param>
    /// <returns>Dictionary of vertex keys with edges path to vertex</returns>
    public Dictionary<Vertex, HashSet<Edge>> Traverse(EdgeType etype, EdgesDirection dir)
    {
      return vertexType.Traverse(graph, this, etype, dir);
    }

    public void SetProperty(PropertyType property, object v)
    {
      if (vertexType != null)
        vertexType.SetPropertyValue(VertexId, property, v);
      else
        throw new InvalidTypeIdException();
    }
  }
}
