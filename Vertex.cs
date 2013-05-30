using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeId = System.Int32;
using VertexId = System.Int32;

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
    TypeId vertexType;
    VertexId vertextId;

    public Vertex(TypeId eType, VertexId eId)
    {
      vertexType = eType;
      vertextId = eId;
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
  }
}
