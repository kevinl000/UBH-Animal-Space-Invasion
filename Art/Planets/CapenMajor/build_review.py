"""Assemble generated raster sprites in SVG; source PNG pixels stay untouched."""
import base64
import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
LASER = ROOT/'Art/Towers/Laser/Redesign/Sweep'
BODY = HERE/'body-clean-source-v1.png'
FACADES = HERE/'facades-pixel-source-v1.png'
ADMIN = HERE/'admin-facade-source-v1.png'
# Upright source foundation center; the enlarged admin is the top landmark.
ADMIN_ORIGIN = (793, 946)
ADMIN_ANCHOR = (608, 192)
ADMIN_SCALE = .30
ADMIN_ANGLE = 0
# Circular cap joins the existing shoulders at roughly (340, 261)/(884, 264).
CAP_LEFT, CAP_RIGHT, CAP_BOTTOM = 340, 884, 340
CAP_STEP = 1
CAP_RADIUS = 512
ADMIN_BLEND_TOP = 180
ADMIN_STONE_BOTTOM = 930
PREVIEW_TOP = -100
PREVIEW_LEFT = -80
PREVIEW_WIDTH = 1392


EDGE = HERE/'steel-edge-source-v2.png'
FLOOR = HERE/'steel-floor-source-v2.png'
CX, CY, RX, RY = 608, 695, 493, 460
WIDTH = 168
SX, SY = WIDTH/1340, .16
SOURCE_X, SOURCE_Y = 1082, 212
# Match the reference tower/planet ratio: 1.5 * 986 / 730 ~= 2.03.
# This is source-canvas normalization, not a gameplay-size decision.
TOWER_SCALE = 2.0
SITES = [(12,0),(1,32),(2,58),(3,83),(4,108),(5,143),
         (6,180),(7,217),(8,252),(9,277),(10,302),(11,328)]
# Measured opaque perimeter along each local ray; facade-covered top is manual.
HORIZONS = {12:(608,CY-CAP_RADIUS),1:(879,261),2:(1031,431),3:(1096,635),
            4:(1057,841),5:(880,1056),6:(608,1155),7:(325,1071),
            8:(151,843),9:(114,634),10:(182,429),11:(337,262)}
# Only these facade regions use the redraw; retain the original hull and bay mask.
FACADE_REGIONS = [
    ('Bus station', '340,0 884,0 884,241 340,241', (330,0,560,260)),
    ('Flag plaza', '80,110 340,110 340,266 315,280 280,306 250,334 226,359 196,398 80,398', (80,110,270,300)),
    ('Transit shelter', '185,900 206,935 235,973 275,1019 318,1058 318,1140 85,1140 85,900', (80,890,250,260)),
    ('Cargo depot', '1045,861 1155,861 1155,1145 885,1145 885,1052 930,1007 972,964 1011,916', (880,850,280,300)),
]
# Remove the old plaza up to the steel surface, including its green/dark fringe.
# Keep this separate from the extraction boundary for the relocated flag sprite.
OLD_PLAZA_CLEARANCE = ('80,110 340,110 340,268 330,275 320,282 310,290 '
                       '300,298 290,306 280,315 270,324 260,333 250,344 '
                       '240,355 230,367 220,379 210,393 200,408 190,423 180,438 80,438')
# Source orientation is removed before placing each facade around the rim.
# Mirror the station angle for the plaza; preserve lower facade contacts.
FACADE_PLACEMENTS = [
    {'name':'Bus station', 'origin':(608,240), 'source_angle':0,
     'anchor':(1044,443), 'angle':60, 'scale':.70},
    {'name':'Flag plaza', 'origin':(250,330), 'source_angle':328,
     'anchor':(182.4,449), 'angle':300, 'scale':1.0},
    {'name':'Transit shelter', 'origin':(250,990), 'source_angle':225,
     'anchor':(250,990), 'angle':225, 'scale':1.0},
    {'name':'Cargo depot', 'origin':(1000,965), 'source_angle':135,
     'anchor':(1000,965), 'angle':135, 'scale':1.0},
]


