using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VelocityDb.Collection.BTree;
using VelocityDb.Session;

namespace VelocityGraph
{
  public class PropertyType<T> : PropertyTypeBase
  {
    Dictionary<UInt32, T> propertyValue;
    BTreeMap<T, UInt32[]> valueIndex;
    BTreeMap<T, UInt32> valueIndexUnique;

    public PropertyType(int typeId, string name, PropertyKind kind, SessionBase session)
      : base(typeId, name)
    {
      propertyValue = new Dictionary<uint, T>();
      switch (kind)
      {
        case PropertyKind.Indexed:
          valueIndex = new BTreeMap<T, UInt32[]>(null, session);
          break;
        case PropertyKind.Unique:
          valueIndexUnique = new BTreeMap<T, uint>(null, session);
          break;
      }
    }

    T GetPropertyT(uint oid)
    {
      T pv = propertyValue[oid];
      return pv;
    }

    void SetPropertyX(uint oid, T aValue)
    {
      Update();
      propertyValue[oid] = aValue;
      if (valueIndex != null)
      {
        UInt32[] oidArray = new UInt32[1];
        if (!valueIndex.TryGetKey(aValue, ref aValue))
        {
          oidArray[0] = oid;
          valueIndex.Add(aValue, oidArray);
        }
        else
        {
          oidArray = valueIndex[aValue];
          int pos = oidArray.Length;
          Array.Resize(ref oidArray, pos + 1);
          oidArray[pos] = oid;
        }
      }
      else if (valueIndexUnique != null)
        valueIndexUnique.Add(aValue, oid);
    }

    public override object GetProperty(uint oid)
    {
      return GetPropertyT(oid);
    }

    public override void SetProperty(uint oid, object aValue)
    {
      SetPropertyX(oid, (T)aValue);
    }
  }
}
