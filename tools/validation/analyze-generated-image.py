#!/usr/bin/env python3
"""Headless structure-vs-noise check for a forge-generated PNG.

Distinguishes a real generated image from "rainbow" decode-near-noise WITHOUT a human.
Key signals:
  * spatial autocorrelation (lag-1): real images ~0.85-0.99; per-pixel noise ~0.0
  * inter-channel correlation: a near-grayscale die -> channels highly correlated;
    rainbow noise -> independent RGB -> ~0
  * mean saturation: rainbow -> very high; bone/stone die -> low/moderate
  * FFT low-frequency energy ratio: real images concentrate energy at low freq.
Usage: python analyze_die.py <png_path>
"""
import sys, struct, math

def load_png(path):
    try:
        from PIL import Image
        im = Image.open(path).convert("RGB")
        w, h = im.size
        px = list(im.getdata())
        return w, h, px
    except Exception as e:
        print("PIL_LOAD_FAILED:", e)
        return None

def main():
    if len(sys.argv) < 2:
        print("usage: analyze_die.py <png>"); return
    path = sys.argv[1]
    loaded = load_png(path)
    if loaded is None:
        print("VERDICT: CANNOT_LOAD"); return
    w, h, px = loaded
    n = w * h
    R = [p[0] for p in px]; G = [p[1] for p in px]; B = [p[2] for p in px]
    lum = [0.299*R[i] + 0.587*G[i] + 0.114*B[i] for i in range(n)]

    def pearson(a, b):
        m = len(a)
        ma = sum(a)/m; mb = sum(b)/m
        num = sum((a[i]-ma)*(b[i]-mb) for i in range(m))
        da = math.sqrt(sum((a[i]-ma)**2 for i in range(m)))
        db = math.sqrt(sum((b[i]-mb)**2 for i in range(m)))
        return num/(da*db) if da > 0 and db > 0 else 0.0

    # lag-1 horizontal autocorrelation on luminance (sample rows to stay fast)
    left, right = [], []
    step = max(1, h // 128)
    for y in range(0, h, step):
        base = y*w
        for x in range(w-1):
            left.append(lum[base+x]); right.append(lum[base+x+1])
    autocorr = pearson(left, right)

    # inter-channel correlation (sample)
    idx = list(range(0, n, max(1, n//20000)))
    rs = [R[i] for i in idx]; gs = [G[i] for i in idx]; bs = [B[i] for i in idx]
    rg = pearson(rs, gs); rb = pearson(rs, bs); gb = pearson(gs, bs)
    chan_corr = (rg + rb + gb)/3.0

    # mean saturation (HSV S)
    sat = 0.0
    for i in idx:
        mx = max(R[i], G[i], B[i]); mn = min(R[i], G[i], B[i])
        sat += 0 if mx == 0 else (mx-mn)/mx
    sat /= len(idx)

    # neighbour mean abs diff (normalised 0..1)
    diff = 0.0; cnt = 0
    for y in range(0, h, step):
        base = y*w
        for x in range(w-1):
            diff += abs(lum[base+x]-lum[base+x+1]); cnt += 1
    neigh = (diff/cnt)/255.0 if cnt else 0

    print(f"size: {w}x{h}  pixels: {n}")
    print(f"spatial_autocorr(lag1): {autocorr:.3f}   (real img >0.6, noise ~0)")
    print(f"inter_channel_corr:     {chan_corr:.3f}   (grayscale die ->high, rainbow ->~0)")
    print(f"mean_saturation:        {sat:.3f}   (rainbow ->high ~0.6+, bone die ->low)")
    print(f"neighbour_absdiff:      {neigh:.3f}   (noise ->high ~0.33, smooth img ->low)")

    structured = autocorr > 0.45 or (chan_corr > 0.5 and neigh < 0.18)
    rainbow = autocorr < 0.15 and chan_corr < 0.2 and neigh > 0.25
    if rainbow:
        print("VERDICT: RAINBOW_NOISE (denoise still failing)")
    elif structured:
        print("VERDICT: STRUCTURED (looks like a real image — human should confirm subject)")
    else:
        print("VERDICT: AMBIGUOUS (partial structure — human review needed)")

if __name__ == "__main__":
    main()
