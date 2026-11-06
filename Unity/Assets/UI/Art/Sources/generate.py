"""Original Siege Ledger artwork. Run from the repository root with Python 3 (stdlib only).
Geometry is authored here, independently of sample images and proprietary art.
Layers: quiet transparent structure, edge rule, corner repair, identity silhouette.
"""
import math, re, struct, zlib, uuid
from pathlib import Path
ROOT=Path('Unity/Assets/UI/Art')
WHITE=(255,255,255,255); CLEAR=(0,0,0,0)
class Canvas:
 def __init__(self,w,h,scale=2):
  self.w=w; self.h=h; self.s=scale; self.W=w*scale; self.H=h*scale; self.p=bytearray(self.W*self.H*4)
 def poly(self,pts,c):
  pts=[(x*self.s,y*self.s) for x,y in pts]
  for y in range(max(0,int(min(p[1] for p in pts))),min(self.H,math.ceil(max(p[1] for p in pts)))):
   yy=y+.5; xx=[]
   for i,(x1,y1) in enumerate(pts):
    x2,y2=pts[(i+1)%len(pts)]
    if (y1<=yy<y2) or (y2<=yy<y1): xx.append(x1+(yy-y1)*(x2-x1)/(y2-y1))
   xx.sort()
   for a,b in zip(xx[::2],xx[1::2]):
    for x in range(max(0,math.ceil(a-.5)),min(self.W,math.ceil(b-.5))):
     j=(y*self.W+x)*4; self.p[j:j+4]=bytes(c)
 def rect(self,x,y,w,h,c): self.poly([(x,y),(x+w,y),(x+w,y+h),(x,y+h)],c)
 def line(self,pts,c=WHITE,width=5):
  for (x,y),(u,v) in zip(pts,pts[1:]):
   dx=u-x;dy=v-y; n=math.hypot(dx,dy)
   if n==0: continue
   a=-dy/n*width/2; b=dx/n*width/2
   self.poly([(x+a,y+b),(u+a,v+b),(u-a,v-b),(x-a,y-b)],c)
 def oval(self,x,y,w,h,c=WHITE): self.poly([(x+w/2+math.cos(t*math.pi/32)*w/2,y+h/2+math.sin(t*math.pi/32)*h/2) for t in range(64)],c)
 def ring(self,x,y,w,h,width=5):
  self.oval(x,y,w,h); self.oval(x+width,y+width,w-2*width,h-2*width,CLEAR)
 def save(self,path):
  raw=bytearray()
  for y in range(self.h):
   raw.append(0)
   for x in range(self.w):
    ps=[self.p[((y*self.s+dy)*self.W+x*self.s+dx)*4:((y*self.s+dy)*self.W+x*self.s+dx)*4+4] for dy in range(self.s) for dx in range(self.s)]
    alpha=sum(p[3] for p in ps)
    # Unassociated alpha: white silhouettes stay white even at antialiased edges.
    raw.extend([round(sum(p[k]*p[3] for p in ps)/alpha) if alpha else 0 for k in range(3)]+[round(alpha/(self.s*self.s))])
  def chunk(k,v): return struct.pack('!I',len(v))+k+v+struct.pack('!I',zlib.crc32(k+v)&0xffffffff)
  path.parent.mkdir(parents=True,exist_ok=True)
  path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('!2I5B',self.w,self.h,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b''))
