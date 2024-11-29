using System;
using System.Security.Cryptography;
using System.Text;

namespace honooru.Code {

    // from https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings
    public class ClientSecretGenerator {

        internal static readonly char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray(); 

        /// <summary>
        ///     generate a random string with alphanumeric characters of length <paramref name="size"/>
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string GetUniqueKey(int size) {            
            byte[] data = new byte[4*size];
            using (RandomNumberGenerator crypto = RandomNumberGenerator.Create()) {
                crypto.GetBytes(data);
            }

            StringBuilder result = new StringBuilder(size);
            for (int i = 0; i < size; i++) {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % chars.Length;

                result.Append(chars[idx]);
            }

            return result.ToString();
        }

    }
}
