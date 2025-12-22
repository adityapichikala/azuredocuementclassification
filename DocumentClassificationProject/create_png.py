import struct
import zlib

def create_png(filename):
    width = 1
    height = 1
    bit_depth = 8
    color_type = 2  # Truecolor
    compression_method = 0
    filter_method = 0
    interlace_method = 0

    # Signature
    png_signature = b'\x89PNG\r\n\x1a\n'

    # IHDR
    ihdr_data = struct.pack('!I I B B B B B', width, height, bit_depth, color_type, compression_method, filter_method, interlace_method)
    ihdr_crc = zlib.crc32(b'IHDR' + ihdr_data)
    ihdr = struct.pack('!I', len(ihdr_data)) + b'IHDR' + ihdr_data + struct.pack('!I', ihdr_crc)

    # IDAT
    raw_data = b'\x00\xff\x00\x00' # Filter byte + RGB (red)
    compressed_data = zlib.compress(raw_data)
    idat_crc = zlib.crc32(b'IDAT' + compressed_data)
    idat = struct.pack('!I', len(compressed_data)) + b'IDAT' + compressed_data + struct.pack('!I', idat_crc)

    # IEND
    iend_crc = zlib.crc32(b'IEND')
    iend = struct.pack('!I', 0) + b'IEND' + struct.pack('!I', iend_crc)

    with open(filename, 'wb') as f:
        f.write(png_signature)
        f.write(ihdr)
        f.write(idat)
        f.write(iend)

create_png('test_image.png')