def icon(c,n):
 p=c.poly;o=c.oval
 def r(x,y,w,h,col): c.rect(x,y,max(w,6),max(h,6),col)
 def l(pts,col=WHITE,width=6): c.line(pts,col,max(width,6))
 def ring(x,y,w,h,width=6): c.ring(x,y,w,h,max(width,6))
 def shield(x=16,y=10,w=32,h=44): p([(x,y),(x+w,y),(x+w-3,y+h*.66),(x+w/2,y+h),(x+3,y+h*.66)],WHITE)
 def sword(x=29):
  p([(x,9),(x+6,4),(x+12,9),(x+12,36),(x,36)],WHITE);r(x-7,35,26,6,WHITE);r(x+3,40,6,15,WHITE)
 def bell():
  p([(15,43),(20,35),(20,23),(25,15),(39,15),(44,23),(44,35),(49,43)],WHITE);r(13,43,38,6,WHITE);o(27,50,10,7);r(29,8,6,9,WHITE)
 def leaf(): p([(13,48),(15,25),(30,9),(51,9),(49,31),(33,50)],WHITE);l([(16,48),(45,15)],CLEAR,5)
 def eye():
  p([(7,32),(19,20),(32,15),(45,20),(57,32),(45,44),(32,49),(19,44)],WHITE);p([(16,32),(25,24),(39,24),(48,32),(39,40),(25,40)],CLEAR);o(26,26,12,12)
 def book():
  p([(7,15),(26,12),(32,17),(38,12),(57,15),(57,51),(37,48),(32,53),(27,48),(7,51)],WHITE);l([(32,20),(32,47)],CLEAR,4);l([(13,27),(25,25)],CLEAR,4);l([(39,25),(51,27)],CLEAR,4)
 def page(): r(15,9,36,46,WHITE);r(20,17,25,4,CLEAR);r(20,27,25,4,CLEAR)
 def candle(): r(25,28,14,25,WHITE);r(17,52,30,5,WHITE);p([(32,5),(24,19),(28,25),(36,25),(40,19)],WHITE)
 def stairs(): p([(8,45),(20,45),(20,34),(32,34),(32,23),(44,23),(44,12),(56,12),(56,53),(8,53)],WHITE)
 def star(): p([(32,5),(39,25),(59,32),(39,39),(32,59),(25,39),(5,32),(25,25)],WHITE)
 def cross(): r(25,10,14,44,WHITE);r(10,25,44,14,WHITE)
 def cards():
  r(8,10,29,39,WHITE);r(13,15,19,29,CLEAR);r(19,17,29,39,WHITE);r(24,22,19,29,CLEAR);r(30,24,27,35,WHITE)
 if n in ('block','hero-iron'): shield()
 elif n=='guarded': shield(6,10,28,38);shield(31,19,27,38)
 elif n=='exposed': shield();l([(34,9),(27,29),(36,33),(28,55)],CLEAR,6);l([(6,13),(12,20)],WHITE,4);l([(53,12),(48,20)],WHITE,4)
 elif n in ('room-combat','attack','hero-steel','attack-all'):
  if n in ('hero-steel','attack-all'): sword(12);sword(39)
  if n!='hero-steel': sword(26)
  if n=='room-combat': p([(25,20),(32,24),(25,29)],CLEAR)
 elif n=='weak': p([(30,5),(39,11),(35,27),(46,34),(40,44),(23,32),(27,24)],WHITE);r(15,35,23,6,WHITE);l([(23,40),(15,54)],WHITE,7)
 elif n in ('hero-thorn',): leaf()
 elif n in ('hero-ash','hex'):
  if n=='hex': ring(6,6,52,52,5);r(29,3,8,10,CLEAR);r(28,51,8,10,CLEAR)
  eye()
 elif n in ('hero-bronze','wait','unknown'):
  bell()
  if n=='unknown': p([(8,12),(56,12),(51,55),(13,55)],WHITE);l([(24,22),(22,46)],CLEAR,5);l([(40,22),(42,46)],CLEAR,5)
  if n=='wait': r(8,55,48,4,WHITE)
 elif n in ('hero-bone','heal'):
  cross()
  if n=='hero-bone':
   for y in (16,46): l([(20,y),(44,y)],CLEAR,3)
  else: p([(48,9),(53,3),(58,9),(53,16)],WHITE)
 elif n in ('room-investigation','journal'): book()
 elif n in ('room-event','empty-folio','hero-ink','remove-card'):
  page()
  if n in ('room-event','remove-card'): p([(28,40),(35,35),(31,46),(39,51),(27,59)],CLEAR)
  if n=='hero-ink': p([(19,49),(31,20),(55,5),(49,28)],WHITE);l([(17,54),(49,15)],WHITE,4)
 elif n in ('room-sanctuary','defeat','hero-tallow'):
  candle()
  if n=='defeat': r(20,3,24,24,CLEAR);l([(26,20),(34,14),(29,8)],WHITE,4)
  if n=='hero-tallow': r(23,8,18,19,WHITE);r(20,46,24,6,WHITE)
 elif n in ('room-secret','stairs'): stairs();
 elif n=='room-entrance':
  r(9,9,7,47,WHITE);r(48,9,7,47,WHITE);r(9,9,46,8,WHITE)
  for x in (21,31,41): r(x,17,5,16,WHITE)
 elif n=='room-elite':
  p([(14,49),(10,25),(18,11),(29,6),(31,15),(39,7),(48,16),(54,29),(50,49),(32,58)],WHITE);l([(17,28),(28,31)],CLEAR,5);l([(37,31),(48,28)],CLEAR,5);r(29,37,6,15,CLEAR)
 elif n=='room-boss':
  r(12,7,40,50,WHITE);r(19,14,26,36,CLEAR);r(29,14,6,36,WHITE);o(24,28,16,16)
 elif n=='bleed': p([(32,5),(50,35),(49,48),(40,57),(24,57),(15,48),(14,35)],WHITE);p([(47,37),(37,44),(49,47)],CLEAR)
 elif n=='riposte': l([(45,14),(21,14),(13,25),(13,42),(25,51),(45,51)],WHITE,8);p([(39,38),(56,51),(39,60)],WHITE)
 elif n in ('fury','hero-ember'):
  for x,y in ((10,24),(27,8),(44,19)): p([(x,y+23),(x+6,y),(x+13,y+14),(x+10,y+28)],WHITE)
 elif n=='hero-smoke':
  ring(17,17,30,30,7);l([(23,23),(10,10)],WHITE,7);l([(41,40),(53,52)],WHITE,7);l([(40,23),(52,11)],WHITE,7);l([(23,41),(15,52)],WHITE,7)
 elif n=='hero-rose': star();l([(38,5),(27,32),(37,39),(27,59)],CLEAR,5)
 elif n=='hero-verdigris':
  p([(13,26),(51,26),(47,56),(36,49),(28,56),(17,49)],WHITE);l([(24,32),(23,46)],CLEAR,4);l([(39,32),(40,45)],CLEAR,4);p([(12,8),(23,17),(29,6),(35,17),(52,8),(49,23),(15,23)],WHITE);r(33,5,5,20,CLEAR)
 elif n in ('buff','boost','debuff','discount'):
  p([(9,37),(32,14),(55,37),(48,45),(32,29),(16,45)],WHITE)
  if n in ('debuff','discount'):
   # reflect vertical
   rows=[c.p[y*c.W*4:(y+1)*c.W*4] for y in range(c.H)];c.p=bytearray(b''.join(reversed(rows)))
 elif n=='apply-status': ring(10,13,43,43,7);r(37,6,19,19,WHITE)
 elif n=='resolve': star();ring(6,6,52,52,4)
 elif n in ('deck','draw','discard'): cards();
 elif n=='keepsake': ring(8,8,29,29,7);l([(30,31),(51,52)],WHITE,8);r(39,43,16,7,WHITE)
 elif n in ('seal-whole','seal-broken','clue','cancelled'):
  p([(32+25*math.cos(i*math.pi/12)*(1 if i%2==0 else .88),32+25*math.sin(i*math.pi/12)*(1 if i%2==0 else .88)) for i in range(24)],WHITE)
  if n!='seal-whole': l([(35,4),(27,25),(37,35),(27,59)],CLEAR,6)
  if n=='clue': o(22,22,20,20,CLEAR)
  if n=='cancelled': r(5,27,54,10,CLEAR)
 elif n=='check': l([(10,31),(25,46),(54,15)],WHITE,9)
 elif n=='lock': ring(17,7,30,36,7);r(11,28,42,29,WHITE);r(29,37,6,12,CLEAR)
 elif n=='current': p([(32,6),(56,53),(32,43),(8,53)],WHITE)
 elif n=='warning': p([(32,5),(60,56),(4,56)],WHITE);r(29,23,6,17,CLEAR);r(29,45,6,5,CLEAR)
 else: raise ValueError(n)
 if n in ('draw','discard'):
  l([(13,31),(47,31)],CLEAR,7);p(([(16,20),(5,31),(16,42)] if n=='draw' else [(46,20),(59,31),(46,42)]),WHITE)
 if n=='room-secret': l([(7,58),(57,58)],WHITE,4)

