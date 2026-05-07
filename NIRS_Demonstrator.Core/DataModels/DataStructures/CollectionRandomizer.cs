using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NIRS_Demonstrator.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class CollectionRandomizer<T>
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members

        #endregion

        #region Public Properties

        #endregion

        #region Public Commands

        #endregion

        #region Public Events

        #endregion

        #region Private Callbacks

        #endregion

        #region Command Methods

        #endregion

        #region Public Methods

        public List<T> CollectionRandomize(List<T> source, int count)
        {
            Random random = new Random();
            if (count > source.Count())
                throw new ArgumentException("count must be less or equals source.Count()", "count");
            List<T> tmp = new List<T>();
            Collection<int> ints = new Collection<int>();
            for (int i = 0; i < count; i++)
                ints.Add(i);

            //Collection<int> indexes = new Collection<int>();
            for (int i = 0; i < count; i++)
            {
                int idx = random.Next(0, (count - i));
                //indexes.Add(ints[idx]);
                tmp.Add(source[ints[idx]]);
                ints.Remove(ints[idx]);
            }
            return tmp;
        }

        public List<T> CollectionRandomize(List<T> source, T reper, int count, bool addReaper = true)
        {
            Random random = new Random();
            if (count > source.Count())
                throw new ArgumentException("count must be less or equals source.Count()", "count");
            List<T> tmp = new List<T>();
            Collection<int> ints = new Collection<int>();
            int randSize = source.Count;
            for (int i = 0; i < randSize; i++)
                ints.Add(i);

            //Collection<int> indexes = new Collection<int>();
            //int c_idx = 0;
            //while()
            //{

            //}
            for (int i = 0; i < (count); i++)
            {
                //if(i == 3)
                //    break;
                int idx = random.Next(0, (randSize - i));
                if(source[ints[idx]].Equals(reper))
                {
                    ints.Remove(ints[idx]);
                    continue;
                }
                //indexes.Add(ints[idx]);
                tmp.Add(source[ints[idx]]);
                ints.Remove(ints[idx]);
            }
            
            if (addReaper)
            {
                if (tmp.Count == 4)
                    tmp.RemoveAt(3);
                tmp.Add(reper);
            }
                
            return tmp;
        }

        #endregion

        #region Private Methods

        #endregion
    }
}
