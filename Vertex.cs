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
  using Vertexes = System.Collections.Generic.HashSet<Vertex>;
  /// <summary>
  /// A vertex maintains pointers to both a set of incoming and outgoing edges.
  /// The outgoing edges are those edges for which the vertex is the tail.
  /// The incoming edges are those edges for which the vertex is the head.
  /// Diagrammatically, ---inEdges---> vertex ---outEdges--->
  /// </summary>
  public struct Vertex
  {
    TypeId vertexType;
    VertexId vertextId;
    Graph graph; // back pointer

    public Vertex(Graph g, TypeId eType, VertexId eId)
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
    /// Gets information about an edge.
    /// </summary>
    /// <returns></returns>
    public EdgeData GetEdgeData()
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

    public Property[] GetProperties()
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the Value for the given Property and OID.
    /// </summary>
    /// <param name="oid">OID</param>
    /// <param name="property">Property type identifier.</param>
    /// <param name="v">Value for the given Property and for the given OID.</param>
    public object GetProperty(PropertyId property)
    {
      VertexTypeId typeId = VertexType;
      VertexType aVertexType = null;
      if (graph.vertexType.Length > typeId)
        aVertexType = graph.vertexType[typeId];
      if (aVertexType != null)
      {
        return aVertexType.GetPropertyValue(graph.propertyType, VertexId, property);
      }
      else
      {
        EdgeType anEdgeType = null;
        if (graph.edgeType.Length > typeId)
          anEdgeType = graph.edgeType[typeId];
        if (anEdgeType != null)
          return anEdgeType.GetPropertyValue(graph.propertyType, VertexId, property);
        else
        {
          anEdgeType = graph.restrictedEdgeType[typeId];
          return anEdgeType.GetPropertyValue(graph.propertyType, VertexId, property);
        }
      }
    }

    /// <summary>
    /// Selects all edges from or to the given vertex oid and for the given edge type. 
    /// </summary>
    /// <param name="oid">the id of a Vertex</param>
    /// <param name="etype">the id of an EdgeType</param>
    /// <param name="dir">direction, one of: Ingoing, Outgoing, Any</param>
    /// <returns>a set of Vertex</returns>
    public Vertexes GetVertexes(EdgeTypeId etype, EdgesDirection dir)
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

    public TypeId VertexType
    {
      get
      {
        return vertexType;
      }
    }

    /// <summary>
    /// Selects all neighbor nodes from or to the given node OID and for the given edge type.
    /// </summary>
    /// <param name="oid">Node OID</param>
    /// <param name="etype">Edge type identifier.</param>
    /// <param name="dir">Direction</param>
    /// <returns>Objects instance</returns>
    public Vertexes Neighbors(EdgeTypeId etype, EdgesDirection dir)
    {
      VertexTypeId typeId = VertexType;
      VertexType aNodeType = graph.vertexType[typeId];
      EdgeType anEdgeType = null;
      if (graph.edgeType.Length > etype)
        anEdgeType = graph.edgeType[etype];
      if (anEdgeType != null)
        return aNodeType.Neighbors(graph, VertexId, anEdgeType, dir);
      anEdgeType = graph.restrictedEdgeType[etype];
      return aNodeType.Neighbors(graph, VertexId, anEdgeType, dir);
    }

    public void SetProperty(PropertyId property, object v)
    {
      VertexTypeId typeId = VertexType;
      VertexType aType = null;
      if (graph.vertexType.Length > typeId)
        aType = graph.vertexType[typeId];
      if (aType != null)
        aType.SetPropertyValue(graph.propertyType, VertexId, property, v);
      else
        throw new InvalidTypeIdException();
    }
  }
}
