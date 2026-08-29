import { describe, it, expect } from 'vitest'
import { generateCodeVerifier, generateCodeChallenge, base64UrlEncode } from '@/utils/pkce'

describe('base64UrlEncode', () => {
  it('encodes a Uint8Array to a base64url string without padding', () => {
    // "test" in bytes → base64 is "dGVzdA==" → base64url is "dGVzdA"
    const input = new Uint8Array([116, 101, 115, 116])
    const result = base64UrlEncode(input)
    expect(result).toBe('dGVzdA')
    expect(result).not.toContain('=')
    expect(result).not.toContain('+')
    expect(result).not.toContain('/')
  })

  it('encodes an ArrayBuffer', () => {
    const input = new TextEncoder().encode('hello')
    const result = base64UrlEncode(input.buffer)
    expect(result).toBe('aGVsbG8')
  })

  it('replaces + with - and / with _', () => {
    // Bytes that produce + and / in standard base64
    // 0xFB 0xFF 0xFF → standard base64 "+///"
    const input = new Uint8Array([251, 255, 255])
    const result = base64UrlEncode(input)
    expect(result).toBe('-___')
  })
})

describe('generateCodeVerifier', () => {
  it('returns a string of 43 characters (32 bytes, base64url, no padding)', () => {
    const verifier = generateCodeVerifier()
    expect(verifier.length).toBe(43)
  })

  it('contains only base64url characters', () => {
    const verifier = generateCodeVerifier()
    expect(verifier).toMatch(/^[A-Za-z0-9_-]+$/)
  })

  it('returns different values on subsequent calls', () => {
    const a = generateCodeVerifier()
    const b = generateCodeVerifier()
    expect(a).not.toBe(b)
  })
})

describe('generateCodeChallenge', () => {
  it('returns a base64url-encoded SHA-256 hash of the verifier', async () => {
    const verifier = 'test-verifier'
    const challenge = await generateCodeChallenge(verifier)

    expect(challenge).toMatch(/^[A-Za-z0-9_-]+$/)
    expect(challenge).not.toContain('=')
  })

  it('produces consistent output for the same input', async () => {
    const verifier = 'consistent-verifier-value'
    const a = await generateCodeChallenge(verifier)
    const b = await generateCodeChallenge(verifier)
    expect(a).toBe(b)
  })

  it('produces different output for different inputs', async () => {
    const a = await generateCodeChallenge('verifier-a')
    const b = await generateCodeChallenge('verifier-b')
    expect(a).not.toBe(b)
  })
})
