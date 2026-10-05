from pathlib import Path
import json, math
from PIL import Image, ImageDraw, ImageFont

root=Path(__file__).resolve().parents[1]
geo=root/'Artifacts/GeoData'
province=next(f for f in json.loads((geo/'provincias.json').read_text(encoding='utf-8'))['features'] if f['properties']['nombre']=='Jujuy')
towns=json.loads((geo/'localidades.json').read_text(encoding='utf-8'))['localidades']
# Fixed geographic extent, shared by the image and every normalized pin coordinate.
bounds=(-68.05,-24.7,-63.15,-21.35)
def uv(lon,lat): return ((lon-bounds[0])/(bounds[2]-bounds[0]),(lat-bounds[1])/(bounds[3]-bounds[1]))
def coords(name): return next(t['centroide'] for t in towns if t['nombre']==name)
W,H=2080,1520
im=Image.new('RGB',(W,H),(5,18,30)); d=ImageDraw.Draw(im)
def px(lon,lat):
    x,y=uv(lon,lat);return (round(x*W),round((1-y)*H))
for i in range(1,40):
    x=i*W/40;d.line((x,0,x,H),fill=(9,30,45),width=1)
for i in range(1,30):
    y=i*H/30;d.line((0,y,W,y),fill=(9,30,45),width=1)
polygon=[px(*p) for p in province['geometry']['coordinates'][0]]
d.polygon(polygon,fill=(12,43,60))
for width,color in [(18,(7,31,45)),(9,(11,58,80)),(3,(36,157,189))]:d.line(polygon+[polygon[0]],fill=color,width=width,joint='curve')
font=ImageFont.truetype('C:/Windows/Fonts/consola.ttf',26)
big=ImageFont.truetype('C:/Windows/Fonts/consola.ttf',32)
for lon in [-67,-66,-65,-64]:
    x,_=px(lon,-23);d.line((x,0,x,H),fill=(17,55,70),width=1);d.text((x+8,H-44),f'{abs(lon)}° O',font=font,fill=(48,94,112))
for lat in [-24,-23,-22]:
    _,y=px(-65,lat);d.line((0,y,W,y),fill=(17,55,70),width=1);d.text((20,y+8),f'{abs(lat)}° S',font=font,fill=(48,94,112))
for label,lon,lat in [('B O L I V I A',-66.6,-21.65),('C H I L E',-67.8,-22.65),('S A L T A',-64.55,-24.5),('P U N A',-66.5,-22.8),('Q U E B R A D A',-66.7,-23.55),('Y U N G A S',-64.7,-23.3)]:
    d.text(px(lon,lat),label,font=big,fill=(46,91,110))
d.text((W-100,40),'N',font=big,fill=(119,201,216));d.line((W-90,90,W-90,160),fill=(119,201,216),width=3);d.polygon([(W-90,80),(W-102,100),(W-78,100)],fill=(119,201,216))
# Geographic scale at latitude 23° S, not an invented city-scale ruler.
length=round(50/(111.32*math.cos(math.radians(23)))/(bounds[2]-bounds[0])*W)
d.line((50,H-85,50+length,H-85),fill=(124,185,201),width=4)
for x in [50,50+length]:d.line((x,H-95,x,H-75),fill=(124,185,201),width=3)
d.text((50,H-132),'50 km aprox.',font=font,fill=(124,185,201))
dest=root/'Assets/UI/Sprites/JujuyMap.png';im.save(dest)
(geo/'jujuy-boundary.geojson').write_text(json.dumps(province,ensure_ascii=False),encoding='utf-8')
(geo/'map-extent.json').write_text(json.dumps(dict(west=bounds[0],south=bounds[1],east=bounds[2],north=bounds[3],source='GeoRef Argentina / IGN',date='2026-10-01'),indent=2),encoding='utf-8')
path=root/'Assets/Resources/Cases/cases.json';cat=json.loads(path.read_text(encoding='utf-8-sig'));cases=cat['cases'][:3]
locations=[('San Salvador de Jujuy','Valles',0,'Un familiar pide ayuda', 'Club Deportivo Norte · San Martín 240 · San Salvador de Jujuy','Estamos en el Club Deportivo Norte, San Martín 240, en San Salvador de Jujuy.'),('San Pedro','Yungas',22,'Llamada desde un taller','Taller Rueda Sur · Belgrano 815 · San Pedro de Jujuy','En el taller Rueda Sur, Belgrano 815, en San Pedro de Jujuy. Tiene un portón azul.'),('Tilcara','Quebrada',0,'Un ruido en la calle','Mitre y Rivadavia · Tilcara','En Mitre y Rivadavia, en Tilcara, frente al kiosco.')]
for c,(town,region,delay,summary,value,response) in zip(cases,locations):
    c.update(locality=town,region=region,arrivalSeconds=delay,incomingSummary=summary)
    c['facts'][0].update(value=value,response=response)

