using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Xml;

namespace Windows_App_Shared_Data
{
    public static class Helpers
    {
        public static int APPX_E_MISSING_REQUIRED_FILE = int.Parse("80080203", System.Globalization.NumberStyles.HexNumber);

        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }

        public static string GetHash(List<byte> inputBuffer)
        {
            System.Security.Cryptography.SHA256 shaHasher = System.Security.Cryptography.SHA256.Create();

            byte[] hash = shaHasher.ComputeHash(inputBuffer.ToArray());

            return Convert.ToBase64String(hash);
        }

        public static T MergeLeft<T, K, V>(this T me, params IDictionary<K, V>[] others)
             where T : IDictionary<K, V>, new()
        {
            T newMap = new T();
            foreach (IDictionary<K, V> src in
                (new List<IDictionary<K, V>> { me }).Concat(others))
            {
                // ^-- echk. Not quite there type-system.
                foreach (KeyValuePair<K, V> p in src)
                {
                    newMap[p.Key] = p.Value;
                }
            }
            return newMap;
        }
    }
}
