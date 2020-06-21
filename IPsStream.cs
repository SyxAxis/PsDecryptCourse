using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VideoDecryption
{
    public interface IPsStream
    {
        void Dispose();
        int Read(byte[] pv, int i, int count);
        void Seek(int offset, SeekOrigin begin);

        long Length { get; }

        int BlockSize { get; }
    }
}
