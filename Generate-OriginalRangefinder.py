"""Original generic housing: analytic cuboids only; no imported mesh data."""
from pathlib import Path
import math, json, hashlib
from PIL import Image, ImageDraw
root = Path(__file__).resolve().parents[1]
boxes = [
 ("mount",(-.096,0,-.1),(.096,.02,.1)),
 ("housing",(-.085,.02,-.088),(.085,.1744,.088)),
 ("bezel_left",(-.062,.080,.088),(-.040,.166,.100)),
 ("bezel_right",(.040,.080,.088),(.062,.166,.100)),
 ("bezel_bottom",(-.040,.080,.088),(.040,.0932,.100)),
 ("bezel_top",(-.040,.1524,.088),(.040,.166,.100)),
]
# Vertex order and six outward quad windings, triangulated for native parser.
quads = [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(0,1,5,4),(3,7,6,2)]
verts, faces, drawfaces = [], [], []
for name, lo, hi in boxes:
 x0,y0,z0=lo; x1,y1,z1=hi
 v=[(x0,y0,z0),(x1,y0,z0),(x1,y1,z0),(x0,y1,z0),
    (x0,y0,z1),(x1,y0,z1),(x1,y1,z1),(x0,y1,z1)]
 offset=len(verts); verts.extend(v)
 for quad in quads:
  faces.extend([(offset+quad[0],offset+quad[1],offset+quad[2]),
                (offset+quad[0],offset+quad[2],offset+quad[3])])
  drawfaces.append(([v[i] for i in quad],False))
 # Closed cuboid, outward normals and positive volume verification.
 localedges={}
 for a,b,c in faces[-12:]:
  p,q,r=verts[a],verts[b],verts[c]
  u=[q[i]-p[i] for i in range(3)]; w=[r[i]-p[i] for i in range(3)]
  n=(u[1]*w[2]-u[2]*w[1],u[2]*w[0]-u[0]*w[2],u[0]*w[1]-u[1]*w[0])
  mid=[(p[i]+q[i]+r[i])/3-(lo[i]+hi[i])/2 for i in range(3)]
  assert sum(n[i]*mid[i] for i in range(3))>0
  for edge in [(a,b),(b,c),(c,a)]:
   key=tuple(sorted(edge));localedges[key]=localedges.get(key,0)+1
 assert all(count==2 for count in localedges.values())
lines=["# Original generic automatic rangefinder housing.",
       "# Created from independent analytic cuboids; metres, +Z front, +Y up.",
       "# Bottom mounting plane Y=0; centered X/Z. No external mesh input."]
lines += ["v %.8f %.8f %.8f"%v for v in verts]
lines += ["f %d %d %d"%tuple(i+1 for i in f) for f in faces]
obj=(chr(10).join(lines)+chr(10)).encode()
# Icon optical pane matches the unchanged runtime IntegratedWindow placement.
pane=[(-.04,.0932,.101),(.04,.0932,.101),(.04,.1524,.101),(-.04,.1524,.101)]
drawfaces.append((pane,True))
view=(.55,.40,.73); scale=1750
def dot(a,b):return sum(x*y for x,y in zip(a,b))
def project(v):
 return (.8*v[0]-.6*v[2],-.30*v[0]-.86*v[1]-.40*v[2])
allp=[project(v) for v in verts+pane]
cx=(min(x for x,y in allp)+max(x for x,y in allp))/2
cy=(min(y for x,y in allp)+max(y for x,y in allp))/2
def pixel(v):
 x,y=project(v);return (256+(x-cx)*scale,256+(y-cy)*scale)
canvas=Image.new("RGBA",(2048,2048),(0,0,0,0)); draw=ImageDraw.Draw(canvas)
visible=[]
for quad,glass in drawfaces:
 u=[quad[1][i]-quad[0][i] for i in range(3)]
 w=[quad[2][i]-quad[0][i] for i in range(3)]
 n=(u[1]*w[2]-u[2]*w[1],u[2]*w[0]-u[0]*w[2],u[0]*w[1]-u[1]*w[0])
 if dot(n,view)<=0:continue
 normal=tuple(a/math.sqrt(dot(n,n)) for a in n)
 shade=.57+.40*max(0,dot(normal,(.25,.80,.55)))
 color=(20,32,37,255) if glass else tuple(round(c*shade) for c in (130,139,101))+(255,)
 visible.append((sum(dot(v,view) for v in quad)/4,quad,color))
for depth,quad,color in sorted(visible,key=lambda item:item[0]):
 points=[tuple(4*c for c in pixel(v)) for v in quad]
 draw.polygon(points,fill=color)
 draw.line(points+[points[0]],fill=(35,40,29,255),width=5)
canvas=canvas.resize((512,512),Image.Resampling.LANCZOS)
for assets in [root/"Source/assets",root/"BepInEx/plugins/SprocketThermalSight/assets"]:
 assets.mkdir(parents=True,exist_ok=True)
 (assets/"automatic-lrf.obj").write_bytes(obj)
 canvas.save(assets/"automatic-lrf-icon.png")
assert len(verts)==48 and len(faces)==72
report={"origin":"Independent analytic cuboids; no third-party mesh input",
        "vertices":48,"triangles":72,"closedComponents":6,
        "boundsMin":[-.096,0,-.1],"boundsMax":[.096,.1744,.1],
        "sizeMetres":[.192,.1744,.2],
        "runtimeGlassCenter":[0,.1228,.101],"runtimeGlassSize":[.08,.0592],
        "icon":[512,512],"nativeAppearanceTested":False}
(root/"ORIGINAL-ASSET-VALIDATION.json").write_text(json.dumps(report,indent=2)+chr(10))
print(json.dumps(report))

