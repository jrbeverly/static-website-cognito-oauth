import { Buffer } from 'node:buffer'

/**
 * Creates a minimal valid ZIP file in a Buffer containing an index.html.
 * Uses "store" (no compression) for simplicity — no external dependencies.
 *
 * ZIP structure:
 *   [Local File Header][File Data][Central Directory Entry][EOCD Record]
 */
export function createTestZip(): Buffer {
  const filename = 'index.html'
  const content = Buffer.from('<!DOCTYPE html>\n<html>\n<head><title>Test</title></head>\n<body><h1>Hello from Playwright</h1></body>\n</html>\n', 'utf-8')
  const filenameBytes = Buffer.from(filename, 'utf-8')

  const localHeader = Buffer.alloc(30 + filenameBytes.length)
  let offset = 0

  // Local file header signature (0x04034b50)
  localHeader.writeUInt32LE(0x04034b50, offset); offset += 4
  // Version needed to extract (2.0)
  localHeader.writeUInt16LE(20, offset); offset += 2
  // General purpose bit flag
  localHeader.writeUInt16LE(0, offset); offset += 2
  // Compression method (0 = store)
  localHeader.writeUInt16LE(0, offset); offset += 2
  // Last mod file time / date
  localHeader.writeUInt16LE(0, offset); offset += 2
  localHeader.writeUInt16LE(0, offset); offset += 2
  // CRC-32 (0 for store method when no CRC is computed — many tools accept this)
  localHeader.writeUInt32LE(0, offset); offset += 4
  // Compressed size
  localHeader.writeUInt32LE(content.length, offset); offset += 4
  // Uncompressed size
  localHeader.writeUInt32LE(content.length, offset); offset += 4
  // Filename length
  localHeader.writeUInt16LE(filenameBytes.length, offset); offset += 2
  // Extra field length
  localHeader.writeUInt16LE(0, offset); offset += 2
  // Filename
  filenameBytes.copy(localHeader, offset)

  // Central directory entry
  const centralDir = Buffer.alloc(46 + filenameBytes.length)
  offset = 0
  // Central directory signature (0x02014b50)
  centralDir.writeUInt32LE(0x02014b50, offset); offset += 4
  // Version made by
  centralDir.writeUInt16LE(20, offset); offset += 2
  // Version needed
  centralDir.writeUInt16LE(20, offset); offset += 2
  // General purpose bit flag
  centralDir.writeUInt16LE(0, offset); offset += 2
  // Compression method
  centralDir.writeUInt16LE(0, offset); offset += 2
  // Last mod time / date
  centralDir.writeUInt16LE(0, offset); offset += 2
  centralDir.writeUInt16LE(0, offset); offset += 2
  // CRC-32
  centralDir.writeUInt32LE(0, offset); offset += 4
  // Compressed size
  centralDir.writeUInt32LE(content.length, offset); offset += 4
  // Uncompressed size
  centralDir.writeUInt32LE(content.length, offset); offset += 4
  // Filename length
  centralDir.writeUInt16LE(filenameBytes.length, offset); offset += 2
  // Extra field length
  centralDir.writeUInt16LE(0, offset); offset += 2
  // File comment length
  centralDir.writeUInt16LE(0, offset); offset += 2
  // Disk number start
  centralDir.writeUInt16LE(0, offset); offset += 2
  // Internal file attributes
  centralDir.writeUInt16LE(0, offset); offset += 2
  // External file attributes
  centralDir.writeUInt32LE(0, offset); offset += 4
  // Relative offset of local header
  centralDir.writeUInt32LE(0, offset); offset += 4
  // Filename
  filenameBytes.copy(centralDir, offset)

  // End of central directory record
  const eocd = Buffer.alloc(22)
  offset = 0
  // EOCD signature (0x06054b50)
  eocd.writeUInt32LE(0x06054b50, offset); offset += 4
  // Number of this disk
  eocd.writeUInt16LE(0, offset); offset += 2
  // Disk where central directory starts
  eocd.writeUInt16LE(0, offset); offset += 2
  // Number of central directory entries on this disk
  eocd.writeUInt16LE(1, offset); offset += 2
  // Total number of central directory entries
  eocd.writeUInt16LE(1, offset); offset += 2
  // Size of central directory
  eocd.writeUInt32LE(centralDir.length, offset); offset += 4
  // Offset of start of central directory
  eocd.writeUInt32LE(localHeader.length + content.length, offset); offset += 4
  // Comment length
  eocd.writeUInt16LE(0, offset)

  return Buffer.concat([localHeader, content, centralDir, eocd])
}

/**
 * Creates a Buffer that is NOT a valid ZIP file (plain text).
 */
export function createNonZipFile(): { name: string; buffer: Buffer; mimeType: string } {
  return {
    name: 'not-a-zip.txt',
    buffer: Buffer.from('This is not a ZIP file.', 'utf-8'),
    mimeType: 'text/plain',
  }
}