def case(id,town,region,delay,title,caller,opening,unit,values):
    templates=cases[0]['facts'];facts=[]
    for t,(value,response) in zip(templates,values):
        facts.append(dict(id=t['id'],label=t['label'],value=value,response=response,keywords=t['keywords'],points=t['points']))
    facts[3]['keywords']=['estado','como esta','como estan','despierto','consciente','responde','herido','lastimado','respira']
    cases.append(dict(id=id,title=title,callerName=caller,personality='Persona nerviosa pero colaboradora. Habla español argentino. Solo informa lo que observó y no inventa datos.',opening=opening,requiredUnitType=unit,locality=town,region=region,arrivalSeconds=delay,incomingSummary=title,facts=facts))

case('CASE_004','La Quiaca','Puna',0,'Una espera difícil','Rosa','Mi papá se descompuso mientras esperábamos. Está despierto, pero no puede levantarse.',0,[
('Refugio de pasajeros del acceso sur · La Quiaca','En La Quiaca, en el refugio de pasajeros del acceso sur. Hay un banco verde y un cartel de parada.'),
('Un adulto se descompensó mientras esperaba','Estaba sentado esperando y dijo que se sentía mal. No se cayó ni se golpeó.'),
('1 persona afectada; acompañada por su hija','Solo mi papá. Yo estoy con él y estoy bien.'),
('Consciente y responde; no logra ponerse de pie','Está despierto y me responde, pero dice que no puede levantarse.'),
('Frío; ambos fuera de la calzada','Hace frío, pero estamos dentro del refugio, lejos de los autos.')])
case('CASE_005','Abra Pampa','Puna',40,'Humo junto al depósito','Tomás','Sale humo de unos cajones afuera del depósito. Estamos del otro lado de la calle.',1,[
('Depósito de mercadería del acceso este · Abra Pampa','Estamos en Abra Pampa, en el depósito del acceso este. El frente tiene un portón amarillo.'),
('Cajones en llamas en un patio exterior','Veo fuego en una pila de cajones en el patio. No sé cómo empezó.'),
('2 personas; ambas fuera del predio','Somos dos trabajadores y los dos salimos. No quedó nadie dentro.'),
('Ambos conscientes; sin lesiones reportadas','Los dos estamos bien y podemos hablar. No nos quemamos.'),
('Viento desplaza el humo hacia una construcción','El viento lleva el humo hacia el techo del galpón. Nosotros estamos lejos.')])
case('CASE_006','Humahuaca','Quebrada',58,'Un paso bloqueado','Javier','Un vehículo quedó cruzado. Los autos se están metiendo por la vereda para pasar.',2,[
('Cruce junto al puente de acceso · Humahuaca','En Humahuaca, en el cruce junto al puente de acceso. Hay un puesto cerrado de artesanías.'),
('Vehículo detenido obstruye el cruce','Veo una camioneta cruzada. No vi un choque, parece que se detuvo y no arranca.'),
('1 conductor visible; peatones en las cercanías','Veo al conductor junto a la camioneta y personas caminando por la zona.'),
('Conductor de pie y hablando; sin lesiones observadas','El conductor está de pie, hablando por teléfono. No veo a nadie lastimado.'),
('Otros vehículos suben a la vereda ocupada por peatones','Hay autos que suben a la vereda para esquivarlo y personas caminando ahí.')])
case('CASE_007','Purmamarca','Quebrada',76,'Una caída en el paseo','Inés','Mi amiga tropezó y ahora le duele mucho el tobillo. No puede seguir caminando.',0,[
('Entrada del paseo peatonal · Purmamarca','En Purmamarca, en la entrada del paseo peatonal. Estamos junto al cartel de bienvenida.'),
('Caída al mismo nivel durante una caminata','Tropezó con una piedra y cayó. No cayó desde altura.'),
('1 persona afectada; acompañada','Solo mi amiga está lastimada. Yo la acompaño.'),
('Consciente; dolor de tobillo y dificultad para caminar','Está despierta, me habla y le duele el tobillo. No puede apoyar el pie.'),
('Suelo irregular; lugar alejado del tránsito','Hay piedras sueltas en el suelo, pero estamos lejos de los vehículos.')])
case('CASE_008','Libertador General San Martín','Yungas',94,'Una cocina con humo','Nora','Vimos fuego en la cocina del comedor. Ya salimos al patio, pero todavía sale humo.',1,[
('Comedor vecinal del acceso oeste · Libertador General San Martín','En Libertador General San Martín, en el comedor vecinal del acceso oeste. Tiene una pared verde.'),
('Fuego y humo observados en una cocina','Vi llamas en la zona de la cocina y después humo. No sé qué lo causó.'),
('4 personas; todas fuera del edificio','Somos cuatro y estamos todos afuera, en el patio.'),
('Todos conscientes; una persona presenta tos','Los cuatro podemos hablar. Una compañera está tosiendo.'),
('Humo persistente; garrafa en el interior','Sigue saliendo humo. Hay una garrafa en la cocina, la vi antes de salir.')])
for c in cases:
    coord=coords(c['locality']); x,y=uv(coord['lon'],coord['lat']);c.update(latitude=coord['lat'],longitude=coord['lon'],mapNormalizedCoord=dict(x=x,y=y))
    # Broad location questions now elicit the exact address instead of hidden, case-specific magic words.
    c['facts'][0]['keywords']=['donde','direccion','ubicacion','lugar','calle','encuentran','esquina','referencia','localidad']
path.write_text(json.dumps(dict(cases=cases),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Generated provincial map and',len(cases),'georeferenced cases.')
