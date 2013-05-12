using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace VelocityGraph
{
	public class Elements : IEnumerable, IEnumerable<long>
	{
    HashSet<long> theElements;

    public Elements()
    {
      theElements = new HashSet<long>();
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
			return obj is Elements && this.Equals((Elements)obj);
		}

		public override int GetHashCode()
		{
      return theElements.GetHashCode();
		}

    IEnumerator<long> IEnumerable<long>.GetEnumerator()
    {
      return theElements.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return theElements.GetEnumerator();
    }
		/// <summary>
    /// Creates a new Objects instance as a copy of the current one. 
		/// </summary>
		/// <returns>The new (copy) instance</returns>
    public Elements Copy()
		{
      Elements result = new Elements();
      foreach (long l in theElements)
        result.theElements.Add(l);
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
        return (long)theElements.Count;
      }
		}

    /// <summary>
    /// Adds an element into the collection.
    /// </summary>
    /// <param name="e">Element to be added.</param>
    /// <returns>true if the element is added, false if the element was already into the collection.</returns>
		public bool Add(long e)
		{
      return theElements.Add(e);
		}

    /// <summary>
    /// Gets if the given element exists in the collection. 
    /// </summary>
    /// <param name="e">Node or Edge checking for</param>
    /// <returns>true if node/edge found, otherwise false.</returns>
		public bool Exists(long e)
		{
      return theElements.Contains(e);
		}

    /// <summary>
    /// Gets an element from the collection.
    /// </summary>
    /// <returns>Any node/edge from the collection.</returns>
		public long Any()
		{
      foreach (long l in theElements)
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
      return theElements.Remove(e);
		}

		/// <summary>
    /// Clears the collection removing all its elements.
		/// </summary>
    public void Clear()
		{
      theElements.Clear();
		}

    /// <summary>
    /// Performs the union operation. This adds all existing elements of the parameter Elements instance to this instance.
    /// </summary>
    /// <param name="objs">The Elements instance forming union with.</param>
    /// <returns>Number of elements into the collection once the operation has been executed.</returns>
		public long Union(Elements objs)
		{
      theElements.UnionWith(objs.theElements);
      return (long) theElements.Count;
		}

    /// <summary>
    /// Updates the Elements calling instance setting those existing elements at both two collections and removing all others.
    /// </summary>
    /// <param name="objs">The Elements instance forming intersection with.</param>
    /// <returns>Number of elements into the collection once the operation has been executed.</returns>
		public long Intersection(Elements objs)
		{
      theElements.IntersectWith(objs.theElements);
      return (long)theElements.Count;
		}

    /// <summary>
    /// Performs the difference operation. This updates the Elements calling instance removing those existing elements at the given Elements instance.
    /// </summary>
    /// <param name="objs">The Elements instance performing difference with.</param>
    /// <returns>Number of elements into the collection once the operation has been executed.</returns>
		public long Difference(Elements objs)
		{
      theElements.ExceptWith(objs.theElements);
      return (long)theElements.Count;
		}

		/// <summary>
    /// Determines whether the specified object is equal to the current object.
		/// </summary>
    /// <param name="objs">The object to compare with the current object.</param>
		/// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
    public bool Equals(Elements objs)
		{
      return theElements.Equals(objs.theElements);
		}

    /// <summary>
    /// Check if this objects contains the other one. 
    /// </summary>
    /// <param name="objs">The object to compare with the current object.</param>
    /// <returns>true if current object contains the specified element; otherwise, false.</returns>
		public bool Contains(Elements objs)
		{
      return theElements.IsSupersetOf(objs.theElements);
		}

		public static Elements CombineUnion(Elements objs1, Elements objs2)
		{
      Elements clone = objs1.Copy();
      clone.Union(objs2);
      return clone;
		}

		public static Elements CombineIntersection(Elements objs1, Elements objs2)
		{
      Elements clone = objs1.Copy();
      clone.Intersection(objs2);
      return clone;
		}

		public static Elements CombineDifference(Elements objs1, Elements objs2)
		{
      Elements clone = objs1.Copy();
      clone.Difference(objs2);
      return clone;
		}

		public long Copy(Elements objs)
		{
      throw new NotImplementedException();
		}

		public Elements Sample(Elements exclude, long samples)
		{
      throw new NotImplementedException();
		}
	}
}
