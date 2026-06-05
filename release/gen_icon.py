"""Generate PdfAutoPrint Pro app icon (multi-res .ico)"""
from PIL import Image, ImageDraw, ImageFont
import os, struct

OUT_DIR = r"C:\Users\Administrator\WorkBuddy\2026-06-05-09-13-35\PdfAutoPrint.Pro\Assets"
SIZES = [16, 24, 32, 48, 64, 128, 256]
COLOR_BG = (83, 74, 183)
COLOR_FG = (255, 255, 255)

def make_icon_img(size):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    m = max(1, size // 14)
    r = max(3, size // 7)
    d.rounded_rectangle([m, m, size - m, size - m], radius=r, fill=COLOR_BG)

    fs = int(size * 0.52)
    try:
        f = ImageFont.truetype("segoeui.ttf", fs)
    except:
        try:
            f = ImageFont.truetype("arial.ttf", fs)
        except:
            f = ImageFont.load_default()

    bbox = d.textbbox((0, 0), "P", font=f)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    x = (size - tw) // 2
    y = (size - th) // 2 - max(1, size // 18)
    d.text((x, y), "P", fill=COLOR_FG, font=f)
    return img

# Build multi-res ICO manually
icon_data = []
raw_images = []
offset = 6 + 16 * len(SIZES)  # header(6) + directory entries

for s in SIZES:
    img = make_icon_img(s)
    raw_images.append(img)
    # Save as BMP DIB
    bmp_data = bytearray()
    # BITMAPINFOHEADER
    bmp_data += struct.pack("<IiiHHIIiiII", 40, s, s * 2, 1, 32, 0, 0, 0, 0, 0, 0)
    # Pixel data (bottom-up)
    pixels = list(img.getdata())
    for y in range(s - 1, -1, -1):
        row = pixels[y * s : (y + 1) * s]
        for r_val, g_val, b_val, a_val in row:
            bmp_data += struct.pack("BBBB", b_val, g_val, r_val, a_val)

    icon_data.append(bmp_data)
    offset += len(bmp_data)

# Build ICO
ico = bytearray()
ico += struct.pack("<HHH", 0, 1, len(SIZES))  # Reserved, Type=1, Count

cur_offset = 6 + 16 * len(SIZES)
for i, s in enumerate(SIZES):
    data_len = len(icon_data[i])
    w = 0 if s == 256 else s
    h = 0 if s == 256 else s
    ico += struct.pack("<BBBBHHII", w, h, 0, 0, 1, 32, data_len, cur_offset)
    cur_offset += data_len

for d in icon_data:
    ico += d

ico_path = os.path.join(OUT_DIR, "app.ico")
with open(ico_path, "wb") as f:
    f.write(bytes(ico))
print(f"ICO: {ico_path} ({len(ico)} bytes, {len(SIZES)} sizes)")

# Save 256x256 PNG
png_path = os.path.join(OUT_DIR, "app.png")
raw_images[-1].save(png_path)
print(f"PNG: {png_path}")

for i, s in enumerate(SIZES):
    print(f"  {s}x{s} OK ({len(icon_data[i])} bytes)")