def chamfer(c,inset,col,width=1):
 w,h=c.w,c.h; a=inset; b=w-inset; d=h-inset; k=3
 pts=[(a+k,a),(b-k,a),(b,a+k),(b,d-k),(b-k,d),(a+k,d),(a,d-k),(a,a+k),(a+k,a)]
 c.line(pts,col,width)
def frame(c,n):
 w,h=c.w,c.h
 edge=(100,116,134,255); brass=(214,175,99,255)
 if n=='focus':
  for x,sx in ((3,1),(w-3,-1)):
   for y,sy in ((3,1),(h-3,-1)): c.line([(x,y+sy*6),(x,y),(x+sx*6,y)],(245,221,163,255),2)
  return
 if n=='reachable-rim':
  for a,alpha in ((1,12),(3,25),(5,45),(7,80),(9,120)): chamfer(c,a,(214,175,99,alpha),2)
  return
 if n in ('cost','owner-medal'):
  c.oval(3,3,w-6,h-6,(100,116,134,255));c.oval(5,5,w-10,h-10,WHITE if n=='cost' else CLEAR);return
 if n=='toggle-check': c.line([(6,16),(13,23),(27,8)],WHITE,5);return
 if n in ('door-shut', 'door-open', 'door-travelled'):
  col=brass if n=='door-travelled' else (233,220,189,255) if n=='door-open' else edge
  c.rect(2,4,4,h-8,col);c.rect(w-6,4,4,h-8,col)
  if n!='door-open': c.rect(5,h/2-3,w-10,6,col)
  if n=='door-travelled': c.line([(8,h-12),(11,h-8),(19,h-18)],brass,3)
  return
 if n in ('health-fill','volume-fill'):
  c.rect(1,1,w-2,h-2,(104,50,63,255) if n=='health-fill' else WHITE);return
 chamfer(c,2,edge,2);chamfer(c,5,(70,83,98,180),1)
 if n in ('button-hover',): chamfer(c,2,brass,2)
 if n=='button-disabled': chamfer(c,2,(70,83,98,255),2)
 if n in ('button','button-hover','button-disabled'): c.line([(11,h-5),(w-11,h-5)],(100,116,134,180),2)
 if n in ('folio','narrative','fighter','portrait-frame','binding','entry'):
  c.line([(8,13),(8,h-13)],(70,83,98,255),2)
  c.line([(4,14),(4,5),(14,5)],brass,2)
  c.line([(w-14,h-5),(w-5,h-5),(w-5,h-14)],(155,129,80,255),2)
 if n in ('volume-grip','scroll-thumb'):
  for y in (h/2-3,h/2+3): c.line([(6,y),(w-6,y)],edge,2)
 if n in ('name-plate','door-tag'): c.rect(6,6,w-12,h-12,(28,38,48,255))
 if n=='slate-field':
  c.rect(0,0,w,h,(16,21,28,255));c.line([(8,70),(40,70),(48,78),(w-10,78)],(20,26,34,255),2)

