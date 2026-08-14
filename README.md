# UsmParser
A low-level CRI USM file parser that supports reading USM files from various sources, such as `ReadOnlySpan<byte>`, `ReadOnlySequence<byte>`, or `Stream`.

It does not include additional features such as content decryption or parsing the data chunk payloads.

The basic data chunk format of CRI USM is very simple, with the following structure:

| Offset | Length | Type | Content |
| - | - | - | - |
| 0 | 4 | Four-character code | Signature |
| 4 | 4 | Big-endian unsigned 32-bit integer | Data length |
| 8 | N | Bytes | Data |

Therefore, this library can also be used to parse other binary TLV formats that employ the same pattern.