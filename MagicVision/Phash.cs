using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MagicVision
{
    public class Phash
    {

        [DllImport("pHash.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ph_dct_imagehash(string file_name, ref UInt64 Hash);

        private static UInt64 m1 = 0x5555555555555555;
        private static UInt64 m2 = 0x3333333333333333;
        private static UInt64 h01 = 0x0101010101010101;
        private static UInt64 m4 = 0x0f0f0f0f0f0f0f0f;

        // Calculate the similarity between two hashes
        public static int HammingDistance(UInt64 hash1, UInt64 hash2)
        {
            UInt64 x = hash1 ^ hash2;


            x -= (x >> 1) & m1;
            x = (x & m2) + ((x >> 2) & m2);
            x = (x + (x >> 4)) & m4;
            UInt64 res = (x * h01) >> 56;

            return (int)res;
        }
    }
}
