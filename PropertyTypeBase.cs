using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VelocityDb;

namespace VelocityGraph
{
  abstract public class PropertyTypeBase : OptimizedPersistable
  {
    string propertyName;
    int propertyTypeId;

    protected PropertyTypeBase(int typeId, string name)
    {
      propertyTypeId = typeId;
      propertyName = name;
    }

    public int PropertyTypeId
    {
      get
      {
        return propertyTypeId;
      }
    }

    abstract public object GetProperty(uint oid);
    abstract public void SetProperty(uint oid, object aValue);
  }
}
