// Original code and comments Copyright (C) 1995-2011, 2016 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

namespace ZLibDotNet;

internal static class Adler32
{
    /// <summary>
    /// Computes the Adler-32 checksum of a data stream.
    /// </summary>
    internal static unsafe uint Update(uint adler, byte* buf, uint len)
    {
        const ushort Base = 65521; // largest prime smaller than 65536

        // split Adler-32 into component sums
        uint sum2 = (adler >> 16) & 0xffff;
        adler &= 0xffff;

        // in case user likes doing a byte at a time, keep it fast
        if (len == 1)
        {
            adler += buf[0];
            if (adler >= Base)
                adler -= Base;
            sum2 += adler;
            if (sum2 >= Base)
                sum2 -= Base;
            return adler | (sum2 << 16);
        }

        // initial Adler-32 value (deferred check for len == 1 speed)
        if (buf == null)
            return 1U;

        // in case short lengths are provided, keep it somewhat fast
        if (len < 16)
        {
            while (len-- > 0)
            {
                adler += *buf++;
                sum2 += adler;
            }
            if (adler >= Base)
                adler -= Base;
            sum2 %= Base; // only added so many BASE's
            return adler | (sum2 << 16);
        }

        const ushort NMAX = 5552;
        uint n;
#pragma warning disable IDE2001
        // do length NMAX blocks -- requires just one modulo operation
        while (len >= NMAX)
        {
            len -= NMAX;
            n = NMAX / 16; // NMAX is divisible by 16
            do
            {
                adler += buf[0]; sum2 += adler; // 16 sums unrolled
                adler += buf[1]; sum2 += adler;
                adler += buf[2]; sum2 += adler;
                adler += buf[3]; sum2 += adler;
                adler += buf[4]; sum2 += adler;
                adler += buf[5]; sum2 += adler;
                adler += buf[6]; sum2 += adler;
                adler += buf[7]; sum2 += adler;
                adler += buf[8]; sum2 += adler;
                adler += buf[9]; sum2 += adler;
                adler += buf[10]; sum2 += adler;
                adler += buf[11]; sum2 += adler;
                adler += buf[12]; sum2 += adler;
                adler += buf[13]; sum2 += adler;
                adler += buf[14]; sum2 += adler;
                adler += buf[15]; sum2 += adler;
                buf += 16;
            } while (--n > 0);
            adler %= Base;
            sum2 %= Base;
        }

        // do remaining bytes (less than NMAX, still just one modulo)
        if (len > 0) // avoid modulos if none remaining
        {
            while (len >= 16)
            {
                len -= 16;
                adler += buf[0]; sum2 += adler;
                adler += buf[1]; sum2 += adler;
                adler += buf[2]; sum2 += adler;
                adler += buf[3]; sum2 += adler;
                adler += buf[4]; sum2 += adler;
                adler += buf[5]; sum2 += adler;
                adler += buf[6]; sum2 += adler;
                adler += buf[7]; sum2 += adler;
                adler += buf[8]; sum2 += adler;
                adler += buf[9]; sum2 += adler;
                adler += buf[10]; sum2 += adler;
                adler += buf[11]; sum2 += adler;
                adler += buf[12]; sum2 += adler;
                adler += buf[13]; sum2 += adler;
                adler += buf[14]; sum2 += adler;
                adler += buf[15]; sum2 += adler;
                buf += 16;
            }
            while (len-- > 0)
            {
                adler += *buf++;
                sum2 += adler;
            }
            adler %= Base;
            sum2 %= Base;
        }
#pragma warning restore IDE2001

        // return recombined sums
        return adler | (sum2 << 16);
    }
}