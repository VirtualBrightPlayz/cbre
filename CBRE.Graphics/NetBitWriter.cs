/* Copyright (c) 2010 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.

*/

namespace CBRE.Graphics {
    public class NetBitWriter {

        public static void WriteByte(byte source, int numberOfBits, byte[] destination, int destBitOffset) {
            if (numberOfBits == 0)
                return;

            //NetException.Assert(((numberOfBits >= 0) && (numberOfBits <= 8)), "Must write between 0 and 8 bits!");

            // Mask out all the bits we dont want
            source = (byte)(source & (0xFF >> (8 - numberOfBits)));

            int p = destBitOffset >> 3;
            int bitsUsed = destBitOffset & 0x7; // mod 8
            int bitsFree = 8 - bitsUsed;
            int bitsLeft = bitsFree - numberOfBits;

            // Fast path, everything fits in the first byte
            if (bitsLeft >= 0) {
                int mask = (0xFF >> bitsFree) | (0xFF << (8 - bitsLeft));

                destination[p] = (byte)(
                    // Mask out lower and upper bits
                    (destination[p] & mask) |

                    // Insert new bits
                    (source << bitsUsed)
                );

                return;
            }

            destination[p] = (byte)(
                // Mask out upper bits
                (destination[p] & (0xFF >> bitsFree)) |

                // Write the lower bits to the upper bits in the first byte
                (source << bitsUsed)
            );

            p += 1;

            destination[p] = (byte)(
                // Mask out lower bits
                (destination[p] & (0xFF << (numberOfBits - bitsFree))) |

                // Write the upper bits to the lower bits of the second byte
                (source >> bitsFree)
            );
        }

        public static int WriteUInt32(uint source, int numberOfBits, byte[] destination, int destinationBitOffset) {
#if BIGENDIAN
			// reorder bytes
			source = ((source & 0xff000000) >> 24) |
				((source & 0x00ff0000) >> 8) |
				((source & 0x0000ff00) << 8) |
				((source & 0x000000ff) << 24);
#endif

            int returnValue = destinationBitOffset + numberOfBits;
            if (numberOfBits <= 8) {
                NetBitWriter.WriteByte((byte)source, numberOfBits, destination, destinationBitOffset);
                return returnValue;
            }
            NetBitWriter.WriteByte((byte)source, 8, destination, destinationBitOffset);
            destinationBitOffset += 8;
            numberOfBits -= 8;

            if (numberOfBits <= 8) {
                NetBitWriter.WriteByte((byte)(source >> 8), numberOfBits, destination, destinationBitOffset);
                return returnValue;
            }
            NetBitWriter.WriteByte((byte)(source >> 8), 8, destination, destinationBitOffset);
            destinationBitOffset += 8;
            numberOfBits -= 8;

            if (numberOfBits <= 8) {
                NetBitWriter.WriteByte((byte)(source >> 16), numberOfBits, destination, destinationBitOffset);
                return returnValue;
            }
            NetBitWriter.WriteByte((byte)(source >> 16), 8, destination, destinationBitOffset);
            destinationBitOffset += 8;
            numberOfBits -= 8;

            NetBitWriter.WriteByte((byte)(source >> 24), numberOfBits, destination, destinationBitOffset);
            return returnValue;
        }
    }
}
