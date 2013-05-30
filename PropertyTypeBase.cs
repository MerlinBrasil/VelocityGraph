using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VelocityDb;
using ElementId = System.Int32;
using PropertyTypeId = System.Int32;
using PropertyId = System.Int32;
using TypeId = System.Int32;

namespace VelocityGraph
{
  abstract public class PropertyTypeBase : OptimizedPersistable
  {
    string propertyName;
    TypeId typeId;
    PropertyId propertyId;

    protected PropertyTypeBase(TypeId typeId, PropertyId propertyId, string name)
    {
      this.typeId = typeId;
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

    abstract public ElementId GetPropertyElementId(object value);
    abstract public object GetPropertyValue(ElementId elementId);
    abstract public void SetPropertyValue(ElementId elementId, object value);
  }
}
