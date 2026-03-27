/**
 * R2 CDN Worker
 *
 * Routing:
 *   /assets/*          → HMAC-signed (private). Validates ?expires=&sig= before serving.
 *   /images/*          → Public (thumbnails, group icons). No auth required.
 *
 * Caching strategy:
 *   Assets  → Edge-cached by pathname only (sig/expires stripped from cache key).
 *             Browser receives Cache-Control: private so it won't share across users.
 *   Images  → Edge-cached by full URL, public, long TTL.
 *
 * Required bindings (wrangler.toml):
 *   R2_BUCKET  – R2 bucket binding
 *   HMAC_SECRET – wrangler secret (matches C# Configuration.HmacSecret)
 */

const ASSET_PREFIX = "/assets/";
const CACHE_TTL_ASSETS = 60 * 60;          // 1 hour  (edge only; browser is private)
const CACHE_TTL_IMAGES = 60 * 60 * 24 * 7; // 7 days

export default {
  async fetch(request, env, ctx) {
    const url = new URL(request.url);

    // ── Strip query params for cache key on assets ────────────────────────────
    const isAsset = url.pathname.startsWith(ASSET_PREFIX);
    const cacheKey = isAsset
      ? new Request(new URL(url.pathname, url.origin).toString())
      : new Request(url.toString());

    const cache = caches.default;

    // ── HMAC validation (assets only) ─────────────────────────────────────────
    if (isAsset) {
      const authResult = await validateHmac(url, env.HMAC_SECRET);
      if (!authResult.ok) {
        return new Response(authResult.reason, {
          status: authResult.status,
          headers: { "Content-Type": "text/plain" },
        });
      }
    }

    // ── Cache lookup ──────────────────────────────────────────────────────────
    const cached = await cache.match(cacheKey);
    if (cached) {
      // Re-attach private header for assets (edge cached version is public-ish internally)
      if (isAsset) {
        const h = new Headers(cached.headers);
        h.set("Cache-Control", `private, max-age=${CACHE_TTL_ASSETS}`);
        h.set("X-Cache", "HIT");
        return new Response(cached.body, { status: cached.status, headers: h });
      }
      const hit = new Response(cached.body, cached);
      hit.headers.set("X-Cache", "HIT");
      return hit;
    }

    // ── Fetch from R2 ─────────────────────────────────────────────────────────
    const key = url.pathname.slice(1); // strip leading "/"
    const object = await env.R2_BUCKET.get(key, {
      onlyIf: {
        etagMatches: request.headers.get("If-None-Match") ?? undefined,
        uploadedAfter: request.headers.get("If-Modified-Since")
          ? new Date(request.headers.get("If-Modified-Since"))
          : undefined,
      },
    });

    if (object === null) {
      return new Response("Not Found", { status: 404 });
    }

    // 304 Not Modified
    if (!(object instanceof Object && object.body !== undefined) || !object.body) {
      return new Response(null, {
        status: 304,
        headers: { ETag: object.httpEtag },
      });
    }

    const headers = buildHeaders(object, isAsset);

    const response = new Response(object.body, { status: 200, headers });

    // ── Store in edge cache ───────────────────────────────────────────────────
    // For assets: cache key has no query params so all valid signed URLs share one entry.
    // We store with a public Cache-Control so Cloudflare's cache accepts it,
    // but we rewrite it to `private` when serving to the client (see HIT branch above).
    if (isAsset) {
      const storeHeaders = new Headers(headers);
      storeHeaders.set("Cache-Control", `public, max-age=${CACHE_TTL_ASSETS}`);
      ctx.waitUntil(cache.put(cacheKey, new Response(response.clone().body, { status: 200, headers: storeHeaders })));
      // Return with private header to actual client
      headers.set("Cache-Control", `private, max-age=${CACHE_TTL_ASSETS}`);
    } else {
      ctx.waitUntil(cache.put(cacheKey, response.clone()));
    }

    headers.set("X-Cache", "MISS");
    return new Response(response.body, { status: 200, headers });
  },
};

// ── HMAC validation ───────────────────────────────────────────────────────────

async function validateHmac(url, secret) {
  const expires = url.searchParams.get("expires");
  const sig = url.searchParams.get("sig");

  if (!expires || !sig) {
    return { ok: false, status: 401, reason: "Missing signature parameters." };
  }

  const expiresMs = parseInt(expires, 10) * 1000;
  if (isNaN(expiresMs) || Date.now() > expiresMs) {
    return { ok: false, status: 410, reason: "Signed URL has expired." };
  }

  // Message must match what C# generates: "{pathname}:{expires}"
  const message = `${url.pathname}:${expires}`;

  const isValid = await verifyHmacSha256(secret, message, sig);
  if (!isValid) {
    return { ok: false, status: 403, reason: "Invalid signature." };
  }

  return { ok: true };
}

async function verifyHmacSha256(secret, message, hexSig) {
  const enc = new TextEncoder();
  const key = await crypto.subtle.importKey(
    "raw",
    enc.encode(secret),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["verify"]
  );
  const sigBytes = hexToBytes(hexSig);
  if (!sigBytes) return false;
  return crypto.subtle.verify("HMAC", key, sigBytes, enc.encode(message));
}

function hexToBytes(hex) {
  if (hex.length % 2 !== 0) return null;
  const bytes = new Uint8Array(hex.length / 2);
  for (let i = 0; i < hex.length; i += 2) {
    bytes[i / 2] = parseInt(hex.slice(i, i + 2), 16);
    if (isNaN(bytes[i / 2])) return null;
  }
  return bytes;
}

// ── Response helpers ──────────────────────────────────────────────────────────

function buildHeaders(object, isAsset) {
  const headers = new Headers();
  object.writeHttpMetadata(headers);
  headers.set("ETag", object.httpEtag);
  headers.set("Last-Modified", object.uploaded.toUTCString());
  headers.set("Accept-Ranges", "bytes");

  if (isAsset) {
    headers.set("Cache-Control", `private, max-age=${CACHE_TTL_ASSETS}`);
  } else {
    headers.set(
      "Cache-Control",
      `public, max-age=${CACHE_TTL_IMAGES}, stale-while-revalidate=86400`
    );
  }

  // Prevent embedding of private assets
  if (isAsset) {
    headers.set("X-Content-Type-Options", "nosniff");
    headers.set("X-Frame-Options", "DENY");
  }

  return headers;
}
