using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VelocityDb.Collection.BTree;
using VelocityDb.Session;
using ElementId = System.Int32;
using PropertyTypeId = System.Int32;
using PropertyId = System.Int32;
using TypeId = System.Int32;


namespace VelocityGraph
{
  public class PropertyType<T> : PropertyTypeBase
  {
    Dictionary<ElementId, T> propertyValue;
    BTreeMap<T, ElementId[]> valueIndex;
    BTreeMap<T, ElementId> valueIndexUnique;

    public PropertyType(TypeId typeId, PropertyId propertyId, string name, PropertyKind kind, SessionBase session)
      : base(typeId, propertyId, name)
    {
      propertyValue = new Dictionary<ElementId, T>();
      switch (kind)
      {
        case PropertyKind.Indexed:
          valueIndex = new BTreeMap<T, ElementId[]>(null, session);
          break;
        case PropertyKind.Unique:
          valueIndexUnique = new BTreeMap<T, ElementId>(null, session);
          break;
      }
    }

    T GetPropertyValueT(ElementId oid)
    {
      T pv = propertyValue[oid];
      return pv;
    }

    void SetPropertyValueX(ElementId element, T aValue)
    {
      Update();
      propertyValue[element] = aValue;
      if (valueIndex != null)
      {
        ElementId[] oidArray = new ElementId[1];
        if (!valueIndex.TryGetKey(aValue, ref aValue))
        {
          oidArray[0] = element;
          valueIndex.Add(aValue, oidArray);
        }
        else
        {
          oidArray = valueIndex[aValue];
          int pos = oidArray.Length;
          Array.Resize(ref oidArray, pos + 1);
          oidArray[pos] = element;
        }
      }
      else if (valueIndexUnique != null)
        valueIndexUnique.Add(aValue, element);
    }

    public ElementId GetPropertyElementId(T value)
    {
      ElementId elementId;
      if (valueIndexUnique != null && valueIndexUnique.TryGetValue(value, out elementId))
        return elementId;
      ElementId[] elementIds;
      if (valueIndex != null && valueIndex.TryGetValue(value, out elementIds))
        return elementIds[0];
      return -1;
    }

    public override ElementId GetPropertyElementId(object value)
    {
      return GetPropertyElementId((T) value);
    }

    public override object GetPropertyValue(ElementId element)
    {
      return GetPropertyValueT(element);
    }

    public override void SetPropertyValue(ElementId element, object value)
    {
      SetPropertyValueX(element, (T)value);
    }
  }
}
