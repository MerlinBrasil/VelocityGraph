using System;
using System.Linq;
using VelocityDb;

namespace VelocityGraph
{
  public class Property : OptimizedPersistable
	{
		public static int InvalidProperty
		{
      get
      {
        return -1;
      }
		}

		public int GetId()
		{
      return 0;
		}

		public int GetTypeId()
		{
      return 0;
		}

		public string GetName()
		{
      return null;
		}

		public DataType GetDataType()
		{
      throw new NotImplementedException();
		}

		public long GetSize()
		{
      throw new NotImplementedException();
		}

		public long GetCount()
		{
      throw new NotImplementedException();
		}

		public PropertyKind GetKind()
		{
      throw new NotImplementedException();
		}

		public bool IsSessionProperty()
		{
      throw new NotImplementedException();
		}
	}
}
