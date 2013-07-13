using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VelocityDb;
using ElementId = System.Int32;
using PropertyTypeId = System.Int32;
using PropertyId = System.Int32;
using TypeId = System.Int32;
using Frontenac.Blueprints;

namespace VelocityGraph
{
  abstract public class PropertyType : OptimizedPersistable
  {
    string propertyName;
    TypeId typeId;
    PropertyId propertyId;
    bool isVertexProperty;

    protected PropertyType(bool isVertexProp, TypeId typeId, PropertyId propertyId, string name)
    {
      this.typeId = typeId;
      isVertexProperty = isVertexProp;
      this.propertyId = propertyId;
      propertyName = name;
    }

    public PropertyId PropertyId
    {
      get
      {
        return propertyId;
      }
    }    
    
    public TypeId TypeId
    {
      get
      {
        return typeId;
      }
    }

    public string Name
    {
      get
      {
        return propertyName;
      }
    }

    public bool IsVertexProperty
    {
      get
      {
        return isVertexProperty;
      }
    }

    abstract public Vertex GetPropertyVertex(object value, Graph g);
    abstract public Edge GetPropertyEdge(object value, Graph g);
    abstract public object GetPropertyValue(ElementId elementId);
    abstract public void SetPropertyValue(ElementId elementId, object value);
    abstract public object RemovePropertyValue(ElementId elementId);
  }
}