def title(c):
 c.rect(0,0,c.w,c.h,(16,21,28,255))
 # Abstract embossed architectural motif, deliberately not a playable floor plan.
 for x,y,w,h in ((550,88,350,368),(590,125,270,294),(640,172,170,200),(520,56,52,62),(878,56,52,62),(520,426,52,62),(878,426,52,62)):
  c.line([(x,y),(x+w,y),(x+w,y+h),(x,y+h),(x,y)],(36,46,54,255),4)
  c.line([(x+7,y+7),(x+w-7,y+7)],(67,63,49,255),1)
 c.line([(724,90),(724,170)],(104,91,62,255),2)
 for y in (510,520): c.line([(530,y),(920,y)],(42,48,52,255),1)

# Read the approved exact paths, dimensions and L/T/R/B values.
spec=Path('Docs/ui-art-upgrade/style-guide.md').read_text()
assets={}
for row in spec.splitlines():
 if not row.startswith('| new `Art/') or '.png`' not in row: continue
 path=re.search(r'`Art/([^`]+\.png)`',row).group(1)
 size=re.search(r'\| (\d+)×(\d+) \|',row)
 if not size: continue
 dims=tuple(map(int,size.groups())); slice_field=row.split('|')[4].strip(); m=re.match(r'(\d+)/(\d+)/(\d+)/(\d+)',slice_field)
 borders=tuple(map(int,m.groups())) if m else (0,0,0,0)
 assets[path]=(dims,borders)
# A full-width ramp avoids magnifying low-resolution alpha steps.
assets['Title/title-fade.png'] = ((1536, 16), (0, 0, 0, 0))
for path,(dims,borders) in assets.items():
 c=Canvas(*dims);n=Path(path).stem
 if path.startswith('Icons/'): icon(c,n)
 elif n=='keep-ledger': title(c)
 elif n=='title-fade':
  for x in range(c.w):
   t = max(0, min(1, (x / (c.w - 1) - .46) / .54))
   # Smoothstep has zero slope at both ends: no visible start or end seam.
   c.rect(x,0,1,c.h,(16,21,28,round(255 * (1 - t*t*(3-2*t)))))
 else: frame(c,n)
 c.save(ROOT/path)
# Sprite borders match the explicit USS slices; neither overrides the other with different values.
for p in [ROOT]+sorted(ROOT.rglob('*')):
 if p.suffix=='.meta': continue
 meta=Path(str(p)+'.meta')
 if meta.exists(): continue
 head='fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'
 if p.is_dir(): body='folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
 elif p.suffix=='.png':
  l,t,r,b=assets[str(p.relative_to(ROOT))][1]
  body=f'''TextureImporter:
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteMode: 1
  spriteExtrude: 4
  spriteMeshType: 0
  spriteAlignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: {l}, y: {b}, z: {r}, w: {t}}}
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: {uuid.uuid4().hex}
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  userData: Siege Ledger original scripted art
  assetBundleName: 
  assetBundleVariant: 
'''
 else: body=('TextScriptImporter:' if p.suffix in ('.md','.txt') else 'DefaultImporter:')+ '\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
 meta.write_text(head+body)
print(f'Exported {len(assets)} PNGs, {sum((ROOT/p).stat().st_size for p in assets):,} bytes; RGBA resident {sum(w*h*4 for (w,h),_ in assets.values()):,} bytes.')
