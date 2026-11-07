from PIL import Image, ImageDraw, ImageFont, ImageFilter

BG=(24,24,24); AMBER=(255,159,28); LIGHT=(255,184,77); WHITE=(242,242,242); GREY=(184,184,184); MUTED=(138,138,138)
BOLD="/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
REG ="/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
icon_src = Image.open("/home/claude/ShotForge/assets/ShotForge_icon_1024.png").convert("RGBA")

def radial_glow(size, center, radius, color, strength):
    """soft amber glow, drawn as a blurred ellipse on its own layer"""
    layer = Image.new("RGBA", size, (0,0,0,0))
    d = ImageDraw.Draw(layer)
    cx, cy = center
    d.ellipse((cx-radius, cy-radius, cx+radius, cy+radius), fill=color+(strength,))
    return layer.filter(ImageFilter.GaussianBlur(radius*0.55))

def font(path, px): return ImageFont.truetype(path, px)

def make_cover():
    S = 2                                     # draw at 2x, then downscale for smooth edges
    W, H = 1280*S, 720*S
    im = Image.new("RGBA", (W, H), BG+(255,))
    im.alpha_composite(radial_glow((W,H), (int(W*0.78), int(H*0.5)), 330*S, AMBER, 70))
    d = ImageDraw.Draw(im)

    # icon on the right (kept near the centre-right so cropped previews still show it)
    ic = icon_src.resize((420*S, 420*S), Image.LANCZOS)
    im.alpha_composite(ic, (int(W*0.78 - 210*S), int(H*0.5 - 210*S)))

    # left column text block
    x = 88*S
    d.text((x, 158*S), "ShotForge", font=font(BOLD, 98*S), fill=WHITE)
    d.rectangle((x, 286*S, x+120*S, 294*S), fill=AMBER)
    d.text((x, 318*S), "Shot list planner for", font=font(REG, 38*S), fill=GREY)
    d.text((x, 366*S), "AI video creators", font=font(REG, 38*S), fill=GREY)

    items = ["Projects  >  Scenes  >  Shots", "Characters with reference images", "One-click, paste-ready prompts"]
    y = 462*S
    for t in items:
        d.ellipse((x, y+11*S, x+16*S, y+27*S), fill=AMBER)
        d.text((x+34*S, y), t, font=font(REG, 27*S), fill=WHITE)
        y += 46*S

    d.text((x, 650*S), "Free: 1 project   |   Pro: unlimited   |   Windows", font=font(REG, 22*S), fill=MUTED)
    return im.resize((1280,720), Image.LANCZOS).convert("RGB")

def make_thumb():
    S = 2
    N = 600*S
    im = Image.new("RGBA", (N, N), BG+(255,))
    im.alpha_composite(radial_glow((N,N), (N//2, int(N*0.40)), 190*S, AMBER, 70))
    d = ImageDraw.Draw(im)
    ic = icon_src.resize((340*S, 340*S), Image.LANCZOS)
    im.alpha_composite(ic, (N//2 - 170*S, 62*S))
    f = font(BOLD, 74*S)
    w = d.textlength("ShotForge", font=f)
    d.text(((N-w)/2, 430*S), "ShotForge", font=f, fill=WHITE)
    f2 = font(REG, 28*S)
    t = "Shot list planner"
    w2 = d.textlength(t, font=f2)
    d.text(((N-w2)/2, 520*S), t, font=f2, fill=GREY)
    return im.resize((600,600), Image.LANCZOS).convert("RGB")

cover = make_cover(); cover.save("ShotForge_Gumroad_Cover_1280x720.png", dpi=(72,72))
thumb = make_thumb(); thumb.save("ShotForge_Gumroad_Thumbnail_600x600.png", dpi=(72,72))

# verify the exact size and the 72 dpi tag
for f in ["ShotForge_Gumroad_Cover_1280x720.png","ShotForge_Gumroad_Thumbnail_600x600.png"]:
    g = Image.open(f); print(f, g.size, "dpi", tuple(round(x) for x in g.info.get("dpi",(0,0))))
