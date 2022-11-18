using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CBRE.Common.Mediator {
    /* http://www.codeproject.com/Articles/35277/MVVM-Mediator-Pattern */
    /// <summary>
    /// The multi dictionary is a dictionary that contains 
    /// more than one value per key
    /// </summary>
    /// <typeparam name="TKey">The type of the key</typeparam>
    /// <typeparam name="TValue">The type of the list contents</typeparam>
    [Serializable]
    public class MultiDictionary<TKey, TValue> : Dictionary<TKey, List<TValue>> {
        public MultiDictionary() {

        }

        protected MultiDictionary(SerializationInfo info, StreamingContext context) : base(info, context) {

        }

        //checks if the key is already present
        private void EnsureKey(TKey key) {
            if (!ContainsKey(key)) this[key] = new List<TValue>(1);
            else if (this[key] == null) this[key] = new List<TValue>(1);
        }

        /// <summary>
        /// Adds a new value in the Values collection
        /// </summary>
        /// <param name="key">The key where to place the 
        /// item in the value list</param>
        /// <param name="newItem">The new item to add</param>
        public void AddValue(TKey key, TValue newItem) {
            EnsureKey(key);
            this[key].Add(newItem);
        }

        /// <summary>
        /// Adds a list of values to append to the value collection
        /// </summary>
        /// <param name="key">The key where to place the item in the value list</param>
        /// <param name="newItems">The new items to add</param>
        public void AddValues(TKey key, IEnumerable<TValue> newItems) {
            EnsureKey(key);
            this[key].AddRange(newItems);
        }

        /// <summary>
        /// Removes a specific element from the dict
        /// If the value list is empty the key is removed from the dict
        /// </summary>
        /// <param name="key">The key from where to remove the value</param>
        /// <param name="value">The value to remove</param>
        /// <returns>Returns false if the key was not found</returns>
        public bool RemoveValue(TKey key, TValue value) {
            if (!ContainsKey(key)) return false;
            this[key].Remove(value);
            if (this[key].Count == 0) Remove(key);
            return true;
        }

        /// <summary>
        /// Removes all items that match the prediacte
        /// If the value list is empty the key is removed from the dict
        /// </summary>
        /// <param name="key">The key from where to remove the value</param>
        /// <param name="match">The predicate to match the items</param>
        /// <returns>Returns false if the key was not found</returns>
        public bool RemoveAllValue(TKey key, Predicate<TValue> match) {
            if (!ContainsKey(key)) return false;
            this[key].RemoveAll(match);
            if (this[key].Count == 0) this.Remove(key);
            return true;
        }
    }
}
