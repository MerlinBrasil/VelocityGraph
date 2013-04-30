using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace VelocityGraph
{
	public class Objects : IEnumerable, IEnumerable<long>
	{
    HashSet<long> theObjects;

    public Objects()
    {
      theObjects = new HashSet<long>();
    }

		public static long InvalidOID
		{
			get
			{
        long result = 0;
				return result;
			}
		}

		public override bool Equals(object obj)
		{
			return obj is Objects && this.Equals((Objects)obj);
		}

		public override int GetHashCode()
		{
      return theObjects.GetHashCode();
		}

    IEnumerator<long> IEnumerable<long>.GetEnumerator()
    {
      return theObjects.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return theObjects.GetEnumerator();
    }
		/// <summary>
    /// Creates a new Objects instance as a copy of the current one. 
		/// </summary>
		/// <returns>The new (copy) instance</returns>
    public Objects Copy()
		{
      Objects result = new Objects();
      foreach (long l in theObjects)
        result.theObjects.Add(l);
			return result;
		}

    /// <summary>
    /// Gets the number of elements into the collection.
    /// </summary>
    /// <returns>The number of elements in the collection.</returns>
		public long Count
		{
      get
      {
        return (long)theObjects.Count;
      }
		}

    /// <summary>
    /// Adds an element into the collection.
    /// </summary>
    /// <param name="e">Element to be added.</param>
    /// <returns>true if the element is added, false if the element was already into the collection.</returns>
		public bool Add(long e)
		{
      return theObjects.Add(e);
		}

    /// <summary>
    /// Gets if the given element exists in the collection. 
    /// </summary>
    /// <param name="e">Node or Edge checking for</param>
    /// <returns>true if node/edge found, otherwise false.</returns>
		public bool Exists(long e)
		{
      return theObjects.Contains(e);
		}

    /// <summary>
    /// Gets an element from the collection.
    /// </summary>
    /// <returns>Any node/edge from the collection.</returns>
		public long Any()
		{
      foreach (long l in theObjects)
        return l;
      return 0;
		}

    /// <summary>
    /// Removes an element from the collection. 
    /// </summary>
    /// <param name="e"></param>
    /// <returns>true if element found and removed from the collection, otherwise false.</returns>
		public bool Remove(long e)
		{
      return theObjects.Remove(e);
		}

		/// <summary>
    /// Clears the collection removing all its elements.
		/// </summary>
    public void Clear()
		{
      theObjects.Clear();
		}

    /// <summary>
    /// Performs the union operation. This adds all existing elements of the parameter Objects instance to this instance.
    /// </summary>
    /// <param name="objs">The Objects instance forming union with.</param>
    /// <returns>Number of elements into the collection once the operation has been executed.</returns>
		public long Union(Objects objs)
		{
      theObjects.UnionWith(objs.theObjects);
      return (long) theObjects.Count;
		}

    /// <summary>
    /// Updates the Objects calling instance setting those existing elements at both two collections and removing all others.
    /// </summary>
    /// <param name="objs">The Objects instance forming intersection with.</param>
    /// <returns>Number of elements into the collection once the operation has been executed.</returns>
		public long Intersection(Objects objs)
		{
      theObjects.IntersectWith(objs.theObjects);
      return (long)theObjects.Count;
		}

    /// <summary>
    /// Performs the difference operation. This updates the Objects calling instance removing those existing elements at the given Objects instance.
    /// </summary>
    /// <param name="objs">The Objects instance performing difference with.</param>
    /// <returns>Number of elements into the collection once the operation has been executed.</returns>
		public long Difference(Objects objs)
		{
      theObjects.ExceptWith(objs.theObjects);
      return (long)theObjects.Count;
		}

		/// <summary>
    /// Determines whether the specified object is equal to the current object.
		/// </summary>
    /// <param name="objs">The object to compare with the current object.</param>
		/// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
    public bool Equals(Objects objs)
		{
      return theObjects.Equals(objs.theObjects);
		}

    /// <summary>
    /// Check if this objects contains the other one. 
    /// </summary>
    /// <param name="objs">The object to compare with the current object.</param>
    /// <returns>true if current object contains the specified element; otherwise, false.</returns>
		public bool Contains(Objects objs)
		{
      return theObjects.IsSupersetOf(objs.theObjects);
		}

		public static Objects CombineUnion(Objects objs1, Objects objs2)
		{
      Objects clone = objs1.Copy();
      clone.Union(objs2);
      return clone;
		}

		public static Objects CombineIntersection(Objects objs1, Objects objs2)
		{
      Objects clone = objs1.Copy();
      clone.Intersection(objs2);
      return clone;
		}

		public static Objects CombineDifference(Objects objs1, Objects objs2)
		{
      Objects clone = objs1.Copy();
      clone.Difference(objs2);
      return clone;
		}

		public long Copy(Objects objs)
		{
      throw new NotImplementedException();
		}

		public Objects Sample(Objects exclude, long samples)
		{
      throw new NotImplementedException();
		}
	}
}
