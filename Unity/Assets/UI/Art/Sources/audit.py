"""Static asset audit; not a substitute for Unity's importer, USS parser or rendered QA.
Run from repo root. Optionally exports a review contact sheet to /tmp/wdd-art-sheet.png.
"""
import re,struct,zlib,xml.etree.ElementTree as ET,subprocess
from pathlib import Path
root=Path('Unity/Assets/UI/Art');ui=root.parent
# Generator exports PNG RGBA scanlines with no filtering. Check every pixel, including opaque title/surface.
def read(p):
 data=p.read_bytes();off=8;compressed=b''
 while off<len(data):
  n=struct.unpack('!I',data[off:off+4])[0];kind=data[off+4:off+8];chunk=data[off+8:off+8+n];off+=n+12
  if kind==b'IHDR': w,h,depth,mode,_,_,_=struct.unpack('!2I5B',chunk);assert (depth,mode)==(8,6),p
  if kind==b'IDAT': compressed+=chunk
 raw=zlib.decompress(compressed);pixels=b''
 for y in range(h):
  start=y*(w*4+1);assert raw[start]==0,p;pixels+=raw[start+1:start+1+w*4]
 return w,h,pixels
spec=Path('Docs/ui-art-upgrade/style-guide.md').read_text();expected={}
for row in spec.splitlines():
 m=re.search(r'new `Art/([^`]+\.png)`.*?\| (\d+)×(\d+) \| ([^|]+)',row)
 if m:
  path,w,h,border=m.groups();v=re.match(r'(\d+)/(\d+)/(\d+)/(\d+)',border.strip());l,t,r,b=map(int,v.groups()) if v else (0,0,0,0);expected[path]=(int(w),int(h),(l,b,r,t))
assert set(expected)=={str(p.relative_to(root)) for p in root.rglob('*.png')}
guids={};alpha_report=[];images=[]
for p in [root]+list(root.rglob('*')):
 if p.suffix=='.meta': continue
 meta=Path(str(p)+'.meta');assert meta.exists(),p
 g=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(),re.M);assert g,meta
 assert g[1] not in guids,(meta,guids.get(g[1]));guids[g[1]]=meta
 if p.suffix!='.png':continue
 w,h,pixels=read(p);ew,eh,borders=expected[str(p.relative_to(root))];assert (w,h)==(ew,eh),p
 s=meta.read_text();m=re.search(r'spriteBorder: \{x: (\d+), y: (\d+), z: (\d+), w: (\d+)\}',s);assert tuple(map(int,m.groups()))==borders,p
 for setting in ('enableMipMap: 0','sRGBTexture: 1','filterMode: 1','alphaIsTransparency: 1','spriteMode: 1','textureType: 8','textureCompression: 0'):assert setting in s,(p,setting)
 alphas=pixels[3::4];low,high=min(alphas),max(alphas)
 if p.parent.name=='Icons':
  assert low==0 and high==255,p
  assert all(tuple(pixels[i:i+3])==(255,255,255) for i in range(0,len(pixels),4) if pixels[i+3]),p
 if p.parent.name in ('Frames','Cards'):assert low==0 and high==255,p
 alpha_report.append(f'{p.relative_to(root)}: {w}x{h}, alpha {low}..{high}')
 images.append((p,w,h,pixels))
for p in ui.glob('*.uss'):
 s=p.read_text();assert not re.search(r'box-shadow|text-shadow|linear-gradient|:before|:after|:focus-within',s),p
 assert s.count('{')==s.count('}'),p
 for u in re.findall(r'url\("([^"]+)"\)',s):assert (p.parent/u).exists(),(p,u)
 # Every explicit USS slice agrees with the corresponding Sprite border.
 for rule in re.finditer(r'([^{}]+)\{([^{}]*)\}',s):
  body=rule[2];u=re.search(r'background-image: url\("Art/([^";]+\.png)"\)',body)
  if not u: continue
  borders=expected[u[1]][2];l,b,r,t=borders
  if any(borders):
   for key,v in (('left',l),('top',t),('right',r),('bottom',b)):
    assert re.search(r'-unity-slice-'+key+r':\s*'+str(v)+r';',body),(p,u[1],key)
   assert '-unity-slice-scale: 1;' in body,(p,u[1])
  else: assert '-unity-slice-' not in body,(p,u[1])
for p in ui.glob('*.uxml'):
 tree=ET.parse(p)
 # Every pre-existing query name survives.
 before=subprocess.check_output(['git','show','HEAD:'+str(p)],text=True)
 old=ET.fromstring(before);names=lambda t:{e.attrib['name'] for e in t.iter() if 'name' in e.attrib}
 assert names(old)<=names(tree.getroot()),p
# Compare all C#-driven style contract names to the guide's required list.
for k in re.findall(r'`\.(is-[\w-]+|has-[\w-]+|hidden|floater-gone)`',spec.split('## 8.')[1].split('\n- Use')[0]):
 assert any('.'+k in p.read_text() for p in ui.glob('*.uss')),k
print('\n'.join(alpha_report));print(f'PASS: {len(images)} PNG dimensions/alpha/borders/import settings, {len(guids)} unique art metadata GUIDs; USS URLs and UXML contracts.')
print(f'PNG bytes: {sum(p.stat().st_size for p,_,_,_ in images)}; RGBA bytes: {sum(w*h*4 for _,w,h,_ in images)}')
# Review contact sheet: original pixels composited over slate; not shipped as runtime art.
W=840;cell=84;H=((len(images)+9)//10)*cell;out=bytearray(bytes((28,38,48,255))*W*H)
for i,(p,w,h,pixels) in enumerate(sorted(images,key=lambda v:str(v[0]))):
 x0=(i%10)*cell;y0=(i//10)*cell;scale=min(72/w,72/h,1);dw,dh=int(w*scale),int(h*scale)
 for y in range(dh):
  for x in range(dw):
   q=(int(y/scale)*w+int(x/scale))*4;a=pixels[q+3]/255;j=((y0+y+6)*W+x0+x+6)*4
   for k in range(3):out[j+k]=round(pixels[q+k]*a+out[j+k]*(1-a))
def chunk(k,v):return struct.pack('!I',len(v))+k+v+struct.pack('!I',zlib.crc32(k+v)&0xffffffff)
raw=b''.join(b'\0'+out[y*W*4:(y+1)*W*4] for y in range(H))
Path('/tmp/wdd-art-sheet.png').write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('!2I5B',W,H,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b''))
Path('/tmp/wdd-alpha-audit.txt').write_text('\n'.join(alpha_report))