def cap_horizon(x):
    horizon = CY-math.sqrt(CAP_RADIUS**2-(x-CX)**2)
    # Join the exposed former plaza edge without a vertical step at x=340.
    left_join = CY-math.sqrt(CAP_RADIUS**2-(CAP_LEFT-CX)**2)
    blend = max(0,1-(x-CAP_LEFT)/50)
    return horizon+(268-left_join)*blend*blend


def uri(path):
    return 'data:image/png;base64,'+base64.b64encode(path.read_bytes()).decode()


def image_def(name,path):
    with Image.open(path) as im:
        w,h = im.size
    return f'<image id="{name}" width="{w}" height="{h}" href="{uri(path)}"/>'


def label(x,y,text,size=18):
    return f'<text x="{x}" y="{y}" font-size="{size}">{text}</text>'


def doc(w,h,defs,body,background=True,view_box=None):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}"'
            +(f' viewBox="{view_box}"' if view_box else '')+'>'
            '<style>text{font-family:Segoe UI,Arial,sans-serif;fill:#e6edf3}'
            'image{image-rendering:pixelated}</style>'
            +(f'<rect width="{w}" height="{h}" fill="#28313b"/>' if background else '')
            +f'<defs>{defs}</defs>{body}</svg>')


def build():
    edge = Image.open(EDGE).convert('RGBA')
    body_image = Image.open(BODY).convert('RGBA')
    hull_alpha = body_image.getchannel('A')
    hull_mask = ImageDraw.Draw(hull_alpha)
    for _,points,_ in FACADE_REGIONS:
        hull_mask.polygon([tuple(map(int,p.split(','))) for p in points.split()],fill=0)
    hull_mask.polygon([tuple(map(int,p.split(','))) for p in OLD_PLAZA_CLEARANCE.split()],fill=0)
    # The original upper hull has a flat station footprint. Refit only this cap.
    cap_columns = []
    for x in range(CAP_LEFT,CAP_RIGHT,CAP_STEP):
        middle = x+CAP_STEP/2
        source_top = next(y for y in range(200,CAP_BOTTOM) if hull_alpha.getpixel((int(middle),y))>=128)
        horizon = cap_horizon(middle)
        cap_columns.append((x,source_top,horizon))
        hull_mask.rectangle((x,0,x+CAP_STEP-1,CAP_BOTTOM-1),fill=0)
        hull_mask.rectangle((x,math.ceil(horizon),x+CAP_STEP-1,CAP_BOTTOM-1),fill=255)
    with Image.open(FACADES) as facade_image:
        if facade_image.size != body_image.size:
            raise ValueError('Facade redraw must retain the original source canvas size.')
    base = Image.open(LASER/'base-sprite.png').convert('RGBA')
    contour = {}
    for u in range(-83,84):
        x = round(SOURCE_X+u/SX)
        ys = [y for y in range(180,550) if edge.getpixel((x,y))[3]>=128]
        contour[u] = (min(ys)-SOURCE_Y)*SY
    feet = []
    for x in range(base.width):
        ys = [y for y in range(64,72) if base.getpixel((x,y))[3]>=128]
        if ys:
            feet.append(((x+.5-34)*TOWER_SCALE,(max(ys)+1-71)*TOWER_SCALE))
    inset = max(contour[round(u)]-v for u,v in feet)+.75
    defs = ''.join(image_def(n,p) for n,p in [('source',BODY),('facade-redraw',FACADES),('admin-source',ADMIN),('edge',EDGE),('floor',FLOOR),
                   ('base',LASER/'base-sprite.png'),('gun',LASER/'gun-sprite.png')])
    defs += (f'<g id="admin-flat" transform="translate({ADMIN_ANCHOR[0]} {ADMIN_ANCHOR[1]}) '
             f'rotate({ADMIN_ANGLE}) scale({ADMIN_SCALE}) '
             f'translate({-ADMIN_ORIGIN[0]} {-ADMIN_ORIGIN[1]})"><use href="#admin-source"/></g>')
    # Keep the facade rigid; only its bottom stone foundation follows the arc.
    defs += (f'<clipPath id="admin-upper"><rect x="300" y="-100" width="620" '
             f'height="{ADMIN_BLEND_TOP+100}"/></clipPath>')
    admin_parts = ['<use href="#admin-flat" clip-path="url(#admin-upper)"/>']
    # Fit the masonry edge, not the dark outline beneath it, to the ground.
    stone_bottom = ADMIN_ANCHOR[1]+(ADMIN_STONE_BOTTOM-ADMIN_ORIGIN[1])*ADMIN_SCALE
    for x,_,horizon in cap_columns:
        scale_y = (horizon+5-ADMIN_BLEND_TOP)/(stone_bottom-ADMIN_BLEND_TOP)
        strip_width = min(CAP_STEP+1,CAP_RIGHT-x)
        defs += f'<clipPath id="admin-column-{x}"><rect x="{x}" y="{ADMIN_BLEND_TOP}" width="{strip_width}" height="180"/></clipPath>'
        admin_parts.append(f'<g clip-path="url(#admin-column-{x})"><use href="#admin-flat" '
                           f'transform="translate(0 {ADMIN_BLEND_TOP}) scale(1 {scale_y:.6f}) '
                           f'translate(0 {-ADMIN_BLEND_TOP})"/></g>')
    defs += '<g id="admin">'+''.join(admin_parts)+'</g>'
    regions = ''.join(f'<polygon points="{points}"/>' for _,points,_ in FACADE_REGIONS)
    defs += f'<clipPath id="facade-regions">{regions}</clipPath>'
    placed_facades = ['<use href="#admin"/>']
    station_contact = []
    flag_contact = []
    for i,((name,points,_),placement) in enumerate(zip(FACADE_REGIONS,FACADE_PLACEMENTS)):
        assert name == placement['name']
        ax,ay = placement['anchor']
        ox,oy = placement['origin']
        turn = placement['angle']-placement['source_angle']
        defs += f'<clipPath id="facade-region-{i}"><polygon points="{points}"/></clipPath>'
        if name == 'Flag plaza':
            # Keep flags and poles rigid; fit only their lower contact strip.
            defs += (f'<g id="flags-flat" transform="rotate({-placement["source_angle"]}) '
                     f'translate({-ox} {-oy})"><use href="#facade-redraw" '
                     f'clip-path="url(#facade-region-{i})"/></g>')
            blend_top = -35
            defs += ('<clipPath id="flags-rigid"><rect x="-220" y="-300" width="500" height="265"/>'
                     '<rect x="-220" y="-35" width="146" height="200"/>'
                     '<rect x="119" y="-35" width="160" height="200"/></clipPath>')
            flag_parts = ['<use href="#flags-flat" clip-path="url(#flags-rigid)"/>']
            source_pixels = Image.open(FACADES).convert('RGBA')
            source_mask = Image.new('L', source_pixels.size)
            ImageDraw.Draw(source_mask).polygon([tuple(map(int,p.split(','))) for p in points.split()],fill=255)
            source_theta = math.radians(placement['source_angle'])
            theta = math.radians(placement['angle'])
            for u in range(-74,119):
                source_contact = []
                for v in range(blend_top+1,61):
                    px = round(ox+u*math.cos(source_theta)-v*math.sin(source_theta))
                    py = round(oy+u*math.sin(source_theta)+v*math.cos(source_theta))
                    color = source_pixels.getpixel((px,py))
                    if source_mask.getpixel((px,py)) and color[3]>=128 and max(color[:3])>=60:
                        source_contact.append(v)
                if not source_contact:
                    continue
                contacts = [next(v for v in range(-60,101)
                    if hull_alpha.getpixel((round(ax+sample_u*math.cos(theta)-v*math.sin(theta)),
                                            round(ay+sample_u*math.sin(theta)+v*math.cos(theta))))>=128)
                            for sample_u in (u,u+1,u+2)]
                contact = max(contacts)+4
                scale_y = (contact-blend_top)/(max(source_contact)-blend_top)
                defs += (f'<clipPath id="flags-column-{u}"><rect x="{u}" y="{blend_top}" '
                         'width="2" height="180"/></clipPath>')
                flag_parts.append(f'<g clip-path="url(#flags-column-{u})"><use href="#flags-flat" '
                                  f'transform="translate(0 {blend_top}) scale(1 {scale_y:.6f}) '
                                  f'translate(0 {-blend_top})"/></g>')
                flag_contact.append([u,contact])
            placed_facades.append(f'<g transform="translate({ax} {ay}) rotate({placement["angle"]})">'
                                  +''.join(flag_parts)+'</g>')
            continue
        if name == 'Bus station':
            # Work in upright, scaled station coordinates. Keep its roof rigid;
            # stretch the lower facade to the actual hull, including both ends.
            defs += (f'<g id="station-flat" transform="scale({placement["scale"]}) '
                     f'translate({-ox} {-oy})"><use href="#facade-redraw" '
                     f'clip-path="url(#facade-region-{i})"/></g>')
            blend_top = -70
            defs += '<clipPath id="station-upper"><rect x="-190" y="-200" width="384" height="130"/></clipPath>'
            station_parts = ['<use href="#station-flat" clip-path="url(#station-upper)"/>']
            theta = math.radians(turn)
            for u in range(-190,194):
                # Sample both strip ends and center to overlap the hull even
                # where its pixel contour steps between adjacent columns.
                contacts = []
                for sample_u in (u,u+1,u+2):
                    contacts.append(next(v for v in range(-80,121)
                        if hull_alpha.getpixel((round(ax+sample_u*math.cos(theta)-v*math.sin(theta)),
                                                round(ay+sample_u*math.sin(theta)+v*math.cos(theta))))>=128))
                contact = max(contacts)+4
                if (u+190)%4 == 0 or u == 193:
                    station_contact.append([u,contact])
                scale_y = (contact-blend_top)/-blend_top
                defs += (f'<clipPath id="station-column-{u}"><rect x="{u}" y="{blend_top}" '
                         'width="2" height="220"/></clipPath>')
                station_parts.append(f'<g clip-path="url(#station-column-{u})"><use href="#station-flat" '
                                     f'transform="translate(0 {blend_top}) scale(1 {scale_y:.6f}) '
                                     f'translate(0 {-blend_top})"/></g>')
            placed_facades.append(f'<g transform="translate({ax} {ay}) rotate({turn})">'
                                  +''.join(station_parts)+'</g>')
            continue
        placed_facades.append(
            f'<g transform="translate({ax} {ay}) rotate({turn}) scale({placement["scale"]}) '
            f'translate({-ox} {-oy})"><use href="#facade-redraw" clip-path="url(#facade-region-{i})"/></g>')
    defs += '<g id="placed-facades">'+''.join(placed_facades)+'</g>'
    defs += ('<mask id="retain-hull" maskUnits="userSpaceOnUse" x="0" y="0" width="1312" height="1199">'
             '<rect width="1312" height="1199" fill="white"/>'
             +f'<g fill="black">{regions}<polygon points="{OLD_PLAZA_CLEARANCE}"/></g></mask>')
    # Normalize near-opaque coverage after composing the strips, so fractional
    # preview scaling cannot expose their anti-aliased edges as dark bands.
    defs += ('<filter id="hull-opacity" color-interpolation-filters="sRGB">'
             '<feComponentTransfer><feFuncA type="linear" slope="1.07"/>'
             '</feComponentTransfer></filter>')
    defs += '<g id="flat-hull"><use href="#source" mask="url(#retain-hull)"/></g>'
    defs += ('<mask id="outside-cap"><rect width="1312" height="1199" fill="white"/>'
             f'<rect x="{CAP_LEFT}" y="0" width="{CAP_RIGHT-CAP_LEFT}" height="{CAP_BOTTOM}" fill="black"/></mask>')
    cap_parts = ['<use href="#flat-hull" mask="url(#outside-cap)"/>']
    for x,source_top,horizon in cap_columns:
        scale_y = (CAP_BOTTOM-horizon)/(CAP_BOTTOM-source_top)
        # Overlap strip edges so fractional review scaling cannot expose seams.
        strip_width = min(CAP_STEP+1,CAP_RIGHT-x)
        defs += f'<clipPath id="cap-column-{x}"><rect x="{x}" y="0" width="{strip_width}" height="{CAP_BOTTOM+2}"/></clipPath>'
        cap_parts.append(f'<g clip-path="url(#cap-column-{x})"><use href="#flat-hull" '
                         f'transform="translate(0 {CAP_BOTTOM}) scale(1 {scale_y:.6f}) translate(0 {-CAP_BOTTOM})"/></g>')
    # Strip overlap can expose neighboring columns above the intended cap arc.
    cap_outline = ' '.join(f'{x},{cap_horizon(x):.4f}' for x in range(CAP_LEFT,CAP_RIGHT+1))
    defs += (f'<clipPath id="cap-outline"><polygon points="0,0 {CAP_LEFT},0 '
             f'{cap_outline} {CAP_RIGHT},0 1312,0 1312,1199 0,1199"/></clipPath>')
    defs += '<g id="curved-hull" clip-path="url(#cap-outline)" filter="url(#hull-opacity)">'+''.join(cap_parts)+'</g>'
    defs += ('<g id="repaired-source"><use href="#source" mask="url(#retain-hull)"/>'
             '<use href="#facade-redraw" clip-path="url(#facade-regions)"/></g>')
    # Buildings are opaque too, but must not extend the surface supporting a rim.
    defs += ('<mask id="hull-alpha" style="mask-type:alpha" maskUnits="userSpaceOnUse" '
             'x="0" y="0" width="1312" height="1199">'
             '<use href="#curved-hull"/></mask>')
    sprite_transform = f'scale({SX} {SY}) translate({-SOURCE_X} {-SOURCE_Y})'
    defs += f'<g id="edge-local" transform="{sprite_transform}"><use href="#edge"/></g>'
    defs += f'<g id="floor-local" transform="{sprite_transform}"><use href="#floor"/></g>'
    # The front terrain is cut to the actual alpha profile of the generated edge.
    # Rear floor pixels fill the opening; the edge is drawn in front of the tower.
    opening = [(-83,0),(83,0)]+[(u,contour[u]+2) for u in range(83,-84,-1)]
    opening_points = ' '.join(f'{u},{v:.3f}' for u,v in opening)
    defs += f'<polygon id="opening" points="{opening_points}"/>'
    cuts, floors, fronts, towers, rows = [], [], [], [], []
    for site,angle in SITES:
        theta = math.radians(angle)
        ax,ay = HORIZONS[site]
        transform = f'translate({ax:.4f} {ay:.4f}) rotate({angle})'
        cuts.append(f'<use href="#opening" transform="{transform}" fill="black"/>')
        floors.append(f'<use href="#floor-local" transform="{transform}"/>')
        fronts.append(f'<use href="#edge-local" transform="{transform}"/>')
        defs += f'<g id="site{site}" transform="{transform}"><use href="#floor-local"/></g>'
        for aim in (-60,0,60):
            defs += (f'<g id="tower{site}a{aim}" transform="{transform}">'
                     f'<g transform="translate(0 {inset:.3f}) scale({TOWER_SCALE})">'
                     '<use href="#base" x="-34" y="-71"/>'
                     f'<g transform="translate(30 -47) rotate({aim})">'
                     '<use href="#gun" x="-12" y="-28"/></g></g></g>')
        towers.append(f'<use href="#tower{site}a0"/>')
        supported = 0
        for u,v in feet:
            ex = round(SOURCE_X+u/SX)
            ey = round(SOURCE_Y+(inset+v)/SY)
            wx = round(ax+u*math.cos(theta)-(inset+v)*math.sin(theta))
            wy = round(ay+u*math.sin(theta)+(inset+v)*math.cos(theta))
            supported += (edge.getpixel((ex,ey))[3]>=128 and hull_alpha.getpixel((wx,wy))>=128)
        if supported != len(feet):
            raise ValueError(f'Site {site}: foundation extends outside the hull or steel edge.')
        rows.append({'site':site,'angle':angle,'horizon':[ax,ay],
                     'foundation_columns_supported_by_edge_and_hull_mask':supported,
                     'mount':[ax-inset*math.sin(theta),ay+inset*math.cos(theta)]})
    defs += ('<mask id="terrain-cut" maskUnits="userSpaceOnUse" x="0" y="0" width="1312" height="1199">'
             '<rect width="1312" height="1199" fill="white"/>'
             +''.join(cuts)+'</mask>')
    defs += ('<g id="terrain" mask="url(#terrain-cut)">'
             '<use href="#curved-hull"/></g>')
    defs += '<g id="rear" mask="url(#hull-alpha)">'+''.join(floors)+'</g>'
    defs += '<g id="fronts" mask="url(#hull-alpha)">'+''.join(fronts)+'</g>'
    defs += '<g id="empty"><use href="#placed-facades"/><use href="#rear"/><use href="#terrain"/><use href="#fronts"/></g>'
    for aim in (-60,0,60):
        defs += (f'<g id="occupied{aim}"><use href="#placed-facades"/><use href="#rear"/><use href="#terrain"/>'
                 +''.join(f'<use href="#tower{site}a{aim}"/>' for site,_ in SITES)
                 +'<use href="#fronts"/></g>')
    page = label(25,36,'CAPEN / FACADE PROPORTIONS AND SURFACE FIT',25)
    page += label(25,70,'Flags at 10 o\u2019clock mirror the station at 2 o\u2019clock; admin and lower facades retained.',17)
    for x,ref,title in [(20,'empty','Empty terrain'),(820,'occupied0','Occupied / same foundation fit at all twelve sites')]:
        page += label(x,110,title,20)
        page += f'<svg x="{x}" y="130" width="760" height="730" viewBox="{PREVIEW_LEFT} {PREVIEW_TOP} 1330 1400"><use href="#{ref}"/></svg>'
    page += label(25,905,'Tower scale normalized to the reference planet diameter. This is a visual proposal, not a gameplay-size change.',17)
    (HERE/'page-review.svg').write_text(doc(1600,950,defs,page),encoding='utf-8')
    detail = label(20,28,'TOP-SITE PROFILE / ACTUAL LASER AT THREE AIMING ANGLES',21)
    for i,aim in enumerate((-60,0,60)):
        detail += label(i*512+12,57,f'{aim:+d} degrees / metal terrain edge',15)
        detail += (f'<svg x="{i*512}" y="70" width="512" height="420" viewBox="480 {HORIZONS[12][1]-147} 256 210">'
                   f'<use href="#occupied{aim}"/></svg>')
    (HERE/'top-review.svg').write_text(doc(1536,510,defs,detail),encoding='utf-8')
    detail = label(20,30,'ALL TWELVE TERRAIN CUTS / LOCALLY UPRIGHT CONTACT VIEWS',22)
    for i,row in enumerate(rows):
        col,line = i%4,i//4
        x,y = col*600,line*265+60
        mx,my = row['mount']
        for j,aim in enumerate((-60,0,60)):
            detail += label(x+j*200+5,y,f'Site {row["site"]} / {aim:+d} deg',12)
            detail += (f'<svg x="{x+j*200}" y="{y+10}" width="195" height="235" viewBox="-120 -180 260 240">'
                       f'<g transform="rotate({-row["angle"]}) translate({-mx} {-my})">'
                       f'<use href="#occupied{aim}"/></g></svg>')
    (HERE/'contact-review.svg').write_text(doc(2400,860,defs,detail),encoding='utf-8')
    radial = label(20,30,'ALL TWELVE SITES / ACTUAL RADIAL ORIENTATION / THREE AIMING ANGLES',22)
    for i,row in enumerate(rows):
        x,y = (i%4)*600,(i//4)*265+60
        mx,my = row['mount']
        for j,aim in enumerate((-60,0,60)):
            radial += label(x+j*200+5,y,f'Site {row["site"]} / {aim:+d} deg',12)
            radial += (f'<svg x="{x+j*200}" y="{y+10}" width="195" height="235" viewBox="{mx-160} {my-160} 320 320">'
                       f'<use href="#occupied{aim}"/></svg>')
    (HERE/'all-sites-aiming.svg').write_text(doc(2400,860,defs,radial),encoding='utf-8')
    (HERE/'planet-empty.svg').write_text(doc(PREVIEW_WIDTH,1420,defs,'<use href="#empty"/>',False,f'{PREVIEW_LEFT} {PREVIEW_TOP} {PREVIEW_WIDTH} 1420'),encoding='utf-8')
    (HERE/'planet-occupied.svg').write_text(doc(PREVIEW_WIDTH,1420,defs,'<use href="#occupied0"/>',False,f'{PREVIEW_LEFT} {PREVIEW_TOP} {PREVIEW_WIDTH} 1420'),encoding='utf-8')
    reference = ROOT/'Art/Planets/UnionPrime/planet-occupied.png'
    compare_defs = defs+image_def('union-reference',reference)
    compare = label(25,40,'REFERENCE COMPARISON / MATCHED GLOBE WIDTH',27)
    compare += label(25,78,'The actual Laser has approximately the same on-screen size in both panels.',18)
    compare += label(25,123,'User-selected Union Prime reference',23)
    compare += label(825,123,'Capen terrain-cut proposal',23)
    compare += '<svg x="20" y="150" width="720" height="800" viewBox="122 -10 1011 1123"><use href="#union-reference"/></svg>'
    compare += '<svg x="820" y="150" width="720" height="800" viewBox="-75 -140 1365 1517"><use href="#occupied0"/></svg>'
    (HERE/'reference-comparison.svg').write_text(doc(1600,980,compare_defs,compare),encoding='utf-8')
    review = label(20,32,'CAPEN / EXISTING FACADES RESTORED TO PIXEL ART',23)
    for i,(name,_,view) in enumerate(FACADE_REGIONS):
        y = 65+i*330
        review += label(20,y,name,20)
        for x,ref,title in [(20,'source','Before'),(640,'repaired-source','After')]:
            review += label(x,y+25,title,16)
            review += (f'<svg x="{x}" y="{y+35}" width="600" height="280" '
                       f'viewBox="{" ".join(map(str,view))}"><use href="#{ref}"/></svg>')
    (HERE/'facade-review.svg').write_text(doc(1260,1390,defs,review),encoding='utf-8')
    junctions = label(20,30,'FACADE / RECESS JUNCTIONS: EMPTY AND OCCUPIED',23)
    for i,(name,view) in enumerate([
            ('Admin left',(330,190,150,160)),('Station base',(880,230,380,440)),
            ('Flag plaza',(-40,230,340,350)),('Lower facade contact',(880,850,280,300))]):
        x,y = (i%2)*640,(i//2)*390+65
        junctions += label(x+12,y,name,20)
        for j,ref in enumerate(('empty','occupied0')):
            junctions += label(x+j*320+12,y+25,'Empty' if j==0 else 'Occupied',15)
            junctions += (f'<svg x="{x+j*320}" y="{y+35}" width="320" height="340" '
                          f'viewBox="{" ".join(map(str,view))}"><use href="#{ref}"/></svg>')
    (HERE/'junction-review.svg').write_text(doc(1280,850,defs,junctions),encoding='utf-8')
    admin_review = label(20,34,'CAPEN / CAMPUS ADMINISTRATION FACADE',24)
    for x,ref,title in [(20,'empty','Empty / central landmark'),(660,'occupied0','Occupied / top tower site')]:
        admin_review += label(x,76,title,19)
        admin_review += (f'<svg x="{x}" y="95" width="620" height="500" viewBox="300 -80 620 480">'
                         f'<use href="#{ref}"/></svg>')
    admin_review += label(20,635,'Spherical steel cap and curved stone foundation; windows and upper floors remain rigid.',17)
    (HERE/'admin-review.svg').write_text(doc(1300,670,defs,admin_review),encoding='utf-8')
    station_review = label(20,34,'CAPEN / BUS STATION CURVED GROUND CONTACT',24)
    for x,ref,title in [(20,'empty','Empty / both foundation corners seated'),(660,'occupied0','Occupied / neighboring tower recesses')]:
        station_review += label(x,76,title,19)
        station_review += (f'<svg x="{x}" y="95" width="620" height="620" viewBox="850 210 430 460">'
                           f'<use href="#{ref}"/></svg>')
    (HERE/'station-review.svg').write_text(doc(1300,735,defs,station_review),encoding='utf-8')
    manifest = {'status':'current administration facade assembly; Unity integration pending',
                'reference_diameter':RX*2,'edge_source_origin':[SOURCE_X,SOURCE_Y],
                'edge_scale':[SX,SY],'tower_scale':TOWER_SCALE,
                'tower_scale_reason':'normalizes roughly 986px Capen diameter to the 730px Union Prime reference at 1.5x',
                'foundation_inset':inset,'sampled_foundation_columns':len(feet),
                'minimum_contour_overlap':min(inset+v-contour[round(u)] for u,v in feet),
                'maximum_contour_overlap':max(inset+v-contour[round(u)] for u,v in feet),
                'sites':rows,
                'source_sha256':{str(p.relative_to(ROOT)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest()
                                 for p in [BODY,FACADES,ADMIN,EDGE,FLOOR,LASER/'base-sprite.png',LASER/'gun-sprite.png']},
                'facade_regions':{name:points for name,points,_ in FACADE_REGIONS},
                'admin_ground_curve':{'span':[CAP_LEFT,CAP_RIGHT],'strip_width':CAP_STEP,
                                      'strip_overlap':1,'hull_bottom_overlap':2,
                                      'composited_hull_alpha_gain':1.07,
                                      'horizon':'695 - sqrt(512**2 - (x-608)**2)',
                                      'left_join':{'x':340,'y':268,'blend_width':50},
                                      'hull_blend_bottom':CAP_BOTTOM,'facade_blend_top':ADMIN_BLEND_TOP,
                                      'foundation_source_contact_y':ADMIN_STONE_BOTTOM,
                                      'foundation_overlap':5},
                'facade_placements':FACADE_PLACEMENTS,
                'old_plaza_hull_clearance':OLD_PLAZA_CLEARANCE,
                'flag_ground_curve':{'space':'upright flags, relative to anchor',
                                     'facade_blend_top':-35,'strip_width':1,'strip_overlap':1,
                                     'minimum_colored_base_overlap':4,'contact_samples':flag_contact},
                'station_ground_curve':{'space':'upright scaled station, relative to anchor',
                                        'facade_blend_top':-70,'strip_width':1,'strip_overlap':1,
                                        'minimum_hull_overlap':4,'contact_samples':station_contact},
                'facade_distribution':'source-fit layout with intentional gaps, not equal angular spacing',
                'full_preview_view_box':[PREVIEW_LEFT,PREVIEW_TOP,PREVIEW_WIDTH,1420],
                'additional_facades':[{'name':'Campus administration','source':ADMIN.name,
                                       'source_origin':ADMIN_ORIGIN,'anchor':ADMIN_ANCHOR,
                                       'angle':ADMIN_ANGLE,'scale':ADMIN_SCALE,
                                       'layer':'behind hull and recess floors; excluded from supporting hull mask'}],
                'limits':['SVG sprite assembly, not Unity import','no generated tower art',
                          'only three aiming poses visually reviewed; no full collision sweep',
                          'measured horizon anchors and source transforms are provisional',
                          'source PNG pixels unchanged; renderer handles masks and transforms',
                          'rear floor is distinct generated material, not a lighting filter']}
    (HERE/'assembly.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
    print('Supported foundation samples by site:', [r['foundation_columns_supported_by_edge_and_hull_mask'] for r in rows])
    print(json.dumps({k:manifest[k] for k in ['tower_scale','foundation_inset','sampled_foundation_columns',
                    'minimum_contour_overlap','maximum_contour_overlap']}))


if __name__=='__main__':
    build()
